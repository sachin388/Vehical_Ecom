using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
  

    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        // Store only the hash, never the raw refresh token.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public Guid? ReplacedByTokenId { get; set; }

        public User User { get; set; } = null!;

        public bool IsExpired =>
            DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsRevoked =>
            RevokedAtUtc.HasValue;

        public bool IsActive =>
            !IsExpired && !IsRevoked;
    }

}
