using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Security
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<User> _hasher;

        public PasswordService()
        {
            _hasher = new PasswordHasher<User>();
        }

        public string HashPassword(
            User user,
            string password)
        {
            return _hasher.HashPassword(
                user,
                password);
        }

        public bool VerifyPassword(
            User user,
            string passwordHash,
            string password)
        {
            var result = _hasher.VerifyHashedPassword(
                user,
                passwordHash,
                password);

            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }

}
