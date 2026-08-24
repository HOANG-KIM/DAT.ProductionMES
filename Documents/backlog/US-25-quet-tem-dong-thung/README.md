# US-25: Quét tem đóng thùng — đếm số lượng, tự động in tem thùng
**Là** Công nhân vận hành (Operator) tại trạm Đóng thùng, có sự hỗ trợ của Tổ trưởng khi cần xác nhận
**Tôi muốn** quét lần lượt tem sản phẩm để hệ thống tự đếm đủ số lượng theo quy cách của Model, tự in tem dán thùng khi đủ và tự chuyển sang thùng kế tiếp
**Để** đóng gói đúng số lượng/thùng theo quy cách, có tem nhận diện thùng hàng, và không phải đếm/tính tay số thùng

**Acceptance Criteria**
- **AC1 — Công đoạn "Đóng thùng" là 1 Stage bình thường trong trình tự**
  - Given "Đóng thùng" đã được khai báo là 1 Stage trong danh mục Công đoạn (US-02) và được cấu hình trong trình tự công đoạn của Line (US-03, `LineStageSequence`)
  - When trạm gắn với công đoạn "Đóng thùng" hoạt động
  - Then mọi lượt scan tại đây tuân thủ đúng FR-08 như bất kỳ Stage nào khác (không có luồng/API riêng ngoài quy tắc chung)
- **AC2 — Scan tem hợp lệ — cộng dồn vào thùng hiện tại**
  - Given tem đã qua đúng công đoạn liền trước (theo trình tự của Line) và chưa từng được quét OK tại "Đóng thùng"
  - When Operator quét tem sản phẩm
  - Then hệ thống ghi nhận scan OK, tăng số lượng đã quét của thùng hiện tại thêm 1, cập nhật hiển thị ngay tại trạm
- **AC3 — Chưa qua công đoạn liền trước**
  - Given tem chưa từng được ghi nhận OK ở công đoạn liền trước (theo trình tự của Line) tại bất kỳ trạm nào trong hệ thống
  - When Operator quét tem tại "Đóng thùng"
  - Then hệ thống từ chối, nêu rõ tên công đoạn còn thiếu, lưu lịch sử lượt scan bị từ chối, không cộng vào số lượng thùng hiện tại
- **AC4 — Đủ số lượng theo quy cách — tự động in tem thùng và chuyển thùng kế tiếp**
  - Given số lượng đã quét trong thùng hiện tại vừa đạt đúng Quy cách đóng gói của Model đang chạy (US-24)
  - When lượt scan đạt đủ số lượng được ghi nhận
  - Then hệ thống tự động in 1 tem dán thùng gồm tối thiểu: Model, Tên sản phẩm, Nhà sản xuất, số lượng sản phẩm/thùng, Khối lượng, Số thùng (BoxNo), ngày giờ đóng thùng, tên trạm/Line thực hiện — theo đúng mẫu tem (template) đã cấu hình cho Model đó; đồng thời bộ đếm reset về 0 và số thùng tự động tăng thêm 1 cho thùng kế tiếp, sẵn sàng nhận tem mới
- **AC5 — Nhập số thùng bắt đầu — lần đầu đóng thùng của 1 kế hoạch**
  - Given kế hoạch sản xuất hiện tại chưa từng có thùng nào được đóng qua công đoạn "Đóng thùng" này
  - When Operator/Tổ trưởng bắt đầu phiên đóng thùng đầu tiên cho kế hoạch đó
  - Then hệ thống bắt buộc nhập số thùng bắt đầu (BoxNo khởi điểm) trước khi cho quét tem đầu tiên, để nối tiếp đúng số nếu kế hoạch đã có thùng đóng trước đó bằng cách khác
- **AC6 — Tự động tăng số thùng, tự nhớ trạng thái đang dở**
  - Given đã đóng xong ít nhất 1 thùng cho kế hoạch hiện tại (đã qua AC5)
  - When Operator tiếp tục quét tem cho thùng kế tiếp, kể cả sau khi tắt/mở lại ứng dụng hoặc rời màn hình rồi quay lại
  - Then hệ thống tự hiển thị đúng số thùng hiện tại và số lượng đã quét dở của thùng đó, không yêu cầu nhập lại số thùng
- **AC7 — Sửa số thùng hiện tại cần Supervisor xác nhận**
  - Given đang đóng thùng dở (đã có BoxNo hiện tại), phát hiện cần sửa lại số thùng (nhập sai/cần đóng bù)
  - When Operator/Tổ trưởng chọn sửa số thùng hiện tại
  - Then hệ thống yêu cầu đăng nhập nâng quyền Supervisor (tái sử dụng cơ chế của US-18) trước khi cho phép đổi giá trị; đổi thành công thì số thùng mới có hiệu lực ngay cho lượt quét tiếp theo
- **AC8 — Tem trùng — yêu cầu Supervisor xác nhận đã biết tình huống (KHÔNG cộng số lượng)** *(chốt lại 20/08/2026, đảo ngược giả định ban đầu)*
  - Given tem đã từng được quét OK tại công đoạn "Đóng thùng" trước đó (trùng theo đúng phạm vi FR-08 — Mã tem + Công đoạn)
  - When Operator quét lại đúng tem đó
  - Then hệ thống từ chối ngay lập tức theo đúng FR-08 (không ghi đè), lưu lịch sử lượt bị từ chối, và yêu cầu đăng nhập nâng quyền Supervisor (tái sử dụng đúng cơ chế popup đăng nhập của US-18) để **xác nhận đã biết tình huống** trước khi Operator được tiếp tục thao tác bình thường — xác nhận này CHỈ lưu vết audit (ai đã xử lý), **KHÔNG cộng thêm số lượng vào thùng hiện tại, KHÔNG tạo thêm bản ghi OK cho tem đó**. Đây KHÔNG phải ngoại lệ ghi đè của FR-08 — hành vi từ chối vẫn giữ nguyên như mọi công đoạn khác (mục 6 quy tắc 16)
