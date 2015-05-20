using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Windows;
using System.ServiceModel.Security;
using System.Threading;
using System.Globalization;
using Signum.Utilities;
using Signum.Windows;
using Signum.Services;
using Agile.Services;
using Signum.Entities.Authorization;
using Signum.Windows.Authorization;
using Agile.Windows.Properties;
using Signum.Windows.Disconnected;
using System.IO;
using Signum.Entities.Disconnected;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Agile.Windows
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                App app = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
                Server.OnOperation += Server.OnOperation_SaveCurrentCulture;
                {
                    Program.GetServer = RemoteServer;
                    DisconnectedClient.GetTransferServer = RemoteServerTransfer;
                }

                Server.SetNewServerCallback(NewServerAndLogin);
                Server.Connect();

                App.Start();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(new Main());
            }
            catch (NotConnectedToServerException)
            {
            }
            catch (Exception e)
            {
                HandleException("Start-up error", e, null);
            }
            finally
            {
                Server.Disconnect();
            }
        }
        public static void HandleException(string errorTitle, Exception e, Window w)
        {
            if (e is MessageSecurityException)
            {
                MessageBox.Show("Session expired", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                try
                {
                    var exception = new ExceptionEntity(e.Follow(ex => ex.InnerException).Last())
                    {
                        User = UserEntity.Current.ToLite<IUserEntity>(),
                        ControllerName = "WindowsClient",
                        ActionName = "WindowClient",
                        Version = typeof(Program).Assembly.GetName().Version.ToString(),
                        UserHostName = Environment.MachineName,
                    };

                    Server.ExecuteNoRetryOnSessionExpired((IBaseServer s) => s.Save(exception));
                }
                catch { }
                finally
                {
                    string message = e.Follow(ex => ex.InnerException).ToString(ex => "{0} : {1}".FormatWith(
                            ex.GetType().Name, ex.Message), "\r\n\r\n");

                    if (w != null)
                        MessageBox.Show(w, message, errorTitle + ":", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show(message, errorTitle + ":", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        static Func<IServerAgile> GetServer;

        public static IBaseServer NewServerAndLogin()
        {
            IServerAgile result = GetServer();

            if (Application.Current == null || Application.Current.CheckAccess())
                return Login(result);
            else
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = Login(result);
                });

            return result;
        }


        static ChannelFactory<IServerAgile> channelFactory;
        private static IServerAgile RemoteServer()
        {
            if (channelFactory == null)
                channelFactory = new ChannelFactory<IServerAgile>("server");

            IServerAgile result = channelFactory.CreateChannel();
            return result;
        }

        static IServerAgile Login(IServerAgile result)
        {
            Login milogin = new Login
            {
                Title = "Welcome to Agile",
                UserName = Settings.Default.UserName,
                Password = "",
                ProductName = "Agile",
                CompanyName = "Signum Software"
            };

            milogin.LoginClicked += (object sender, EventArgs e) =>
            {
                try
                {
                    result.Login(milogin.UserName, Security.EncodePassword(milogin.Password));

                    Settings.Default.UserName = milogin.UserName;
                    Settings.Default.Save();

                    UserEntity.Current = result.GetCurrentUser();

                    if (UserEntity.Current.CultureInfo != null)
                        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(UserEntity.Current.CultureInfo.Name);

                    // verificar el tiempo de expiracion
                    var alerta = result.PasswordNearExpired();
                    if (alerta.HasText())
                        MessageBox.Show(alerta);


                    milogin.DialogResult = true;
                }
                catch (IncorrectUsernameException ex)
                {
                    milogin.Error = ex.Message;
                    milogin.FocusUserName();
                }
                catch (IncorrectPasswordException ex)
                {
                    milogin.Error = ex.Message;
                    milogin.FocusPassword();
                }
            };

            milogin.FocusUserName();

            bool? dialogResult = milogin.ShowDialog();
            if (dialogResult == true)
            {
                UserEntity user = result.GetCurrentUser();
                UserEntity.Current = user;

                return result;
            }
            else
            {
                return null;
            }
        } //Login

        static ChannelFactory<IServerAgileTransfer> channelFactoryRemote;
        public static IServerAgileTransfer RemoteServerTransfer()
        {
            if (channelFactoryRemote == null)
                channelFactoryRemote = new ChannelFactory<IServerAgileTransfer>("serverTransfer");

            return channelFactoryRemote.CreateChannel();
        }
    }
}
