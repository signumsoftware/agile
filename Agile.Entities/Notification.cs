using Signum.Entities;
using Signum.Entities.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
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

        SubscriptionMethod method;
        public SubscriptionMethod Method
        {
            get { return method; }
            set { Set(ref method, value); }
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

    public enum SubscriptionMethod
    {
        Internal,
        Email,
    }

    public interface ISubscriptionTarget : IEntity
    {
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class NotificationEntity : Entity
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

        [ImplementedBy(typeof(CardEntity))]
        Lite<ISubscriptionTarget> target;
        [NotNullValidator]
        public Lite<ISubscriptionTarget> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        DateTime? attended;
        public DateTime? Attended
        {
            get { return attended; }
            set { Set(ref attended, value); }
        }
    }

    public interface INotificationTarget : IEntity
    {
    }


    public enum NotificationType
    {
        Created,
        Modified,
        Moved,
        Commented,
        Deleted,
    }
}
