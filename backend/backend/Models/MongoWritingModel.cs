using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models;

public class WritingEssay
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("content")]
    public string Content { get; set; }
    
    [BsonElement("targetBandScore")]
    public string TargetBandScore { get; set; }

    [BsonElement("bandScore")]
    public string BandScore { get; set; }

    [BsonElement("feedback")]
    public string Feedback { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}