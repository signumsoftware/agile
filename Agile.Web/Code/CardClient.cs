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

namespace Agile.Web.Card
{
    public static class CardClient
    {
        public static string ViewPrefix = "~/Views/Admin/Card/{0}.cshtml";
        public static JsModule CardModule = new JsModule("Card");
        public static JsModule CommentModule = new JsModule("Comment");
    
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<CardEntity>() { PartialViewName = c => ViewPrefix.FormatWith("Card") },
                    new EntitySettings<AttachmentEntity>() { PartialViewName = a => ViewPrefix.FormatWith("Attachment") },
                    new EntitySettings<CommentEntity>() { PartialViewName = c => ViewPrefix.FormatWith("Comment") },
                });
        
                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    //new EntityOperationSettings<T>(operation){ ... }
                });
            }
        }
    }
}
