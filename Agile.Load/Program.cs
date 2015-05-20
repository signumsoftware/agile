﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities;
using Agile.Load.Properties;
using Agile.Logic;
using Agile.Entities;
using Signum.Services;
using Signum.Entities;
using Signum.Engine.Authorization;
using Signum.Entities.Reflection;
using Signum.Engine.Chart;
using Signum.Engine.Operations;
using Signum.Entities.Translation;
using System.Globalization;
using Signum.Entities.Mailing;
using Signum.Entities.Files;
using Signum.Entities.SMS;
using Signum.Entities.Basics;
using Signum.Engine.Translation;
using Signum.Engine.Help;
using Signum.Entities.Word;
using Signum.Engine.Basics;
using Signum.Engine.Migrations;
using Signum.Entities.Authorization;
using Signum.Engine.CodeGeneration;

namespace Agile.Load
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                using (AuthLogic.Disable())
                using (CultureInfoUtils.ChangeCulture("en"))
                using (CultureInfoUtils.ChangeCultureUI("en"))
                {
                    Starter.Start(UserConnections.Replace(Settings.Default.ConnectionString));

                    Console.WriteLine("..:: Welcome to Agile Loading Application ::..");
                    Console.WriteLine("Database: {0}", Regex.Match(((SqlConnector)Connector.Current).ConnectionString, @"Initial Catalog\=(?<db>.*)\;").Groups["db"].Value);
                    Console.WriteLine();

                    if (args.Any())
                    {
                        switch (args.First().ToLower().Trim('-', '/'))
                        {
                            case "sql": SqlMigrationRunner.SqlMigrations(true); return 0;
                            case "csharp": CSharpMigrations(true); return 0;
                            case "load": Load(args.Skip(1).ToArray()); return 0;
                            default:
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.Red, "Unkwnown command " + args.First());
                                Console.WriteLine("Examples:");
                                Console.WriteLine("   sql: SQL Migrations");
                                Console.WriteLine("   csharp: C# Migrations");
                                Console.WriteLine("   load 1-4,7: Load processes 1 to 4 and 7");
                                return -1;
                            }
                        }
                    } //if(args.Any())

                    while (true)
                    {
                        Action action = new ConsoleSwitch<string, Action>
                        {
                            {"N", NewDatabase},
                            {"G", CodeGenerator.GenerateCodeConsole },
                            {"SQL", SqlMigrationRunner.SqlMigrations},
                            {"CS", () => CSharpMigrations(false), "C# Migrations"},
                            {"S", Synchronize},
                            {"L", () => Load(null), "Load"},
                        }.Choose();

                        if (action == null)
                            return 0;

                        action();
                    }
                }
            }
            catch (Exception e)
            {
                SafeConsole.WriteColor(ConsoleColor.DarkRed, e.GetType().Name + ": ");
                SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.StackTrace.Indent(4));
                return -1;
            }
        }

        private static void CSharpMigrations(bool autoRun)
        {
            Schema.Current.Initialize();

            OperationLogic.AllowSaveGlobally = true;

            new CSharpMigrationRunner
            {
                CreateCultureInfo,
                ChartScriptLogic.ImportChartScriptsAuto,
                ImportSpanishInstanceTranslations,
                ImportWordReportTemplateForOrder, 
            }.Run(autoRun); 
        } //CSharpMigrations

        private static void Load(string[] args)
        {
            Schema.Current.Initialize();

            OperationLogic.AllowSaveGlobally = true;

            while (true)
            {
                Action[] actions = new ConsoleSwitch<int, Action>
                {
                    {20, EmployeeLoader.CreateUsers },
                    {21, CreateSystemUser },
                    {42, ChartScriptLogic.ImportExportChartScripts},
                    {43, AuthLogic.ImportExportAuthRules},
                    {44, ImportSpanishInstanceTranslations},
                    {45, HelpXml.ImportExportHelp},
                    {48, ImportWordReportTemplateForOrder},
                    {100, ShowOrder},
                }.ChooseMultiple();

                if (actions == null)
                    return;

                foreach (var acc in actions)
                {
                    Console.WriteLine("------- Executing {0} ".FormatWith(acc.Method.Name.SpacePascal(true)).PadRight(Console.WindowWidth - 2, '-'));
                    acc();
                }
            }
        }

        public static void NewDatabase()
        {
            Console.WriteLine("You will lose all your data. Sure? (Y/N)");
            string val = Console.ReadLine();
            if (!val.StartsWith("y") && !val.StartsWith("Y"))
                return;

            Console.Write("Creating new database...");
            Administrator.TotalGeneration();
            Console.WriteLine("Done.");
        }

        static void Synchronize()
        {
            Console.WriteLine("Check and Modify the synchronization script before");
            Console.WriteLine("executing it in SQL Server Management Studio: ");
            Console.WriteLine();

            SqlPreCommand command = Administrator.TotalSynchronizeScript();
            if (command == null)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Green, "Already synchronized!");
                return;
            }

            command.OpenSqlFileRetry();
        }

        static void ShowOrder()
        {
            var query = Database.Query<OrderEntity>()
              .Where(a => a.Details.Any(l => l.Discount != 0))
              .OrderByDescending(a => a.TotalPrice);

            OrderEntity order = query.First();
        }//ShowOrder

        internal static void CreateSystemUser()
        {
            using (OperationLogic.AllowSave<UserEntity>())
            using (Transaction tr = new Transaction())
            {
                UserEntity system = new UserEntity
                {
                    UserName = "System",
                    PasswordHash = Security.EncodePassword("System"),
                    Role = Database.Query<RoleEntity>().Where(r => r.Name == "Super user").SingleEx(),
                    State = UserState.Saved,
                }.Save();

                tr.Commit();
            }
        } //CreateSystemUser

        public static void CreateCultureInfo()
        {
            using (Transaction tr = new Transaction())
            {
                var en = new CultureInfoEntity(CultureInfo.GetCultureInfo("en")).Save();
                var es = new CultureInfoEntity(CultureInfo.GetCultureInfo("es")).Save();

                new ApplicationConfigurationEntity
                {
                    Environment = "Development",
                    Email = new EmailConfigurationEntity
                    {
                        SendEmails = true,
                        DefaultCulture = en,
                        UrlLeft = "http://localhost/Agile"
                    },
                    SmtpConfiguration = new SmtpConfigurationEntity
                    {
                        Name = "localhost",
                        Network = new SmtpNetworkDeliveryEntity
                        {
                            Host = "localhost"
                        }
                    }, //Email
                    Sms = new SMSConfigurationEntity
                    {
                        DefaultCulture = en,
                    } //Sms
                }.Save();

                tr.Commit();
            }

        }

        public static void ImportSpanishInstanceTranslations()
        {
            TranslatedInstanceLogic.ImportExcelFile("Category.es.View.xlsx");
        }

        public static void ImportWordReportTemplateForOrder()
        {
            new WordTemplateEntity
            {
                Name = "Order template",
                Query = QueryLogic.GetQueryEntity(typeof(OrderEntity)),
                Culture = CultureInfo.GetCultureInfo("en").ToCultureInfoEntity(),
                Template = new FileEntity("../../WordTemplates/Order.docx").ToLiteFat(),
                FileName = "Order.docx"
            }.Save();
        }
    }
}
