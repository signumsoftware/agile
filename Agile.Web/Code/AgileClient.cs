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
using Signum.Engine.Chart;

namespace Agile.Web
{
    public static class AgileClient
    {
        public static string ViewPrefix = "~/Views/Admin/Agile/{0}.cshtml";
        public static JsModule ProductModule = new JsModule("Product");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ApplicationConfigurationEntity>() { PartialViewName = e => ViewPrefix.FormatWith("ApplicationConfiguration") },
                    new EmbeddedEntitySettings<AttachmentRepositoryEntity>() { PartialViewName = e => ViewPrefix.FormatWith("AttachmentRepository") },
                });

                Constructor.Register(ctx => new ApplicationConfigurationEntity
                {
                    Email = new EmailConfigurationEntity()
                });

                Navigator.EntitySettings<UserEntity>().CreateViewOverrides().AfterLine(u => u.PasswordSetDate, 
                    (html, tc) => html.ValueLine(tc, u => u.Mixin<NotificationUserMixin>().SendNotificationDigestEvery));
            }
        }

        static List<string> ColorPalette = ChartColorLogic.Palettes["Category20"].Split(' ').ToList();

        public static string HexColor(Lite<UserEntity> user)
        {
            return ChartColorLogic.ColorFor(user).TryToHtml() ?? ColorPalette[user.Id.GetHashCode().Mod(ColorPalette.Count)]; 
        }

        public static string Initials(Lite<UserEntity> u)
        {
            return new string(u.ToString().SplitNoEmpty(' ').Take(2).Select(a => char.ToUpper(a[0])).ToArray());
        }
    }
}