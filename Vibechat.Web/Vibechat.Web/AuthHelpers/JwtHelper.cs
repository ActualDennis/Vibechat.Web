﻿using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace VibeChat.Web
{
    public static class JwtHelper
    {
        public static string JwtUserClaimName => "UserId";
        /// <summary>
        /// Generates jwt token and returns it as string
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GenerateJwtToken(this UserInApplication user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.UserName),
                //add custom claim to identify the user by his id
                new Claim(JwtUserClaimName, user.Id)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DI.Configuration["Jwt:SecretKey"])),
                SecurityAlgorithms.HmacSha256
                );

            var token = new JwtSecurityToken(
                issuer: DI.Configuration["Jwt:Issuer"],
                audience: DI.Configuration["Jwt:Audience"],
                claims: claims,
                signingCredentials: credentials,
                expires: DateTime.UtcNow.AddMonths(2)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
