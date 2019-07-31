﻿using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using VibeChat.Web;

namespace Vibechat.Web.Data.Repositories
{
    public interface IUsersRepository
    {
        Task<bool> CheckPassword(string password, AppUser user);

        Task<IdentityResult> CreateUser(AppUser user, string password);

        Task<IdentityResult> CreateUser(AppUser user);

        Task<IdentityResult> DeleteUser(AppUser user);

        Task<IQueryable<AppUser>> FindByUsername(string username);
        Task<AppUser> GetByUsername(string username);
        Task<AppUser> GetByEmail(string email);
        Task<AppUser> GetById(string id);
        Task MakeUserOffline(string userId);
        Task MakeUserOnline(string userId, string signalRConnectionId);

        Task MakeUserOnline(string userId);

        Task ChangeUserPublicState(string userId);

        Task UpdateAvatar(string thumbnail, string fullSized, string userId);

        Task ChangeLastName(string newName, string userId);

        Task ChangeName(string newName, string userId);

        Task ChangeUsername(string newName, string userId);

        Task<string> GetRefreshToken(string userId);

        Task UpdateRefreshToken(string userId, string token);
    }
}