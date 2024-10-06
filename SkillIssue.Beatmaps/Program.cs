using SkillIssue.Beatmaps.Commands.TestCommand;
using SkillIssue.Common;
using SkillIssue.Common.Broker;
using SkillIssue.Common.Database;
using SkillIssue.Common.Messages;

namespace SkillIssue.Beatmaps;

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
        builder.Services.RegisterContext<BeatmapContext>(builder.Configuration, BeatmapContext.SCHEMA);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        await app.Services.RunMigrations<BeatmapContext>();

        app.Services.RegisterConsumer<TestMessage, TestCommandRequest>("test-queue", message => new TestCommandRequest
            {
                Message = message.Message,
            })
            .RegisterConsumer<TestMessage, TestCommandRequest>("test-queue-2", message => new TestCommandRequest
            {
                Message = message.Message,
            });

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