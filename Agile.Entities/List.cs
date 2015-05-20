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

        bool archived;
        public bool Archived
        {
            get { return archived; }
            set { Set(ref archived, value); }
        }

        decimal order;
        public decimal Order
        {
            get { return order; }
            set { Set(ref order, value); }
        }

        [NotNullable]
        Lite<BoardEntity> board;
        [NotNullValidator]
        public Lite<BoardEntity> Board
        {
            get { return board; }
            set { Set(ref board, value); }
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
        public static readonly ExecuteSymbol<ListEntity> Move = OperationSymbol.Execute<ListEntity>();
        public static readonly ExecuteSymbol<ListEntity> Archive = OperationSymbol.Execute<ListEntity>();
        public static readonly ConstructSymbol<ListEntity>.From<BoardEntity> CreateListFromBoard = OperationSymbol.Construct<ListEntity>.From<BoardEntity>();

    }

    public class ListMoveOptions
    {
        public Lite<BoardEntity> Board;
        public Lite<ListEntity> Next;
        public Lite<ListEntity> Previous;
    }
}
