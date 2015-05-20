using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities;

namespace Agile.Entities
{
    [Serializable]
    public class AddressEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 60)]
        string address;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 60)]
        public string Address
        {
            get { return address; }
            set { Set(ref address, value); }
        }

        [NotNullable, SqlDbType(Size = 15)]
        string city;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 15)]
        public string City
        {
            get { return city; }
            set { Set(ref city, value); }
        }

        [SqlDbType(Size = 15)]
        string region;
        [StringLengthValidator(AllowNulls = true, Min = 2, Max = 15)]
        public string Region
        {
            get { return region; }
            set { Set(ref region, value); }
        }

        [SqlDbType(Size = 10)]
        string postalCode;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 10)]
        public string PostalCode
        {
            get { return postalCode; }
            set { Set(ref postalCode, value); }
        }

        [NotNullable, SqlDbType(Size = 15)]
        string country;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 15)]
        public string Country
        {
            get { return country; }
            set { Set(ref country, value); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => PostalCode))
            {
                if (string.IsNullOrEmpty(postalCode) && Country != "Ireland")
                    return Signum.Entities.ValidationMessage._0IsNotSet.NiceToString().FormatWith(pi.NiceName());
            }

            return null;
        }

        public override string ToString()
        {
            return "{0}\r\n{1} {2} ({3})".FormatWith(Address, PostalCode, City, Country);
        }

        public AddressEntity Clone()
        {
            return new AddressEntity
            {
                Address = this.Address,
                City = this.City,
                Region = this.Region,
                PostalCode = this.PostalCode,
                Country = this.Country
            };
        }
    }
}
