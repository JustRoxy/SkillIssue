namespace SkillIssue.Domain.Migrations;

public abstract class DomainMigration
{
    public abstract string MigrationName { get; }
    public event Action<Progress>? OnProgess;

    protected abstract Task OnMigration();

    public Task Migrate()
    {
        return OnMigration();
    }

    protected void Progressed(Progress obj)
    {
        OnProgess?.Invoke(obj);
    }

    public class Progress
    {
        public string MigrationStage { get; set; } = "";
        public int Processed { get; set; }
        public int Total { get; set; }
    }
}