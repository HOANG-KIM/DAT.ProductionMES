# DAT.ProductionMES

Hệ thống quản lý kế hoạch sản xuất (Production MES) — theo dõi sản xuất qua scan tem tại từng trạm làm việc, đối chiếu kế hoạch sản xuất, tích hợp Arduino cho các công đoạn kiểm tra tự động.

Tài liệu nghiệp vụ đầy đủ: [`Documents/SRS-he-thong-quan-ly-ke-hoach-san-xuat.md`](Documents/SRS-he-thong-quan-ly-ke-hoach-san-xuat.md)
Quyết định kiến trúc: [`Documents/ADR-001-lua-chon-wpf-hay-winforms.md`](Documents/ADR-001-lua-chon-wpf-hay-winforms.md)

## Kiến trúc

- **Backend**: ASP.NET Core Web API, 3-layer (Controller → Service → Repository), Unit of Work, SOLID.
- **CSDL**: MySQL 5.7.16, EF Core (Pomelo.EntityFrameworkCore.MySql 8.0.2) cho migration/query đơn giản, Dapper cho query phức tạp/báo cáo.
- **Real-time**: SignalR.
- **Client trạm làm việc**: WPF + MVVM (xem ADR-001).
- **Logging**: Serilog.
- **Auth**: JWT Bearer, phân quyền theo role.

## Cấu trúc solution

```
ProductionMES.sln
src/
  ProductionMES.Api/              API, Controllers, SignalR Hub, Program.cs
  ProductionMES.Application/      Service, DTO, FluentValidation
  ProductionMES.Domain/           Entity, Enum, business exception (không phụ thuộc project khác)
  ProductionMES.Infrastructure/   DbContext, Repository, UnitOfWork, Dapper query
  ProductionMES.Shared/           Constant, helper dùng chung
  ProductionMES.Station.Wpf/      Client WPF tại trạm làm việc (MVVM)
tests/
  ProductionMES.Application.Tests/  Unit test (xUnit + Moq)
```

## Yêu cầu môi trường

- .NET 8 SDK
- MySQL Server 5.7.16
- Windows (bắt buộc để build/chạy `ProductionMES.Station.Wpf`)

## Build & chạy

```bash
# Build toàn bộ solution
dotnet build ProductionMES.sln

# Chạy API (Swagger tại /swagger, health check tại /health)
dotnet run --project src/ProductionMES.Api

# Chạy unit test
dotnet test tests/ProductionMES.Application.Tests
```

Trước khi chạy API, cập nhật connection string MySQL thật vào `src/ProductionMES.Api/appsettings.Development.json` (không commit file này — đã có trong `.gitignore`).
