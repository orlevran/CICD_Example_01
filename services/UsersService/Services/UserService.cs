using UsersService.Models;
using UsersService.Models.DTOs;
using UsersService.Repositories;
using MongoDB.Bson;

namespace UsersService.Services
{
    public class UserService : IUsersService
    {
        private readonly IDatabaseStorage DatabaseStorage;

        public UserService(IDatabaseStorage DatabaseStorage)
        {
            this.DatabaseStorage = DatabaseStorage;
        }

        /// <summary>
        /// Creates a new user in the database after validating the request. Returns the created user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<User?> CreateUserAsync(RegisterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var emailExists = await DatabaseStorage.GetUserByEmailAsync(request.Email);
                if (emailExists != null)
                {
                    throw new Exception("Email already exists");
                }

                if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
                {
                    throw new Exception("Password must be at least 6 characters long");
                }

                var role = request.Role.ToLower() switch
                {
                    "admin" => Role.Admin,
                    "user" => Role.User,
                    "guest" => Role.Guest,
                    _ => Role.Guest
                };

                // Hash the password using BCrypt. BCrypt automatically handles salting.
                var user = new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = role,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    BirthDate = request.BirthDate
                };

                await DatabaseStorage.CreateUserAsync(user);

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating user", ex);
            }
        }

        /// <summary>
        /// Logs in a user by verifying their credentials. Returns the user if successful, null otherwise.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<User?> LoginUserAsync(LoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var user = await DatabaseStorage.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return null;
                }
                if(!BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
                {
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Error logging in user", ex);
            }
        }
        
        /// <summary>
        /// Updates an existing user's details based on the provided request. Returns the updated user.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<User?> UpdateUserAsync(string id, EditUserRequest request)
        {
            if (string.IsNullOrEmpty(id) || request == null)
            {
                throw new ArgumentNullException("Invalid id or request");
            }

            try
            {
                var user = await DatabaseStorage.GetUserByIdentifierAsync(id);
                if (user == null)
                {
                    return null;
                }

                // Check if any field is actually being updated.
                bool edited = false;
                if ((!string.IsNullOrEmpty(request.FirstName) && !request.FirstName.Equals(user.FirstName)) ||
                    (!string.IsNullOrEmpty(request.LastName) && !request.LastName.Equals(user.LastName)) ||
                    (!string.IsNullOrEmpty(request.Email) && !request.Email.Equals(user.Email)) ||
                    (!string.IsNullOrEmpty(request.Password) && !BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword)) ||
                    (request.BirthDate != null && request.BirthDate != user.BirthDate) ||
                    (!string.IsNullOrEmpty(request.Role) && !request.Role.Equals(user.Role.ToString(), StringComparison.OrdinalIgnoreCase)) ||
                    (request.LastLogin != null))
                {
                    edited = true;
                }

                if (!edited)
                {
                    return user; // No changes detected, return the existing user
                }

                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.Email = request.Email ?? user.Email;
                user.HashedPassword = !string.IsNullOrEmpty(request.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                    : user.HashedPassword;
                user.BirthDate = request.BirthDate ?? user.BirthDate;
                user.Role = request.Role != null
                    ? (request.Role.ToLower() switch
                    {
                        "admin" => Role.Admin,
                        "user" => Role.User,
                        "guest" => Role.Guest,
                        _ => user.Role
                    })
                    : user.Role;
                user.UpdatedAt = request.UpdatedAt ?? DateTime.UtcNow;
                user.LastLogin = request.LastLogin ?? user.LastLogin;

                await DatabaseStorage.UpdateUserAsync(user.Id, user);
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating user", ex);
            }
        }

        /// <summary>
        /// Deletes a user by their ID. Returns true if deletion was successful, false otherwise.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> DeleteUserAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            return await DatabaseStorage.DeleteUserAsync(id);
        }
    }
}