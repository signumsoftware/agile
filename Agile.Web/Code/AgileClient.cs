using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Web;
using Signum.Utilities;
using Agile.Entities;
using System.Web.Mvc;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.SMS;
using Signum.Entities.Mailing;
using Signum.Entities.Files;
using Signum.Web.Files;
using Signum.Web.Operations;
using Agile.Web.Controllers;
using Signum.Engine.Operations;
using System.Web.Mvc.Html;

namespace Agile.Web
{
    public static class AgileClient
    {
        public static string ViewPrefix = "~/Views/Agile/{0}.cshtml";
        public static string ThemeSessionKey = "swCurrentTheme";
        public static JsModule ProductModule = new JsModule("Product");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ApplicationConfigurationEntity>() { PartialViewName = e => ViewPrefix.FormatWith("ApplicationConfiguration") },
                });

                Constructor.Register(ctx => new ApplicationConfigurationEntity
                {
                    Email = new EmailConfigurationEntity()
                });

                QuerySettings.RegisterPropertyFormat((CategoryEntity e) => e.Picture,
                    new CellFormatter((html, obj) => obj == null ? null :
                        new HtmlTag("img")
                       .Attr("src", Base64Data((EmbeddedFileEntity)obj))
                      .Attr("alt", obj.ToString())
                      .Attr("style", "width:48px").ToHtmlSelf()) { TextAlign = "center" }); // Category

                QuerySettings.RegisterPropertyFormat((EmployeeEntity e) => e.Photo,
                    new CellFormatter((html, obj) => obj == null ? null :
                      new HtmlTag("img")
                      .Attr("src", RouteHelper.New().Action((FileController c) => c.Download(new RuntimeInfo((Lite<FileEntity>)obj).ToString())))
                      .Attr("alt", obj.ToString())
                      .Attr("style", "width:48px").ToHtmlSelf()) { TextAlign = "center" }); //Emmployee
            }
        }
        public static string Base64Data(EmbeddedFileEntity file)
        {
            return "data:" + MimeType.FromFileName(file.FileName) + ";base64," + Convert.ToBase64String(file.BinaryFile);
        } //Base64Data
    }
}