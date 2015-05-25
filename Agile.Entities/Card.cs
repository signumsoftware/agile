using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Files;
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
        string title;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Title
        {
            get { return title; }
            set { SetToStr(ref title, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
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

        static Expression<Func<CardEntity, string>> ToStringExpression = e => e.title;
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


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class CardTransitionEntity : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        Lite<UserEntity> user;
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [NotNullable]
        Lite<CardEntity> card;
        [NotNullValidator]
        public Lite<CardEntity> Card
        {
            get { return card; }
            set { Set(ref card, value); }
        }

        [NotNullable]
        Lite<ListEntity> from;
        [NotNullValidator]
        public Lite<ListEntity> From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        decimal fromOrder;
        public decimal FromOrder
        {
            get { return fromOrder; }
            set { Set(ref fromOrder, value); }
        }

        [NotNullable]
        Lite<ListEntity> to;
        [NotNullValidator]
        public Lite<ListEntity> To
        {
            get { return to; }
            set { Set(ref to, value); }
        }

        decimal toOrder;
        public decimal ToOrder
        {
            get { return toOrder; }
            set { Set(ref toOrder, value); }
        }
    }

    public enum CardMessage
    {
        [Description("Create card...")]
        CreateCard,
        Create
    }
}
