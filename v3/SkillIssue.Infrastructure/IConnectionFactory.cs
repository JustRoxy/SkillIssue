using System.Data.Common;

namespace SkillIssue.Infrastructure;

public interface IConnectionFactory
{
    public Task<DbConnection> GetConnectionAsync();
}