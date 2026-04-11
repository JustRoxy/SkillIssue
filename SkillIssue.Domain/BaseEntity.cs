// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel.DataAnnotations.Schema;
using SkillIssue.Domain.Events;

namespace SkillIssue.Domain;

public class BaseEntity
{
    [NotMapped] private readonly List<BaseEvent> _events = [];
    [NotMapped] public IReadOnlyCollection<BaseEvent> Events => _events.AsReadOnly();

    public void AddDomainEvent(BaseEvent @event)
    {
        _events.Add(@event);
    }

    public void ClearDomainEvents()
    {
        _events.Clear();
    }
}