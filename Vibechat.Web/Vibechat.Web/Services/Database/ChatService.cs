﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vibechat.Web.Data.ApiModels.Conversation;
using Vibechat.Web.Data.ApiModels.Messages;
using Vibechat.Web.Data.DataModels;
using Vibechat.Web.Extensions;
using Vibechat.Web.Services.ChatDataProviders;
using Vibechat.Web.Services.Crypto;
using Vibechat.Web.Services.FileSystem;
using Vibechat.Web.Services.Repositories;
using VibeChat.Web;
using VibeChat.Web.ApiModels;
using VibeChat.Web.ChatData;
using VibeChat.Web.Data.DataModels;

namespace Vibechat.Web.Services
{
    public class ChatService
    {
        public ChatService(
            IChatDataProvider chatDataProvider,
            IUsersRepository usersRepository,
            IMessagesRepository messagesRepository,
            IAttachmentRepository attachmentRepository,
            IAttachmentKindsRepository attachmentKindsRepository,
            IUsersConversationsRepository usersConversationsRepository,
            IConversationRepository conversationRepository,
            IDhPublicKeysRepository dh,
            FilesService imagesService,
            CryptoService cryptoService)
        {
            this.chatDataProvider = chatDataProvider;
            this.usersRepository = usersRepository;
            this.messagesRepository = messagesRepository;
            this.attachmentRepository = attachmentRepository;
            this.attachmentKindsRepository = attachmentKindsRepository;
            this.usersConversationsRepository = usersConversationsRepository;
            this.conversationRepository = conversationRepository;
            this.publicKeys = dh;
            ImagesService = imagesService;
            this.cryptoService = cryptoService;
        }

        protected readonly IChatDataProvider chatDataProvider;

        protected readonly IUsersRepository usersRepository;

        protected readonly IMessagesRepository messagesRepository;

        protected readonly IAttachmentRepository attachmentRepository;

        protected readonly IAttachmentKindsRepository attachmentKindsRepository;

        protected readonly IUsersConversationsRepository usersConversationsRepository;

        protected readonly IConversationRepository conversationRepository;

        private readonly IDhPublicKeysRepository publicKeys;

        private const int MaxThumbnailLengthMB = 5;

        private const int MaxNameLength = 200;

        private const int MaxParticipantsToReturn = 200;

        protected readonly FilesService ImagesService;

        private readonly CryptoService cryptoService;

        #region Conversations

        public async Task UpdateAuthKey(int chatId, string authKeyId, string deviceId, string thisUserId)
        {
            if(!await ExistsInConversation(chatId, thisUserId))
            {
                throw new InvalidDataException($"Wrong conversation id was provided.");
            }

            ConversationDataModel chat = conversationRepository.GetById(chatId);

            if (chat == null)
            {
                throw new InvalidDataException($"Wrong conversation id was provided.");
            }

            await conversationRepository.UpdateAuthKey(chat, authKeyId);

            usersConversationsRepository.UpdateDeviceId(deviceId, thisUserId, chatId);
        }

