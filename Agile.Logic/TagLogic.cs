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
using Signum.Entities.Basics;

namespace Agile.Logic
{
    public static class TagLogic
    {
        static Expression<Func<ProjectEntity, IQueryable<TagEntity>>> TagsExpression = 
            b => Database.Query<TagEntity>().Where(t => t.Project.RefersTo(b));
        public static IQueryable<TagEntity> Tags(this ProjectEntity e)
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
                        t.Project,
                    });

                dqm.RegisterExpression((ProjectEntity b) => b.Tags(), () => typeof(TagEntity).NicePluralName());
        
                new Graph<TagEntity>.Execute(TagOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();

                new Graph<TagEntity>.ConstructFrom<ProjectEntity>(TagOperation.CreteTagFromProject)
                {
                    Construct = (b, args) => new TagEntity 
                    { 
                        Project = b.ToLite(),
                        Name = args.TryGetArgC<string>(),
                        Color = args.TryGetArgC<ColorEntity>(),
                    }
                }.Register();
            }
        }
    }
}
