using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Agile.Logic;
using Agile.Test.Environment.Properties;
using Agile.Entities;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq;
using Signum.Entities.Mailing;
using System.Net.Mail;

namespace Agile.Test.Environment
{
    [TestClass]
    public class EnvironmentTest
    {
        [TestMethod]
        public void GenerateEnvironment()
        {
            var authRules = XDocument.Load(@"D:\Signum\Agile\Agile.Load\AuthRules.xml"); //Change this route if necessary. Only god knows where MSTest is running. 

            AgileEnvironment.Start();

            Administrator.TotalGeneration();

            Schema.Current.Initialize();

            OperationLogic.AllowSaveGlobally = true;

            using (AuthLogic.Disable())
            {
                AgileEnvironment.LoadBasics();

                AuthLogic.LoadRoles(authRules);
                AgileEnvironment.LoadUsers();

                using(AuthLogic.UnsafeUserSession("Super"))
                {
                    var p = new ProjectEntity { Name = "Change the World" }.Execute(ProjectOperation.Save);

                    var wontFix = p.ConstructFrom(TagOperation.CreteTagFromProject, "Won't fix", ColorEntity.FromRGBHex("#ff0000")).Execute(TagOperation.Save);

                    var todo = p.ConstructFrom(ListOperation.CreateListFromProject, "ToDo").Execute(ListOperation.Save);
                    var inProgress = p.ConstructFrom(ListOperation.CreateListFromProject, "In Progress").Execute(ListOperation.Save);
                    var done = p.ConstructFrom(ListOperation.CreateListFromProject, "Done").Execute(ListOperation.Save);

                    var b = new BoardEntity
                    {
                        Name = "Importan Issues",
                        Project = p.ToLite(),
                        Lists = { todo.ToLite(), inProgress.ToLite(), done.ToLite() }
                    }.Execute(BoardOperation.Save);

                    todo.ConstructFrom(CardOperation.CreateCardFromList, "Global Warming").Do(c=>c.Tags.Add(wontFix)).Execute(CardOperation.Save);
                   
                    inProgress.ConstructFrom(CardOperation.CreateCardFromList, "Electric Car").Execute(CardOperation.Save);
                    done.ConstructFrom(CardOperation.CreateCardFromList, "Internet").Execute(CardOperation.Save);
                }


                new ApplicationConfigurationEntity
                {
                    Environment = "Development",
                    Email = new EmailConfigurationEntity
                    {
                        DefaultCulture = Database.Query<CultureInfoEntity>().Single(a => a.Name == "en"),
                    },
                    SmtpConfiguration = new SmtpConfigurationEntity
                    {
                        Name = "EmailFolder",
                        DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                        PickupDirectoryLocation = @"D:\EmailFolder",
                    },
                    Repositories = 
                     {
                         new AttachmentRepositoryEntity 
                         {
                             PhysicalPrefix = @"D:\Signum\Agile\Agile.Web\Files",
                             WebPrefix = "Files",
                         }
                     }
                }.Save();

                AuthLogic.ImportRulesScript(authRules, interactive: false).PlainSqlCommand().ExecuteLeaves();
            }

            OperationLogic.AllowSaveGlobally = false;
        }
    }
}