        public async Task<ConversationTemplate> CreateConversation(CreateConversationCredentialsApiModel credentials)
        {

            var defaultError = new FormatException("Error while creating the conversation..");

            //if there was no group info or creator info

            if (string.IsNullOrWhiteSpace(credentials.CreatorId))
            {
                throw defaultError;
            }

            var user = await usersRepository.GetById(credentials.CreatorId);

            if (user == null)
            {
                throw defaultError;
            }

            if ((!credentials.IsGroup) && (credentials.DialogUserId == null))
            {
                throw new FormatException("No dialogue user was provided...");
            }
            
            if(credentials.IsGroup && credentials.ConversationName.Length > MaxNameLength)
            {
                throw new FormatException("Chat name was too long...");
            }

            AppUser SecondDialogueUser = null;
            DhPublicKeyDataModel dhPublicKey = null;

            string imageUrl = string.Empty;

            if (!credentials.IsGroup)
            {
                //if this is a dialogue , find a user with whom to create conversation
                SecondDialogueUser = await usersRepository.GetById(credentials.DialogUserId);

                if (SecondDialogueUser == null)
                {
                    throw defaultError;
                }

                if (credentials.IsSecure)
                {
                    dhPublicKey = await publicKeys.GetRandomKey();
                }
            }
            else
            {
                imageUrl = credentials.ImageUrl ?? chatDataProvider.GetGroupPictureUrl();
            }

            var ConversationToAdd = new ConversationDataModel()
            {
                IsGroup = credentials.IsGroup,
                Name = credentials.IsGroup ? credentials.ConversationName : null,
                FullImageUrl = imageUrl,
                ThumbnailUrl = imageUrl,
                Creator = user,
                IsPublic = credentials.IsPublic,
                IsSecure = credentials.IsSecure,
                PublicKeyId = dhPublicKey?.Id,
                PublicKey = dhPublicKey
            };

            await conversationRepository.Add(ConversationToAdd);

            try
            {
                if (!credentials.IsGroup)
                {
                    await usersConversationsRepository.Add(credentials.DialogUserId, ConversationToAdd.Id);
                }

                await usersConversationsRepository.Add(credentials.CreatorId, ConversationToAdd.Id, credentials.DeviceId);
            }
            catch (Exception ex)
            {
                conversationRepository.Remove(ConversationToAdd);

                throw new InvalidDataException("Couldn't create group/dialog. Probably there exists one already.", ex);
            }

            return ConversationToAdd.ToConversationTemplate(
                credentials.IsGroup ? new List<UserInfo>() { user.ToUserInfo() } : null,
                null,
                SecondDialogueUser,
                dhPublicKey,
                credentials.DeviceId
                );
        }

        public async Task<int> GetUnreadMessagesAmount(int conversationId, string userId)
        {
            ConversationDataModel conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new InvalidDataException($"Wrong conversation id was provided.");
            }

            if (messagesRepository.Empty())
            {
                return 0;
            }

            return messagesRepository.GetUnreadAmount(conversationId, userId);
        }
        public async Task<UpdateThumbnailResponse> UpdateThumbnail(int conversationId, IFormFile image, string userId)
        {
            if ((image.Length / (1024 * 1024)) > MaxThumbnailLengthMB)
            {
                throw new InvalidDataException($"Thumbnail was larger than {MaxThumbnailLengthMB}");
            }

            ConversationDataModel conversation = conversationRepository.GetById(conversationId);

            if(conversation == null)
            {
                throw new InvalidDataException($"Wrong conversation id was provided.");
            }

            try
            {
                using (var buffer = new MemoryStream())
                {
                    image.CopyTo(buffer);
                    buffer.Seek(0, SeekOrigin.Begin);
                    var thumbnailFull = await ImagesService.SaveProfileOrChatPicture(image, buffer, image.FileName, conversationId.ToString(), userId);

                    conversationRepository.UpdateThumbnail(thumbnailFull.Item1, thumbnailFull.Item2, conversation);
                }

                return new UpdateThumbnailResponse()
                {
                    ThumbnailUrl = conversation.ThumbnailUrl,
                    FullImageUrl = conversation.FullImageUrl
                };
            }
            catch (Exception ex)
            {

                throw new Exception("Failed to update chat image. Try different one." , ex);
            }
        }

        public async Task<List<Message>> GetAttachments(string kind, int conversationId, string whoAccessedId, int offset, int count)
        {
            var unAuthorizedError = new UnauthorizedAccessException("You are unauthorized to do such an action.");

            if (messagesRepository.Empty())
            {
                return null;
            }

            var conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new FormatException("Wrong conversation to get attachments from.");
            }

            var members = usersConversationsRepository.GetConversationParticipants(conversationId);

            //only member of conversation could request messages of non-public conversation.

            if (members.FirstOrDefault(x => x.Id == whoAccessedId) == null && !conversation.IsPublic)
            {
                throw new UnauthorizedAccessException("You are unauthorized to do such an action.");
            }

            var messages = messagesRepository.GetAttachments(
                whoAccessedId,
                conversationId,
                kind,
                offset,
                count);


