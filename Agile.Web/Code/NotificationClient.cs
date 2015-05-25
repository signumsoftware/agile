using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Web;
using Signum.Web.Operations;
using Agile.Entities;

namespace Agile.Web.Notification
{
    public static class NotificationClient
    {
        public static string ViewPrefix = "~/Views/Admin/Notification/{0}.cshtml";
        public static JsModule NotificationModule = new JsModule("Notification");
    
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<NotificationEntity>() { PartialViewName = n => ViewPrefix.FormatWith("Notification") },
                    new EntitySettings<SubscriptionEntity>() { PartialViewName = s => ViewPrefix.FormatWith("Subscription") },
                });
        
                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    //new EntityOperationSettings<T>(operation){ ... }
                });
            }
        }
    }
}
