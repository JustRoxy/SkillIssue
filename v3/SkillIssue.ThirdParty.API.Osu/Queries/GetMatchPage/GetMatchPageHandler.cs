using System.Net.Http.Json;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatchPage;

public class GetMatchPageHandler(HttpClient client, OsuRequestBuilder requestBuilder)
{
    public async Task<GetMatchPageResponse> Handle(GetMatchPageRequest handlerRequest,
        CancellationToken cancellationToken)
    {
        var request = requestBuilder.Create(HttpMethod.Get,
            $"matches?sort=id_asc&cursor[match_id]={handlerRequest.Cursor}");

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<GetMatchPageResponse>(cancellationToken);
        return content!;
    }
}