using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Agile.Entities;

namespace Agile.Test.Environment
{
    public static class AgileExtensions
    {
        public static OrderDetailsEntity AddLine(this OrderEntity order, string productName, int quantity = 1, decimal discount = 0)
        {   
            var product = Database.Query<ProductEntity>().SingleEx(p => p.ProductName.Contains(productName));

            return AddLine(order, product, quantity, discount);  
        }

        public static OrderDetailsEntity AddLine(this OrderEntity order, ProductEntity product, int quantity = 1, decimal discount = 0)
        {
            var result = new OrderDetailsEntity
            {
                Product = product.ToLite(),
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                Discount = discount,
            };

            order.Details.Add(result);

            return result;
        }
    }
}
