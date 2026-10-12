namespace ProductionMES.Station.Tester;

/// <summary>
/// Abstraction ghi log của thư viện máy thông điện (Station.Wpf hiện thực bằng Serilog ghi file theo ngày — US-14 AC11:
/// log serial vào/ra mức Debug ra FILE, KHÔNG ghi vào DB).
/// </summary>
public interface ITesterLog
{
    void Debug(string message);

    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}

/// <summary>Bỏ qua mọi log (dùng trong test).</summary>
public sealed class NullTesterLog : ITesterLog
{
    public static readonly NullTesterLog Instance = new();

    public void Debug(string message)
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
