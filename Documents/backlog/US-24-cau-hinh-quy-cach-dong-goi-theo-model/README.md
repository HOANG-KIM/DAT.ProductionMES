# US-24: Cấu hình Quy cách đóng gói theo Model
**Là** Admin (tại web-admin) hoặc Tổ trưởng (đăng nhập nâng quyền tại trạm)
**Tôi muốn** thêm/sửa/xem bộ cấu hình đóng gói (quy cách, khối lượng, tên sản phẩm, nhà sản xuất, mẫu tem in) cho từng Model sản phẩm
**Để** hệ thống có đủ thông tin tự động đếm đủ số lượng và in đúng nội dung tem dán thùng khi vận hành công đoạn Đóng thùng

**Acceptance Criteria**
- **AC1 — Thêm cấu hình đóng gói mới cho 1 Model**
  - Given Model chưa có cấu hình đóng gói
  - When tôi nhập Model, Quy cách đóng gói (số lượng sản phẩm/thùng), Khối lượng, Tên sản phẩm, Nhà sản xuất và lưu
  - Then hệ thống tạo mới 1 bộ cấu hình gắn với đúng Model đó
- **AC2 — Sửa cấu hình đã có**
  - Given Model đã có cấu hình đóng gói
  - When tôi cập nhật bất kỳ trường nào (quy cách, khối lượng, tên sản phẩm, nhà sản xuất) và lưu
  - Then cấu hình được cập nhật; các thùng đã đóng và in tem trước đó không bị thay đổi ngược lại (không hồi tố)
- **AC3 — Xem danh sách cấu hình theo Model**
  - Given đã có ít nhất 1 cấu hình đóng gói
  - When tôi mở màn hình quản lý cấu hình (web-admin hoặc tại trạm)
  - Then thấy danh sách đầy đủ Model kèm đủ 5 thông tin cấu hình (quy cách, khối lượng, tên sản phẩm, nhà sản xuất, có/chưa có mẫu tem)
- **AC4 — Tải lên file mẫu tem in (template) cho 1 Model**
  - Given tôi đang sửa/tạo cấu hình cho 1 Model
  - When tôi chọn và tải lên 1 file mẫu tem
  - Then hệ thống lưu file, thay thế file mẫu cũ (nếu có) của đúng Model đó
- **AC5 — Tải xuống file mẫu tem đang cấu hình**
  - Given Model đã có file mẫu tem được tải lên trước đó
  - When tôi bấm tải xuống
  - Then nhận đúng file đã tải lên gần nhất cho Model đó
- **AC6 — Dữ liệu đồng nhất giữa web-admin và trạm**
  - Given 1 cấu hình được tạo/sửa từ web-admin (Admin)
  - When Tổ trưởng mở màn hình cấu hình tương ứng tại trạm (hoặc ngược lại)
  - Then thấy đúng dữ liệu mới nhất, không lệch giữa 2 nơi
- **AC7 — Ràng buộc dữ liệu bắt buộc**
  - Given tôi đang tạo/sửa cấu hình
  - When tôi bỏ trống Quy cách đóng gói, hoặc nhập giá trị ≤ 0 cho Quy cách/Khối lượng, hoặc bỏ trống Model/Tên sản phẩm
  - Then hệ thống từ chối lưu và báo lỗi rõ ràng cho từng trường sai
- **AC8 — Chưa có cấu hình cho Model đang cần đóng thùng**
  - Given 1 kế hoạch sản xuất đang chạy tại công đoạn Đóng thùng với Model X
  - When Model X chưa từng được cấu hình quy cách đóng gói
  - Then hệ thống không cho phép/không hiển thị đủ dữ liệu để bắt đầu quét tại công đoạn Đóng thùng cho kế hoạch đó (chi tiết hành vi chặn xem US-25/AC11)
- **AC9 — So khớp Model không phân biệt hoa/thường, có gợi ý autocomplete** *(chốt 20/08/2026)*
  - Given tôi đang tạo cấu hình đóng gói mới hoặc hệ thống đang tra cứu cấu hình cho 1 kế hoạch đang chạy tại Đóng thùng
  - When Model nhập/khai báo lệch hoa-thường hoặc dư khoảng trắng so với Model đã cấu hình trước đó (vd "abc-123 " so với "ABC-123")
  - Then hệ thống vẫn khớp đúng cấu hình (so khớp không phân biệt hoa/thường, tự trim khoảng trắng); màn hình tạo/sửa cấu hình gợi ý autocomplete các Model đã có cấu hình để giảm rủi ro gõ sai. Thay đổi này CHỈ áp dụng cho bước tra cứu cấu hình đóng gói, KHÔNG đổi cách `ProductionPlan.Model` được nhập/lưu ở màn Cài đặt kế hoạch (US-05) hay bất kỳ nơi nào khác trong hệ thống

