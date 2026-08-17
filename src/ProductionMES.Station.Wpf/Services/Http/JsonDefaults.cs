using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>Tùy chọn JSON dùng chung cho mọi lệnh gọi API — khớp quy ước camelCase + enum dạng chuỗi (API-Conventions.md mục 3, 10).</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
