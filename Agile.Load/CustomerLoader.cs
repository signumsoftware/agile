using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Agile.Entities;
using Signum.Utilities;
using Signum.Entities;
using Signum.Services;
using System.Globalization;

namespace Agile.Load
{
    internal static class CustomerLoader
    {
        public static void LoadCompanies()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                db.Customers.Where(c => !c.ContactTitle.Contains("Owner")).Select(c =>
                    new CompanyEntity
                    {
                        CompanyName = c.CompanyName,
                        ContactName = c.ContactName,
                        ContactTitle = c.ContactTitle,
                        Address = new AddressEntity
                        {
                            Address = c.Address,
                            City = c.City,
                            Region = c.Region,
                            PostalCode = c.PostalCode,
                            Country = c.Country,
                        },
                        Phone = c.Phone.Replace(".", " "),
                        Fax = c.Fax.Replace(".", " "),
                    }).SaveList();
            }
        }

        public static void LoadPersons()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                db.Customers.Where(c => c.ContactTitle.Contains("Owner")).Select(c =>
                     new PersonEntity
                     {
                         FirstName = c.ContactName.Substring(0, c.ContactName.LastIndexOf(' ')),
                         LastName = c.ContactName.Substring(c.ContactName.LastIndexOf(' ') + 1),
                         DateOfBirth = null,
                         Title = null, 
                         Address = new AddressEntity
                         {
                             Address = c.Address,
                             City = c.City,
                             Region = c.Region,
                             PostalCode = c.PostalCode,
                             Country = c.Country,
                         },
                         Phone = c.Phone.Replace(".", " "),
                         Fax = c.Fax.Replace(".", " "),
                         Corrupt = true,
                     }).SaveList();
            }
        }
    }
}
