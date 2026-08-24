# US-16: Hàng đợi cục bộ chống mất lượt scan khi mất mạng/crash
**Là** Công nhân vận hành trạm / Hệ thống
**Tôi muốn** mỗi lượt scan được ghi vào hàng đợi cục bộ trước khi gửi server, và tự động thử gửi lại nếu thất bại
**Để** không mất bất kỳ lượt scan nào kể cả khi mất mạng, server tạm ngưng, hoặc crash/mất điện đột ngột

**Acceptance Criteria**
- **AC1 — Ghi vào hàng đợi cục bộ trước tiên**
  - Given công nhân scan 1 tem
  - When hệ thống xử lý lượt scan
  - Then ghi ngay vào hàng đợi cục bộ (SQLite) ở trạng thái `Chờ gửi`, kèm 1 GUID sinh tại client, trước khi gửi lên server
- **AC2 — Gửi thành công, cập nhật trạng thái Đã đồng bộ**
  - Given lượt scan đã ghi vào hàng đợi cục bộ
  - When server phản hồi thành công
  - Then trạng thái cục bộ chuyển thành `Đã đồng bộ`, hiển thị kết quả OK/NG chính thức
- **AC3 — Không gửi được, giữ trạng thái Chờ gửi** *(AC-06 gốc)*
  - Given mất kết nối mạng giữa trạm và server trong lúc scan
  - When gửi lượt scan lên server không thành công (mất mạng/server tạm ngưng/timeout)
  - Then ứng dụng báo lỗi rõ ràng, không ghi nhận lượt scan "treo"; giữ nguyên `Chờ gửi`, hiển thị "Chờ đồng bộ"; tự kết nối lại khi mạng phục hồi *(AC-06)*
- **AC4 — Thử lại tự động định kỳ** *(AC-21 gốc)*
  - Given có nhiều bản ghi ở trạng thái `Chờ gửi` do mất mạng/server tạm ngưng
  - When tiến trình nền định kỳ (vd mỗi 5 giây) quét hàng đợi cục bộ
  - Then không chặn scan; tiếp tục thử gửi lại định kỳ theo đúng thứ tự thời gian đã scan; khi kết nối phục hồi, toàn bộ bản ghi tồn đọng tự động đồng bộ, không cần thao tác thủ công *(AC-21)*
- **AC5 — Khôi phục sau crash/mất điện, gửi lại không cần scan lại** *(AC-10 gốc)*
  - Given ngắt mạng/rút điện đột ngột ngay sau khi công nhân scan 1 tem (trước khi có phản hồi từ server), sau đó khởi động lại ứng dụng
  - When ứng dụng đọc lại hàng đợi cục bộ
  - Then lượt scan đó vẫn còn ở trạng thái "Chờ đồng bộ", tự động gửi lại lên server khi có mạng, không yêu cầu scan lại tem, và không bị ghi nhận trùng 2 lần trên server *(AC-10)*
- **AC6 — Chống gửi trùng (idempotency) qua GUID**
  - Given client gửi lại 1 lượt scan có GUID đã tồn tại (do gửi lặp vì crash giữa lúc chờ phản hồi)
  - When server nhận lượt gửi này
  - Then server trả về kết quả đã ghi nhận trước đó, không báo lỗi "trùng tem"
- **AC7 — Áp dụng đồng nhất cho mọi trạm/công đoạn**
  - Given hệ thống gồm nhiều trạm với công đoạn khác nhau
  - When triển khai cơ chế hàng đợi cục bộ
  - Then áp dụng như nhau cho mọi trạm, không phân biệt trạm nào đang là "công đoạn cuối chuỗi" (vì trình tự công đoạn có thể thay đổi theo kế hoạch)
- **AC8 — Không có cơ chế cảnh báo ngưỡng hay báo cáo đối soát riêng** (đã rút gọn theo mục 8.1)
  - Given cơ chế hàng đợi cục bộ + retry + idempotency đã đủ xử lý gián đoạn ngắn hạn
  - When thiết kế/triển khai FR-16
  - Then không xây dựng cơ chế cảnh báo theo ngưỡng số bản ghi tồn đọng/thời gian mất kết nối, và không tạo báo cáo đối soát riêng cho giai đoạn mất kết nối

**Nguồn FR:** FR-16, mục 6 quy tắc 8
**Phụ thuộc:** US-07, US-08 (cần có luồng scan cơ bản để gắn hàng đợi cục bộ vào)
**Cờ cảnh báo mục 8.2:** Không trực tiếp (đã rút gọn và chốt phạm vi rõ ràng trong SRS).

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


