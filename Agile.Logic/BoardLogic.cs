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
                Projects = board.Project,
                Name = board.Name,
                Subscription = board.SubscriptionMethod(),
                Lists = board.MListElements(b => b.Lists).OrderBy(a => a.Order).Select(c => c.Element.Entity.ToListInfo()).ToList(),
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
        
                dqm.RegisterQuery(typeof(BoardEntity), () =>
                    from b in Database.Query<BoardEntity>()
                    select new
                    {
                        Entity = b,
                        b.Id,
                        b.Name,
                        b.State,
                        b.Project,
                    });
        
                dqm.RegisterQuery(typeof(ListEntity), () =>
                    from l in Database.Query<ListEntity>()
                    select new
                    {
                        Entity = l,
                        l.Id,
                        l.Name,
                        l.Project,
                    });

             


                new Graph<ListEntity>.ConstructFrom<ProjectEntity>(ListOperation.CreateListFromProject)
                {
                    Construct = (p, args) => new ListEntity
                    {
                        Project = p.ToLite(),
                        Name = args.TryGetArgC<string>()
                    }
                }.Register();

                new Graph<ListEntity>.Execute(ListOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (l, _) =>
                    {
                    }
                }.Register();

                BoardGraph.Register();
            }
        }
    }

    public class BoardGraph : Graph<BoardEntity, ArchivedState>
    {
        public static void Register()
        {
            new ConstructFrom<ProjectEntity>(BoardOperation.CreateBoardFromProject)
            {
                ToStates = { ArchivedState.Alive }, 
                Construct = (p, args) => new BoardEntity
                {
                    Project = p.ToLite(),
                    Name = args.TryGetArgC<string>()
                }
            }.Register();

            new Graph<BoardEntity>.Execute(BoardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (b, _) => { }
            }.Register();

            new Execute(BoardOperation.Archive)
            {
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Archived },
                Execute = (c, _) =>
                {
                    c.State = ArchivedState.Archived;
                  
                }
            }.Register();

            new Execute(BoardOperation.Unarchive)
            {
                FromStates = { ArchivedState.Archived },
                ToStates = { ArchivedState.Alive },
                Execute = (c, args) =>
                {
                    c.State = ArchivedState.Alive;
                }
            }.Register();
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
        public Lite<ProjectEntity> Projects;
        public string Name;
        public SubscriptionMethod? Subscription;
        public List<ListInfo> Lists;
    }
}
