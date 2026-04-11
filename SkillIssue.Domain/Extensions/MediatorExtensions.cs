// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using MediatR;

namespace SkillIssue.Domain.Extensions;

public static class MediatorExtensions
{
    public static async Task PublishAndClear(this IMediator mediator, IEnumerable<BaseEntity> entities)
    {
        foreach (var entity in entities)
        {
            var events = entity.Events.ToList();
            entity.ClearDomainEvents();

            foreach (var @event in events) await mediator.Publish(@event);
        }
    }
}