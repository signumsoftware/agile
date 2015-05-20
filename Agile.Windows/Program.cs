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
using Agile.Local;
using Signum.Entities.Disconnected;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Agile.Windows
{
    public class Program
    {
        public enum StartOption
        {
            RunLocally,
            UploadAndSync
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                App app = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
                Server.OnOperation += Server.OnOperation_SaveCurrentCulture;

                if (RunLocally())
                {
                    LocalServer.Start(Settings.Default.LocalDatabaseConnectionString);
                    Server.OfflineMode = true;

                    Program.GetServer = LocalServer.GetLocalServer;
                    DisconnectedClient.GetTransferServer = LocalServer.GetLocalServerTransfer;
                }
                else //Run remotely
                {
                    Program.GetServer = RemoteServer;
                    DisconnectedClient.GetTransferServer = RemoteServerTransfer;
                }

                Server.SetNewServerCallback(NewServerAndLogin);
                Server.Connect();

                App.Start();

                if (!Server.OfflineMode)
                {
                    UploadIfNecessary();
                }

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
                if (Server.OfflineMode)
                    LocalServer.Stop();
            }
        }

        private static bool RunLocally()
        {
            if (!File.Exists(DisconnectedClient.DatabaseFile) && !File.Exists(DisconnectedClient.DownloadBackupFile))
                return false;

            StartOption result;
            if (!SelectorWindow.ShowDialog(
                EnumExtensions.GetValues<StartOption>(), out result,
                elementIcon: so => AgileImageLoader.GetImageSortName(so == StartOption.RunLocally ? "local.png" : "server.png"),
                elementText: so => so.NiceToString(),
                title: "Startup mode",
                message: "A local database has been found on your system.\r\nWhat you want to do?"))
                Environment.Exit(0);

            if (result == StartOption.RunLocally)
            {
                if (File.Exists(DisconnectedClient.DownloadBackupFile))
                {
                    ProgressWindow.Wait("Waiting", "Restoring database...", () =>
                    {
                        LocalServer.RestoreDatabase(
                            Settings.Default.LocalDatabaseConnectionString,
                            DisconnectedClient.DownloadBackupFile,
                            DisconnectedClient.DatabaseFile,
                            DisconnectedClient.DatabaseLogFile);

                        File.Delete(DisconnectedClient.DownloadBackupFile);
                    });
                }

                return true;
            }
            else
            {
                return false;
            }
        }//RunLocally

        private static void UploadIfNecessary()
        {
            var dmLite = Server.Return((IDisconnectedServer s) => s.GetDisconnectedMachine(Environment.MachineName));

                
            switch (dmLite.Try(t=>t.Retrieve().State))
            {
                case DisconnectedMachineState.Faulted:
                    {
                        string message = @"The last import had en exception. You have two options:
- Contact IT and wait until they fix the uploaded database
- Restart the application and work locally AT YOUR OWN RISK";

                        MessageBox.Show(message, "Last import failed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        Environment.Exit(0);

                        break;
                    }

                case DisconnectedMachineState.Fixed:
                    {
                        string message = "Good News!!\r\nThe IT department already fixed your last upload so you can continue working.";
                        MessageBox.Show(message, "Upload fixed", MessageBoxButton.OK, MessageBoxImage.Information);

                        if (File.Exists(DisconnectedClient.UploadBackupFile))
                            File.Delete(DisconnectedClient.UploadBackupFile);

                        if (File.Exists(DisconnectedClient.DatabaseFile))
                            LocalServer.DropDatabase(Settings.Default.LocalDatabaseConnectionString);

                        Server.Execute((IDisconnectedServer ds) => ds.ConnectAfterFix(DisconnectedMachineEntity.Current));

                        break;
                    }

                case null:
                case DisconnectedMachineState.Connected:
                    {
                        if (File.Exists(DisconnectedClient.DownloadBackupFile) || File.Exists(DisconnectedClient.DatabaseFile))
                        {
                            string message = "The server does not expect you to be disconnected, but you have a local database.\r\nRemove you local database manually (loosing your work) or contact IT department.";

                            MessageBox.Show(message, "Unexpected situation", MessageBoxButton.OK, MessageBoxImage.Error);

                            Environment.Exit(0);
                        }

                        break;
                    }

                case DisconnectedMachineState.Disconnected:
                    {
                        if (File.Exists(DisconnectedClient.DownloadBackupFile))
                        {
                            File.Delete(DisconnectedClient.DownloadBackupFile);
                            Server.Execute((IDisconnectedServer ds) => ds.SkipExport(DisconnectedMachineEntity.Current));
                        }
                        else
                        {
                            if (File.Exists(DisconnectedClient.DatabaseFile))
                            {
                                ProgressWindow.Wait("Waiting", "Backing up...", () =>
                                {
                                    LocalServer.BackupDatabase(
                                        Settings.Default.LocalDatabaseConnectionString,
                                        DisconnectedClient.UploadBackupFile);
                                });
                            }

                            if (File.Exists(DisconnectedClient.UploadBackupFile))
                            {
                                if (new UploadProgress().ShowDialog() == false)
                                {
                                    MessageBox.Show("Contact IT to fix the error", "Failed import", MessageBoxButton.OK, MessageBoxImage.Error);
                                    Environment.Exit(0);
                                }

                                LocalServer.DropDatabase(Settings.Default.LocalDatabaseConnectionString);
                                File.Delete(DisconnectedClient.UploadBackupFile);
                            }

                        }

                        break;
                    }
            }
        }//UploadIfNecessary

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
