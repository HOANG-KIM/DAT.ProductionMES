using System.IO.Ports;
using System.Text;
using ProductionMES.Tester.Simulator;

// Công cụ giả lập máy thông điện ESP32 (US-14 / spec-app 12.2) để phát triển/kiểm thử Station.Wpf khi chưa có máy thật.
//
// Cách dùng với cặp cổng COM ảo (com0com, vd COM5 <-> COM6):
//   - Station.Wpf/appsettings.json: "Tester": { "PortName": "COM5", ... }
//   - Chạy công cụ này với cổng còn lại:  dotnet run --project tools/ProductionMES.Tester.Simulator -- COM6
// Phím điều khiển (giả lập thao tác tại máy):
//   i  đặt dây lên jig (bắt đầu thông điện)      p  kết quả PASS (OK)       f  kết quả FAIL (NG)
//   r  rút dây khỏi jig                           m  bật/tắt trả PONG (giả lập máy treo)
//   b  reset máy (EN): BOOT mới, SEQ về 0         g  bật/tắt "chưa có mẫu" (GM=0000)
//   s  in trạng thái                              q  thoát
if (args.Length < 1)
{
    Console.WriteLine("Cách dùng: ProductionMES.Tester.Simulator <COMx> [baud=115200]");
    return 1;
}

var portName = args[0];
var baud = args.Length > 1 && int.TryParse(args[1], out var b) ? b : 115200;

using var port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
{
    Handshake = Handshake.None,
    DtrEnable = false,
    RtsEnable = false,
    ReadTimeout = 200,
    WriteTimeout = 1000,
};

try
{
    port.Open();
}
catch (Exception ex)
{
    Console.WriteLine($"Không mở được {portName}: {ex.Message}");
    return 2;
}

var writeLock = new object();
var machine = new MachineSimulator();
machine.Send += line =>
{
    lock (writeLock)
    {
        try
        {
            var bytes = Encoding.ASCII.GetBytes(line + "\n");
            port.Write(bytes, 0, bytes.Length);
            Console.WriteLine($"  máy -> App: {line}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  (ghi lỗi: {ex.Message})");
        }
    }
};

using var cts = new CancellationTokenSource();

var reader = new Thread(() =>
{
    var buffer = new byte[256];
    var line = new StringBuilder();
    while (!cts.IsCancellationRequested)
    {
        int read;
        try
        {
            read = port.Read(buffer, 0, buffer.Length);
        }
        catch (TimeoutException)
        {
            continue;
        }
        catch
        {
            return;
        }

        for (var i = 0; i < read; i++)
        {
            if (buffer[i] == (byte)'\n')
            {
                var text = line.ToString().TrimEnd('\r');
                line.Clear();
                if (!text.StartsWith("#PING", StringComparison.Ordinal))
                {
                    Console.WriteLine($"  App -> máy: {text}");
                }

                machine.HandleLine(text);
            }
            else
            {
                line.Append((char)buffer[i]);
            }
        }
    }
})
{
    IsBackground = true,
};
reader.Start();

var ticker = new Thread(() =>
{
    while (!cts.IsCancellationRequested)
    {
        machine.Tick();
        Thread.Sleep(100);
    }
})
{
    IsBackground = true,
};
ticker.Start();

Console.WriteLine($"Giả lập máy thông điện trên {portName} @ {baud}. BOOT={machine.BootId}. Phím: i p f r m b g s q");
machine.Boot(machine.BootId);

while (true)
{
    var key = Console.ReadKey(true).KeyChar;
    switch (char.ToLowerInvariant(key))
    {
        case 'i':
            machine.InsertWire();
            Console.WriteLine("[đặt dây] -> TESTING");
            break;
        case 'p':
            machine.FinishTest(pass: true);
            Console.WriteLine($"[PASS] state={machine.StateName} (App kết nối: {machine.IsAppConnected})");
            break;
        case 'f':
            machine.FinishTest(pass: false);
            Console.WriteLine("[FAIL] -> WAIT_REMOVE (không gửi gì)");
            break;
        case 'r':
            machine.RemoveWire();
            Console.WriteLine("[rút dây] -> IDLE");
            break;
        case 'm':
            machine.RespondToPing = !machine.RespondToPing;
            Console.WriteLine($"[trả PONG] = {machine.RespondToPing}");
            break;
        case 'b':
            machine.Boot();
            Console.WriteLine($"[reset máy] BOOT={machine.BootId}");
            break;
        case 'g':
            machine.GoldenCrc = machine.GoldenCrc == "0000" ? "3A7F" : "0000";
            Console.WriteLine($"[GM] = {machine.GoldenCrc}");
            break;
        case 's':
            Console.WriteLine($"state={machine.StateName} seq={machine.PendingSeq} boot={machine.BootId} appKetNoi={machine.IsAppConnected}");
            break;
        case 'q':
            cts.Cancel();
            return 0;
    }
}
