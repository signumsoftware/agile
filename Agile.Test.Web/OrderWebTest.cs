using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Web.Selenium;
using Agile.Entities;
using Agile.Test.Environment;

namespace Agile.Test.Web
{
    [TestClass]
    public class OrderWebTest : AgileTestClass
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            AgileEnvironment.StartAndInitialize();
            AuthLogic.GloballyEnabled = false;
        }
    }
}
