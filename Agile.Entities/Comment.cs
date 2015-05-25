using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class CommentEntity : Entity
    {
        [NotNullable]
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        [NotNullable]
        Lite<CardEntity> card;
        [NotNullValidator]
        public Lite<CardEntity> Card
        {
            get { return card; }
            set { Set(ref card, value); }
        }
        
        [NotNullable, SqlDbType( Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls=false, Min = 3, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value); }
        }

        static Expression<Func<CommentEntity, string>> ToStringExpression = e => "Comment ( " + e.Id  + ")";
        public override string ToString()
        {
            return "Comment ( " + IdOrNull + ")";
        }
    }

    public static class CommentOperation
    {
        public static readonly ExecuteSymbol<CommentEntity> Save = OperationSymbol.Execute<CommentEntity>();
        public static readonly ConstructSymbol<CommentEntity>.From<CardEntity> CreateCommentFromCard = OperationSymbol.Construct<CommentEntity>.From<CardEntity>();
    }

    public enum HistoryMessage
    {
        commented,
        moved,
        attached,
    }
}
