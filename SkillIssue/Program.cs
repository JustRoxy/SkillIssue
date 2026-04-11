// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using System.Text.Json;
using CommandLine;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenSkill.Models;
using PlayerPerformanceCalculator.Services;
using Polly;
using Serilog;
using Serilog.Events;
using SkillIssue;
using SkillIssue.API.Commands.Compliance;
using SkillIssue.API.Commands.Integrations;
using SkillIssue.Authorization;
using SkillIssue.Database;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Services;
using SkillIssue.Domain.TGML.Entities;
using SkillIssue.Domain.Unfair;
using SkillIssue.Integrations.Spreadsheet;
using TheGreatMultiplayerLibrary.HttpHandlers;
using TheGreatMultiplayerLibrary.Services;
using TheGreatSpy.Handlers;
using TheGreatSpy.HostedServices;
using TheGreatSpy.Services;
using Unfair;
using Unfair.Seeding;
using Unfair.Services;
using Unfair.Strategies;
using AlphaMigration = TheGreatMultiplayerLibrary.AlphaMigration;
using DiscordConfig = SkillIssue.DiscordConfig;
using Options = OpenSkill.Options;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var cliParser = Parser.Default.ParseArguments<DomainMigrationOptions>(args);
if (cliParser.Value == null) return;

Console.WriteLine($"Running with arguments: {JsonSerializer.Serialize(cliParser.Value, new JsonSerializerOptions()
{
    WriteIndented = true
})}");
#if !DEBUG
builder.Services.AddHostedService<TheGreatWatcher>();
builder.Services.AddHostedService<TheGreatArchiving>();
builder.Services.AddHostedService<PlayerStatisticsService>();
#endif

if (builder.Environment.IsProduction())
{
    var logger = new LoggerConfiguration()
        .WriteTo.Async(x =>
            x.Discord(configuration.GetSection("Discord:OnError").GetValue<ulong>("WebhookId"),
                configuration.GetSection("Discord:OnError").GetValue<string>("Token")!,
                restrictedToMinimumLevel: LogEventLevel.Error))
        .WriteTo.Seq(builder.Configuration.GetSection("Seq:Url").Value!,
            LogEventLevel.Information,
            apiKey: builder.Configuration.GetSection("Seq:Token").Value)
        .WriteTo.Console(LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Default", LogEventLevel.Debug)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.AtLevel(LogEventLevel.Error,
            enrichmentConfiguration => enrichmentConfiguration.WithThreadName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessName()
                .Enrich.WithProcessId())
        .CreateLogger();

    Log.Logger = logger;
}
else
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Console()
        .CreateLogger();
}

