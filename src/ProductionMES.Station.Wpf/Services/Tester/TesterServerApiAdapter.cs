using System.Net.Http;
using ProductionMES.Station.Tester.Station;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.Scans;

namespace ProductionMES.Station.Wpf.Services.Tester;

/// <summary>
/// Cầu nối <see cref="ITesterServerApi"/> (thư viện Station.Tester, không biết HTTP) với các API client của Station.Wpf.
/// Mọi lỗi mạng/HTTP được đổi thành exception với <see cref="Exception.Message"/> là thông báo tiếng Việt sẵn để hiển thị
/// (không có hàng đợi offline — US-12 AC14).
/// </summary>
public class TesterServerApiAdapter : ITesterServerApi
{
    private readonly IScanApiClient _scanApiClient;
    private readonly ITesterApiClient _testerApiClient;
    private readonly StationOptions _options;

    public TesterServerApiAdapter(IScanApiClient scanApiClient, ITesterApiClient testerApiClient, StationOptions options)
    {
        _scanApiClient = scanApiClient;
        _testerApiClient = testerApiClient;
        _options = options;
    }

    public async Task<TagCheckResult> CheckTagAsync(string tagCode, CancellationToken cancellationToken)
    {
        var dto = await WrapAsync(() => _scanApiClient.CheckAsync(tagCode, _options.WorkStationId, cancellationToken));
        return dto.Result == ScanResult.Ok
            ? new TagCheckResult(true, Payload: dto)
            : new TagCheckResult(false, dto.RejectionReason ?? "Scan bị từ chối.", dto);
    }

    public async Task<TesterSaveResult> SaveTesterScanAsync(TesterScanSave save, CancellationToken cancellationToken)
    {
        var request = new CreateTesterScanRequest
        {
            TagCode = save.TagCode,
            WorkStationId = _options.WorkStationId,
            TesterBootId = save.BootId,
            TesterSeq = save.Seq,
            GoldenCrc = save.GoldenCrc,
            FirmwareVersion = save.FirmwareVersion,
            TestedAt = save.TestedAt,
        };

        var dto = await WrapAsync(() => _scanApiClient.CreateTesterScanAsync(request, cancellationToken));
        return dto.Result == ScanResult.Ok
            ? new TesterSaveResult(true, Payload: dto)
            : new TesterSaveResult(false, dto.RejectionReason ?? "Server từ chối lưu lượt scan.", dto);
    }

    public Task RecordEventAsync(TesterEvent testerEvent, CancellationToken cancellationToken)
    {
        var request = new RecordTesterEventRequest
        {
            EventType = testerEvent.Type,
            TagCode = testerEvent.TagCode,
            TesterSeq = testerEvent.Seq,
            Detail = testerEvent.Detail,
            ApprovedByUserName = testerEvent.ApprovedBy,
        };

        return _testerApiClient.RecordEventAsync(request, cancellationToken);
    }

    private static async Task<T> WrapAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (ApiException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(NetworkErrorMessage.ForConnectionFailure(ex), ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException(NetworkErrorMessage.ForTimeout(), ex);
        }
    }
}
