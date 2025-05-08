using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeekApp.Shared.Lists
{
    public class ListItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string ListId { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int TmdbId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
