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
using Signum.Engine.Files;
using System.IO;
using Signum.Entities.Files;
using Signum.Entities.Authorization;

namespace Agile.Logic
{
    public static class AttachmentLogic
    {
        static Expression<Func<CardEntity, IQueryable<AttachmentEntity>>> AttachmentsExpression = 
            c => Database.Query<AttachmentEntity>().Where(c2 => c2.Card.RefersTo(c));
        public static IQueryable<AttachmentEntity> Attachments(this CardEntity e)
        {
            return AttachmentsExpression.Evaluate(e);
        }

        static Expression<Func<AttachmentEntity, AttachmentInfo>> ToAttachmentInfoExpression =
            a => new AttachmentInfo { Entity = a.ToLite(), User = a.User, CreationDate = a.CreationDate, File = a.File, Type = a.Type }; 
        public static AttachmentInfo ToAttachmentInfo(this AttachmentEntity a)
        {
            return ToAttachmentInfoExpression.Evaluate(a);
        }
    
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AttachmentEntity>();
        
                dqm.RegisterQuery(typeof(AttachmentEntity), () =>
                    from c in Database.Query<AttachmentEntity>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Card,
                        c.File,
                    });
        
                dqm.RegisterExpression((CardEntity c) => c.Attachments(), () => typeof(AttachmentEntity).NicePluralName());
        
                new Graph<AttachmentEntity>.Execute(AttachmentOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => { }
                }.Register();
                
                new Graph<AttachmentEntity>.ConstructFrom<CardEntity>(AttachmentOperation.CreateAttachmentFromCard)
                {
                    Construct = (c, _) => new AttachmentEntity
                    {
                        User = UserEntity.Current.ToLite(),
                        Card = c.ToLite(),
                    }
                }.Register();

                FilePathLogic.Register(AttachmentFileType.Attachment, new FileTypeAlgorithm
                {
                    GetPrefixPair = fp =>
                    {
                        var repos = Starter.Configuration.Value.Repositories;

                        var first = fp.IsNew ? repos.FirstEx() :
                            repos.FirstEx(r => File.Exists(Path.Combine(r.PhysicalPrefix, fp.Sufix)));

                        return new PrefixPair(first.PhysicalPrefix) { WebPrefix = first.WebPrefix };
                    }
                });

                Schema.Current.EntityEvents<CardEntity>().PreUnsafeDelete += query => query.SelectMany(q => q.Attachments()).UnsafeDelete();
        
            }
        }
    }

    public class AttachmentInfo : HistoryInfo
    {
        public FilePathEntity File;
        public AttachmentType Type;

        public override string ToString()
        {
            return HistoryMessage.attached.NiceToString();
        }
    }
}
