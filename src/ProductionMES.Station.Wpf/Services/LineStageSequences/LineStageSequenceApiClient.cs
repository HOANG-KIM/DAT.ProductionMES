using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.LineStageSequences;

/// <inheritdoc cref="ILineStageSequenceApiClient"/>
public class LineStageSequenceApiClient : ApiClientBase, ILineStageSequenceApiClient
{
    public LineStageSequenceApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<LineStageSequenceDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/lines/{lineId}/stage-sequence");
        return await SendAsync<IReadOnlyList<LineStageSequenceDto>>(request, cancellationToken);
    }

    public async Task<LineStageSequenceDto> AddStageAsync(int lineId, int stageId, int? sequenceNumber, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/lines/{lineId}/stage-sequence")
        {
            Content = JsonContent.Create(new AddStageToLineRequest { StageId = stageId, SequenceNumber = sequenceNumber }, options: JsonDefaults.Options),
        };
        return await SendAsync<LineStageSequenceDto>(request, cancellationToken);
    }

    public async Task RemoveStageAsync(int lineId, int stageId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/lines/{lineId}/stage-sequence/{stageId}");
        await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<LineStageSequenceDto>> ReorderAsync(
        int lineId, IReadOnlyList<(int StageId, int SequenceNumber)> items, CancellationToken cancellationToken = default)
    {
        var body = new ReorderLineStageSequenceRequest
        {
            Items = items.Select(i => new ReorderLineStageSequenceItem { StageId = i.StageId, SequenceNumber = i.SequenceNumber }).ToList(),
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/lines/{lineId}/stage-sequence/reorder")
        {
            Content = JsonContent.Create(body, options: JsonDefaults.Options),
        };
        return await SendAsync<IReadOnlyList<LineStageSequenceDto>>(request, cancellationToken);
    }
}
