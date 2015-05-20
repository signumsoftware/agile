using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Web;
using Agile.Entities;
using Agile.Logic;
using Agile.Web;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Engine.Dashboard;
using Signum.Web.Dashboard;
using System.Globalization;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Web.Operations;
using Signum.Entities.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;

namespace Agile.Web.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        
        public ActionResult Index()
        {
            var panel = DashboardLogic.GetHomePageDashboard();
            if (panel != null)
                return View(DashboardClient.ViewPrefix.FormatWith("Dashboard"), panel);
            
            return View();
        }

        
        public ActionResult ChangeLanguage()
        {
            var ci = CultureInfo.GetCultureInfo(Request.Params["culture"]);

            if (UserEntity.Current == null)
                this.Response.Cookies.Add(new HttpCookie("language", ci.Name) { Expires = DateTime.Now.AddMonths(6) });
            else
            {
                UserEntity.Current.CultureInfo = ci.ToCultureInfoEntity();
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                    UserEntity.Current.Save();
            }

            return Redirect(Request.UrlReferrer.ToString());
        } //ChangeLanguage
        public ActionResult ChangeTheme()
        {
            Session[AgileClient.ThemeSessionKey] = Request.Params["themeSelector"];
            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}
