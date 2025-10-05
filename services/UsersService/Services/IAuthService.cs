using UsersService.Models;

namespace UsersService.Services
{
    public interface IAuthService
    {
        Task<(User? user, string? jwt, DateTime? expiresAt)> GenerateJwtToken(string userId);
    }
}