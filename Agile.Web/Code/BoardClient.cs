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

namespace Agile.Web.Board
{
    public static class BoardClient
    {
        public static string ViewPrefix = "~/Views/Admin/Board/{0}.cshtml";
        public static string ViewClientPrefix = "~/Views/Board/{0}.cshtml";
        public static JsModule BoardModule = new JsModule("Board");
    
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<BoardEntity>() { PartialViewName = b => ViewPrefix.FormatWith("Board") },
                    new EntitySettings<ListEntity>() { PartialViewName = l => ViewPrefix.FormatWith("List") },
                });
        
                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    new EntityOperationSettings<BoardEntity>(BoardOperation.Archive){  Style = BootstrapStyle.Warning, HideOnCanExecute = true },
                    new EntityOperationSettings<BoardEntity>(BoardOperation.Unarchive){  Style = BootstrapStyle.Success, HideOnCanExecute = true },
                });
            }
        }
    }
}
