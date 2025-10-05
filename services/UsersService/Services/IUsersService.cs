using UsersService.Models;
using UsersService.Models.DTOs;

namespace UsersService.Services
{
    public interface IUsersService
    {
        Task<User?> CreateUserAsync(RegisterRequest request);
        Task<User?> LoginUserAsync(LoginRequest request);
        Task<User?> UpdateUserAsync(string id, EditUserRequest request);
        Task<bool> DeleteUserAsync(string id);
    }
}