- **AC12 — Đổi Quy cách đóng gói khi đang đóng dở 1 thùng** *(bổ sung 20/08/2026)*
  - Given thùng hiện tại đang đóng dở (đã quét 1 phần, chưa đủ), số lượng mục tiêu của thùng này đã được snapshot đúng Quy cách đóng gói tại thời điểm mở thùng
  - When Admin/Tổ trưởng sửa Quy cách đóng gói của Model đó (US-24/AC2) ngay trong lúc thùng đang đóng dở
  - Then thùng đang dở tiếp tục dùng đúng số lượng mục tiêu đã snapshot (không đổi theo), quy cách mới chỉ áp dụng cho thùng MỞ SAU thời điểm sửa (mục 6 quy tắc 17)
- **AC9 — Theo dõi thời gian thực tại trạm**
  - Given đang trong 1 phiên đóng thùng
  - When có lượt quét OK mới được ghi nhận
  - Then màn hình trạm cập nhật ngay số lượng đã quét trong thùng hiện tại / quy cách cần đủ, không cần thao tác làm mới thủ công
- **AC10 — Lưu lịch sử đầy đủ, không có scan biến mất**
  - Given bất kỳ lượt quét nào tại công đoạn "Đóng thùng" (OK, chưa qua công đoạn trước, trùng tem bị từ chối, trùng tem đã được Supervisor xác nhận)
  - When lượt quét xảy ra
  - Then hệ thống lưu lại lịch sử đầy đủ, nhất quán với nguyên tắc chung "mọi lượt scan đều lưu lịch sử" của toàn hệ thống (mục 6 quy tắc 6)
- **AC11 — Chưa có cấu hình đóng gói cho Model**
  - Given kế hoạch đang chạy tại "Đóng thùng" có Model chưa được cấu hình quy cách đóng gói (US-24)
  - When Operator cố quét tem đầu tiên
  - Then hệ thống chặn, báo lỗi rõ ràng yêu cầu cấu hình Quy cách đóng gói cho Model trước, không ghi nhận lượt scan là hợp lệ
- **AC13 — Lỗi in không chặn đóng thùng kế tiếp, luôn có In lại thủ công** *(chốt 20/08/2026)*
  - Given tem dán thùng vừa được lệnh in tự động (AC4) nhưng không ra giấy được (lỗi vật lý máy in — kẹt/hết giấy — không phát hiện được ở tầng code, khác lỗi lệnh gọi cứng như thiếu template/Excel/máy in mặc định)
  - When Operator/Tổ trưởng tiếp tục quét tem cho thùng kế tiếp
  - Then hệ thống KHÔNG chặn, cho phép đóng thùng kế tiếp bình thường; luôn có sẵn thao tác "In lại" để chủ động in bù bất cứ lúc nào khi phát hiện tem chưa ra giấy — chỉ chặn/báo lỗi khi CHÍNH lệnh gọi in thất bại (thiếu template/Excel/máy in)
- **AC14 — Trạm KHÔNG phải Đóng thùng không có bất kỳ chức năng nào ở trên** *(làm rõ 20/08/2026, không phải AC mới về mặt nghiệp vụ)*
  - Given 1 trạm được cấu hình cho công đoạn KHÁC "Đóng thùng" (vd Lắp ráp, Thông điện...) — mỗi `WorkStation` gắn cố định đúng 1 cặp (Line, Công đoạn)
  - When Operator vận hành scan tại trạm đó
  - Then hệ thống chạy đúng luồng scan tiêu chuẩn hiện có (US-07/US-08, popup OK/NG/WAITING) — KHÔNG hiển thị bộ đếm số lượng/số thùng, KHÔNG tự động in tem; tem trùng vẫn bị từ chối cứng theo đúng FR-08 mặc định, KHÔNG có bước "Supervisor xác nhận đã biết" như AC8 (đặc thù riêng của Đóng thùng, không áp dụng công đoạn khác)

**Nguồn FR:** FR-25
**Phụ thuộc:** US-24 (cấu hình quy cách — bắt buộc có trước, AC11), US-01/US-02/US-03 (khai báo Stage + trình tự), US-07/US-08 (nền tảng luồng scan + FR-08 kế thừa trực tiếp), US-18 (mẫu tham chiếu bắt buộc cho AC7/AC8 — cơ chế re-auth Supervisor mỗi lần), US-16 (Hàng đợi cục bộ chống mất lượt scan — hiện ⬜ Chưa làm; phụ thuộc gián tiếp vì đếm lũy kế theo thùng nhạy với mất đồng bộ hơn scan độc lập từng lượt, cân nhắc thứ tự triển khai)
**Cờ cảnh báo mục 8.2:** Không còn — cả 3 điểm mở phát sinh từ US-25 đã chốt 20/08/2026 (xem AC8, AC12, AC13).

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-20

## Lịch sử triển khai (ghi chú backlog)

Story mới 20/08/2026 — phụ thuộc US-24 (chưa có cấu hình thì chặn scan, AC11). Xem AC đầy đủ ở mục 3.8, nguồn FR-25 (SRS)
