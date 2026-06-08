using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskApp.Domain.Enums;

namespace TaskApp.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    public UserRole Role { get; set; } = UserRole.User;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    [BsonElement("isLocked")]
    public bool IsLocked { get; set; } = false;

    [BsonElement("lockedAt")]
    public DateTime? LockedAt { get; set; }
}
