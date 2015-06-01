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
using Signum.Entities.Files;
using Signum.Engine.Files;
using System.IO;
using Signum.Entities.Authorization;

namespace Agile.Logic
{
    public static class CardLogic
    {
        static Expression<Func<ListEntity, IQueryable<CardEntity>>> CardsExpression =
            l => Database.Query<CardEntity>().Where(c => c.List.RefersTo(l));
        public static IQueryable<CardEntity> Cards(this ListEntity e)
        {
            return CardsExpression.Evaluate(e);
        }

        static Expression<Func<CardEntity, IQueryable<CardTransitionEntity>>> CardTransitionsExpression =
            c => Database.Query<CardTransitionEntity>().Where(ct => ct.Card.RefersTo(c));
        public static IQueryable<CardTransitionEntity> CardTransitions(this CardEntity c)
        {
            return CardTransitionsExpression.Evaluate(c);
        }

        static Expression<Func<CardEntity, CardInfo>> ToCardInfoExpression =
            c => new CardInfo
            {
                Lite = c.ToLite(),
                Title = c.Title,
                DueDate = c.DueDate,
                Attachments = c.Attachments().Count(),
                HasDescription = c.Description.HasText(),
                Comments = c.Comments().Count(),
                Tags = c.Tags.ToList(),
                FirstImage = c.Attachments().OrderBy(a=>a.CreationDate).Where(a=>a.Type == AttachmentType.Image).Select(a=>a.File).FirstOrDefault(),
                Subscription = c.SubscriptionMethod(),
            }; 
        public static CardInfo ToCardInfo(this CardEntity c)
        {
            return ToCardInfoExpression.Evaluate(c);
        }

        static Expression<Func<CardTransitionEntity, CardTransitionInfo>> ToCardTransitionInfoExpression =
            ct => new CardTransitionInfo { 
                CreationDate = ct.CreationDate, 
                Entity = ct.ToLite(), 
                From = ct.From, 
                To = ct.To,
                FromPositon = ct.FromPosition,
                ToPosition = ct.ToPosition, 
                User = ct.User }; 
        public static CardTransitionInfo ToCardTransitionInfo(this CardTransitionEntity ct)
        {
            return ToCardTransitionInfoExpression.Evaluate(ct);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CardEntity>();
                sb.AddUniqueIndex((CardEntity e) => new { e.List, e.Order }, a => a.List != null);

                sb.Include<CardTransitionEntity>();


                dqm.RegisterQuery(typeof(CardEntity), () =>
                    from c in Database.Query<CardEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Title,
                        c.State,
                        c.Order,
                        c.List,
                    });


                dqm.RegisterQuery(typeof(CardTransitionEntity), () =>
                    from c in Database.Query<CardTransitionEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Card,
                        c.User,
                        c.From,
                        FromOrder = c.FromPosition,
                        c.To,
                        ToOrder = c.ToPosition,
                    });
     
     
                dqm.RegisterExpression((ListEntity l) => l.Cards(), () => typeof(CardEntity).NicePluralName());

                CardGraph.Register();
            }
        }

        
    }

    public class CardGraph: Graph<CardEntity, ArchivedState>
    {
        public static void Register()
        {
            GetState = a => a.State;

            new Execute(CardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Alive },
                Execute = (c, _) =>
                {
                    if (c.IsNew)
                        c.Order = (c.List.InDB().SelectMany(a => a.Cards()).Max(a => (decimal?)a.Order) ?? -1) + 1;
                }
            }.Register();

            
            new Execute(CardOperation.Archive)
            {
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Archived },
                Execute = (c, _) => 
                {
                    new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = c.List,
                        FromPosition = Position(c),
                        FromState = ArchivedState.Alive,
                        To = null,
                        ToPosition = null,
                        ToState = ArchivedState.Archived,
                    }.Save();

                    c.State = ArchivedState.Archived;
                    c.List = null;
                }
            }.Register();

            new Execute(CardOperation.Unarchive)
            {
                FromStates = { ArchivedState.Archived },
                ToStates = { ArchivedState.Alive },
                Execute = (c, args) =>
                {
                    c.State = ArchivedState.Alive;
                    c.List = args.GetArg<Lite<ListEntity>>();
                    c.Order = (c.List.InDB().SelectMany(a => a.Cards()).Max(a => (decimal?)a.Order) ?? -1) + 1;

                    new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = null,
                        FromPosition = null,
                        FromState = ArchivedState.Archived,
                        To = c.List,
                        ToPosition = Position(c),
                        ToState = ArchivedState.Alive,
                    }.Save();
                }
            }.Register();

            new Graph<CardEntity, ArchivedState>.Delete(CardOperation.Delete)
            {
                FromStates = { ArchivedState.Archived },
                Delete = (c, _) => { c.Delete(); }
            }.Register();

            Schema.Current.EntityEvents<CardEntity>().PreUnsafeDelete += query => query.SelectMany(q => q.CardTransitions()).UnsafeDelete();

            new Execute(CardOperation.Move)
            {
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Alive },
                Execute = (c, args) =>
                {
                    var trans = new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = c.List,
                        FromPosition = Position(c),
                    };

                    var ops = args.GetArg<CardMoveOptions>();

                    c.List = ops.List;

                    SetOrder(c, ops);

                    trans.To = c.List;
                    trans.ToPosition = Position(c);

                    trans.Save();
                }
            }.Register();

            new ConstructFrom<ListEntity>(CardOperation.CreateCardFromList)
            {
                ToStates = { ArchivedState.Alive },
                Construct = (l, args) => new CardEntity
                {
                    List = l.ToLite(),
                    Project = l.Project,
                    Title = args.TryGetArgC<string>(),
                }
            }.Register();

        }

        private static int Position(CardEntity c)
        {
            return c.List.InDB(l => l.Cards().Count(a => a.Order < c.Order));
        }

        private static void SetOrder(CardEntity c, CardMoveOptions ops)
        {
            if (ops.Previous == null && ops.Next == null)
                c.Order = 0;
            else if (ops.Previous == null)
                c.Order = ops.Next.InDB(a => a.Order) - 1;
            else if (ops.Next == null)
                c.Order = ops.Previous.InDB(a => a.Order) + 1;
            else
            {
                c.Order = ops.Next.InDB(a => a.Order);

                ops.List.InDB()
                    .SelectMany(l => l.Cards())
                    .Where(a => a.Order >= c.Order) // && a != c
                    .UnsafeUpdate()
                    .Set(a => a.Order, a => a.Order + 1)
                    .Execute();
            }
        }
    }

    public class CardInfo
    {
        public Lite<CardEntity> Lite;
        public string Title;
        public DateTime? DueDate;     
        public bool HasDescription;
        public int Comments;
        public int Attachments;
        public FilePathEntity FirstImage;
        public List<TagEntity> Tags;
        public SubscriptionMethod? Subscription;

    }

    public class CardTransitionInfo : HistoryInfo
    {
        public Lite<ListEntity> From;
        public Lite<ListEntity> To;

        public int? ToPosition;
        public int? FromPositon;

        public override string ToString()
        {
            return HistoryMessage.moved.NiceToString();
        }
    }
}
