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
            ct => new CardTransitionInfo { CreationDate = ct.CreationDate, Entity = ct.ToLite(), From = ct.From, To = ct.To, User = ct.User }; 
        public static CardTransitionInfo ToCardTransitionInfo(this CardTransitionEntity ct)
        {
            return ToCardTransitionInfoExpression.Evaluate(ct);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CardEntity>();
                sb.AddUniqueIndex((CardEntity e) => new { e.List, e.Order });

                sb.Include<CardTransitionEntity>();


                dqm.RegisterQuery(typeof(CardEntity), () =>
                    from c in Database.Query<CardEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        Name = c.Title,
                        c.Archived,
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
                        c.FromOrder,
                        c.To,
                        c.ToOrder,
                    });
     
     
                dqm.RegisterExpression((ListEntity l) => l.Cards(), () => typeof(CardEntity).NicePluralName());

                new Graph<CardEntity>.Execute(CardOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) =>
                    {
                        if (c.IsNew)
                            c.Order = (c.List.InDB().SelectMany(a => a.Cards()).Max(a => (decimal?)a.Order) ?? -1) + 1;
                    }
                }.Register();

                new Graph<CardEntity>.Execute(CardOperation.Archive)
                {
                    Execute = (c, _) => { c.Archived = true; }
                }.Register();

                new Graph<CardEntity>.Execute(CardOperation.Move)
                {
                    Execute = (c, args) => 
                    {
                        var trans = new CardTransitionEntity
                        {
                            Card = c.ToLite(),
                            User = UserEntity.Current.ToLite(),
                            From = c.List,
                            FromOrder = c.Order,
                        };

                        var ops = args.GetArg<CardMoveOptions>();

                        c.List = ops.List;

                        SetOrder(c, ops);

                        trans.To = c.List;
                        trans.ToOrder = c.Order;

                        trans.Save();
                    }
                }.Register();

                new Graph<CardEntity>.ConstructFrom<ListEntity>(CardOperation.CreateCardFromList)
                {
                    Construct = (l, args) => new CardEntity 
                    { 
                        List = l.ToLite(), 
                        Title = args.TryGetArgC<string>(), 
                    }
                }.Register();
            }
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
                    .Where(a => a.Order >= c.Order && a != c)
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

        public override string ToString()
        {
            return HistoryMessage.moved.NiceToString();
        }
    }
}
