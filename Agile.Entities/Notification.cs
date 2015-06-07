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
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SubscriptionEntity : Entity
    {
        [NotNullable]
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [ImplementedBy(typeof(ListEntity), typeof(BoardEntity), typeof(CardEntity))]
        Lite<ISubscriptionTarget> target;
        [NotNullValidator]
        public Lite<ISubscriptionTarget> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }
    }

    public interface ISubscriptionTarget : IEntity
    {
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class NotificationEntity : Entity
    {
        DateTime creationDate;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value); }
        }

        [NotNullable]
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        Lite<CardEntity> card;
        [NotNullValidator]
        public Lite<CardEntity> Card
        {
            get { return card; }
            set { Set(ref card, value); }
        }

        NotificationType type;
        public NotificationType Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string message;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value); }
        }

        DateTime? readOn;
        public DateTime? ReadOn
        {
            get { return readOn; }
            set { Set(ref readOn, value); }
        }

        bool emailSent;
        public bool EmailSent
        {
            get { return emailSent; }
            set { Set(ref emailSent, value); }
        }

        Lite<UserEntity> triggeredBy;
        public Lite<UserEntity> TriggeredBy
        {
            get { return triggeredBy; }
            set { Set(ref triggeredBy, value); }
        }

        static Expression<Func<NotificationEntity, string>> ToStringExpression = e => e.TriggeredBy + " " + e.Message;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Flags]
    public enum NotificationType
    {
        Created,
        DueDateModified, 
        Modified, 
        Moved, 
        Commented,
        Attached, 
        Archived, 
        Unarchived, 
    }

    public static class NotificationAttendedIn
    {
        public static readonly NotificationType[] Board = new[]
        {
            NotificationType.Created, 
            NotificationType.DueDateModified, 
            NotificationType.Moved, 
            NotificationType.Unarchived
        };

        public static readonly NotificationType[] Card = new[]
        {
            NotificationType.Archived, 
            NotificationType.Unarchived, 
            NotificationType.Created, 
            NotificationType.DueDateModified,
            NotificationType.Modified,
        };

        public static readonly NotificationType[] CardHistory = new[]
        {
            NotificationType.Commented, 
            NotificationType.Attached, 
            NotificationType.Moved, 
        };
    }

    public static class SubscriptionOperation
    {
        public static readonly ExecuteSymbol<ISubscriptionTarget> Subscribe = OperationSymbol.Execute<ISubscriptionTarget>();
        public static readonly ExecuteSymbol<ISubscriptionTarget> Unsubscribe = OperationSymbol.Execute<ISubscriptionTarget>();
    }

    public enum NotificationMessage
    {
        [Description("Created in '{0}'")]
        CreatedIn0,
        [Description("Archived from '{0}'")]
        ArchivedFrom0,
        [Description("Unarchived to '{0}'")]
        UnarchivedTo0,
        [Description("Moved from {0} to '{1}'")]
        MovedFrom0To1,
        [Description("Moved in '{0}' from position {1} to {2}")]
        MovedIn0FromPosition1To2,
        [Description("Commented:")]
        Commented,
        [Description("Attached {0} '{1}'")]
        AttachedA01,

        [Description("Due Date changed from '{0}' to '{1}'")]
        ChangedDueDateFrom0To1,
        [Description("Due Date set to '{0}'")]
        DueDateSetTo0,
        [Description("Due Date '{0}' removed")]
        DueDate0Removed,
        [Description("Modified {0}")]
        Modified0,
        AlreadySubscribed,
        NotSubscribed
    }

    [Serializable]
    public class NotificationUserMixin : MixinEntity
    {
        NotificationUserMixin(Entity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        int? sendNotificationDigestEvery;
        [Unit("mins")]
        public int? SendNotificationDigestEvery
        {
            get { return sendNotificationDigestEvery; }
            set { Set(ref sendNotificationDigestEvery, value); }
        }
    }
}
