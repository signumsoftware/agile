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

namespace Agile.Logic
{
    public static class CommentLogic
    {
        static Expression<Func<CardEntity, IQueryable<CommentEntity>>> CommentsExpression = 
            c => Database.Query<CommentEntity>().Where(c2 => c2.Card.RefersTo(c));
        public static IQueryable<CommentEntity> Comments(this CardEntity e)
        {
            return CommentsExpression.Evaluate(e);
        }

        static Expression<Func<CommentEntity, CommentInfo>> ToCommentInfoExpression =
            c => new CommentInfo
            {
                 Entity = c.ToLite(),
                 Text = c.Text,
                 CreationDate = c.CreationDate,
                 User = c.User,
            }; 
        public static CommentInfo ToCommentInfo(this CommentEntity c)
        {
            return ToCommentInfoExpression.Evaluate(c);
        }
    
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CommentEntity>();
        
                dqm.RegisterQuery(typeof(CommentEntity), () =>
                    from c in Database.Query<CommentEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Card,
                        c.Text,
                    });
        
                dqm.RegisterExpression((CardEntity c) => c.Comments(), () => typeof(CommentEntity).NicePluralName());
        
                new Graph<CommentEntity>.Execute(CommentOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => 
                    {
                        if (c.IsNew)
                        {
                            NotificationLogic.Notify(c.Card.Retrieve(), NotificationType.Commented,
                                () => NotificationMessage.Commented.NiceToString() + c.Text.Etc(200),
                                overrideCreationDate: c.CreationDate);
                        }
                    }
                }.Register();

                new Graph<CommentEntity>.Delete(CommentOperation.Delete)
                {
                    Delete = (c, _) => c.Delete()
                }.Register();

                Schema.Current.EntityEvents<CardEntity>().PreUnsafeDelete += query => query.SelectMany(q => q.Attachments()).UnsafeDelete();
            }
        }
    }

    public class CommentInfo : HistoryInfo
    {
        public string Text;

        public override string ToString()
        {
            return HistoryMessage.commented.NiceToString();
        }

        public override NotificationType NotificationType
        {
            get { return NotificationType.Commented; }
        }
    }

    public abstract class HistoryInfo 
    {
        public Lite<Entity> Entity;
        public Lite<UserEntity> User;
        public DateTime CreationDate;

        public abstract NotificationType NotificationType { get; }
    }

    public class CardCreatedInfo : HistoryInfo
    {
        public override string ToString()
        {
            return HistoryMessage.created.NiceToString();
        }

        public override NotificationType NotificationType
        {
            get { return NotificationType.Created; }
        }
    }


}
