using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models;

public class MongoListeningModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("topic")]
    public string Topic { get; set; }
    
    [BsonElement("content")]
    public string Content { get; set; }
    
    [BsonElement("bandScore")]
    public string BandScore { get; set; }

    [BsonElement("linkVideo")]
    public string LinkVideo { get; set; }

    [BsonElement("srtSubtitles")]
    public string SrtSubtitles { get; set; }
}

// Trong mã khởi tạo cơ sở dữ liệu
public class DatabaseInitializer
{
    private readonly IMongoCollection<MongoListeningModel> _collection;

    public DatabaseInitializer(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoListeningModel>("listening");

        // Tạo chỉ mục duy nhất cho LinkVideo
        CreateUniqueIndexForLinkVideo();
    }

    private async void CreateUniqueIndexForLinkVideo()
    {
        var indexKeysDefinition = Builders<MongoListeningModel>.IndexKeys.Ascending(model => model.LinkVideo);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<MongoListeningModel>(indexKeysDefinition, indexOptions);

        await _collection.Indexes.CreateOneAsync(indexModel);
    }
}