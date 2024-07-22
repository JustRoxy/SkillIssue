using MediatR;
using SkillIssue.Application.Commands.Stage5ComposeUpdatedData.Contracts;

namespace SkillIssue.Application.Commands.Stage5ComposeUpdatedData;

public class ComposeUpdatedDataHandler : IRequestHandler<ComposeUpdatedDataRequest>
{
    public ComposeUpdatedDataHandler()
    {
    }

    public Task Handle(ComposeUpdatedDataRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}