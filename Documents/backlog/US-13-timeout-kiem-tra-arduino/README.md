# US-13: [ĐÃ HỦY] Timeout xác định kết quả kiểm tra Arduino

> **⛔ Hủy / Không áp dụng từ 12/10/2026.** Story này bị thay thế bởi cơ chế **heartbeat PING/PONG** ở **US-14** (phát hiện mất kết nối máy thông điện). Giữ lại file để link ổn định và để người sau hiểu vì sao không còn timeout 45 giây.

## Vì sao hủy
- Mô hình cũ: công nhân scan tem trước, App chờ Arduino gửi "OK"; hết 45 giây không có OK thì **suy luận NG** rồi nhờ Tổ trưởng xác nhận.
- Mô hình mới (`Documents/spec-firmware-app-link.md`, quyết định 12/10/2026): **thông điện TRƯỚC, quét QR SAU**. Máy chỉ gửi kết quả OK; NG máy không gửi gì và không khóa. Không còn thời điểm "tem đã scan, đang chờ OK" nên không còn khái niệm timeout suy luận NG.
- Thứ cần giám sát bây giờ là **sức khỏe đường truyền** (App PING mỗi 1 giây, mất PONG quá 5 giây = mất kết nối) → thuộc US-14.

## Phần bị bỏ
- Timeout mặc định 45 giây và cấu hình `ArduinoTimeoutSeconds` trong `Station.Wpf/appsettings.json`.
- Bước "hết timeout → suy luận NG → popup Tổ trưởng xác nhận Lưu NG / Hủy — kiểm tra lại" (US-12 cũ AC4–AC6).
- Dây NG nay xử lý bằng Chế độ Scan NG sẵn có (US-18/FR-18) + Mở khóa rework (US-19/FR-19).

## Thay thế bằng
- **US-14**: `PingIntervalMs = 1000`, `LinkTimeoutMs = 5000`, `CommandTimeoutMs = 2000`, `ReconnectIntervalMs = 2000` (section `Tester` trong `appsettings.json` cục bộ tại trạm).
- **US-12**: toàn bộ luồng nghiệp vụ mới.

**Nguồn FR:** FR-13 (đã hủy 12/10/2026)
**Phụ thuộc:** — (không còn)
**Cờ cảnh báo mục 8.2:** Không.

---

## Trạng thái triển khai

- **Trạng thái:** ⛔ Hủy (không áp dụng — thay bằng heartbeat ở US-14)
- **Cập nhật:** 2026-10-12

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-12 (ba, hủy story theo spec máy thông điện ESP32)
- Đánh dấu ⛔ Hủy: bỏ timeout 45s, suy luận NG và cấu hình `ArduinoTimeoutSeconds`. Thay bằng heartbeat PING/PONG ở US-14.
- Chưa từng có code triển khai nên không cần gỡ code.
