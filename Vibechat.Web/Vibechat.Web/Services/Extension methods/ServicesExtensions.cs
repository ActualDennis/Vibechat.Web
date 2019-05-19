﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Vibechat.Web.AuthHelpers;
using Vibechat.Web.Services.ChatDataProviders;
using Vibechat.Web.Services.FileSystem;
using Vibechat.Web.Services.Hashing;
using Vibechat.Web.Services.Images;
using Vibechat.Web.Services.Login;
using Vibechat.Web.Services.Paths;
using Vibechat.Web.Services.Repositories;
using Vibechat.Web.Services.Users;
using VibeChat.Web;
using VibeChat.Web.UserProviders;

namespace Vibechat.Web.Services.Extension_methods
{
    public static class ServicesExtensions
    {
        public static void AddDefaultServices(this IServiceCollection services)
        {
            services.AddScoped<ConversationsInfoService, ConversationsInfoService>();
            services.AddScoped<UsersInfoService, UsersInfoService>();
            services.AddScoped<LoginService, LoginService>();
        }

        public static void AddDefaultRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<IAttachmentKindsRepository, AttachmentKindsRepository>();
            services.AddScoped<IUsersConversationsRepository, UsersConversationsRepository>();
            services.AddScoped<IConversationRepository, ConversationsRepository>();
        }

        public static void AddBusinessLogic(this IServiceCollection services)
        {
            services.AddSingleton<ICustomHubUserIdProvider, DefaultUserIdProvider>();
            services.AddScoped<ITokenClaimValidator, JwtTokenClaimValidator>();
            services.AddScoped<JwtSecurityTokenHandler, JwtSecurityTokenHandler>();
            services.AddSingleton<IChatDataProvider, DefaultChatDataProvider>();
            services.AddSingleton<IImageCompressionService, ImageCompressionService>();
            services.AddSingleton<IImageScalingService, ImageCompressionService>();
            services.AddSingleton<IHexHashingService, Sha256Service>();
            services.AddSingleton<UniquePathsProvider, UniquePathsProvider>();
            services.AddScoped<ImagesService, ImagesService>();
        }
    }
}
