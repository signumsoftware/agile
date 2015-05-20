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


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CardEntity>();
                sb.AddUniqueIndex((CardEntity e) => new { e.List, e.Order });

                dqm.RegisterQuery(typeof(CardEntity), () =>
                    from c in Database.Query<CardEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Name,
                        c.Archived,
                        c.Order,
                        c.List,
                    });
     
                dqm.RegisterExpression((ListEntity l) => l.Cards(), () => typeof(CardEntity).NicePluralName());

                new Graph<CardEntity>.Execute(CardOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => { }
                }.Register();

                new Graph<CardEntity>.Execute(CardOperation.Archive)
                {
                    Execute = (c, _) => { c.Archived = true; }
                }.Register();

                new Graph<CardEntity>.Execute(CardOperation.Move)
                {
                    Execute = (c, args) => 
                    {
                        var ops = args.GetArg<CardMoveOptions>();

                        c.List = ops.List;

                        SetOrder(c, ops);
                    }
                }.Register();

                new Graph<CardEntity>.ConstructFrom<ListEntity>(CardOperation.CreateCardFromBoard)
                {
                    Construct = (l, _) => new CardEntity { List = l.ToLite() }
                }.Register();


                FilePathLogic.Register(AttachmentFileType.Attachment, new FileTypeAlgorithm
                {
                    GetPrefixPair = fp =>
                    {
                        var first = Starter.Configuration.Value.Repositories.FirstEx(r => File.Exists(Path.Combine(r.PhysicalPrefix, fp.Sufix)));

                        return new PrefixPair(first.PhysicalPrefix) { WebPrefix = first.WebPrefix };
                    }
                });
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
}
