using System.Net.Http.Json;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatchPage;

public class GetMatchPageHandler
{
    private readonly HttpClient _client;
    private readonly OsuRequestBuilder _requestBuilder;

    public GetMatchPageHandler(HttpClient client, OsuRequestBuilder requestBuilder)
    {
        _client = client;
        _requestBuilder = requestBuilder;
    }

    public async Task<GetMatchPageResponse> Handle(GetMatchPageRequest handlerRequest,
        CancellationToken cancellationToken)
    {
        var request = _requestBuilder.Create(HttpMethod.Get,
            $"matches?sort=id_asc&cursor[match_id]={handlerRequest.Cursor}");

        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<GetMatchPageResponse>(cancellationToken);
        return content!;
    }
}