using System.Text;
using KiloTx.ArrlBulletins;
using Microsoft.AspNetCore.Mvc;
using KiloTx.FeedService.ArrlBulletins;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<DataContextOptions>(
    builder.Configuration.GetSection(DataContextOptions.SectionName));
builder.Services.AddScoped<DataContext>();

builder.Services.Configure<ArrlOptions>(
    builder.Configuration.GetSection(ArrlOptions.SectionName));
builder.Services.AddScoped<ArrlFeedBuilder>();

var app = builder.Build();

app.MapGet("/", () => "/arrl/bulletins/[yyyy] | /arrl/bulletins/[yyyy]/[mm]  | /arrl/bulletins/recent/[days] | /arrl/bulletins/update");
app.MapGet("/arrl/bulletins/{year:int:length(4)}", GetFeed);
app.MapGet("/arrl/bulletins/{year:int:length(4)}/{month:int:length(1,2)}", GetFeed);
app.MapGet("/arrl/bulletins/recent/{days:int}", GetRecentFeed);
app.MapGet("/arrl/bulletins/update", Update);

(DateOnly startDate, DateOnly endDate) GetDateRange(int year, int? month)
{
    var startDate = new DateOnly(year, month ?? 1, 1);

    if (month.HasValue)
        return (startDate, startDate.AddMonths(1).AddDays(-1));
    
    return (startDate, startDate.AddYears(1).AddDays(-1));
}

async Task<object?> GetFeed(
    int year, 
    int? month, 
    HttpContext context, 
    [FromServices]ArrlFeedBuilder feedBuilder)
{
    if (year < 1995 || year > DateTime.Now.Year + 1 || month is < 1 or > 12)
    {
        context.Response.StatusCode = 404;
        return "Date is invalid";
    }

    var (startDate, endDate) = GetDateRange(year, month);
    context.Response.ContentType = "application/rss+xml";
    return await feedBuilder.BuildRssFeed(startDate, endDate);
}


async Task<object?> GetRecentFeed(
    int days, 
    HttpContext context, 
    [FromServices]ArrlFeedBuilder feedBuilder)
{
    if (days > 366)
    {
        context.Response.StatusCode = 404;
        return "Days must be within 366 days";
    }

    var startDate = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(-days));
    var endDate = DateOnly.FromDateTime(DateTime.Now.Date);
    context.Response.ContentType = "application/rss+xml";
    return await feedBuilder.BuildRssFeed(startDate, endDate);
}

async Task Update(HttpContext context, [FromServices]ArrlFeedBuilder feedBuilder, CancellationToken cancellationToken)
{
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/plain";
    var writer = context.Response.BodyWriter;
    
    async Task Write(string message) =>
        await writer!.WriteAsync(Encoding.UTF8.GetBytes(message));

    await Write("Starting");

    await feedBuilder.UpdateCategories(Write, cancellationToken);
    
    await context.Response.CompleteAsync();
}


app.Run();