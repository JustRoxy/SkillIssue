namespace SkillIssue.Domain.Migrations;

public class DomainMigrationJournal
{
    public string MigrationName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsCompleted { get; set; }
}