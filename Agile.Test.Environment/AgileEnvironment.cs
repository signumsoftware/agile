using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Utilities;
using Agile.Entities;
using Agile.Logic;
using Agile.Test.Environment.Properties;

namespace Agile.Test.Environment
{
    public static class AgileEnvironment
    {
        internal static void LoadUsers()
        {
            var roles = Database.Query<RoleEntity>().ToDictionary(a => a.Name);

            CreateUser("Super", roles.GetOrThrow("Super user"));
            CreateUser("Advanced", roles.GetOrThrow("Advanced user"));
            CreateUser("Normal", roles.GetOrThrow("User"));
        }

        static void CreateUser(string userName, RoleEntity role)
        {
            var user = new UserEntity
            {
                UserName = userName,
                PasswordHash = Security.EncodePassword(userName),
                Role = role,
                State = UserState.Saved,
            };
            user.Save();
        }//LoadUsers

        static bool started = false;
        public static void Start()
        {
            if (!started)
            {
                var cs = UserConnections.Replace(Settings.Default.ConnectionString);

                if (!cs.Contains("Test")) //Security mechanism to avoid passing test on production
                    throw new InvalidOperationException("ConnectionString does not contain the word 'Test'.");

                Starter.Start(cs);
                started = true;
            }
        }

        public static void StartAndInitialize()
        {
            Start();
            Schema.Current.Initialize();
        }

        internal static void LoadBasics()
        {
            var en = new CultureInfoEntity(CultureInfo.GetCultureInfo("en")).Save();
            var es = new CultureInfoEntity(CultureInfo.GetCultureInfo("es")).Save();
        }
    }
}
