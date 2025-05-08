using GeekApp.Shared.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace GeekApp.Server.Models
{
    public class ApiUser : User
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public ApiUser()
        {
            UserId = Guid.NewGuid().ToString(); // Auto-generate UserId

        }
    }
}
