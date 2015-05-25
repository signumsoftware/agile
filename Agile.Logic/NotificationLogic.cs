using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Agile.Entities;
using Signum.Entities.Authorization;
using Signum.Engine.Mailing;

namespace Agile.Logic
{
    public static class NotificationLogic
    {
        static Expression<Func<UserEntity, IQueryable<NotificationEntity>>> NotificationsExpression = 
            u => Database.Query<NotificationEntity>().Where(n => n.User.RefersTo(u));
        public static IQueryable<NotificationEntity> Notifications(this UserEntity e)
        {
            return NotificationsExpression.Evaluate(e);
        }

        static Expression<Func<ISubscriptionTarget, IQueryable<NotificationEntity>>> NotificationsTargetExpression =
            target => Database.Query<NotificationEntity>().Where(n => n.Target.RefersTo(target));
        [ExpressionField("NotificationsTargetExpression")]
        public static IQueryable<NotificationEntity> Notifications(this ISubscriptionTarget target)
        {
            return NotificationsTargetExpression.Evaluate(target);
        }
    
        static Expression<Func<UserEntity, IQueryable<SubscriptionEntity>>> SubscriptionsExpression = 
            u => Database.Query<SubscriptionEntity>().Where(s => s.User.RefersTo(u));
        public static IQueryable<SubscriptionEntity> Subscriptions(this UserEntity e)
        {
            return SubscriptionsExpression.Evaluate(e);
        }

        static Expression<Func<ISubscriptionTarget, IQueryable<SubscriptionEntity>>> SubscriptionsTargetExpression =
              target => Database.Query<SubscriptionEntity>().Where(s => s.Target.RefersTo(target));
        [ExpressionField("SubscriptionsTargetExpression")]
        public static IQueryable<SubscriptionEntity> Subscriptions(this ISubscriptionTarget target)
        {
            return SubscriptionsTargetExpression.Evaluate(target);
        }

        static Expression<Func<ISubscriptionTarget, SubscriptionMethod?>> SubscriptionMethodExpression =
            c => c.Subscriptions().Where(s => s.User.RefersTo(UserEntity.Current)).Select(a => (SubscriptionMethod?)a.Method).SingleOrDefault(); 
        public static SubscriptionMethod? SubscriptionMethod(this ISubscriptionTarget c)
        {
            return SubscriptionMethodExpression.Evaluate(c);
        }
    
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NotificationEntity>();
                sb.Include<SubscriptionEntity>();
                sb.AddUniqueIndex((NotificationEntity n) => new { n.User, n.Target });
        
                dqm.RegisterQuery(typeof(NotificationEntity), () =>
                    from n in Database.Query<NotificationEntity>()
                    select new
                    {
                        Entity = n,
                        n.Id,
                        n.CreationDate,
                        n.User,
                        n.Target,
                        n.Attended,
                    });
        
                dqm.RegisterQuery(typeof(SubscriptionEntity), () =>
                    from s in Database.Query<SubscriptionEntity>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.User,
                        s.Target,
                    });
        
                dqm.RegisterExpression((UserEntity u) => u.Notifications(), () => typeof(NotificationEntity).NicePluralName());
                dqm.RegisterExpression((UserEntity u) => u.Subscriptions(), () => typeof(SubscriptionEntity).NicePluralName());

                dqm.RegisterExpression((ISubscriptionTarget u) => u.Notifications(), () => typeof(NotificationEntity).NicePluralName());
                dqm.RegisterExpression((ISubscriptionTarget u) => u.Subscriptions(), () => typeof(SubscriptionEntity).NicePluralName());
                dqm.RegisterExpression((ISubscriptionTarget u) => u.SubscriptionMethod(), () => typeof(SubscriptionMethod).NiceName());
            }
        }

        public static void Notify(CardEntity card, NotificationType notification)
        {
            var subscriptions = card.Subscriptions().ToList();
            subscriptions.AddRange(card.List.InDB().SelectMany(l => l.Subscriptions()));
            subscriptions.AddRange(card.List.InDB().SelectMany(l => l.Board.Entity.Subscriptions()));

            foreach (var gr in subscriptions.GroupBy(a => a.User))
            {
                new NotificationEntity
                {
                    Target = card.ToLite(),
                    User = gr.Key,
                }.Save();

                if (gr.Any(a => a.Method == Entities.SubscriptionMethod.Email))
                {
                    new CardNotificationEmail(card)
                    {
                        User = gr.Key.Retrieve(),
                        NotificationType = notification,
                    }.SendMail();
                }
            }
        }

        public class CardNotificationEmail : SystemEmail<CardEntity>
        {
            public CardNotificationEmail(CardEntity card) : base(card)
            {
            }

            public NotificationType NotificationType { get; set; }
            public UserEntity User { get; set; }

            public override List<EmailOwnerRecipientData> GetRecipients()
            {
                return SendTo(User.EmailOwnerData);
            }
        }
    }
}
