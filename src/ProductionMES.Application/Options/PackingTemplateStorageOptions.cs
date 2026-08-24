namespace ProductionMES.Application.Options;

/// <summary>
/// Cấu hình nơi lưu file mẫu tem in (template .xlsx, US-24) trên filesystem server (đọc qua Options pattern,
/// section "PackingTemplateStorage" trong appsettings). <see cref="BasePath"/> được tầng Api tính thành đường
/// dẫn TUYỆT ĐỐI lúc khởi động (kết hợp <c>IWebHostEnvironment.ContentRootPath</c> với đường dẫn tương đối cấu
/// hình — xem <c>Program.cs</c>) trước khi truyền vào đây, vì Infrastructure không reference ASP.NET Core Hosting.
/// </summary>
public class PackingTemplateStorageOptions
{
    public const string SectionName = "PackingTemplateStorage";

    /// <summary>
    /// Thư mục lưu file mẫu tem — đường dẫn TUYỆT ĐỐI khi Service sử dụng (Api tự resolve từ đường dẫn tương đối
    /// cấu hình, mặc định "App_Data/PackingTemplates" tính từ ContentRootPath). Thư mục không commit nội dung
    /// thật (tương tự appsettings.Development.json) nhưng phải tồn tại lúc chạy — Infrastructure tự tạo nếu
    /// chưa có (<c>Directory.CreateDirectory</c>, idempotent).
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}
