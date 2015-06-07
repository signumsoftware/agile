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
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;
using Signum.Engine.Scheduler;

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
            target => Database.Query<NotificationEntity>().Where(n => n.Card.RefersTo(target));
        [ExpressionField("NotificationsTargetExpression")]
        public static IQueryable<NotificationEntity> Notifications(this ISubscriptionTarget target)
        {
            return NotificationsTargetExpression.Evaluate(target);
        }

        static Expression<Func<ISubscriptionTarget, IQueryable<NotificationEntity>>> MyUnreadNotificationsExpression =
            target => target.Notifications().Where(a => a.User.RefersTo(UserEntity.Current) && a.ReadOn == null);
        public static IQueryable<NotificationEntity> MyUnreadNotifications(this ISubscriptionTarget target)
        {
            return MyUnreadNotificationsExpression.Evaluate(target);
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

        static Expression<Func<ISubscriptionTarget, bool>> HasSubscriptionsExpression =
            st => st.Subscriptions().Any(a => a.User.RefersTo(UserEntity.Current));
        public static bool HasSubscriptions(this ISubscriptionTarget st)
        {
            return HasSubscriptionsExpression.Evaluate(st);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NotificationEntity>();
                sb.Include<SubscriptionEntity>();
                sb.AddUniqueIndex((SubscriptionEntity n) => new { n.User, n.Target });

                dqm.RegisterQuery(typeof(NotificationEntity), () =>
                    from n in Database.Query<NotificationEntity>()
                    select new
                    {
                        Entity = n,
                        n.Id,
                        n.CreationDate,
                        n.User,
                        n.ReadOn,
                        n.EmailSent,
                        n.Card,
                        n.Type,
                        n.TriggeredBy,
                        n.Message,
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

                new Graph<ISubscriptionTarget>.Execute(SubscriptionOperation.Subscribe)
                {
                    CanExecute = a => a.Subscriptions().Any(s => s.User.RefersTo(UserEntity.Current)) ? NotificationMessage.AlreadySubscribed.NiceToString() : null,
                    Execute = (a, _) =>
                    {
                        new SubscriptionEntity
                        {
                            Target = a.ToLite(),
                            User = UserEntity.Current.ToLite(),
                        }.Save();
                    },
                }.Register();

                new Graph<ISubscriptionTarget>.Execute(SubscriptionOperation.Unsubscribe)
                {
                    CanExecute = a => !a.Subscriptions().Any(s => s.User.RefersTo(UserEntity.Current)) ? NotificationMessage.NotSubscribed.NiceToString() : null,
                    Execute = (a, _) =>
                    {
                        a.Subscriptions().Where(s => s.User.RefersTo(UserEntity.Current)).UnsafeDelete();
                    },
                }.Register();

                SystemEmailLogic.RegisterSystemEmail<NotificationDigestEmail>(() => new EmailTemplateEntity
                {
                    Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEntity(culture)
                    {
                        Subject = "@[m:Subject]",
                        Text = @"Last Notifications in <a href=""@[g:UrlLeft]"">Agile</a>:
<ul><!--@foreach[Entity.Notifications.Element.Card]-->
	<li><a href=""@[g:UrlLeft]/View/Card/@[Entity.Notifications.Element.Card.Id]"">@[Entity.Notifications.Element.Card]</a>
	<ul><!--@foreach[Entity.Notifications.Element]-->
		<li><strong>@[Entity.Notifications.Element.TriggeredBy]</strong> @[Entity.Notifications.Element.Message]</li>
		<!--@endforeach-->
	</ul>
	</li>
	<!--@endforeach-->
</ul>",

                    }).ToMList()
                });

                SimpleTaskLogic.Register(AgileTasks.SendNotificationDigests, SendNotificationDigests);
            }
        }

        public static void Notify(CardEntity card, NotificationType type, Func<string> message, DateTime? overrideCreationDate = null)
        {
            var users = GetUsersToNotify(card, type);

            foreach (var u in users)
            {
                new NotificationEntity
                {
                    CreationDate = overrideCreationDate ??  TimeZoneManager.Now,
                    Card = card.ToLite(),
                    User = u,
                    Type = type,
                    Message = CultureInfoUtils.ChangeBothCultures(u.Retrieve().CultureInfo.Try(c => c.Name)).Using(_ => message()),
                    TriggeredBy = UserEntity.Current.ToLite(),
                }.Save();
            }
        }

        private static HashSet<Lite<UserEntity>> GetUsersToNotify(CardEntity card, NotificationType notification)
        {
            var users = card.Subscriptions().Select(a => a.User).ToHashSet();

            if (notification == NotificationType.Commented)
                users.Add(card.CreatedBy);

            users.AddRange(card.Members);

            if (card.List != null)
            {
                if (notification == NotificationType.Created || notification == NotificationType.Moved || notification == NotificationType.Unarchived)
                {
                    users.AddRange(card.List.InDB().SelectMany(l => l.Subscriptions()).Select(a => a.User));
                }

                users.AddRange(Database.Query<BoardEntity>().Where(b => b.Lists.Contains(card.List)).SelectMany(b => b.Subscriptions()).Select(a => a.User));
            }

            users.Remove(UserEntity.Current.ToLite());

            return users;
        }

        public static Lite<IEntity> SendNotificationDigests()
        {
            var notifications = (from n in Database.Query<NotificationEntity>()
                                 where n.ReadOn == null && !n.EmailSent
                                 where n.User.Entity.Mixin<NotificationUserMixin>().SendNotificationDigestEvery.HasValue
                                 select new { Notification = n, User = n.User.Entity }).ToList();

            foreach (var gr in notifications.GroupBy(a => a.User, a => a.Notification))
            {
                if (gr.Any(n => n.CreationDate.AddMinutes(gr.Key.Mixin<NotificationUserMixin>().SendNotificationDigestEvery.Value) < TimeZoneManager.Now))
                {
                    using (Transaction tr = new Transaction())
                    {
                        string subject = gr.GroupBy(a => a.Type).CommaAnd(nots => nots.Count() + " " + nots.Key.NiceToString());

                        new NotificationDigestEmail(gr.Key) { Subject = subject }.SendMailAsync();

                        gr.Key.Notifications()
                            .Where(n => n.ReadOn == null && !n.EmailSent)
                            .UnsafeUpdate()
                            .Set(n => n.EmailSent, n => true)
                            .Set(n => n.ReadOn, n => n.Type == NotificationType.Archived ? TimeZoneManager.Now : (DateTime?)null)
                            .Execute();

                        tr.Commit();
                    }
                }
            }

            return null;
        }

        public class NotificationDigestEmail : SystemEmail<UserEntity>
        {
            public string Subject { get; set; }

            public NotificationDigestEmail(UserEntity user)
                : base(user)
            {
            }

            public override List<EmailOwnerRecipientData> GetRecipients()
            {
                return SendTo(this.Entity.EmailOwnerData);
            }

            public override List<Filter> GetFilters(QueryDescription qd)
            {
                var list = base.GetFilters(qd);

                list.Add(new Filter(QueryUtils.Parse("Entity.Notifications.Element.ReadOn", qd, SubTokensOptions.CanElement), FilterOperation.EqualTo, null));
                list.Add(new Filter(QueryUtils.Parse("Entity.Notifications.Element.EmailSent", qd, SubTokensOptions.CanElement), FilterOperation.EqualTo, false));

                return list;
            }
        }
    }
}
