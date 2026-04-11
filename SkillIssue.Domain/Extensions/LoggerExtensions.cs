// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using Microsoft.Extensions.Logging;

namespace SkillIssue.Domain.Extensions;

public static class LoggerExtensions
{
    public static void LogAndThrow(this ILogger logger, Exception? exception, string template, params object[] objects)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        exception ??= new Exception(string.Format(template, objects));
        logger.LogCritical(exception, template, objects);
        throw exception;
    }
}