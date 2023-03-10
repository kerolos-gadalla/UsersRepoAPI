using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

using System.Dynamic;


/**
 * This is my first time working with minimal APIs and .Net core
 * This is my first time to work with c# in 3 years
 * 
 * 
 */ 

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
        public bool MarketingConcent { get; set; } = false;
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
        //dynamic? Validate(string token);
        TokenValidationParameters GetValidationParameters();
    }
    class BasicJWTService : IJWTService
    {
        static SymmetricSecurityKey GetSecretKey() => new SymmetricSecurityKey(Encoding.ASCII.GetBytes("SOmekeyhjdhfkjfdbmfn"));
        SigningCredentials mSigningCredentials = new SigningCredentials(GetSecretKey(), SecurityAlgorithms.HmacSha256Signature);

        TokenValidationParameters validationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256Signature },
            IssuerSigningKey = GetSecretKey(),
        };

        public string Issue(string email)
        {
            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("sub", email),
                    new Claim("tokenId", Guid.NewGuid().ToString())
                }),
                SigningCredentials = mSigningCredentials
            });

            return token;

        }

        // trying validation to be able to setup proper configurations
        public dynamic? Validate(string token)
        {
            var handler = new JsonWebTokenHandler();

            var val = handler.ValidateToken(token, validationParameters);
            var claimsObj = new ExpandoObject();
            //return val;
            var sub = val.Claims.FirstOrDefault(x => x.Key == "sub", new KeyValuePair<string, object?>("", null));
            var claims = val.Claims.Select(x => new KeyValuePair<string, string>(x.Key, x.Value?.ToString()));
            foreach (var c in claims)
            {
                ((IDictionary<string, object>)claimsObj)[c.Key] = c.Value;
            }
            //var claimsIdentity = val.ClaimsIdentity.ToString();
            return new
            {
                IsValid = val.IsValid,
                Claims = claimsObj,
                Claims2 = claims,
                Sub = sub.Value,
            };
        }

        public TokenValidationParameters GetValidationParameters()
        {
            return validationParameters;
        }
    }
}



