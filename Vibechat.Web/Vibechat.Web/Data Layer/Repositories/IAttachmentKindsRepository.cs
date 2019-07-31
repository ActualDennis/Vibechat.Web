﻿using System.Threading.Tasks;
using Vibechat.Web.Data.Messages;
using VibeChat.Web.Data.DataModels;

namespace Vibechat.Web.Data.Repositories
{
    public interface IAttachmentKindsRepository
    {
        Task<AttachmentKindDataModel> GetById(AttachmentKind id);
    }
}