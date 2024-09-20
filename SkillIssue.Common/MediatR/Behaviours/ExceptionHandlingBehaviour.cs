using MediatR;
using Microsoft.Extensions.Logging;

namespace SkillIssue.Common.MediatR.Behaviours;

public class ExceptionHandlingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ILogger<ExceptionHandlingBehaviour<TRequest, TResponse>> _logger;

    public ExceptionHandlingBehaviour(ILogger<ExceptionHandlingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "!!!!!! A critical error occurred while handling the request {RequestName}",
                request.GetType().Name);
            return default!; //We assume most of our requests are passed from RabbitMQ
        }
    }
}