﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Windows;
using Signum.Windows.Basics;
using Agile.Entities;
using Agile.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using Signum.Windows.Operations;
using Signum.Windows.Authorization;
using Signum.Windows.Excel;
using Signum.Windows.Chart;
using Signum.Entities.Authorization;
using Signum.Windows.UserQueries;
using Signum.Windows.Disconnected;
using Agile.Windows.Properties;
using System.IO;
using Signum.Windows.Omnibox;
using Signum.Windows.Dashboard;
using Signum.Entities.Disconnected;
using Signum.Windows.Processes;
using Signum.Windows.Notes;
using Signum.Windows.Alerts;
using Signum.Windows.Profiler;
using Agile.Windows.Code;
using Signum.Windows.Scheduler;
using Signum.Windows.SMS;
using Signum.Windows.Files;
using Signum.Windows.Help;

namespace Agile.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.InvariantCulture.IetfLanguageTag)));

            this.DispatcherUnhandledException += (sender, args) => { Program.HandleException("Unexpected error", args.Exception, App.Current.MainWindow); args.Handled = true; };
            Async.DispatcherUnhandledException += (e, w) => Program.HandleException("Error in async call", e, w);
            Async.AsyncUnhandledException += (e, w) => Program.HandleException("Error in async call", e, w);

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Fix so App.xaml InitializeComponent gets generated
        }

        protected override void OnStartup(StartupEventArgs args)
        {
        }

        static bool started = false;
        public static void Start()
        {
            if (started)
                return;

            started = true;

            Navigator.Start(new NavigationManager(multithreaded: true));
            Finder.Start(new FinderManager());
            Constructor.Start(new ConstructorManager());

            OperationClient.Start(new OperationManager());

            AuthClient.Start(
                types: true,
                property: true,
                queries: true,
                permissions: true,
                operations: true,
                defaultPasswordExpiresLogic: false);

            Navigator.EntitySettings<UserEntity>().OverrideView += (usr, ctrl) =>
            {
                ctrl.Child<EntityLine>("Role").After(new ValueLine().Set(Common.RouteProperty, "[UserEmployeeMixin].AllowLogin"));
                ctrl.Child<EntityLine>("Role").After(new EntityLine().Set(Common.RouteProperty, "[UserEmployeeMixin].Employee"));

                return ctrl;
            };

            LinksClient.Start(widget: true, contextualMenu: true);

            ProcessClient.Start(package: true, packageOperation: true);
            SchedulerClient.Start();

            FilePathClient.Start();
            ExcelClient.Start(toExcel: true, excelReport: false);
            UserQueryClient.Start();
            ChartClient.Start();
            DashboardClient.Start();

            HelpClient.Start(); 

            ExceptionClient.Start();

            NoteClient.Start(typeof(UserEntity), /*Note*/typeof(OrderEntity));
            ProfilerClient.Start();

            OmniboxClient.Start();
            OmniboxClient.Register(new SpecialOmniboxProvider());
            OmniboxClient.Register(new EntityOmniboxProvider());
            OmniboxClient.Register(new DynamicQueryOmniboxProvider());
            OmniboxClient.Register(new UserQueryOmniboxProvider());
            OmniboxClient.Register(new ChartOmniboxProvider());
            OmniboxClient.Register(new UserChartOmniboxProvider());
            OmniboxClient.Register(new DashboardOmniboxProvider());

            AgileClient.Start();
            Navigator.Initialize();
        }
    }
}
