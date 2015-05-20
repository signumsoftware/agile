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
    public class BoardEntity : Entity, ISubscriptionTarget
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        bool archived;
        public bool Archived
        {
            get { return archived; }
            set { Set(ref archived, value); }
        }

        [NotNullable]
        Lite<ProjectEntity> project;
        [NotNullValidator]
        public Lite<ProjectEntity> Project
        {
            get { return project; }
            set { Set(ref project, value); }
        }

        static Expression<Func<BoardEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class BoardOperation
    {
        public static readonly ExecuteSymbol<BoardEntity> Save = OperationSymbol.Execute<BoardEntity>();
        public static readonly ExecuteSymbol<BoardEntity> Archive = OperationSymbol.Execute<BoardEntity>();
        public static readonly ConstructSymbol<BoardEntity>.From<ProjectEntity> CreateBoardFromProject = OperationSymbol.Construct<BoardEntity>.From<ProjectEntity>();
    }
}
