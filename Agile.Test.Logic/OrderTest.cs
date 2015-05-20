using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Operations;
using Signum.Utilities;
using Agile.Entities;
using Agile.Test.Environment;

namespace Agile.Test.Logic
{
    [TestClass]
    public class OrderTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            AgileEnvironment.StartAndInitialize();
        }
    }
}
