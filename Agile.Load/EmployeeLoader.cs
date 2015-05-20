using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Agile.Entities;
using Signum.Utilities;
using Signum.Entities;
using Signum.Services;
using Signum.Entities.Authorization;
using Signum.Engine.Operations;
using Signum.Entities.Files;

namespace Agile.Load
{
    internal static class EmployeeLoader
    {
        public static void LoadRegions()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                Administrator.SaveListDisableIdentity(db.Regions.Select(r =>
                    new RegionEntity
                    {
                        Description = r.RegionDescription.Trim()
                    }.SetId(r.RegionID)));
            }
        }

        public static void LoadTerritories()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                var regionDic = Database.RetrieveAll<RegionEntity>().ToDictionary(a => a.Id);

                var territories = (from t in db.Territories.ToList()
                                   group t by t.TerritoryDescription into g
                                   select new
                                   {
                                       Description = g.Key.Trim(),
                                       Id = g.Select(t => t.TerritoryID).OrderBy().First(),
                                       RegionID = g.Select(r => r.RegionID).Distinct().Single(),
                                   }).ToList();

                Administrator.SaveListDisableIdentity(territories.Select(t =>
                    new TerritoryEntity
                    {
                        Description = t.Description.Trim(),
                        Region = regionDic[t.RegionID]
                    }.SetId(int.Parse(t.Id))));
            }
        }

        public static void LoadEmployees()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                var duplicateMapping = (from t in db.Territories.ToList()
                                        group int.Parse(t.TerritoryID) by t.TerritoryDescription into g
                                        where g.Count() > 1
                                        let min = g.Min()
                                        from item in g.Except(new[] { min })
                                        select new
                                        {
                                            Min = min,
                                            Item = item
                                        }).ToDictionary(a => a.Item, a => a.Min);

                var territoriesDic = Database.RetrieveAll<TerritoryEntity>().ToDictionary(a => a.Id);


                var exmployeeTerritories = (from e in db.Employees
                                            from t in e.EmployeeTerritories
                                            select new { e.EmployeeID, t.TerritoryID }).ToList()
                          .AgGroupToDictionary(a => a.EmployeeID, gr =>
                              gr.Select(a => int.Parse(a.TerritoryID))
                              .Select(id => duplicateMapping.TryGet(id, id))
                              .Select(id => territoriesDic[id])
                              .Distinct().ToMList());


                Administrator.SaveListDisableIdentity(
                    from e in db.Employees
                    select new EmployeeEntity
                    {
                        BirthDate = e.BirthDate,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        TitleOfCourtesy = e.TitleOfCourtesy,
                        HomePhone = e.HomePhone,
                        Extension = e.Extension,
                        HireDate = e.HireDate,
                        Photo = new FileEntity { FileName = e.PhotoPath.AfterLast('/'), BinaryFile = RemoveOlePrefix(e.Photo.ToArray()) }.ToLiteFat(),
                        PhotoPath = e.PhotoPath,
                        Address = new AddressEntity
                        {
                            Address = e.Address,
                            City = e.City,
                            Country = e.Country,
                            Region = e.Region,
                            PostalCode = e.PostalCode,
                        },
                        Notes = e.Notes,
                        Territories = exmployeeTerritories[e.EmployeeID],
                    }.SetId(e.EmployeeID));

                var pairs = (from e in db.Employees
                             where e.ReportsTo != null
                             select new { e.EmployeeID, e.ReportsTo });

                foreach (var pair in pairs)
                {
                    EmployeeEntity employee = Database.Retrieve<EmployeeEntity>(pair.EmployeeID);
                    employee.ReportsTo = Lite.Create<EmployeeEntity>(pair.ReportsTo.Value);
                    employee.Save();
                }
            }
        }

        public static byte[] RemoveOlePrefix(byte[] bytes)
        {
            byte[] clean = new byte[bytes.Length - 78];
            Array.Copy(bytes, 78, clean, 0, bytes.Length - 78);
            return clean;
        } //RemoveOlePrefix

        internal static void CreateUsers()
        {
            using (Transaction tr = new Transaction())
            {
                RoleEntity su = new RoleEntity() { Name = "Super user", MergeStrategy = MergeStrategy.Intersection }.Save();
                RoleEntity u = new RoleEntity() { Name = "User", MergeStrategy = MergeStrategy.Union }.Save();

                RoleEntity au = new RoleEntity()
                {
                    Name = "Advanced user",
                    Roles = new MList<Lite<RoleEntity>> { u.ToLite() },
                    MergeStrategy = MergeStrategy.Union
                }.Save();

                var employees = Database.Query<EmployeeEntity>().OrderByDescending(a => a.Notes.Length).ToList();

                using (OperationLogic.AllowSave<UserEntity>())
                    for (int i = 0; i < employees.Count; i++)
                    {
                        var employee = employees[i];
                        new UserEntity
                        {
                            UserName = employee.FirstName,
                            PasswordHash = Security.EncodePassword(employee.FirstName),
                            Role = i < 2 ? su :
                                   i < 5 ? au : u,
                            State = UserState.Saved,

                        }.SetMixin((UserEmployeeMixin e)=>e.Employee, employee).Save();
                    }

                tr.Commit();
            }
        } //CreateUsers
    }
}
