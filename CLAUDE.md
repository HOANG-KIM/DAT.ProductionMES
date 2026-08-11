# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

DAT.ProductionMES — hệ thống MES quản lý kế hoạch sản xuất: theo dõi sản xuất qua scan tem tại từng trạm làm việc, đối chiếu kế hoạch sản xuất, tích hợp Arduino cho công đoạn kiểm tra tự động (vd. Thông điện).

Trước khi implement bất kỳ tính năng nào liên quan đến business rule, đọc:
- `Documents/SRS-he-thong-quan-ly-ke-hoach-san-xuat.md` — đặc tả nghiệp vụ đầy đủ (FR-01 → FR-23), các quy tắc đã chốt ở mục 6, acceptance criteria ở mục 7.
- `Documents/ADR-001-lua-chon-wpf-hay-winforms.md` — quyết định dùng WPF (không phải WinForms) cho client trạm.

## Commands

```bash
# Build toàn bộ solution
dotnet build ProductionMES.sln

# Chạy API (Swagger tại /swagger, health check tại /health, chỉ ở Development mới bật Swagger UI)
dotnet run --project src/ProductionMES.Api

# Chạy toàn bộ test
dotnet test tests/ProductionMES.Application.Tests

# Chạy 1 test cụ thể
dotnet test tests/ProductionMES.Application.Tests --filter "FullyQualifiedName~SmokeTests.MethodName"

# Thêm package vào đúng project (không dùng NuGet UI/PackageReference tay để tránh version tự nâng cấp ngoài ý muốn)
dotnet add src/ProductionMES.Infrastructure package <TenPackage> --version <version>
```

Trước khi chạy API cần MySQL Server 5.7.16 và connection string thật trong `src/ProductionMES.Api/appsettings.Development.json` (file này bị `.gitignore`, không commit).

## Kiến trúc

Solution 3-layer: `Api → Application → Domain` (Domain không phụ thuộc project nào khác), và `Infrastructure → Application + Domain`. `Api` reference cả `Application` lẫn `Infrastructure` chỉ để wiring DI ở `Program.cs` — Controller không được gọi thẳng Infrastructure.

```
src/
  ProductionMES.Api/              Controllers, SignalR Hub, Program.cs (wiring toàn bộ DI/middleware)
  ProductionMES.Application/      Service (business logic), DTO, FluentValidation validator
  ProductionMES.Domain/           Entity, Enum, business exception — không có project reference
  ProductionMES.Infrastructure/   DbContext (EF Core), Repository, UnitOfWork, Dapper query
  ProductionMES.Shared/           Constant, helper dùng chung nhiều layer
  ProductionMES.Station.Wpf/      Client WPF tại trạm làm việc (MVVM) — KHÔNG reference project backend nào, chỉ gọi qua HTTP/SignalR
tests/
  ProductionMES.Application.Tests/  xUnit + Moq, chỉ reference Application
```

Luật layer bắt buộc: Controller chỉ gọi Service; Service chứa business rule, không viết SQL/LINQ trực tiếp mà luôn qua Repository/UnitOfWork; Repository chỉ làm data access thuần túy. DTO dùng để truyền qua lại giữa Controller ↔ Service ↔ Client, không để lộ EF Entity ra Controller.

### Data access: EF Core + Dapper song song

`ApplicationDbContext` (trong `Infrastructure/Persistence/`) hiện **chưa có `DbSet` nào** — solution mới ở dạng scaffold, entity sẽ được thiết kế sau. Quy ước đã chốt: EF Core Migrations là nguồn schema duy nhất (kể cả cho bảng chỉ dùng Dapper để query) — không dùng công cụ migration thứ hai. Dùng EF Core cho CRUD/query đơn giản, Dapper cho query phức tạp/báo cáo.

`Pomelo.EntityFrameworkCore.MySql` và `Microsoft.EntityFrameworkCore.Design` **phải luôn cùng version** (hiện pin ở `8.0.2`) — MySQL server thật là **5.7.16**, nên `MySqlServerVersion` trong `InfrastructureServiceCollectionExtensions.cs` phải khớp đúng bản này (không dùng `ServerVersion.AutoDetect`, vì cần biết version ngay cả khi chưa kết nối được DB).

### Business rule cốt lõi cần nhớ khi implement Service (xem SRS để biết chi tiết/số FR)

- Chống trùng tem xét theo `(Mã tem, Công đoạn)` **trên toàn hệ thống**, không theo Line — cùng công đoạn không được scan 2 lần dù khác Line.
- Kiểm tra "đã qua công đoạn liền trước" cũng tra cứu toàn hệ thống, không giới hạn theo Line.
- Mọi lượt scan (kể cả bị từ chối) đều lưu lịch sử — không có scan "biến mất".
- Trạm WPF có hàng đợi cục bộ (SQLite) ghi trước khi gửi server, chống mất dữ liệu khi crash/mất mạng, dùng GUID để chống ghi trùng khi retry — áp dụng đồng nhất cho mọi trạm, không phân biệt theo vị trí trong chuỗi công đoạn.
- `UNIQUE(MaTem, CongDoanId)` kiểu truyền thống không áp dụng được vì 1 tem có thể có nhiều bản ghi NG + 1 bản ghi OK tại cùng công đoạn — ràng buộc "tối đa 1 OK" phải xử lý ở Service, không dựa vào DB constraint.
- "Đóng thùng" không thuộc phạm vi hệ thống này (module riêng, khác mô hình dữ liệu — xem mục 1.2 SRS).

### Cấu hình

Dùng Options pattern (`IOptions<T>`), không đọc `IConfiguration` rải rác. Timeout nghiệp vụ (chờ Arduino 45s, chế độ Scan NG 30s — xem `ArduinoTimeoutSeconds`/`NgModeTimeoutSeconds` trong `Station.Wpf/appsettings.json`) đọc từ **file cấu hình cục bộ tại từng trạm**, không phải từ server — cho phép chỉnh riêng theo trạm không cần deploy lại.
