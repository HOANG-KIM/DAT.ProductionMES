namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>
/// Chuẩn hoá message tiếng Việt cho lỗi mạng (không phải lỗi nghiệp vụ từ server) — dùng ở mọi ViewModel gọi
/// API, để lỗi kiểu sai địa chỉ/API chưa chạy/timeout luôn hiển thị rõ ràng thay vì rơi ra ngoài mọi catch
/// (chỉ bắt <see cref="ApiException"/>) khiến màn hình trông như không phản hồi gì.
/// </summary>
public static class NetworkErrorMessage
{
    public static string ForConnectionFailure(Exception ex) =>
        $"Không kết nối được tới server. Kiểm tra API đã chạy chưa và đúng địa chỉ (ApiBaseUrl) trong appsettings.json chưa.\n({ex.Message})";

    public static string ForTimeout() =>
        "Kết nối tới server quá lâu không phản hồi (timeout). Kiểm tra lại API/địa chỉ trong appsettings.json.";
}
