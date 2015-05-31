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
using Signum.Entities.Authorization;

namespace Agile.Logic
{
    public static class ProjectLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProjectEntity>();
        
                dqm.RegisterQuery(typeof(ProjectEntity), () =>
                    from p in Database.Query<ProjectEntity>()
                    select new
                    {
                        Entity = p,
                        p.Id,
                        p.Name,
                    });

                new Graph<ProjectEntity>.Construct(ProjectOperation.Create)
                {
                    Construct = (_) => new ProjectEntity { Members = { UserEntity.Current.ToLite() } }
                }.Register();

                new Graph<ProjectEntity>.Execute(ProjectOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (p, _) => { }
                }.Register();
            }
        }
    }
}
