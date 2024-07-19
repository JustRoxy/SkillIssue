namespace SkillIssue.Common.Exceptions;

public class SeriousValidationException(string message, Exception? exception = null)
    : Exception($"SERIOUS VALIDATION FAILED. ACTION IS REQUIRED IMMIDIATELY: {message}", exception);