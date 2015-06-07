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
using Signum.Entities.Files;
using Signum.Engine.Files;
using System.IO;
using Signum.Entities.Authorization;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;

namespace Agile.Logic
{
    public static class CardLogic
    {
        static Expression<Func<ListEntity, IQueryable<CardEntity>>> CardsExpression =
            l => Database.Query<CardEntity>().Where(c => c.List.RefersTo(l));
        public static IQueryable<CardEntity> Cards(this ListEntity e)
        {
            return CardsExpression.Evaluate(e);
        }

        static Expression<Func<CardEntity, IQueryable<CardTransitionEntity>>> CardTransitionsExpression =
            c => Database.Query<CardTransitionEntity>().Where(ct => ct.Card.RefersTo(c));
        public static IQueryable<CardTransitionEntity> CardTransitions(this CardEntity c)
        {
            return CardTransitionsExpression.Evaluate(c);
        }

        static Expression<Func<CardEntity, CardInfo>> ToCardInfoExpression =
            c => new CardInfo
            {
                Lite = c.ToLite(),
                Title = c.Title,
                DueDate = c.DueDate,
                Attachments = c.Attachments().Count(),
                HasDescription = c.Description.HasText(),
                Comments = c.Comments().Count(),
                Tags = c.Tags.ToList(),
                Members = c.Members.ToList(),
                FirstImage = c.Attachments().OrderBy(a => a.CreationDate).Where(a => a.Type == AttachmentType.Image).Select(a => a.File).FirstOrDefault(),
                HasSubscription = c.HasSubscriptions(),
                Notifications = c.MyUnreadNotifications().Select(a => a.Type).ToList(),
            };
        public static CardInfo ToCardInfo(this CardEntity c)
        {
            return ToCardInfoExpression.Evaluate(c);
        }

        static Expression<Func<CardTransitionEntity, CardTransitionInfo>> ToCardTransitionInfoExpression =
            ct => new CardTransitionInfo
            {
                CreationDate = ct.CreationDate,
                Entity = ct.ToLite(),
                From = ct.From,
                To = ct.To,
                FromPositon = ct.FromPosition,
                ToPosition = ct.ToPosition,
                User = ct.User
            };
        public static CardTransitionInfo ToCardTransitionInfo(this CardTransitionEntity ct)
        {
            return ToCardTransitionInfoExpression.Evaluate(ct);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CardEntity>();
                sb.AddUniqueIndex((CardEntity e) => new { e.List, e.Order }, a => a.List != null);

                sb.Include<CardTransitionEntity>();


                dqm.RegisterQuery(typeof(CardEntity), () =>
                    from c in Database.Query<CardEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Title,
                        c.State,
                        c.Order,
                        c.List,
                    });


                dqm.RegisterQuery(typeof(CardTransitionEntity), () =>
                    from c in Database.Query<CardTransitionEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Card,
                        c.User,
                        c.From,
                        FromOrder = c.FromPosition,
                        c.To,
                        ToOrder = c.ToPosition,
                    });


                dqm.RegisterExpression((ListEntity l) => l.Cards(), () => typeof(CardEntity).NicePluralName());

