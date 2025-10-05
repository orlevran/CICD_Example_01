using UsersService.Models;
using UsersService.Repositories;
using UsersService.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;


namespace UsersService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDatabaseStorage DatabaseStorage;
        private readonly JWTSettings JwtSettings;

        public AuthService(IDatabaseStorage DatabaseStorage, IOptions<JWTSettings> JwtSettings)
        {
            this.DatabaseStorage = DatabaseStorage;
            this.JwtSettings = JwtSettings.Value;
        }

        /// <summary>
        /// Generates a JWT token for the user with the given userId. Returns the user and the JWT token if successful, null otherwise.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<(User? user, string? jwt, DateTime? expiresAt)> GenerateJwtToken(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return (null, null, null);

            var user = await DatabaseStorage.GetUserByIdentifierAsync(userId);

            if (user == null) return (null, null, null);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(JwtSettings.SecretKey);
            var expiresAt = DateTime.UtcNow.AddMinutes(JwtSettings.ExpiryMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = expiresAt,
                Issuer = JwtSettings.Issuer,
                Audience = JwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            if (string.IsNullOrEmpty(jwt)) return (null, null, null);

            return (user, jwt, expiresAt);
        }
    }
}
