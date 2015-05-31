using Agile.Entities;
using Agile.Logic;
using Agile.Web.Board;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Web;
using Signum.Web.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Agile.Web.Controllers
{
    public class CardController : Controller
    {
        public ActionResult Unarchive()
        {
            Lite<ListEntity> list = this.ParseLite<ListEntity>("list");

            MappingContext<CardEntity> ctx = this.ExtractEntity<CardEntity>().ApplyChanges(this);

            if (ctx.HasErrors())
                return ctx.ToJsonModelState();

            try
            {
                ctx.Value.Execute(CardOperation.Unarchive, list);
            }
            catch (IntegrityCheckException e)
            {
                ctx.ImportErrors(e.Errors);

                return ctx.ToJsonModelState();
            }
            
            return this.DefaultExecuteResult(ctx.Value);
        }
    }
}