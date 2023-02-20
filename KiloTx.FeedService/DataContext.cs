using KiloTx.FeedService.ArrlBulletins;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KiloTx.ArrlBulletins;

public class DataContextOptions
{
    public const string SectionName = "Db";
    public string ConnectionString { get; set; } = null!;
    public string Database { get; set; } = null!;
}

public class DataContext
{
    public DataContext(IOptions<DataContextOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        var database = client.GetDatabase(options.Value.Database);
        Bulletins = database.GetCollection<Bulletin>(nameof(Bulletins));
    }

    public IMongoCollection<Bulletin> Bulletins { get; }
}