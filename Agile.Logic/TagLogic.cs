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
    public static class TagLogic
    {
        static Expression<Func<BoardEntity, IQueryable<TagEntity>>> TagsExpression = 
            b => Database.Query<TagEntity>().Where(t => t.Board.RefersTo(b));
        public static IQueryable<TagEntity> Tags(this BoardEntity e)
        {
            return TagsExpression.Evaluate(e);
        }
    
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<TagEntity>();
        
                dqm.RegisterQuery(typeof(TagEntity), () =>
                    from t in Database.Query<TagEntity>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        t.Board,
                    });
        
                dqm.RegisterExpression((BoardEntity b) => b.Tags(), () => typeof(TagEntity).NicePluralName());
        
                new Graph<TagEntity>.Execute(TagOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();

                new Graph<TagEntity>.ConstructFrom<BoardEntity>(TagOperation.CreteTagFromBoard)
                {
                    Construct = (b, _) => new TagEntity { Board = b.ToLite() }
                }.Register();
            }
        }
    }
}
