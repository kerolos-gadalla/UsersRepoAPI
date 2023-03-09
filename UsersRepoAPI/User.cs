using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace UsersRepoAPI
{
    public class User
    {
        public string Id { get; set; }
        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string Email { get; set; }
        public string? MarketingConcent { get; set; }
    }


    public class MyDataContext : DbContext
    {
        public MyDataContext(DbContextOptions<MyDataContext> options)
       : base(options) { }

        public DbSet<User> Users => Set<User>();
    }


    interface IHasher
    {
        string Hash(string email);
    }

    class BasicHasher : IHasher
    {
        public string Hash(string email)
        {
            var sha1 = SHA1.Create();
            var str = Encoding.UTF8.GetBytes(email + "450d0b0db2bcf4adde5032eca1a7c416e560cf44");
            var hash = sha1.ComputeHash(str);

            var sb = new StringBuilder(hash.Length * 2);


            foreach (byte b in hash)
            {
                // "x2"
                sb.Append(b.ToString("X2").ToLower());
            }
            return sb.ToString();

        }
    }

    interface IJWTService
    {
        string Issue(string email);
        dynamic? Validate(string token);
    }
    class BasicJWTService : IJWTService
    {
        static SymmetricSecurityKey GetSecretKey() => new SymmetricSecurityKey(Encoding.ASCII.GetBytes("SOmekeyhjdhfkjfdbmfn"));
        SigningCredentials mSigningCredentials = new SigningCredentials(GetSecretKey(), SecurityAlgorithms.HmacSha256Signature);

        public string Issue(string email)
        {
            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("sub", email),
                    new Claim("tokenId", new Guid().ToString())
                }),
                SigningCredentials = mSigningCredentials
            });

            return token;

        }


        public dynamic? Validate(string token)
        {
            return token;
        }

    }
}



