using MediatR;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Models;

namespace SkillIssue.Matches.Queries.GetPageFromAPI;

public class GetPageFromAPIHandler(IHttpClientFactory clientFactory) : IRequestHandler<GetPageFromAPIRequest, Page>
{
    private readonly HttpClient _client = clientFactory.CreateClient(Constants.HTTP_CLIENT);

    public async Task<Page> Handle(GetPageFromAPIRequest request, CancellationToken cancellationToken)
    {
        var page = await _client.GetFromJsonAsync<Page>($"matches?sort=id_asc&cursor[match_id]={request.Cursor}",
            cancellationToken);

        return page!;
    }
}