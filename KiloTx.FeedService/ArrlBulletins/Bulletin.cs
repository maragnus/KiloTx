using MongoDB.Bson.Serialization.Attributes;

namespace KiloTx.FeedService.ArrlBulletins;

public class Bulletin
{
    [BsonId]
    public object? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string Content { get; set; } = null!;
    public string Url { get; set; } = null!;
}