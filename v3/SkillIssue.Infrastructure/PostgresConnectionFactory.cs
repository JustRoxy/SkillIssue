using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using SkillIssue.Common;
using SkillIssue.Infrastructure.Configuration;

namespace SkillIssue.Infrastructure;

public class PostgresConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresConnectionFactory(IOptions<ConnectionStringConfiguration> configuration)
    {
        if (configuration.Value.Postgres.IsNullOrWhiteSpace())
            throw new Exception("Failde to get postgres configuration");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.Value.Postgres);
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task<DbConnection> GetConnectionAsync()
    {
        try
        {
            var connection = _dataSource.CreateConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            return connection;
        }
        catch (Exception e)
        {
            throw new Exception("Failed to create new connection", e);
        }
    }
}