using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UsersService.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("FirstName")]
        public string FirstName { get; set; } = string.Empty;
        [BsonElement("LastName")]
        public string LastName { get; set; } = string.Empty;
        [BsonElement("Email")]
        public string Email { get; set; } = string.Empty;
        [BsonElement("HashesPassword")]
        public string HashedPassword { get; set; } = string.Empty;
        [BsonElement("Role")]
        public Role Role { get; set; } = Role.Guest;
        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [BsonElement("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
        [BsonElement("BirthDate")]
        public DateTime? BirthDate { get; set; }
        [BsonElement("LastLogin")]
        public DateTime? LastLogin { get; set; } = DateTime.MinValue;
        public string JwtToken { get; set; } = string.Empty;
    }
    
    public enum Role
    {
        Admin,
        User,
        Guest
    }
}