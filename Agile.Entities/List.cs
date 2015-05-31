using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ListEntity : Entity, ISubscriptionTarget
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotNullable]
        Lite<ProjectEntity> project;
        [NotNullValidator]
        public Lite<ProjectEntity> Project
        {
            get { return project; }
            set { Set(ref project, value); }
        }

        static Expression<Func<ListEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class ListOperation
    {
        public static readonly ExecuteSymbol<ListEntity> Save = OperationSymbol.Execute<ListEntity>();
        public static readonly ConstructSymbol<ListEntity>.From<ProjectEntity> CreateListFromProject = OperationSymbol.Construct<ListEntity>.From<ProjectEntity>();

    }
}
