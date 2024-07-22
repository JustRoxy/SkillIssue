using FluentMigrator.Runner;
using SkillIssue;
using SkillIssue.Application;
using SkillIssue.Infrastructure;
using SkillIssue.Scheduler;
using SkillIssue.ThirdParty.API.Osu;
using SkillIssue.ThirdParty.OsuGame;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterApplication(builder.Configuration);
builder.Services.RegisterInfrastructure(builder.Configuration);
builder.Services.RegisterOsu(builder.Configuration);
builder.Services.RegisterOsuCalculator(builder.Configuration);
builder.Services.AddScoped<ScriptingEnvironment>();

builder.Services.AddSingleton<JobScheduler>();
builder.Services.AddHostedService<JobScheduler>();
builder.Configuration.AddUserSecrets<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#region Migrations

using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrationRunner.MigrateUp();

    if (app.Environment.IsDevelopment())
        await scope.ServiceProvider.GetRequiredService<ScriptingEnvironment>().ExecuteScript();
}

#endregion

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

namespace SkillIssue
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}