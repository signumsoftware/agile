﻿using System;
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
        internal static void LoadEmployees()
        {
            var america = new RegionEntity { Description = "America" };

            var east = new TerritoryEntity { Description = "East coast", Region = america }.Save();
            var west = new TerritoryEntity { Description = "South coast", Region = america }.Save();

            var super = new EmployeeEntity
            {
                FirstName = "Super",
                LastName = "User",
                Address = RandomAddress(1),
                HomePhone = RandomPhone(1),
                Territories = { east, west },
            }.Save();

            new EmployeeEntity
            {
                FirstName = "Advanced",
                LastName = "User",
                Address = RandomAddress(2),
                HomePhone = RandomPhone(2),
                Territories = { west },
                ReportsTo = super.ToLite(),
            }.Save();

            new EmployeeEntity
            {
                FirstName = "Normal",
                LastName = "User",
                Address = RandomAddress(3),
                HomePhone = RandomPhone(4),
                Territories = { east },
                ReportsTo = super.ToLite(),
            }.Save();

        } //LoadEmployees

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

        internal static void LoadProducts()
        {
            SupplierEntity lego = new SupplierEntity
            {
                CompanyName = "Lego Corp",
                Address = RandomAddress(4),
                Phone = RandomPhone(4),
                Fax = RandomPhone(4),
                ContactName = "Billund",
            }.Save();

            var construction = new CategoryEntity
            {
                CategoryName = "Construction games",
                Description = "Let your imagination create your toys"
            }.Save();

            new ProductEntity
            {
                ProductName = "Lego Mindstorms EV3",
                Category = construction.ToLite(),
                Supplier = lego.ToLite(),
                QuantityPerUnit = "1 Box",
                UnitPrice = 159.90m,
                ReorderLevel = 1,
                UnitsInStock = 10
            }.Save();


            SupplierEntity sega = new SupplierEntity
            {
                CompanyName = "Sega Inc",
                Phone = RandomPhone(10),
                Fax = RandomPhone(10),
                Address = RandomAddress(10),
                ContactName = "Kalinske",
            }.Save();

            var videoGames = new CategoryEntity
            {
                CategoryName = "Video games",
                Description = "Enjoy virtual worlds and fabulous adventures"
            }.Save();

            new ProductEntity
            {
                ProductName = "Sonic the Hedgehog",
                Category = videoGames.ToLite(),
                Supplier = sega.ToLite(),
                QuantityPerUnit = "1 Case",
                UnitPrice = 49.90m,
                ReorderLevel = 2,
                UnitsInStock = 30
            }.Save();
        }

        internal static void LoadCustomers()
        {
            new PersonEntity
            {
                FirstName = "John",
                LastName = "Connor",
                Phone = RandomPhone(5),
                Address = RandomAddress(5),
            }.Save();

            new PersonEntity
            {
                FirstName = "Sara",
                LastName = "Connor",
                Phone = RandomPhone(6),
                Address = RandomAddress(6),
            }.Save();

            new CompanyEntity
            {
                CompanyName = "Cyberdyne Systems Corporation",
                ContactName = "Miles Dyson",
                ContactTitle = "Dr.",
                Phone = RandomPhone(7),
                Address = RandomAddress(7),
            }.Save();
        }

        private static AddressEntity RandomAddress(int seed)
        {
            Random r = new Random(seed);
            return new AddressEntity
            {
                Address = r.NextElement(new[] { "Madison Av.", "Sessame Str.", "5th Av.", "Flamingo Way" }) + " " + r.Next(100),
                City = r.NextElement(new[] { "New York", "Los Angeles", "Miami", "Seattle" }),
                Country = "USA",
                Region = r.NextElement(new[] { "NY", "FL", "WA", "CA" }),
                PostalCode = r.NextString(5, "0123456789")
            };
        }

        private static string RandomPhone(int seed)
        {
            Random r = new Random(seed);
            return r.NextString(10, "0123456789");
        }

        internal static void LoadShippers()
        {
            new ShipperEntity
            {
                CompanyName = "FedEx",
                Phone = RandomPhone(11),
            }.Save();
        }//LoadShippers

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
