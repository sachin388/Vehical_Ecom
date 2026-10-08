using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(User user, string password);

        bool VerifyPassword(
            User user,
            string passwordHash,
            string password);
    }

}
