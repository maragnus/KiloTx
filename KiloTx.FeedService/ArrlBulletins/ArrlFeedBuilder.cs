using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using KiloTx.ArrlBulletins;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KiloTx.FeedService.ArrlBulletins;

public class ArrlOptions
{
    public const string SectionName = "ArrlBulletins";
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Url { get; set; } = null!;
    
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, string> CategoryUrls { get; set; } = new();
    
    /// <summary>This is how many URLs are downloaded concurrently</summary>
    public int Concurrency { get; set; } = 4;
}

public class ArrlFeedBuilder
{
    private readonly ArrlOptions _options;
    private readonly DataContext _dataContext;

    public ArrlFeedBuilder(IOptions<ArrlOptions> options, DataContext dataContext)
    {
        _options = options.Value;
        _dataContext = dataContext;
    }

    private record BulletinInfo(string Category, DateOnly Date, string Url, string Name, string Title);
    
    public async Task UpdateCategories(Func<string, Task> write, CancellationToken cancellationToken)
    {
        await write("Aggregating bulletins...\r\n");
        var bulletins = await AggregateBulletins(cancellationToken);
        await write(
            $"Found {bulletins.Count} across {bulletins.Select(b => b.Category).Distinct().Count()} categories.\r\n");

        await Parallel.ForEachAsync(
            bulletins,
            new ParallelOptions
                { MaxDegreeOfParallelism = _options.Concurrency, CancellationToken = cancellationToken },
            async (bulletin, ct) => { await UpdateBulletin(bulletin, write, ct); }
        );
    }

    private async Task<List<BulletinInfo>> AggregateBulletins(CancellationToken cancellationToken)
    {
        var bulletins = new List<BulletinInfo>(1024);

        foreach (var (category, categoryUrl) in _options.CategoryUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var http = new HttpClient();
            http.BaseAddress = new Uri("http://www.arrl.org");
            var body = await http.GetStringAsync(categoryUrl, cancellationToken);

            var matches = Regex.Matches(body, @"<p>(2[\d-]+) <a href=""([^""]+)"">([^<]+)</a> (.*?)</p>",
                RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            bulletins.AddRange(matches.Select(match =>
            {
                var date = DateOnly.ParseExact(match.Groups[1].Value, "yyyy-MM-dd");
                var url = match.Groups[2].Value;
                var name = match.Groups[3].Value;
                var title = match.Groups[4].Value;
                return new BulletinInfo(category, date, url, name, title);
            }));
        }

        return bulletins;
    }

    private async Task UpdateBulletin(BulletinInfo bulletin, Func<string, Task> write,
        CancellationToken cancellationToken)
    {
        var http = new HttpClient();
        http.BaseAddress = new Uri("http://www.arrl.org");

        var exists = await _dataContext.Bulletins.Find(x => x.Date == bulletin.Date && x.Name == bulletin.Name)
            .AnyAsync(cancellationToken);
        if (exists) return;

        await write($"Downloading {bulletin.Date} {bulletin.Name} in {bulletin.Category} category...\r\n");

        var page = await http.GetStringAsync(bulletin.Url, cancellationToken);
        var match = Regex.Match(page, "<pre>(.*)</pre>",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var content = match.Groups[1].Value;

        var result = new Bulletin()
        {
            Id = ObjectId.GenerateNewId(),
            Category = bulletin.Category,
            Date = bulletin.Date,
            Name = bulletin.Name,
            Title = bulletin.Title,
            Content = WebUtility.HtmlDecode(content),
            Url = bulletin.Url
        };

        await _dataContext.Bulletins.InsertOneAsync(result, new InsertOneOptions(), cancellationToken);
    }

    public async Task<SyndicationFeed> BuildSyndicationFeed(DateOnly startDate, DateOnly? endDate)
    {
        endDate ??= DateOnly.MaxValue;

        var bulletins = await _dataContext.Bulletins
            .Find(x => x.Date >= startDate && x.Date <= endDate)
            .ToListAsync();

        var feed = new SyndicationFeed(_options.Title, _options.Description, new Uri(_options.Url));

        var categories = bulletins
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToDictionary(x => x, x => new SyndicationCategory(x));

        var items = bulletins
            .Select(bulletin =>
                new SyndicationItem
                {
                    Title = new TextSyndicationContent(bulletin.Title, TextSyndicationContentKind.Plaintext),
                    Content = new TextSyndicationContent(bulletin.Content, TextSyndicationContentKind.Plaintext),
                    BaseUri = new Uri($"{_options.Url}/{bulletin.Url}"),
                    PublishDate = bulletin.Date.ToDateTime(new TimeOnly(0, 0)),
                    Categories = { categories[bulletin.Category] }
                })
            .ToList();

        feed.Items = items;

        return feed;
    }

    public async Task<string> BuildRssFeed(DateOnly startDate, DateOnly? endDate)
    {
        var feed = await BuildSyndicationFeed(startDate, endDate);

        var xml = new StringBuilder();
        await using (var writer = XmlWriter.Create(xml, new XmlWriterSettings { Async = true }))
        {
            var rssFormatter = new Rss20FeedFormatter(feed);
            rssFormatter.WriteTo(writer);
        }

        return xml.ToString();
    }
}