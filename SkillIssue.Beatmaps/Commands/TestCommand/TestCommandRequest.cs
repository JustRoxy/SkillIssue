using MediatR;

namespace SkillIssue.Beatmaps.Commands.TestCommand;

public class TestCommandRequest : IRequest
{
    public required string Message { get; set; }
}