builder.Host.UseSerilog();
builder.Services.Configure<ApiAuthorizationConfiguration>(configuration.GetSection("Authorization"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
DomainMigrationRunner.RegisterDomainMigrations(builder.Services);
builder.Services.AddMediatR(x =>
    x.RegisterServicesFromAssemblyContaining<BeatmapLookup>()
        .RegisterServicesFromAssemblyContaining<AlphaMigration>()
        .RegisterServicesFromAssemblyContaining<DiscordConfig>()
        .RegisterServicesFromAssemblyContaining<UnfairContext>()
        .RegisterServicesFromAssemblyContaining<UpdatePlayerInfoHandler>());

builder.Services.AddDbContext<DatabaseContext>(x =>
{
    var detailErrors = builder.Environment.IsDevelopment() ? "Include Error Detail=True;" : "";
    x.UseNpgsql($"{builder.Configuration.GetConnectionString("Postgres")}{detailErrors}",
            c => c.MigrationsAssembly("SkillIssue"))
        .UseSnakeCaseNamingConvention();

    if (builder.Environment.IsDevelopment()) x.EnableSensitiveDataLogging().EnableDetailedErrors();
});


builder.Services.AddTransient<PlayerService>();

builder.Services.AddTransient<BeatmapLookup>();
builder.Services.AddTransient<BeatmapProcessing>();
builder.Services.AddHttpClient<BeatmapLookup>(x => { x.BaseAddress = new Uri("https://osu.ppy.sh/"); });

builder.Services.AddScoped<TheGreatArchiver>();

builder.Services.Configure<TgmlRateLimitConfiguration>(builder.Configuration.GetSection("OsuSecrets:TGML"));
builder.Services.AddTransient<TgmlRateLimitHandler>();
builder.Services.Configure<TgsRateLimitConfiguration>(builder.Configuration.GetSection("OsuSecrets:TGS"));
builder.Services.AddTransient<TgsRateLimitHandler>();
builder.Services.AddTransient<ScoreProcessing>();

builder.Services.AddTransient<IPerformancePointsCalculator, DomainPerformancePointsCalculator>();
builder.Services.AddSingleton<PlayerStatisticsService>();

builder.Services.AddHttpClient<PlayerService>(x =>
    {
        x.BaseAddress = new Uri("https://osu.ppy.sh/api/v2/");
        x.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddHttpMessageHandler<TgsRateLimitHandler>()
    .AddTransientHttpErrorPolicy(x =>
        x.WaitAndRetryForeverAsync(iteration => TimeSpan.FromSeconds(iteration > 10 ? 30 : Math.Pow(2, iteration))));

builder.Services.AddHttpClient<TheGreatArchiver>(x =>
    {
        x.BaseAddress = new Uri("https://osu.ppy.sh/api/v2/");
        x.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddHttpMessageHandler<TgmlRateLimitHandler>()
    .AddTransientHttpErrorPolicy(x =>
        x.WaitAndRetryForeverAsync(iteration => TimeSpan.FromSeconds(iteration > 10 ? 30 : Math.Pow(2, iteration))));
builder.Services.AddSingleton<TheGreatArchiving>();
builder.Services.AddSingleton<TheGreatWatcher>();
builder.Services.AddSingleton(new OpenSkill.OpenSkill(new Options
{
    Gamma = x => 1d / x.K,
    Model = new BradleyTerryFull()
}));

builder.Services.AddTransient<IOpenSkillCalculator, OpenSkillCalculator>();

builder.Services.AddDiscord();

builder.Services.AddTransient<IBannedTournament, UnfairBannedTournament>();
builder.Services.AddTransient<UnfairContext>();
builder.Services.AddSingleton(new SpreadsheetProvider(builder.Configuration.GetValue<string>("Google:Spreadsheet")!));
builder.Services.AddSingleton<OneTimeStorage>();

builder.Services.Configure<DiscordConfig>(builder.Configuration.GetSection("Discord"));

#region Integrations

builder.Services.AddTransient<SpreadsheetIntegrationSettings>();
builder.Services.Configure<SpreadsheetIntegrationSettings>(
    builder.Configuration.GetSection("Integrations:Spreadsheets"));
builder.Services.AddScoped<SpreadsheetIntegration>();

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.RunDiscord(app.Environment.IsProduction());

#region Migrations And Seeding

using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    database.Database.SetCommandTimeout(TimeSpan.FromHours(10));
    await database.Database.MigrateAsync();
    await new UnfairSeeder(database).Seed();
}

var domainMigrationRunner = app.Services.GetRequiredService<DomainMigrationRunner>();
await domainMigrationRunner.RunDomainMigrations(cliParser.Value);

#endregion


app.UseHttpsRedirection();


app.MapPost("/history", async (
    IOptions<ApiAuthorizationConfiguration> allowedSources,
    [FromHeader] string source,
    [FromBody] GetHistoryRequest request,
    [FromServices] IMediator mediator
) =>
{
    if (!allowedSources.Value.IsAllowed(source)) return Results.StatusCode(403);
    DomainMigrationProgress.ThrowIfMigrationInProgress();
    var response = await mediator.Send(request);
    if (response.PlayersNotFound.Any())
        return Results.BadRequest(new
        {
            error = $"Players {string.Join(", ", response.PlayersNotFound)} does not exist"
        });

    return Results.Ok(new
    {
        response.Ratings
    });
});

app.MapGet("/compliance/lookup/{token}", async (
    string token,
    [FromServices] DatabaseContext context,
    [FromServices] OneTimeStorage oneTimeStorage,
    [FromServices] IMediator mediator,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
{
    if (!oneTimeStorage.Contains(token)) return Results.StatusCode(403);

    var request = oneTimeStorage.Get<string, LookupRatingsOnTimestampRequest>(token);
    if (request is null) return Results.BadRequest();

    var mediatorResponse = await mediator.Send(request, cancellationToken);

    var streamWriter = new CsvStreamWriter<LookupRatingsOnTimestampResponse.ResponseRating>("user_id,username,rating,last_updated_date", [
        x => x.UserId,
        x => x.Username,
        x => x.Rating,
        x => x.LastUpdateTime?.Date.ToString("yyyy-MM-dd")
    ]);

    await streamWriter.StreamToResponse(
        mediatorResponse.RatingStream,
        httpContext.Response,
        $"compliance_lookup_{request.Timestamp:yy_MM_dd}.csv",
        cancellationToken: cancellationToken
    );

    return Results.Empty;
});

app.MapGet("/compliance/match_listing/{token}", async (
    string token,
    [FromServices] DatabaseContext context,
    [FromServices] OneTimeStorage oneTimeStorage,
    [FromServices] IMediator mediator,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
{
    if (!oneTimeStorage.Contains(token)) return Results.StatusCode(403);

    var request = oneTimeStorage.Get<string, EnumerateMatchesOnTimestampRequest>(token);
    if (request is null) return Results.BadRequest();

    var mediatorResponse = await mediator.Send(request, cancellationToken);

    var streamWriter = new CsvStreamWriter<EnumerateMatchesOnTimestampResponse.AcceptedMatch>("match_id,name,start_time,end_time,reason", [
        x => x.MatchId,
        x => x.Name,
        x => x.EndTime.ToString("yyyy-MM-dd"),
        x => x.Reason
    ]);

    var statusString = request.Status switch
    {
        EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Accepted => "accepted",
        EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException()
    };

    await streamWriter.StreamToResponse(
        mediatorResponse.AcceptedMatchesStream,
        httpContext.Response,
        $"compliance_matches_{statusString}_{request.Timestamp:yy_MM_dd}.csv",
        cancellationToken: cancellationToken
    );

    return Results.Empty;
});

app.MapGet("/matches/{matchId:int}", async (IOptions<ApiAuthorizationConfiguration> allowedSources, int matchId, [FromHeader] string source,
    [FromServices] DatabaseContext databaseContext, HttpContext context) =>
{
    if (!allowedSources.Value.IsAllowed(source)) return Results.StatusCode(403);
    var match = await databaseContext.TgmlMatches.AsNoTracking().FirstOrDefaultAsync(x => x.MatchId == matchId);
    if (match?.CompressedJson is null) return Results.NotFound();

    context.Response.Headers.ContentEncoding = "br";
    return Results.Bytes(match.CompressedJson, "application/json");
});

app.MapGet("/matches", async (IOptions<ApiAuthorizationConfiguration> allowedSources,
    [FromQuery(Name = "cursor")] int? cursor,
    [FromQuery(Name = "is_active")] bool isActive,
    [FromHeader] string source,
    [FromServices] DatabaseContext databaseContext) =>
{
    if (!allowedSources.Value.IsAllowed(source)) return Results.StatusCode(403);

    var matches = await databaseContext.TgmlMatches
        .AsNoTracking()
        .OrderByDescending(x => x.MatchId)
        .Case(cursor is not null, x => x.Where(z => z.MatchId < cursor))
        .Case(isActive, x => x.Where(z => z.EndTime == null && z.MatchStatus != TgmlMatchStatus.Gone))
        .Take(100)
        .Select(x => new
        {
            x.MatchId,
            x.Name,
            x.StartTime,
            x.EndTime
        })
        .ToListAsync();

    return Results.Ok(new
    {
        Matches = matches,
        Cursor = matches.Last().MatchId
    });
});


app.MapGet("/ratings/{playerId:int}", async (
    IOptions<ApiAuthorizationConfiguration> allowedSources,
    int playerId,
    [FromHeader] string source,
    IMediator mediator,
    CancellationToken token) =>
{
    if (!allowedSources.Value.IsAllowed(source)) return Results.StatusCode(403);
    DomainMigrationProgress.ThrowIfMigrationInProgress();
    var response = await mediator.Send(new GetPlayerRatingsRequest
    {
        PlayerId = playerId
    }, token);
    if (response is null) return Results.NotFound(playerId);
    return Results.Ok(response);
});

app.MapGet("/integrations/spreadsheets/sip", async (HttpContext context,
    SpreadsheetIntegration integration,
    [FromQuery] int userId,
    [FromQuery] bool estimate = false,
    CancellationToken token = default) =>
{
    DomainMigrationProgress.ThrowIfMigrationInProgress();
    return await integration.GetSIP(context.Request, userId, estimate, token);
});

// bad endpoint naming design, but I can't be bothered before rewrite
app.MapGet("/integrations/spreadsheets/player_rating", async (HttpContext context,
    SpreadsheetIntegration integration,
    [FromQuery] int userId,
    [FromQuery] bool estimate = false,
    CancellationToken token = default) =>
{
    DomainMigrationProgress.ThrowIfMigrationInProgress();
    return await integration.GetPlayerRating(context.Request, userId, estimate, token);
});

app.MapGet("/integrations/spreadsheets/player_ratings", async (HttpContext context,
    SpreadsheetIntegration integration,
    [FromQuery] string username,
    [FromQuery] bool estimate = false,
    CancellationToken token = default) =>
{
    DomainMigrationProgress.ThrowIfMigrationInProgress();
    return await integration.GetPlayerRatings(context.Request, username, estimate, token);
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Oops");
}
finally
{
    Log.Fatal("Sukiru Ishue owari da...");
    await Log.CloseAndFlushAsync();
}