            return (from msg in messages
                    select msg.ToMessage()).ToList();

        }

        public async Task ChangeName (int conversationId, string name)
        {
            if(name.Length > MaxNameLength)
            {
                throw new InvalidDataException($"Name length couldn't be more than {MaxNameLength}.");
            }

            var conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new InvalidDataException($"Wrong conversation id was provided.");
            }

            conversationRepository.ChangeName(conversation, name);
        }

        public async Task<List<ConversationTemplate>> SearchForGroups(string name, string whoAccessedId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidDataException($"Search string coudn't be null.");
            }

            var whoSearches = await usersRepository.GetById(whoAccessedId);

            var groups = await conversationRepository.SearchByName(name, whoSearches, usersConversationsRepository);

            if(groups == null)
            {
                return null;
            }

            var result = new List<ConversationTemplate>();

            foreach (ConversationDataModel conversation in groups)
            {
                result.Add(
                    conversation.ToConversationTemplate(
                        await GetParticipants(conversation.Id, MaxParticipantsToReturn),
                        await GetMessages(conversation.Id, offset: 0, count: 1, whoAccessedId),
                        null,
                        conversation.PublicKey,
                        null
                    ));
            }

            return result;
        }

        public async Task ChangePublicState(int conversationId, string whoAccessedId)
        {
            var conversation = conversationRepository.GetById(conversationId);

            if(conversation.Creator.Id != whoAccessedId)
            {
                throw new FormatException("This method is only allowed to group creator.");
            }

            await conversationRepository.ChangePublicState(conversationId);
        }


        /// <summary>
        /// Should be used once per request as it's quiet expensive.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="whoRemovedId"></param>
        /// <param name="conversationId"></param>
        /// <param name="IsSelf"></param>
        /// <returns></returns>
        public async Task RemoveUserFromConversation(string userId, string whoRemovedId, int conversationId, bool IsSelf)
        {
            var conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new FormatException("Wrong conversation.");
            }

            var userConversation = await usersConversationsRepository.Get(userId, conversationId);

            if (userConversation == null)
            {
                throw new FormatException("User is not a part of this conversation.");
            }

            if(conversation.Creator.Id != whoRemovedId && conversation.IsGroup && !IsSelf)
            {
                throw new FormatException("Only creator can remove users in group.");
            }

            await usersConversationsRepository.Remove(userConversation);

            //dumbest thing to do, will fix later. TODO
            if (usersConversationsRepository.GetConversationParticipants(conversation.Id).Count().Equals(0))
            {
                conversationRepository.Remove(conversation);
            }
        }

        public async Task RemoveConversation(ConversationTemplate Conversation, string whoRemoves)
        {
            ConversationDataModel conversation = conversationRepository.GetById(Conversation.ConversationID);

            if(conversation == null)
            {
                throw new FormatException("Wrong conversation to remove.");
            }

            //secure chats may be removed by any of participants.

            if (conversation.Creator.Id != whoRemoves && conversation.IsGroup && !conversation.IsSecure)
            {
                throw new FormatException("Only creator can remove group.");
            }

            //this removes all messages and users-chats links
            conversationRepository.Remove(conversation);
        }

        public async Task<UserInfo> AddUserToConversation(AddToConversationApiModel UserProvided)
        {
            var defaultError = new FormatException("Invalid credentials were provided.");

            var FoundConversation = conversationRepository.GetById(UserProvided.ConvId);

            if (FoundConversation == null)
            {
                throw defaultError;
            }

            var FoundUser = await usersRepository.GetById(UserProvided.UserId).ConfigureAwait(false);

            if (FoundUser == null)
            {
                throw defaultError;
            }

            if(await usersConversationsRepository.Exists(FoundUser.Id, FoundConversation.Id))
            {
                throw new FormatException("User already exists in converation.");
            }

            var addedUser = (await usersConversationsRepository.Add(UserProvided.UserId, UserProvided.ConvId)).User;

            return addedUser.ToUserInfo();
        }

        public async Task ValidateDialog(string firstUserId, string secondUserId, int conversationId)
        {
            var dialog = await usersConversationsRepository.GetDialog(firstUserId, secondUserId);

            if(dialog == null)
            {
                throw new InvalidDataException("Wrong id of a user in dialog.");
            }
        }

        public async Task<List<ConversationTemplate>> GetConversations(string deviceId, string whoAccessedId)
        {
            var defaultError = new FormatException("User info provided was not correct.");

            if (whoAccessedId == null)
            {
                throw defaultError;
            }

            var user = await usersRepository.GetById(whoAccessedId).ConfigureAwait(false);

            if (user == null)
            {
                throw defaultError;
            }

            IQueryable<ConversationDataModel> conversations = usersConversationsRepository.GetUserConversations(deviceId, whoAccessedId);

            var returnData = new List<ConversationTemplate>();

            foreach (ConversationDataModel conversation in conversations)
            {
                AppUser DialogUser = conversation.IsGroup ? null : usersConversationsRepository.GetUserInDialog(conversation.Id, whoAccessedId);

                returnData.Add
                    (
                    conversation.ToConversationTemplate(
                         conversation.IsGroup ? await GetParticipants(conversation.Id, MaxParticipantsToReturn) : null,
                         //only get last message here, client should fetch messages after he opened the conversation.
                         await GetMessages(conversation.Id, offset: 0, count: 1, whoAccessedId),
                         DialogUser,
                         conversation.PublicKey,
                         conversation.IsSecure ? await GetDeviceId(conversation.Id, whoAccessedId) : null
                         )
                    );
            }

            return returnData;

        }

        private async Task<string> GetDeviceId(int chatId, string userId)
        {
            var chat = await usersConversationsRepository.Get(userId, chatId);
            return chat.DeviceId;
        }

        public async Task<List<UserInfo>> GetParticipants(int chatId, int maximum = 0)
        {
            var defaultErrorMessage = new FormatException("Wrong conversation was provided.");

            var conversation = conversationRepository.GetById(chatId);

            if (conversation == null)
            {
                throw defaultErrorMessage;
            }

            var participants = usersConversationsRepository.GetConversationParticipants(chatId);

            var users = (from participant in participants
                    select participant.ToUserInfo()
                               ).ToList();

            if (maximum.Equals(0))
            {
                return users;
            }
            else
            {
                return users.Take(maximum).ToList();
            }
        }

        public async Task<List<Message>> GetMessages(int chatId, int offset, int count, string whoAccessedId)
        {
            var defaultErrorMessage = new FormatException("Wrong conversation was provided.");

            var unAuthorizedError = new UnauthorizedAccessException("You are unauthorized to do such an action.");

            if (messagesRepository.Empty())
            {
                return null;
            }

            var conversation = conversationRepository.GetById(chatId);

            if (conversation == null)
            {
                throw defaultErrorMessage;
            }

            var members = usersConversationsRepository.GetConversationParticipants(chatId);

            //only member of conversation could request messages of non-public conversation.

            if (members.FirstOrDefault(x => x.Id == whoAccessedId) == null && !conversation.IsPublic)
            {
                throw unAuthorizedError;
            }

            var messages = messagesRepository.Get(
                whoAccessedId,
                chatId,
                false,
                offset,
                count);

            return (from msg in messages
                    select msg.ToMessage()).ToList();

        }

        public async Task<bool> ExistsInConversation(int conversationsId, string userId)
        {
            return await usersConversationsRepository.Exists(userId, conversationsId);
        }

        /// <summary>
        /// Gets specified chat info by id. Doesn't support secure chats.
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="whoAccessedId"></param>
        /// <returns></returns>
        public async Task<ConversationTemplate> GetById(int conversationId, string whoAccessedId)
        {
             
            ConversationDataModel conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new FormatException("No such conversation was found.");
            }

            var dialogUser = new AppUser();
            var members = new List<UserInfo>();
            var messages = new List<Message>();

            if (conversation.IsGroup)
            {
                members = usersConversationsRepository.GetConversationParticipants(conversationId)
                .Select(x => x.ToUserInfo())
                .ToList();
            }
            else
            {
                dialogUser = usersConversationsRepository.GetUserInDialog(conversationId,whoAccessedId);

                if (dialogUser == null)
                {
                    throw new FormatException("Unexpected error: no corresponding user in dialogue.");
                }
            }

            return conversation.ToConversationTemplate(members, null, dialogUser, conversation.PublicKey, null);
        }

        public async Task<ConversationTemplate> GetByIdSimplified(int conversationId, string whoAccessedId)
        {
            ConversationDataModel conversation = conversationRepository.GetById(conversationId);

            if (conversation == null)
            {
                throw new FormatException("No such conversation was found.");
            }

            var dialogUser = new AppUser();

            if (!conversation.IsGroup)
            {
                dialogUser = usersConversationsRepository.GetUserInDialog(conversationId, whoAccessedId);

                if (dialogUser == null)
                {
                    throw new FormatException("Unexpected error: no corresponding user in dialogue.");
                }
            }

            return new ConversationTemplate()
            {
                DialogueUser = dialogUser?.ToUserInfo(),
                IsGroup = conversation.IsGroup
            }; 
        }

        public async Task MarkMessageAsRead(int msgId, int conversationId, string whoAccessedId)
        {
            MessageDataModel message = messagesRepository.GetById(msgId);

            if (message.User.Id == whoAccessedId)
            {
                throw new UnauthorizedAccessException("Couldn't mark this message as read because it was sent by you.");
            }

            if(!await ExistsInConversation(conversationId, whoAccessedId))
            {
                throw new UnauthorizedAccessException("User was not present in conversation. Couldn't mark the message as read.");
            }

            messagesRepository.MarkAsRead(message);
        }

        public async Task<MessageDataModel> AddMessage(Message message, int groupId, string SenderId)
        {
            AppUser whoSent = await usersRepository.GetById(SenderId);

            if (whoSent == null)
            {
                throw new FormatException($"Failed to retrieve user with id {SenderId} from database: no such user exists");
            }

            MessageDataModel forwardedMessage = null;

            if(message.ForwardedMessage != null)
            {
                var foundMessage = messagesRepository.GetByIds(new List<int>() { message.ForwardedMessage.Id }).ToList();

                if (!foundMessage.Count().Equals(1))
                {
                    throw new FormatException($"Forwarded message was not found.");
                }

                forwardedMessage = foundMessage[0];
            }

            return await messagesRepository.Add(whoSent, message, groupId, forwardedMessage);
        }

        public async Task<MessageDataModel> AddEncryptedMessage(string message, int groupId, string SenderId)
        {
            AppUser whoSent = await usersRepository.GetById(SenderId);

            if (whoSent == null)
            {
                throw new FormatException($"Failed to retrieve user with id {SenderId} from database: no such user exists");
            }

            return await messagesRepository.AddSecureMessage(whoSent, message, groupId);
        }

        public async Task<MessageDataModel> AddAttachmentMessage(Message message, int groupId, string SenderId)
        {
            AppUser whoSent = await usersRepository.GetById(SenderId);

            if (whoSent == null)
            {
                throw new FormatException($"Failed to retrieve user with id {SenderId} from database: no such user exists");
            }

            AttachmentKindDataModel attachmentKind = await attachmentKindsRepository.GetById(message.AttachmentInfo.AttachmentKind);

            var attachment = await attachmentRepository.Add(attachmentKind, message);

            return await messagesRepository.AddAttachment(whoSent, attachment, message, groupId);
        }

        public async Task DeleteConversationMessages(DeleteMessagesRequest messagesInfo, string whoAccessedId)
        {
            var unAuthorizedError = new UnauthorizedAccessException("You are unauthorized to do such an action.");

            IQueryable<MessageDataModel> messagesToDelete = messagesRepository.GetByIds(messagesInfo.MessagesId);

            if(!messagesToDelete.All(x => x.ConversationID == messagesInfo.ConversationId))
            {
                throw new ArgumentException("All messages must be from same conversation passed as ConversationId parameter.");
            }

            var conversation = await usersConversationsRepository.Get(whoAccessedId, messagesInfo.ConversationId);

            if(conversation == null)
            {
                throw unAuthorizedError;
            }

            try
            {
                await messagesRepository.Remove(messagesInfo.MessagesId, whoAccessedId);
            }
            catch(Exception ex)
            {
                throw new MemberAccessException("Failed to delete this message. Probably it was already deleted.", ex);
            }
        } 

        #endregion
    }
}
