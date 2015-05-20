using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class ProjectEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        int removed;
        public int Removed
        {
            get { return removed; }
            set { Set(ref removed, value); }
        }

        [NotNullable, PreserveOrder]
        MList<Lite<UserEntity>> members = new MList<Lite<UserEntity>>();
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<UserEntity>> Members
        {
            get { return members; }
            set { Set(ref members, value); }
        }

        static Expression<Func<ProjectEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class ProjectOperation
    {
        public static readonly ExecuteSymbol<ProjectEntity> Save = OperationSymbol.Execute<ProjectEntity>();
    }
}
