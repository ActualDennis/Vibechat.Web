﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vibechat.Web.ApiModels;
using Vibechat.Web.Data.ApiModels.Conversation;
using Vibechat.Web.Data.ApiModels.Messages;
using Vibechat.Web.Services;
using Vibechat.Web.Services.Bans;
using Vibechat.Web.Services.FileSystem;
using VibeChat.Web.ApiModels;
using VibeChat.Web.ChatData;

namespace VibeChat.Web.Controllers
{
    public class ConversationsController : Controller
    {
        protected ConversationsInfoService mConversationService;

        public BansService BansService { get; }

        public ConversationsController(
            ConversationsInfoService mDbService,
            BansService bansService)
        {
            this.mConversationService = mDbService;
            BansService = bansService;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/Create")]
        public async Task<ResponseApiModel<ConversationTemplate>> Create([FromBody] CreateConversationCredentialsApiModel convInfo)
        {
            try
            {
                var result = await mConversationService.CreateConversation(convInfo);

                return new ResponseApiModel<ConversationTemplate>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<ConversationTemplate>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/AddUserTo")]
        public async Task<ResponseApiModel<string>> AddUserTo([FromBody]AddToConversationApiModel UserProvided)
        {
            try
            {
                await mConversationService.AddUserToConversation(UserProvided);

                return new ResponseApiModel<string>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = null
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<string>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }

        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/GetAll")]
        public async Task<ResponseApiModel<List<ConversationTemplate>>> GetAll()
        {
            try
            {
                var thisUserId = JwtHelper.GetNamedClaimValue(User.Claims);

                List<ConversationTemplate> result = await mConversationService.GetConversations(thisUserId);

                result.ForEach(async x => x.IsMessagingRestricted = await BansService.IsBannedFromConversation(x.ConversationID, thisUserId));

                return new ResponseApiModel<List<ConversationTemplate>>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<List<ConversationTemplate>>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }
        }

        public class GetByIdRequest
        {
            public int conversationId { get; set; }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/GetById")]
        public async Task<ResponseApiModel<ConversationTemplate>> GetById([FromBody]GetByIdRequest request)
        {
            try
            {
                string thisUserId = JwtHelper.GetNamedClaimValue(User.Claims);

                ConversationTemplate result = await mConversationService.GetById(request.conversationId, thisUserId).ConfigureAwait(false);

                result.IsMessagingRestricted = await BansService.IsBannedFromConversation(request.conversationId, thisUserId).ConfigureAwait(false);

                return new ResponseApiModel<ConversationTemplate>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<ConversationTemplate>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/GetParticipants")]
        public async Task<ResponseApiModel<GetParticipantsResultApiModel>> GetParticipants([FromBody] GetParticipantsApiModel convInfo)
        {
            try
            {
                var result = await mConversationService.GetParticipants(convInfo);

                return new ResponseApiModel<GetParticipantsResultApiModel>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<GetParticipantsResultApiModel>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/GetMessages")]
        public async Task<ResponseApiModel<GetMessagesResultApiModel>> GetMessages([FromBody] GetMessagesApiModel convInfo)
        {
            try
            {
                var result = await mConversationService.GetMessages(
                    convInfo,
                     JwtHelper.GetNamedClaimValue(User.Claims));

                return new ResponseApiModel<GetMessagesResultApiModel>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<GetMessagesResultApiModel>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }

        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/DeleteMessages")]
        public async Task<ResponseApiModel<string>> DeleteConversationMessages([FromBody] DeleteMessagesRequest messagesInfo)
        {
            try
            {
                await mConversationService.DeleteConversationMessages(
                    messagesInfo,
                     JwtHelper.GetNamedClaimValue(User.Claims));

                return new ResponseApiModel<string>()
                {
                    IsSuccessfull = true,
                    ErrorMessage = null,
                    Response = null
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<string>()
                {
                    IsSuccessfull = false,
                    ErrorMessage = ex.Message,
                    Response = null
                };
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/UpdateThumbnail")]
        public async Task<ResponseApiModel<UpdateThumbnailResponse>> UpdateThumbnail([FromForm]UpdateThumbnailRequest updateThumbnail)
        {
            try
            {
                var result = await mConversationService.UpdateThumbnail(updateThumbnail.conversationId, updateThumbnail.thumbnail);
                return new ResponseApiModel<UpdateThumbnailResponse>()
                {
                    IsSuccessfull = true,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<UpdateThumbnailResponse>()
                {
                    ErrorMessage = ex.Message,
                    IsSuccessfull = false
                };
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/ChangeName")]
        public async Task<ResponseApiModel<bool>> ChangeName([FromBody] ChangeConversationNameRequest request)
        {
            try
            {
                await mConversationService.ChangeName(request.ConversationId, request.Name);

                return new ResponseApiModel<bool>()
                {
                    IsSuccessfull = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<bool>()
                {
                    ErrorMessage = ex.Message,
                    IsSuccessfull = false
                };
            }
        }

        public class BanRequest
        {
            public string userId { get; set; }

            public int conversationId { get; set; }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/SearchGroups")]
        public async Task<ResponseApiModel<List<ConversationTemplate>>> SearchGroups([FromBody] SearchRequest request)
        {
            try
            {
                var result = await mConversationService.SearchForGroups(request.SearchString,
                     JwtHelper.GetNamedClaimValue(User.Claims));

                return new ResponseApiModel<List<ConversationTemplate>>()
                {
                    IsSuccessfull = true,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<List<ConversationTemplate>>()
                {
                    ErrorMessage = ex.Message,
                    IsSuccessfull = false
                };
            }
        }

        public class ChangeConversationPublicStateRequest
        {
            public int conversationId{ get; set; }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/ChangePublicState")]
        public async Task<ResponseApiModel<bool>> ChangeConversationPublicState([FromBody]ChangeConversationPublicStateRequest request)
        { 
            try
            {
               await mConversationService.ChangePublicState(request.conversationId,
                     JwtHelper.GetNamedClaimValue(User.Claims));

                return new ResponseApiModel<bool>()
                {
                    IsSuccessfull = true,
                    Response = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<bool>()
                {
                    ErrorMessage = ex.Message,
                    IsSuccessfull = false
                };
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("api/Conversations/BanFrom")]
        public async Task<ResponseApiModel<bool>> BanFrom([FromBody] BanRequest request)
        {
            try
            {
                await BansService.BanUserFromConversation(
                    request.conversationId,
                    request.userId,
                    JwtHelper.GetNamedClaimValue(User.Claims));

                return new ResponseApiModel<bool>()
                {
                    IsSuccessfull = true,
                    Response = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseApiModel<bool>()
                {
                    ErrorMessage = ex.Message,
                    IsSuccessfull = false
                };
            }
        }

        //public class IsBannedFromRequest
        //{
        //    public int[] conversationIds{ get; set; }
        //}

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[Route("api/Conversations/isBannedFrom")]
        //public async Task<ResponseApiModel<bool[]>> IsBannedFrom([FromBody]IsBannedFromRequest request)
        //{
        //    try
        //    {
        //        var result = await BansService.IsBannedFromConversations(request.conversationIds,
        //              JwtHelper.GetNamedClaim(User.Claims));

        //        return new ResponseApiModel<bool[]>()
        //        {
        //            IsSuccessfull = true,
        //            Response = result
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ResponseApiModel<bool[]>()
        //        {
        //            ErrorMessage = ex.Message,
        //            IsSuccessfull = false
        //        };
        //    }
        //}
    }
}
