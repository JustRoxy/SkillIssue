using MediatR;
using Microsoft.Extensions.Logging;

namespace SkillIssue.Application;

public class TestRequest : IRequest;

public class TestRequestHandler : IRequestHandler<TestRequest>
{
    private readonly ILogger<TestRequestHandler> _logger;

    public TestRequestHandler(ILogger<TestRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TestRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received ping at {Now}", DateTime.Now);
        await Task.Delay(100, cancellationToken);
    }
}