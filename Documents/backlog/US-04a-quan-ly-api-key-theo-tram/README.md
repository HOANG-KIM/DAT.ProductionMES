# US-04a: Quản lý API Key theo trạm
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấp, xem trạng thái, thu hồi và cấp lại API Key riêng cho từng trạm làm việc
**Để** `Station.Wpf` xác thực được vào luồng scan thường (`StationApiKey` scheme, ADR-005) mà Operator không phải đăng nhập cá nhân

**Acceptance Criteria**
- **AC1 — Cấp API Key cho trạm**
  - Given trạm đã tồn tại (US-04)
  - When Admin chọn "Cấp API Key" cho trạm đó
  - Then hệ thống sinh 1 key ngẫu nhiên đủ dài, hiển thị giá trị thô **đúng 1 lần duy nhất** ngay sau khi cấp để Admin sao chép vào file cấu hình cục bộ của trạm (`appsettings.json`); server chỉ lưu **hash** của key, không lưu giá trị thô (cùng nguyên tắc `RefreshToken.TokenHash` — ADR-003/ADR-005)
- **AC2 — Không xem lại được giá trị thô sau khi rời màn hình**
  - Given API Key đã được cấp và Admin đã đóng màn hình xác nhận
  - When Admin quay lại xem thông tin trạm sau đó
  - Then chỉ thấy metadata (ngày cấp, trạng thái Active/Revoked), không hiển thị lại được giá trị thô
- **AC3 — Thu hồi API Key**
  - Given trạm đang có 1 API Key ở trạng thái Active
  - When Admin chọn "Thu hồi"
  - Then key chuyển trạng thái Revoked (ghi nhận thời điểm thu hồi), mọi request `StationApiKey` dùng key đó bị từ chối ngay từ lần gọi kế tiếp
- **AC4 — Xoay vòng: cấp lại key mới cho trạm đã có key**
  - Given trạm nghi ngờ lộ key hoặc thay thiết bị, đang có 1 key Active
  - When Admin cấp lại key mới cho trạm đó
  - Then key cũ tự động chuyển Revoked, key mới có hiệu lực; lịch sử key cũ vẫn được giữ lại (không xóa bản ghi) để truy vết theo đúng thiết kế `StationApiKey` tách riêng khỏi `WorkStation` (ADR-005)
- **AC5 — Từ chối request không có/sai API Key**
  - Given request gọi endpoint dùng scheme `StationApiKey` (vd `POST api/v1/scans`) thiếu header `X-Station-Api-Key` hoặc giá trị không khớp hash đã lưu
  - When server xác thực
  - Then trả về 401, không xử lý request
- **AC6 — Từ chối khi key hợp lệ nhưng sai trạm**
  - Given API Key hợp lệ (hash khớp) nhưng thuộc về `WorkStationId` khác với `WorkStationId` gửi trong request body
  - When server xác thực
  - Then từ chối request — chống trạm A dùng key của mình gọi giả danh trạm B (ADR-005)

**Nguồn FR:** Không có FR tương ứng trực tiếp trong SRS — đây là yêu cầu kỹ thuật phát sinh từ `Documents/ADR-005-auth-station-wpf.md` (mục "Hệ quả/Tiêu cực", dòng 76: *"web-admin chưa có UI cho việc này, cần bổ sung vào US-04 hoặc 1 story riêng trước/song song khi code US-07/08"*), cần thiết để `Station.Wpf` xác thực được vào luồng scan.
**Phụ thuộc:** US-04 (trạm phải tồn tại trước khi cấp key). **Là điều kiện tiên quyết bắt buộc của US-07/US-08** — nếu chưa có story này, `Station.Wpf` không có cách nào lấy được API Key hợp lệ để gọi `POST api/v1/scans`.
**Cờ cảnh báo mục 8.2:** Không.
**UI:** `web-admin` (Admin) — gắn liền màn hình quản lý trạm (US-04), không phải `Station.Wpf`.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

`4f0a3ed`
