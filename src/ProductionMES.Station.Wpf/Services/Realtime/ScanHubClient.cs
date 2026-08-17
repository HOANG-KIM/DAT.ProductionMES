using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Realtime;

/// <inheritdoc cref="IScanHubClient"/>
public class ScanHubClient : IScanHubClient, IAsyncDisposable
{
    /// <summary>Mirror <c>ScanHub.ScanRecordedMethodName</c> phía backend — Station.Wpf không reference project backend nào.</summary>
    private const string ScanRecordedMethodName = "ScanRecorded";

    /// <summary>Mirror tên method Hub <c>JoinStationGroupAsync</c> phía backend.</summary>
    private const string JoinStationGroupMethodName = "JoinStationGroupAsync";

    private readonly StationOptions _options;
    private readonly HubConnection _connection;

    public event Action<ScanResultDto>? ScanRecorded;

    public ScanHubClient(StationOptions options)
    {
        _options = options;
        // Server (Program.cs AddSignalR().AddJsonProtocol(...)) đã đồng bộ enum Result ra dạng chuỗi giống HTTP
        // (JsonDefaults.Options) — cần thêm JsonStringEnumConverter ở đây để đọc đúng, mặc định JsonHubProtocol
        // chỉ đọc enum dạng số.
        _connection = new HubConnectionBuilder()
            .WithUrl(options.SignalRHubUrl)
            .WithAutomaticReconnect()
            .AddJsonProtocol(jsonOptions =>
                jsonOptions.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        _connection.On<ScanResultDto>(ScanRecordedMethodName, dto => ScanRecorded?.Invoke(dto));

        // Kết nối lại tự động (mất mạng tạm thời) chỉ phục hồi được connection, không tự join lại group — phải
        // tự gọi lại JoinStationGroupAsync mỗi khi Reconnected để tiếp tục nhận sự kiện đúng trạm.
        _connection.Reconnected += _ => JoinStationGroupAsync();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.StartAsync(cancellationToken);
            await JoinStationGroupAsync();
        }
        catch
        {
            // AC2 là realtime "cộng thêm" cho luồng scan cơ bản — nếu hub chưa chạy được/mất mạng lúc khởi động,
            // không được làm crash toàn app (scan qua HTTP vẫn hoạt động độc lập). Không có hạ tầng logger dùng
            // chung trong Station.Wpf ở thời điểm này (US-16 sẽ bổ sung hàng đợi cục bộ + log), nên tạm nuốt lỗi.
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _connection.StopAsync();
        }
        catch
        {
            // Best-effort khi app đang đóng — không cần xử lý gì thêm.
        }
    }

    private Task JoinStationGroupAsync() => _connection.InvokeAsync(JoinStationGroupMethodName, _options.WorkStationId);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
