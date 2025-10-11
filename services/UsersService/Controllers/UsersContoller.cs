using UsersService.Models;
using UsersService.Models.DTOs;
using UsersService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersService.Configurations;
using Microsoft.Extensions.Options;

namespace UsersService.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService UsersService;
        private readonly IAuthService AuthService;
        private readonly JWTSettings JWTSettings;

        public UsersController(IUsersService UsersService, IAuthService AuthService, IOptions<JWTSettings> JWTSettings)
        {
            this.UsersService = UsersService;
            this.AuthService = AuthService;
            this.JWTSettings = JWTSettings.Value;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                Console.WriteLine("Registering user: " + request.Email);
                var user = await UsersService.CreateUserAsync(request);

                if (user == null)
                    return BadRequest(new { Error = "User could not be created" });

                return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (DuplicateEmailException ex)
            {
                return Conflict(new { Error = ex.Message }); // 409
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Unexpected error occurred", Details = ex.Message }); // 500
            }
        }

        [HttpGet("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Error = "Invalid request" });
            }

            try
            {
                var login = await UsersService.LoginUserAsync(request);
                if (login == null)
                {
                    return Unauthorized(new { Error = $"Invalid email ({request.Email}) or password ({request.Password})" });
                }

                /*
                * Generate JWT token
                * Note: In a real-world application, you might want to include more claims in the token.
                * Here, we are just using the user's email as the identifier.
                * You can modify this as per your requirements.
                * Also, ensure that the AuthService is properly implemented to generate JWT tokens.
                * This is a simplified example for demonstration purposes.
                * Make sure to handle exceptions and errors appropriately in production code.
                * Additionally, consider using HTTPS to secure the transmission of sensitive data like JWT tokens.
                * This example assumes that the AuthService.GenerateJwtToken method is implemented correctly.
                * Adjust the implementation based on your specific authentication and authorization requirements.
                * Always follow best practices for security and data protection when handling user authentication.
                * This example is for educational purposes and may need further enhancements for production use.
                * Ensure that you have proper error handling and logging mechanisms in place for better maintainability.
                * Review and test the authentication flow thoroughly before deploying to a live environment.
                * Keep your dependencies up to date to mitigate potential security vulnerabilities.
                * Regularly audit your authentication and authorization mechanisms to ensure compliance with security standards.
                * Stay informed about the latest security practices and updates in the authentication domain.
                * Consider implementing additional security measures such as multi-factor authentication (MFA) for enhanced protection.
                * Always prioritize user privacy and data security in your application design and implementation.
                * This example serves as a starting point; adapt it to fit your application's specific needs and requirements.
                * Consult with security experts if necessary to ensure robust authentication practices are in place.
                * Remember that security is an ongoing process; continuously monitor and improve your authentication strategies.
                * Thank you for considering these important aspects of user authentication and security in your application development.
                */
                var (user, jwt, expiresAt) = await AuthService.GenerateJwtToken(login.Id);

                if (string.IsNullOrEmpty(jwt) || user == null)
                {
                    return Unauthorized(new { Error = $"Could not generate JWT token. User found: {user != null}" });
                }

                user = await UsersService.UpdateUserAsync(user.Id, new EditUserRequest { LastLogin = DateTime.UtcNow, JwtToken = jwt });
                return Ok(new LoginResponse { User = user, Token = jwt, ExpiresAt = expiresAt });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] EditUserRequest request)
        {
            if (string.IsNullOrEmpty(id) || request == null)
            {
                return BadRequest("Invalid id or request.");
            }

            try
            {
                var user = await UsersService.UpdateUserAsync(id, request);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                return Ok(user);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Invalid user ID.");
            }

            try
            {
                var result = await UsersService.DeleteUserAsync(id);
                if (!result)
                {
                    return NotFound("User not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
    
    public sealed class DuplicateEmailException : Exception
    {
        public DuplicateEmailException(string email)
            : base($"A user with email '{email}' already exists.") { }
    }
}