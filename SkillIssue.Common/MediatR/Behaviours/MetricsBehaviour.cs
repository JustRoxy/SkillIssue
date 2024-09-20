using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SkillIssue.Common.MediatR.Behaviours;

public class MetricsBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ILogger<MetricsBehaviour<TRequest, TResponse>> _logger;

    public MetricsBehaviour(ILogger<MetricsBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        var response = await next();

        var elapsed = Stopwatch.GetElapsedTime(startTime);

        _logger.LogInformation("{RequestName} executed in {ElapsedMilliseconds}ms", request.GetType().Name,
            elapsed.TotalMilliseconds);

        return response;
    }
}