                CardGraph.Register();
            }
        }


    }

    public class CardGraph : Graph<CardEntity, ArchivedState>
    {
        public static void Register()
        {
            GetState = a => a.State;

            new Execute(CardOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Alive },
                Execute = (c, _) =>
                {
                    if (c.IsNew)
                    {
                        c.CreatedBy = UserEntity.Current.ToLite();
                        c.Order = (c.List.InDB().SelectMany(a => a.Cards()).Max(a => (decimal?)a.Order) ?? -1) + 1;
                        c.Save();
                        NotificationLogic.Notify(c, NotificationType.Created, () => NotificationMessage.CreatedIn0.NiceToString(c.List));

                        if (c.DueDate.HasValue)
                        {
                            NotificationLogic.Notify(c, NotificationType.DueDateModified, () => NotificationMessage.DueDateSetTo0.NiceToString(c.DueDate.Value.SmartDatePattern()));
                        }

                        if (c.Tags.Any() || c.Members.Any() || c.Description.HasText())
                        {
                            NotificationLogic.Notify(c, NotificationType.Modified, () => NotificationMessage.Modified0.NiceToString(new string[] { 
                                c.Tags.Any() ? ReflectionTools.GetPropertyInfo(()=>c.Tags).NiceName() : null,
                                c.Members.Any() ? ReflectionTools.GetPropertyInfo(()=>c.Members).NiceName() : null,
                                c.Description.HasText() ? ReflectionTools.GetPropertyInfo(()=>c.Description).NiceName() : null,
                            }.NotNull().CommaAnd()));
                        }

                    }
                    else
                    {
                        var old = c.InDBEntity(card => new { card.DueDate, card.Description, card.Title });

                        bool tagsModified = c.Tags.Modified == ModifiedState.SelfModified;
                        bool membersModified = c.Members.Modified == ModifiedState.SelfModified;

                        c.Save();

                        if (old.DueDate != c.DueDate)
                        {
                            NotificationLogic.Notify(c, NotificationType.DueDateModified, () =>
                                old.DueDate == null ? NotificationMessage.DueDateSetTo0.NiceToString(c.DueDate.Value.SmartDatePattern()) :
                                c.DueDate == null ? NotificationMessage.DueDate0Removed.NiceToString(old.DueDate.Value.SmartDatePattern()) :
                                NotificationMessage.ChangedDueDateFrom0To1.NiceToString(old.DueDate.Value.SmartDatePattern(), c.DueDate.Value.SmartDatePattern()));
                        }

                        if (tagsModified || membersModified || c.Description != old.Description || c.Title != old.Title)
                        {
                            NotificationLogic.Notify(c, NotificationType.Modified, () => NotificationMessage.Modified0.NiceToString(new string[] { 
                                tagsModified ? ReflectionTools.GetPropertyInfo(()=>c.Tags).NiceName() : null,
                                membersModified ? ReflectionTools.GetPropertyInfo(()=>c.Members).NiceName() : null,
                                c.Description != old.Description ? ReflectionTools.GetPropertyInfo(()=>c.Description).NiceName() : null,
                                c.Title != old.Title ? ReflectionTools.GetPropertyInfo(()=>c.Title).NiceName() : null
                            }.NotNull().CommaAnd()));
                        }
                    }
                }
            }.Register();


            new Execute(CardOperation.Archive)
            {
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Archived },
                Execute = (c, _) =>
                {
                    var oldList = c.List;
                    var oldPosition = Position(c);

                    c.State = ArchivedState.Archived;
                    c.List = null;

                    new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = oldList,
                        FromPosition = oldPosition,
                        FromState = ArchivedState.Alive,
                        To = null,
                        ToPosition = null,
                        ToState = ArchivedState.Archived,
                    }.Save();

                    NotificationLogic.Notify(c, NotificationType.Archived, () => NotificationMessage.ArchivedFrom0.NiceToString(oldList));
                }
            }.Register();

            new Execute(CardOperation.Unarchive)
            {
                FromStates = { ArchivedState.Archived },
                ToStates = { ArchivedState.Alive },
                Execute = (c, args) =>
                {
                    c.State = ArchivedState.Alive;
                    c.List = args.GetArg<Lite<ListEntity>>();
                    c.Order = (c.List.InDB().SelectMany(a => a.Cards()).Max(a => (decimal?)a.Order) ?? -1) + 1;

                    new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = null,
                        FromPosition = null,
                        FromState = ArchivedState.Archived,
                        To = c.List,
                        ToPosition = Position(c),
                        ToState = ArchivedState.Alive,
                    }.Save();

                    NotificationLogic.Notify(c, NotificationType.Unarchived, () => NotificationMessage.UnarchivedTo0.NiceToString(c.List));
                }
            }.Register();

            new Graph<CardEntity, ArchivedState>.Delete(CardOperation.Delete)
            {
                FromStates = { ArchivedState.Archived },
                Delete = (c, _) => { c.Delete(); }
            }.Register();

            Schema.Current.EntityEvents<CardEntity>().PreUnsafeDelete += query => query.SelectMany(q => q.CardTransitions()).UnsafeDelete();

            new Execute(CardOperation.Move)
            {
                FromStates = { ArchivedState.Alive },
                ToStates = { ArchivedState.Alive },
                Execute = (c, args) =>
                {
                    var oldList = c.List;
                    var oldPosition = Position(c);

                    var ops = args.GetArg<CardMoveOptions>();

                    c.List = ops.List;

                    SetOrder(c, ops);

                    var trans = new CardTransitionEntity
                    {
                        Card = c.ToLite(),
                        User = UserEntity.Current.ToLite(),
                        From = oldList,
                        FromPosition = oldPosition,
                        To = c.List,
                        ToPosition = Position(c)
                    }.Save();

                    NotificationLogic.Notify(c, NotificationType.Moved, () =>
                        oldList.Is(c.List) ? NotificationMessage.MovedIn0FromPosition1To2.NiceToString(oldList, oldPosition, trans.ToPosition.Value) :
                        NotificationMessage.MovedFrom0To1.NiceToString(oldList, trans.To + " (" + trans.ToPosition.Value + ")"));
                }
            }.Register();

            new ConstructFrom<ListEntity>(CardOperation.CreateCardFromList)
            {
                ToStates = { ArchivedState.Alive },
                Construct = (l, args) => new CardEntity
                {
                    List = l.ToLite(),
                    Project = l.Project,
                    Title = args.TryGetArgC<string>(),
                }
            }.Register();

        }

        private static int Position(CardEntity c)
        {
            return c.List.InDB(l => l.Cards().Count(a => a.Order < c.Order));
        }

        private static void SetOrder(CardEntity c, CardMoveOptions ops)
        {
            if (ops.Previous == null && ops.Next == null)
                c.Order = 0;
            else if (ops.Previous == null)
                c.Order = ops.Next.InDB(a => a.Order) - 1;
            else if (ops.Next == null)
                c.Order = ops.Previous.InDB(a => a.Order) + 1;
            else
            {
                c.Order = ops.Next.InDB(a => a.Order);

                ops.List.InDB()
                    .SelectMany(l => l.Cards())
                    .Where(a => a.Order >= c.Order) // && a != c
                    .UnsafeUpdate()
                    .Set(a => a.Order, a => a.Order + 1)
                    .Execute();
            }
        }
    }

    public class CardInfo
    {
        public Lite<CardEntity> Lite;
        public string Title;
        public DateTime? DueDate;
        public bool HasDescription;
        public int Comments;
        public int Attachments;
        public FilePathEntity FirstImage;
        public List<TagEntity> Tags;
        public List<Lite<UserEntity>> Members;
        public bool HasSubscription;
        public List<NotificationType> Notifications;

    }

    public class CardTransitionInfo : HistoryInfo
    {
        public Lite<ListEntity> From;
        public Lite<ListEntity> To;

        public int? ToPosition;
        public int? FromPositon;

        public override string ToString()
        {
            return HistoryMessage.moved.NiceToString();
        }

        public override NotificationType NotificationType
        {
            get { return Entities.NotificationType.Moved; }
        }
    }
}
