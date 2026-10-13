using System.Text;

namespace ProductionMES.Station.Tester.Protocol;

/// <summary>
/// Ghép dòng từ luồng byte đọc được ở cổng COM (US-14 AC10, spec-app mục 5.2): ghép theo <c>\n</c>, cắt <c>\r</c> cuối dòng;
/// dòng dài hơn <see cref="ProtocolParser.MaxLineLength"/> byte (kể cả <c>\n</c>) mà chưa gặp <c>\n</c> thì bỏ cả dòng — bỏ
/// qua mọi byte cho tới <c>\n</c> kế tiếp rồi mới đồng bộ lại. Không thread-safe: chỉ gọi từ luồng đọc.
/// </summary>
public sealed class LineAssembler
{
    private readonly StringBuilder _buffer = new();
    private bool _overflow;

    /// <summary>Đưa <paramref name="count"/> byte vào; mỗi dòng đủ gọi <paramref name="onLine"/>, mỗi dòng quá dài bị bỏ gọi <paramref name="onOverflow"/>.</summary>
    public void Feed(byte[] data, int offset, int count, Action<string> onLine, Action? onOverflow = null)
    {
        for (var i = offset; i < offset + count; i++)
        {
            var b = data[i];
            if (b == (byte)'\n')
            {
                if (_overflow)
                {
                    _overflow = false;
                    _buffer.Clear();
                    onOverflow?.Invoke();
                    continue;
                }

                var line = _buffer.ToString().TrimEnd('\r');
                _buffer.Clear();
                onLine(line);
                continue;
            }

            if (_overflow)
            {
                continue;
            }

            // Nội dung + '\n' không được vượt quá MaxLineLength.
            if (_buffer.Length >= ProtocolParser.MaxLineLength - 1)
            {
                _overflow = true;
                _buffer.Clear();
                continue;
            }

            // ASCII: byte >= 0x80 (rác bootloader) vẫn đưa vào dưới dạng ký tự Latin-1 để dòng đó bị xếp vào "không phải giao thức".
            _buffer.Append((char)b);
        }
    }

    /// <summary>Bỏ dữ liệu đang ghép dở (khi đóng/mở lại cổng).</summary>
    public void Reset()
    {
        _buffer.Clear();
        _overflow = false;
    }
}
