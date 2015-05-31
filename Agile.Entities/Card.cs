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
        Lite<ListEntity> list;
        public Lite<ListEntity> List
        {
            get { return list; }
            set { Set(ref list, value); }
        }

        [NotNullable]
        Lite<ProjectEntity> project;
        [NotNullValidator]
        public Lite<ProjectEntity> Project
        {
            get { return project; }
            set { Set(ref project, value); }
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

        ArchivedState state;
        public ArchivedState State
        {
            get { return state; }
            set { Set(ref state, value); }
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

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => List))
            {
                if (List == null && State == ArchivedState.Alive)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
                if (List != null && state == ArchivedState.Archived)
                    return ValidationMessage._0IsSet.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }


    public enum ArchivedState
    {
        Alive,
        Archived,
    }

    public static class CardOperation
    {
        public static readonly ExecuteSymbol<CardEntity> Save = OperationSymbol.Execute<CardEntity>();
        public static readonly ExecuteSymbol<CardEntity> Archive = OperationSymbol.Execute<CardEntity>();
        public static readonly ExecuteSymbol<CardEntity> Unarchive = OperationSymbol.Execute<CardEntity>();
        public static readonly DeleteSymbol<CardEntity> Delete = OperationSymbol.Delete<CardEntity>();
        public static readonly ExecuteSymbol<CardEntity> Move = OperationSymbol.Execute<CardEntity>();
        public static readonly ConstructSymbol<CardEntity>.From<ListEntity> CreateCardFromList = OperationSymbol.Construct<CardEntity>.From<ListEntity>();
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

        [NotNullable]
        Lite<UserEntity> user;
        [NotNullValidator]
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

        Lite<ListEntity> from;
        public Lite<ListEntity> From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        int? fromPosition;
        public int? FromPosition
        {
            get { return fromPosition; }
            set { Set(ref fromPosition, value); }
        }

        ArchivedState fromState;
        public ArchivedState FromState
        {
            get { return fromState; }
            set { Set(ref fromState, value); }
        }

        Lite<ListEntity> to;
        public Lite<ListEntity> To
        {
            get { return to; }
            set { Set(ref to, value); }
        }

        int? toPosition;
        public int? ToPosition
        {
            get { return toPosition; }
            set { Set(ref toPosition, value); }
        }

        ArchivedState toState;
        public ArchivedState ToState
        {
            get { return toState; }
            set { Set(ref toState, value); }
        }
    }

    public enum CardMessage
    {
        [Description("Create card...")]
        CreateCard,
        Create,
        Comment
    }
}
