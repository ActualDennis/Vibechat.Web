﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Vibechat.Web.Data.Messages;
using VibeChat.Web;
using VibeChat.Web.ChatData;
using VibeChat.Web.Data.DataModels;

namespace Vibechat.Web.Data.Repositories
{
    public interface IMessagesRepository
    {
        MessageDataModel Add(AppUser whoSent, Message message, int groupId, MessageDataModel forwardedMessage);
        MessageDataModel AddAttachment(AppUser whoSent, MessageAttachmentDataModel attachment, Message message, int groupId);
        bool Empty();
        IQueryable<MessageDataModel> GetByIds(List<int> ids);

        MessageDataModel GetById(int id);

        int GetUnreadAmount(int conversationId, string userId);

        void MarkAsRead(MessageDataModel message);

        IIncludableQueryable<MessageDataModel, AppUser> Get(
            string userId, int conversationId, bool AllMessages = false, int offset = 0, int count = 0);
        IIncludableQueryable<MessageDataModel, AppUser> GetAttachments(
            string userId, int conversationId, AttachmentKind attachmentKind, int offset, int count);

        MessageDataModel AddSecureMessage(AppUser whoSent, string message, int groupId);

        void Remove(List<int> messagesIds, string whoRemovedId);

        void RemovePermanent(List<MessageDataModel> messages);
    }
}