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
using Agile.Web.Controllers;

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
                    new EntityOperationSettings<CardEntity>(CardOperation.Archive){  Style = BootstrapStyle.Warning, HideOnCanExecute = true },
                    new EntityOperationSettings<CardEntity>(CardOperation.Unarchive){  
                        Style = BootstrapStyle.Success, 
                        HideOnCanExecute = true ,
                       Click = ctx => CardModule["unarchive"](ctx.Options(), 
                             new FindOptions(typeof(ListEntity), "Project", ctx.Entity.Project) { SearchOnLoad = true }.ToJS(ctx.Prefix, "list"), 
                              ctx.Url.Action((CardController c)=>c.Unarchive()), 
                             JsFunction.Event)
                    },
                    new EntityOperationSettings<CardEntity>(CardOperation.Delete){ HideOnCanExecute = true },
                    new EntityOperationSettings<CardEntity>(CardOperation.Move){ IsVisible = a => false },
                    new EntityOperationSettings<CardEntity>(CommentOperation.CreateCommentFromCard){ IsVisible = a => false },
                    new EntityOperationSettings<CardEntity>(AttachmentOperation.CreateAttachmentFromCard){ IsVisible = a => false },
                });
            }
        }
    }
}
