using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Data;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Entities.Files;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmployeeEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 20)]
        string lastName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 20)]
        public string LastName
        {
            get { return lastName; }
            set { SetToStr(ref lastName, value); }
        }

        [NotNullable, SqlDbType(Size = 10)]
        string firstName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 10)]
        public string FirstName
        {
            get { return firstName; }
            set { SetToStr(ref firstName, value); }
        }

        [SqlDbType(Size = 30)]
        string title;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 30)]
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }

        [SqlDbType(Size = 25)]
        string titleOfCourtesy;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 25)]
        public string TitleOfCourtesy
        {
            get { return titleOfCourtesy; }
            set { Set(ref titleOfCourtesy, value); }
        }

        DateTime? birthDate;
        [DateTimePrecissionValidator(DateTimePrecision.Days)]
        public DateTime? BirthDate
        {
            get { return birthDate; }
            set { Set(ref birthDate, value); }
        }

        DateTime? hireDate;
        public DateTime? HireDate
        {
            get { return hireDate; }
            set { Set(ref hireDate, value); }
        }

        [NotNullable]
        AddressEntity address;
        [NotNullValidator]
        public AddressEntity Address
        {
            get { return address; }
            set { Set(ref address, value); }
        }

        [SqlDbType(Size = 25)]
        string homePhone;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 25), TelephoneValidator]
        public string HomePhone
        {
            get { return homePhone; }
            set { Set(ref homePhone, value); }
        }

        [SqlDbType(Size = 4)]
        string extension;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 4), TelephoneValidator]
        public string Extension
        {
            get { return extension; }
            set { Set(ref extension, value); }
        }

        Lite<FileEntity> photo;
        public Lite<FileEntity> Photo
        {
            get { return photo; }
            set { Set(ref photo, value); }
        }

        [SqlDbType(Size = int.MaxValue),]
        string notes;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Notes
        {
            get { return notes; }
            set { Set(ref notes, value); }
        }

        Lite<EmployeeEntity> reportsTo;
        public Lite<EmployeeEntity> ReportsTo
        {
            get { return reportsTo; }
            set { Set(ref reportsTo, value); }
        }

        [SqlDbType(Size = 255)]
        string photoPath;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255), URLValidator]
        public string PhotoPath
        {
            get { return photoPath; }
            set { Set(ref photoPath, value); }
        }

        [NotNullable]
        MList<TerritoryEntity> territories = new MList<TerritoryEntity>();
        [NoRepeatValidator]
        public MList<TerritoryEntity> Territories
        {
            get { return territories; }
            set { Set(ref territories, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(FirstName, LastName);
        }

        public static EmployeeEntity Current
        {
            get { return UserEntity.Current.Mixin<UserEmployeeMixin>().Employee; } //get { return null; }
        } //Current
    }

    public static class EmployeeOperation
    {
        public static readonly ExecuteSymbol<EmployeeEntity> Save = OperationSymbol.Execute<EmployeeEntity>();
    }

    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class TerritoryEntity : Entity
    {
        RegionEntity region;
        [NotNullValidator]
        public RegionEntity Region
        {
            get { return region; }
            set { Set(ref region, value); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string description;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Description
        {
            get { return description; }
            set { SetToStr(ref description, value); }
        }

        static Expression<Func<TerritoryEntity, string>> ToStringExpression = e => e.Description;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class TerritoryOperation
    {
        public static readonly ExecuteSymbol<TerritoryEntity> Save = OperationSymbol.Execute<TerritoryEntity>();
    }

    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class RegionEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 50), UniqueIndex]
        string description;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string Description
        {
            get { return description; }
            set { SetToStr(ref description, value); }
        }

        static Expression<Func<RegionEntity, string>> ToStringExpression = e => e.Description;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class RegionOperation
    {
        public static readonly ExecuteSymbol<RegionEntity> Save = OperationSymbol.Execute<RegionEntity>();
    }

    public enum EmployeeQuery
    {
        EmployeesByTerritory
    }
}
