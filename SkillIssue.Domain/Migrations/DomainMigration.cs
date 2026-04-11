// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Domain.Migrations;

public abstract class DomainMigration
{
    public abstract string MigrationName { get; }
    public event Action<Progress>? OnProgress;

    protected abstract Task OnMigration();

    public Task Migrate()
    {
        return OnMigration();
    }

    protected void Progressed(Progress obj)
    {
        OnProgress?.Invoke(obj);
    }

    public class Progress
    {
        public string MigrationStage { get; set; } = "";
        public int Processed { get; set; }
        public int Total { get; set; }
    }
}