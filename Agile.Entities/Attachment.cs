using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agile.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class AttachmentEntity : Entity
    {
        [NotNullable]
        Lite<CardEntity> card;
        [NotNullValidator]
        public Lite<CardEntity> Card
        {
            get { return card; }
            set { Set(ref card, value); }
        }

        [NotNullable]
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        [NotNullable]
        FilePathEntity file;
        [NotNullValidator]
        public FilePathEntity File
        {
            get { return file; }
            set { Set(ref file, value); }
        }

        AttachmentType type;
        public AttachmentType Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }
    }

    public enum AttachmentType
    {
        File,
        Image,
    }

    public static class AttachmentOperation
    {
        public static readonly ExecuteSymbol<AttachmentEntity> Save = OperationSymbol.Execute<AttachmentEntity>();
        public static readonly DeleteSymbol<AttachmentEntity> Delete = OperationSymbol.Delete<AttachmentEntity>();
    }
}
