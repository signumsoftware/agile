using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Agile.Entities;

namespace Agile.Logic
{
    public static class BoardLogic
    {
        static Expression<Func<BoardEntity, IQueryable<ListEntity>>> ListsExpression = 
            b => Database.Query<ListEntity>().Where(l => l.Board.RefersTo(b));
        public static IQueryable<ListEntity> Lists(this BoardEntity e)
        {
            return ListsExpression.Evaluate(e);
        }

        static Expression<Func<ListEntity, ListInfo>> ToListInfoExpression =
            list => new ListInfo
            {
                 Lite = list.ToLite(),
                 Name = list.Name,
                 Subscription = list.SubscriptionMethod(),
                 Cards = list.Cards().OrderBy(a=>a.Order).Select(c => c.ToCardInfo()).ToList(),
            }; 
        public static ListInfo ToListInfo(this ListEntity list)
        {
            return ToListInfoExpression.Evaluate(list);
        }

        static Expression<Func<BoardEntity, BoardInfo>> ToBoardInfoExpression = 
            board => new BoardInfo
            {
                 Lite = board.ToLite(),
                 Name = board.Name,
                 Subscription = board.SubscriptionMethod(),
                 List = board.Lists().OrderBy(a => a.Order).Select(c => c.ToListInfo()).ToList(),
            };
        public static BoardInfo ToBoardInfo(this BoardEntity board)
        {
            return ToBoardInfoExpression.Evaluate(board);
        }
    
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<BoardEntity>();
                sb.Include<ListEntity>();
                sb.AddUniqueIndex((ListEntity e) => new { e.Board, e.Order });
        
                dqm.RegisterQuery(typeof(BoardEntity), () =>
                    from b in Database.Query<BoardEntity>()
                    select new
                    {
                        Entity = b,
                        b.Id,
                        b.Name,
                        b.Archived,
                        b.Project,
                    });
        
                dqm.RegisterQuery(typeof(ListEntity), () =>
                    from l in Database.Query<ListEntity>()
                    select new
                    {
                        Entity = l,
                        l.Id,
                        l.Name,
                        l.Archived,
                        l.Order,
                        l.Board,
                    });
        
                dqm.RegisterExpression((BoardEntity b) => b.Lists(), () => typeof(ListEntity).NicePluralName());

                new Graph<BoardEntity>.ConstructFrom<ProjectEntity>(BoardOperation.CreateBoardFromProject)
                {
                    Construct = (p, args) => new BoardEntity { Project = p.ToLite(), Name = args.TryGetArgC<string>() }
                }.Register();

                new Graph<BoardEntity>.Execute(BoardOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (b, _) => { }
                }.Register();

                new Graph<BoardEntity>.Execute(BoardOperation.Archive)
                {
                    Execute = (b, _) => { b.Archived = true; }
                }.Register();


                new Graph<ListEntity>.ConstructFrom<BoardEntity>(ListOperation.CreateListFromBoard)
                {
                    Construct = (b, args) => new ListEntity { Board = b.ToLite(), Name = args.TryGetArgC<string>() }
                }.Register();

                new Graph<ListEntity>.Execute(ListOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (l, _) =>
                    {
                        if (l.IsNew)
                            l.Order = (l.Board.InDB().SelectMany(a => a.Lists()).Max(a => (decimal?)a.Order) ?? -1) + 1;
                    }
                }.Register();

                new Graph<ListEntity>.Execute(ListOperation.Move)
                {
                    Execute = (list, args) =>
                    {
                        var ops = args.GetArg<ListMoveOptions>();

                        list.Board = ops.Board;

                        SetOrder(list, ops);
                    }
                }.Register();

                new Graph<ListEntity>.Execute(ListOperation.Archive)
                {
                    Execute = (b, _) => { b.Archived = true; }
                }.Register();
            }
        }

        private static void SetOrder(ListEntity li, ListMoveOptions ops)
        {
            if (ops.Previous == null && ops.Next == null)
                li.Order = 0;
            else if (ops.Previous == null)
                li.Order = ops.Next.InDB(a => a.Order) - 1;
            else if (ops.Next == null)
                li.Order = ops.Previous.InDB(a => a.Order) + 1;
            else
            {
                li.Order = ops.Next.InDB(a => a.Order);

                ops.Board.InDB()
                    .SelectMany(l => l.Lists())
                    .Where(a => a.Order >= li.Order && a != li)
                    .UnsafeUpdate()
                    .Set(a => a.Order, a => a.Order + 1)
                    .Execute();
            }
        }
    }

    public class ListInfo
    {
        public Lite<ListEntity> Lite;
        public string Name;
        public SubscriptionMethod? Subscription;
        public List<CardInfo> Cards;
    }

    public class BoardInfo
    {
        public Lite<BoardEntity> Lite;
        public string Name;
        public SubscriptionMethod? Subscription;
        public List<ListInfo> List;
    }
}
