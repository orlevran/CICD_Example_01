using UsersService.Models;

namespace UsersService.Repositories
{
    public interface IDatabaseStorage
    {
        Task CreateUserAsync(User user);
        Task<User?> GetUserByIdentifierAsync(string identifier);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> UpdateUserAsync(string id, User user);
        Task<bool> DeleteUserAsync(string id);
    }
}