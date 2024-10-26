using SkillIssue.Common;
using SkillIssue.Common.Broker;
using SkillIssue.Common.Database;
using SkillIssue.Common.Http;
using SkillIssue.Matches.Database;
using SkillIssue.Matches.Services;

namespace SkillIssue.Matches;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCommonServices(builder.Configuration);
        builder.Services.RegisterOsuAPIv2Client(Constants.HTTP_CLIENT);

        // builder.Services.RegisterContext<MatchesContext>(builder.Configuration, MatchesContext.SCHEMA);
        builder.Services.RegisterMongo(builder.Configuration);
        builder.Services.RegisterMongoRepository<MongoMatchesRepository>();

        builder.Services.AddSingleton<BackgroundPageUpdater>();
        builder.Services.AddSingleton<BackgroundMatchUpdater>();
        // builder.Services.AddHostedService<BackgroundPageUpdater>();
        builder.Services.AddHostedService<BackgroundMatchUpdater>();

        var app = builder.Build();

        // await app.Services.RunMigrations<MatchesContext>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        await app.Services.CreateCompressedCollections(MongoMatchesRepository.DATABASE_NAME,
            MongoMatchesRepository.MATCHES_COLLECTION);

        await app.Services.InitializeRepository<MongoMatchesRepository>();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = summaries[Random.Shared.Next(summaries.Length)]
                        })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

        await app.RunAsync();
    }
}