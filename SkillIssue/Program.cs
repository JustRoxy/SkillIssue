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
using SkillIssue.API.Commands;
using SkillIssue.Authorization;
using SkillIssue.Database;
using SkillIssue.Domain.Services;
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
if (cliParser.Value == default) return;

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

    //TODO: migrate useful migrations to DomainMigrations
    //
    // var handler =
    //     new NotifyGuildChannelOnRatingUpdate(database, scope.ServiceProvider.GetRequiredService<IDiscordClient>());
    // var match = database.Matches.First(x => x.MatchId == 112602542);
    // var ratingUpdates = database.RatingHistories.Where(x => x.MatchId == 112602542).ToList();
    // var playerHistory = database.PlayerHistories.Where(x => x.MatchId == 112602542).ToList();
    // await handler.Handle(new MatchCalculated
    // {
    //     Match = match,
    //     RatingChanges = ratingUpdates,
    //     PlayerHistories = playerHistory
    // }, new CancellationToken());
    // var ppcMigration =
    //     new PeePeeCee.AlphaMigration(database,
    //         scope.ServiceProvider.GetRequiredService<ILogger<PeePeeCee.AlphaMigration>>());
    // await ppcMigration.AddArtistAndSongName();
    // await ppcMigration.MigrateFromTGML();
    // await ppcMigration.FetchMissingBeatmaps(scope.ServiceProvider.GetRequiredService<BeatmapLookup>());
    // await ppcMigration.CalculateCurrentPerformances();

    // var tgmlMigration = new AlphaMigration(database,
    //     scope.ServiceProvider.GetRequiredService<ILogger<AlphaMigration>>(),
    //     scope.ServiceProvider.GetRequiredService<TheGreatArchiver>());
    // await tgmlMigration.NullGames();

    // var tgsMigration = new AlphaMigration(app.Services.GetRequiredService<IServiceScopeFactory>(),
    //     scope.ServiceProvider.GetRequiredService<ILogger<AlphaMigration>>());
    // await tgsMigration.MigrateFromTgml();
    // return;
}

var domainMigrationRunner = app.Services.GetRequiredService<DomainMigrationRunner>();
await domainMigrationRunner.RunDomainMigrations(cliParser.Value);

#endregion


app.UseHttpsRedirection();

// app.MapGet("/matches/{matchId:int}",
//     async ([FromServices] DatabaseContext context, int matchId, HttpContext httpContext) =>
//     {
//         var match = await context.TgmlMatches
//             .Where(x => x.MatchId == matchId)
//             .Select(x => x.CompressedJson)
//             .FirstOrDefaultAsync();
//         httpContext.Response.Headers.ContentEncoding = "br";
//         return match is null ? Results.NotFound() : Results.File(match);
//     });

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

app.MapPost("/settings", async () => { });
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