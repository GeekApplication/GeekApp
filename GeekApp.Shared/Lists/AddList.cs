using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeekApp.Shared.Lists
{
    public class AddList
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsWatchlist { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonIgnoreIfNull]
        public List<ListItem> Items { get; set; } = new List<ListItem>();
    }
}
