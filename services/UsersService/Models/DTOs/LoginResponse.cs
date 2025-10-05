namespace UsersService.Models.DTOs
{
    public class LoginResponse
    {
        public User? User { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