**Nguồn FR:** FR-24
**Phụ thuộc:** US-01/US-02 (danh mục Line/Công đoạn — Đóng thùng chỉ là 1 Stage bình thường), ADR-002 mục "Cập nhật phạm vi" (tiền lệ Tổ trưởng cấu hình tại `Station.Wpf`, giống US-05/US-03)
**Cờ cảnh báo mục 8.2:** Không còn — đã chốt 20/08/2026: Model khớp với `ProductionPlan.Model` **không phân biệt hoa/thường, tự trim khoảng trắng** (không so chuỗi tuyệt đối), kèm autocomplete gợi ý khi nhập tại AC1/AC2 — giữ nguyên free-text, **không** tách thành danh mục Model riêng. Phạm vi thay đổi **chỉ áp dụng cho bước tra cứu cấu hình đóng gói này** — không đổi cách `ProductionPlan.Model` được nhập/lưu/hiển thị/so khớp ở bất kỳ nơi nào khác trong hệ thống (US-05, báo cáo, Andon board...), xem SRS mục 6 quy tắc 19.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-24

## Lịch sử triển khai (ghi chú backlog)

**20/08/2026 (dev, implement đầy đủ AC1-AC9)**: Domain: entity mới `PackingModelConfig` (`Model`/`ModelNormalized` snapshot trim+upper invariant cho AC9/`PackingQuantity`/`GrossWeight` decimal?/`PartName`/`Manufacturer`/`HasTemplate`/`TemplateUpdatedAtUtc`/`TemplateUpdatedByUserName`/`CreatedAtUtc`/`UpdatedAtUtc`/`UpdatedByUserName`, không FK, `CreatedAtUtc`/`UpdatedAtUtc`/`TemplateUpdatedAtUtc` dùng UTC thật `DateTime.UtcNow` — KHÔNG nằm trong 4 field ngoại lệ giờ local đã chốt ở API-Conventions.md mục 10). Infrastructure: `PackingModelConfigConfiguration` (index thường theo `ModelNormalized`, không unique DB — tính duy nhất do Service đảm bảo, cùng nguyên tắc `Lot.Code`), `DbSet` mới, migration `AddPackingModelConfig` đã tạo VÀ **áp dụng thành công vào MySQL cục bộ** (`dotnet ef database update`, xác nhận qua `dotnet ef migrations list` không còn Pending). File mẫu tem lưu filesystem server (KHÔNG BLOB DB) qua abstraction mới `IPackingTemplateStorage` (Application.Abstractions.Storage) + implementation `PackingTemplateFileStorage` (Infrastructure.Storage, đặt tên file `{Id}.xlsx`) — `PackingTemplateStorageOptions.BasePath` (Options pattern) do tầng Api resolve thành đường dẫn tuyệt đối lúc khởi động (`Program.cs`, kết hợp `IWebHostEnvironment.ContentRootPath` + `appsettings.json` section `PackingTemplateStorage:RelativeFolderPath`, mặc định `App_Data/PackingTemplates`) vì Infrastructure không reference ASP.NET Core Hosting. Thư mục `src/ProductionMES.Api/App_Data/PackingTemplates/` giữ qua `.gitkeep`, nội dung thật không commit (`.gitignore` mới). Application: `IPackingModelConfigService`/`PackingModelConfigService` (`GetAllAsync`/`GetByIdAsync`/`GetByModelAsync`/`SuggestModelsAsync`/`CreateAsync`/`UpdateAsync`/`UploadTemplateAsync`/`DownloadTemplateAsync`), so khớp Model AC9 chuẩn hoá tường minh (`Trim().ToUpperInvariant()`, không phụ thuộc collation MySQL) qua `ModelNormalized`; `CreatePackingModelConfigRequestValidator`/`UpdatePackingModelConfigRequestValidator` (FluentValidation, AC7 — Model/Tên sản phẩm not empty, Quy cách/Khối lượng > 0 nếu nhập). Api: `PackingModelConfigsController` (`api/v1/packing-model-configs`) — CRUD + `GET lookup?model=` (AC9, 404 nếu chưa có)+ `GET suggest-models` (AC9 autocomplete) + `POST {id}/template` (AC4, `IFormFile`, `[RequestSizeLimit(5MB)]`, ĐẦU TIÊN dự án có multipart upload — chỉ nhận `.xlsx`, sai định dạng/rỗng ném `BusinessRuleException`→409) + `GET {id}/template` (AC5, trả file stream, 404 nếu chưa từng upload). Permission mới `PermissionResource.PackingModelConfig=9` (View/Create/Update, không Delete — chưa có AC xoá), policy `PackingModelConfig.View/Create/Update`, seed cho **Admin + Supervisor** (AC6 — khác đa số resource danh mục trước đây chỉ Admin) qua `DbSeeder` (catalog mới + `EnsurePackingModelConfigPermissionGrantsAsync` lưới an toàn, cùng idiom `EnsureReportViewPermissionGrantsAsync`). `web-admin`: `PackingModelConfigListPage`/`PackingModelConfigFormModal`/`usePackingModelConfigs` (`features/packing-model-configs/`, Ant Design + TanStack Query + React Hook Form/Zod, cùng convention `StageListPage`/`WorkStationFormModal`) — bảng liệt kê AC3, modal tạo/sửa AC1/AC2 (Model dùng `AutoComplete` gợi ý AC9 khi tạo, disabled khi sửa vì AC2 không đổi Model), `Upload.Dragger` tải lên AC4 + nút tải xuống AC5 (`responseType:'blob'`, cùng idiom `exportLotReport` US-23); route `/packing-model-configs` (`PermissionGuard permission="PackingModelConfig.View"`) + menu "Cấu hình đóng gói theo Model" trong nhóm Danh mục (`AppLayout`). `Station.Wpf`: `PackingModelConfigPage`/`PackingModelConfigViewModel` (Tổ trưởng nâng quyền, DÙNG LẠI đúng cơ chế elevate phiên chung `SupervisorAuthHandler`/`ISupervisorSessionService` như `PlanSettingsPage`/`LineStageSequencePage` — KHÔNG viết cơ chế đăng nhập mới, KHÔNG re-auth-mỗi-lần như US-18/19 vì đây là cấu hình danh mục thuần túy) — DataGrid + form bên phải cùng bố cục `PlanSettingsPage`, `OpenFileDialog`/`SaveFileDialog` (`Microsoft.Win32`, LẦN ĐẦU dùng trong dự án) đặt ở code-behind cho AC4/AC5; `IPackingModelConfigApiClient`/`PackingModelConfigApiClient` (multipart upload + binary download viết tay bằng `HttpClient` trực tiếp, KHÔNG qua `ApiClientBase.SendAsync<T>` vốn chỉ phục vụ JSON — đã đổi `ApiClientBase.ToApiExceptionAsync` từ `private` sang `protected` để tái dùng); tile thứ 5 "📦 Cấu hình đóng gói" ở `HomePage` (UniformGrid 4→5 cột) + nút điều hướng chéo ở `PlanSettingsPage`/`PlanSelectionPage`/`LineStageSequencePage`, `MainWindow.NavigateToPackingModelConfig()` (ADR-006). Test: `PackingModelConfigServiceTests` (16 test — AC1/AC2/AC3/AC4/AC5/AC9, gồm so khớp lệch hoa-thường+khoảng trắng) + `CreatePackingModelConfigRequestValidatorTests` (7 test AC7) trong `ProductionMES.Application.Tests`. Build `dotnet build ProductionMES.sln` sạch 0 Warning/0 Error (cả `Station.Wpf`); `dotnet test tests/ProductionMES.Application.Tests` 285/285 pass; `web-admin`: `npm run lint` (oxlint) sạch, `npm run build` (tsc -b && vite build) pass. **24/08/2026**: người giao việc đã tự chạy thử GUI thực tế trên `web-admin` và `Station.Wpf`, xác nhận CRUD + upload/download mẫu tem hoạt động đúng, đồng bộ dữ liệu giữa 2 nơi (AC6) — chuyển **✅ Xong**. Code đã commit (`7564aa2`).
