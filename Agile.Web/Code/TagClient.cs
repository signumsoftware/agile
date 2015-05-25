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
using Signum.Web.Basic;

namespace Agile.Web.Tag
{
    public static class TagClient
    {
        public static string ViewPrefix = "~/Views/Admin/Tag/{0}.cshtml";
        public static JsModule TagModule = new JsModule("Tag");
    
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                BasicClient.Start();

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<TagEntity>() { PartialViewName = t => ViewPrefix.FormatWith("Tag") },
                });
        
                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    //new EntityOperationSettings<T>(operation){ ... }
                });
            }
        }
    }
}
