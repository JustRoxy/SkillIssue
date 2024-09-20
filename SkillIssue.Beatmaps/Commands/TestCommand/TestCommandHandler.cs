using MediatR;

namespace SkillIssue.Beatmaps.Commands.TestCommand;

public class TestCommandHandler : IRequestHandler<TestCommandRequest>
{
    private readonly ILogger<TestCommandHandler> _logger;

    public TestCommandHandler(ILogger<TestCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TestCommandRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Test command handler. Message = {Message}", request.Message);

        return Task.CompletedTask;
    }
}