using Agile.Entities;
using Agile.Logic;
using Agile.Web.Board;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Agile.Web.Controllers
{
    public class BoardController : Controller
    {
        public ActionResult View(Lite<BoardEntity> board)
        {
            var info = board.InDB(b => b.ToBoardInfo());

            return View(BoardClient.ViewClientPrefix.FormatWith("Board"), info);
        }

        public ActionResult CreateCard()
        {
            var list = this.ParseLite<ListEntity>("list").Retrieve();
            var text = this.ParseValue<string>("text");

            list.ConstructFrom(CardOperation.CreateCardFromList, text).Execute(CardOperation.Save);

            var info = list.Board.InDB(b => b.ToBoardInfo());

            return PartialView(BoardClient.ViewClientPrefix.FormatWith("BoardPartial"), info);
        }

    }
}