using Signum.Entities;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class CardEntity : Entity, ISubscriptionTarget, INotificationTarget 
    {
        [NotNullable]
        Lite<ListEntity> list;
        [NotNullValidator]
        public Lite<ListEntity> List
        {
            get { return list; }
            set { Set(ref list, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
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
        MList<TagEntity> tags = new MList<TagEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<TagEntity> Tags
        {
            get { return tags; }
            set { Set(ref tags, value); }
        }

        [NotNullable, PreserveOrder]
        MList<AttachmentEntity> attachment = new MList<AttachmentEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<AttachmentEntity> Attachment
        {
            get { return attachment; }
            set { Set(ref attachment, value); }
        }

        static Expression<Func<CardEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class CardOperation
    {
        public static readonly ExecuteSymbol<CardEntity> Save = OperationSymbol.Execute<CardEntity>();
        public static readonly ExecuteSymbol<CardEntity> Archive = OperationSymbol.Execute<CardEntity>();
        public static readonly ExecuteSymbol<CardEntity> Move = OperationSymbol.Execute<CardEntity>();
        public static readonly ConstructSymbol<CardEntity>.From<ListEntity> CreateCardFromBoard = OperationSymbol.Construct<CardEntity>.From<ListEntity>();
    }

    public class CardMoveOptions
    {
        public Lite<ListEntity> List;
        public Lite<CardEntity> Next;
        public Lite<CardEntity> Previous;
    }

    public static class AttachmentFileType
    {
        public static readonly FileTypeSymbol Attachment = new FileTypeSymbol();
    }

    [Serializable]
    public class AttachmentEntity : EmbeddedEntity
    {
        [NotNullable]
        FilePathEntity file;
        [NotNullValidator]
        public FilePathEntity File
        {
            get { return file; }
            set { Set(ref file, value); }
        }

    }
}
