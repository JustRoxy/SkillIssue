using MediatR;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Models;

namespace SkillIssue.Matches.Queries.GetPageFromAPI;

public class GetPageFromAPIRequest : IRequest<Page>
{
    public long Cursor { get; set; }
}