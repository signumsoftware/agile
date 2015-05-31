using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class TagEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotNullable]
        ColorEntity color;
        [NotNullValidator]
        public ColorEntity Color
        {
            get { return color; }
            set { Set(ref color, value); }
        }

        [NotNullable]
        Lite<ProjectEntity> project;
        [NotNullValidator]
        public Lite<ProjectEntity> Project
        {
            get { return project; }
            set { Set(ref project, value); }
        }

        static Expression<Func<TagEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class TagOperation
    {
        public static readonly ExecuteSymbol<TagEntity> Save = OperationSymbol.Execute<TagEntity>();
        public static readonly ConstructSymbol<TagEntity>.From<ProjectEntity> CreteTagFromProject = OperationSymbol.Construct<TagEntity>.From<ProjectEntity>();
    }
}
