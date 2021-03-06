﻿using Agile.Entities;
using Agile.Logic;
using Agile.Web.Board;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Web;
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
            var info = GetBoardInfoConsumingNotifications(board);

            return View(BoardClient.ViewClientPrefix.FormatWith("Board"), info);
        }

        public ActionResult CreateCard()
        {
            var list = this.ParseLite<ListEntity>("list").Retrieve();
            var text = this.ParseValue<string>("text");

            list.ConstructFrom(CardOperation.CreateCardFromList, text).Execute(CardOperation.Save);

            return PartialBoard();
        }

        public ActionResult PartialBoard()
        {
            var board = this.ParseLite<BoardEntity>("board");

            var info = GetBoardInfoConsumingNotifications(board);

            return PartialView(BoardClient.ViewClientPrefix.FormatWith("BoardPartial"), info);
        }

        private static BoardInfo GetBoardInfoConsumingNotifications(Lite<BoardEntity> board)
        {
            var info = board.InDB(b => b.ToBoardInfo());

            if (info.Lists.SelectMany(l => l.Cards).SelectMany(a => a.Notifications).Any(NotificationAttendedIn.Board.Contains))
            {
                board.InDB().SelectMany(b => b.Lists)
                  .SelectMany(l => l.Entity.Cards())
                  .SelectMany(c => c.MyUnreadNotifications())
                  .Where(n => NotificationAttendedIn.Board.Contains(n.Type))
                  .UnsafeUpdate()
                  .Set(a => a.ReadOn, a => TimeZoneManager.Now)
                  .Execute();
            }
            return info;
        }

        public ActionResult MoveBoard()
        {
            var card = this.ParseLite<CardEntity>("card").Retrieve();
            var list = this.ParseLite<ListEntity>("list").FillToString();
            var next = this.ParseLite<CardEntity>("next");
            var prev = this.ParseLite<CardEntity>("prev");

            card.Execute(CardOperation.Move, new CardMoveOptions { List = list, Next = next, Previous = prev  });

            return PartialBoard();
        }

        public ActionResult SaveAndRefresh()
        {
            var card = this.ExtractEntity<CardEntity>();

            MappingContext context = card.ApplyChanges(this).Validate();

            if (context.HasErrors())
                return context.ToJsonModelState();

            try
            {
                card.Execute(CardOperation.Save);
            }
            catch (IntegrityCheckException e)
            {
                context.ImportErrors(e.Errors);
                return context.ToJsonModelState();
            }

            return null;
        }
    }
}