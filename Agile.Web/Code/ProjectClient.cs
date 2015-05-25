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

namespace Agile.Web.Project
{
    public static class ProjectClient
    {
        public static string ViewPrefix = "~/Views/Admin/Project/{0}.cshtml";
        public static JsModule ProjectModule = new JsModule("Project");
    
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProjectEntity>() { PartialViewName = p => ViewPrefix.FormatWith("Project") },
                });
        
                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    //new EntityOperationSettings<T>(operation){ ... }
                });
            }
        }
    }
}
