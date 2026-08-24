# US-05a: Vòng đời trạng thái kế hoạch theo từng công đoạn (tạm dừng — chạy lại — đóng độc lập)
**Là** Tổ trưởng
**Tôi muốn** mỗi công đoạn áp dụng 1 kế hoạch có vòng đời trạng thái riêng (`Draft/Running/Paused/Completed/Cancelled`), tiến độ "đã chạy/còn lại" luôn tính đúng theo thời gian thực, và có thể tạm dừng hoặc đóng độc lập từng công đoạn
**Để** tạm dừng 1 lô đang chạy dở (đổi Line/trạm sang việc khác) rồi chạy lại chính xác sau này mà không mất/nhầm số lượng còn lại, đồng thời cho phép công đoạn nào xong trước tự đóng mà không phải chờ các công đoạn khác của cùng kế hoạch

**Acceptance Criteria**
- **AC1 — Áp dụng kế hoạch cho 1 (Line, Công đoạn), chuyển `Running`**
  - Given kế hoạch đang `Draft` hoặc `Paused` cho công đoạn X của Line Y
  - When Tổ trưởng bấm "Áp dụng"
  - Then kế hoạch chuyển `Running` cho đúng cặp (Line Y, Công đoạn X); nếu (Line Y, Công đoạn X) đang có 1 kế hoạch khác ở `Running`, hệ thống từ chối và yêu cầu Tạm dừng/Đóng kế hoạch đó trước
- **AC2 — Ràng buộc active theo (Line, Công đoạn), không theo cả Line**
  - Given công đoạn A của Line 1 đang `Running` kế hoạch X
  - When Tổ trưởng áp dụng kế hoạch Y cho công đoạn B của cùng Line 1
  - Then được chấp nhận bình thường — công đoạn B chạy kế hoạch khác song song, không bị chặn bởi trạng thái của công đoạn A (đúng thực tế dây chuyền có WIP giữa các trạm)
- **AC3 — Tạm dừng, giữ nguyên tiến độ**
  - Given kế hoạch đang `Running` tại 1 (Line, Công đoạn)
  - When Tổ trưởng bấm "Tạm dừng"
  - Then chuyển `Paused`; tiến độ không mất (vì tính động từ lịch sử scan OK, xem AC4), có thể "Áp dụng" lại bất kỳ lúc nào sau này
- **AC4 — Tính "đã chạy"/"còn lại" động, không lưu số liệu tĩnh**
  - Given kế hoạch đã có lượt scan OK tại đúng công đoạn đó
  - When xem tiến độ (màn hình chọn kế hoạch — US-05b, hoặc dashboard trạm)
  - Then "đã chạy" = tổng số lượt scan kết quả OK theo đúng cặp (kế hoạch, công đoạn); "còn lại" = số lượng kế hoạch − đã chạy; giá trị luôn tính lại theo dữ liệu scan hiện có, không đọc từ 1 cột số lượng lưu sẵn
- **AC5 — Tự động `Completed` khi đủ số lượng**
  - Given 1 cặp (kế hoạch, công đoạn) đang `Running`, số đã chạy sắp đạt đủ số lượng kế hoạch
  - When có thêm 1 lượt scan OK làm đủ số lượng
  - Then hệ thống tự động chuyển cặp đó sang `Completed` ngay, không cần Tổ trưởng thao tác thêm
- **AC6 — Đóng sớm thủ công, yêu cầu xác nhận nếu chưa đủ số lượng**
  - Given 1 cặp (kế hoạch, công đoạn) đang `Running`/`Paused`, số đã chạy còn thấp hơn số lượng kế hoạch
  - When Tổ trưởng bấm "Đóng kế hoạch"
  - Then hệ thống hiển thị rõ số lượng còn thiếu, yêu cầu xác nhận trước khi chuyển sang `Cancelled`
- **AC7 — Kế hoạch `Completed`/`Cancelled` không tự "Áp dụng" lại được như `Paused`**
  - Given 1 cặp (kế hoạch, công đoạn) đã `Completed` hoặc `Cancelled`
  - When Tổ trưởng mở màn hình chọn kế hoạch
  - Then cặp đó không xuất hiện trong danh sách áp dụng mặc định (US-05b AC2) — coi như đã kết thúc vòng đời tại công đoạn đó

**Nguồn FR:** FR-05a, mục 6 quy tắc 12
**Phụ thuộc:** US-05 (kế hoạch phải được tạo trước), US-03 (cần biết công đoạn nào thuộc trình tự của Line, để suy ra danh sách (Kế hoạch, Công đoạn) cần theo dõi vòng đời), US-08 (nguồn dữ liệu scan OK để tính tiến độ động)
**Cờ cảnh báo mục 8.2:** Không (đã chốt đầy đủ với người dùng ngày 13/08/2026).
**Lưu ý kỹ thuật:** Thay đổi so với thiết kế backend ban đầu (`ProductionPlanService.ActivateAsync`/`DeactivateAsync` hiện chỉ kiểm tra duy nhất theo `LineId`, cờ `IsActive` dạng bool) — cần đổi model sang trạng thái theo cặp `(LineId, StageId)` khi implement, không chỉ đổi UI.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-17

## Lịch sử triển khai (ghi chú backlog)

Backend xong (`0c5b944`). UI Áp dụng/Tạm dừng/Đóng code trong `PlanSelectionPage` (US-05b), gọi đúng `ProductionPlanStagesController`. **17/08: người dùng đã tự chạy app, xác nhận trực quan hoạt động đúng**
