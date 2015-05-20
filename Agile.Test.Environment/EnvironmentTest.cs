using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Agile.Logic;
using Agile.Test.Environment.Properties;

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
                AuthLogic.ImportRulesScript(authRules, interactive: false).PlainSqlCommand().ExecuteLeaves();
            }

            OperationLogic.AllowSaveGlobally = false;
        }
    }
}
