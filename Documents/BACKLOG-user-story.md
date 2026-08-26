# BACKLOG USER STORY — Hệ thống Quản lý Kế hoạch Sản xuất (DAT.ProductionMES)

**Nguồn căn cứ:** `Documents/SRS-he-thong-quan-ly-ke-hoach-san-xuat.md` (FR-01 → FR-26, mục 6 quy tắc chốt, mục 7 AC, mục 8.2 điểm còn mở), `Documents/ADR-001-lua-chon-wpf-hay-winforms.md`.
**Ngày lập:** 11/08/2026
**Cập nhật:** 13/08/2026 — (1) bổ sung AC cho US-09 để đồng bộ với FR-09a (khung giờ nghỉ theo Line) được thêm vào SRS ngày 13/08/2026, sau thời điểm backlog được lập lần đầu; (2) tách 2 khoảng trống phát sinh sau khi story gốc đã code xong thành story riêng — **US-01a** (khung giờ nghỉ theo Line, do FR-01/FR-09a bổ sung sau khi US-01 code xong) và **US-04a** (API Key theo trạm, do ADR-005 chốt sau khi US-04 code xong) — cả 2 đều là điều kiện tiên quyết bắt buộc trước khi triển khai US-07/US-08/US-09, đã cập nhật vào lộ trình triển khai đề xuất; (3) cập nhật **US-05** theo FR-05 mới (Khách hàng/Model/Lot/Revision, bỏ ca làm việc) và bổ sung 2 story mới — **US-05a** (vòng đời trạng thái kế hoạch `Draft/Running/Paused/Completed/Cancelled` theo từng cặp (Line, Công đoạn), tạm dừng/chạy lại/đóng độc lập, tính tiến độ động — FR-05a) và **US-05b** (màn hình "Chọn kế hoạch": chọn Công đoạn + Kế hoạch, hiển thị tiến độ, Áp dụng) — phát sinh từ phân tích UI kế hoạch sản xuất ngày 13/08/2026, đã cập nhật vào lộ trình triển khai đề xuất. **Cập nhật 14/08/2026** — (4) bổ sung US-05/AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi kế hoạch đã có scan) và US-10/AC4 (snapshot bất biến 6 field trong lịch sử scan) — xử lý gap phát hiện khi rà soát US-10: sửa kế hoạch sau khi đã scan làm lịch sử scan cũ hiển thị sai nếu chỉ tra cứu qua join động tới `ProductionPlan` hiện tại; đã đồng bộ vào SRS mục 6 quy tắc 14, mục 8.1.
**Ghi chú chung:** Backlog này là đầu vào để dev implement dần theo thứ tự đề xuất ở cuối tài liệu. Xem tiến độ thực tế ở bảng "TRẠNG THÁI TRIỂN KHAI" ngay bên dưới.
**Cập nhật 24/08/2026** — (8) tách toàn bộ nội dung chi tiết từng story (mô tả, Acceptance Criteria, ghi chú lịch sử triển khai) từ file gộp này sang file riêng theo quy ước `Documents/backlog/US-XX-ten-tinh-nang/README.md` (1 story = 1 folder), theo yêu cầu người giao việc để dễ theo dõi/diff hơn khi số lượng story tăng lên; quy ước đặt tên đã đồng bộ vào `CLAUDE.md` mục "Theo dõi tiến độ backlog". File này từ nay chỉ còn giữ vai trò mục lục: bảng trạng thái tổng quan (link sang từng file), nhóm chức năng (link sang từng file), thứ tự triển khai đề xuất, và ghi chú chung về điểm còn mở — không còn chứa nội dung chi tiết story.
**Cập nhật 25/08/2026** — (9) bổ sung story mới **US-27** (Xác nhận Tổ trưởng trước khi lưu lịch sử scan bị từ chối) — đảo ngược 1 phần quy tắc nghiệp vụ nền tảng "mọi lượt scan kể cả bị từ chối đều tự động lưu lịch sử" (SRS FR-10/mục 6 quy tắc 6), theo yêu cầu người giao việc, do agent `ba` phân tích qua 2 vòng làm rõ (xác nhận rõ US-18 Chế độ Scan NG KHÔNG thuộc phạm vi, giữ nguyên 100%; đối tượng thực sự là banner lỗi scan bình thường DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock và mọi lỗi tương lai). **Supersede US-25/AC8** (tem trùng tại "Đóng thùng" nay dùng chung cơ chế Lưu/Thoát của US-27 thay vì cơ chế audit riêng cũ). Đồng bộ SRS: FR-07 (mục 3.3), FR-27 mới, mục 6 quy tắc 6/16 sửa lại, mục 7 AC-31 sửa lại + AC-37 mới, mục 8.1/8.2.
**Cập nhật 20/08/2026** — (5) bổ sung 3 story mới **US-24/US-25/US-26** (mục 3.8 "Đóng thùng") — tính năng port lại từ 1 app WinForms cũ độc lập (`PrintLabel`, DB `line_andon_history1`) vào đúng kiến trúc MES hiện tại, do agent `ba` viết theo yêu cầu người dùng (đã đảo ngược quyết định "Đóng thùng ngoài phạm vi SRS" — xem SRS mục 1.2/8.1); đồng bộ SRS FR-24/FR-25/FR-26, mục 6 quy tắc 16, mục 7 AC-29→AC-32, mục 8.2 (5 điểm mở mới). **Cùng ngày, vòng 2** — (6) người giao việc chốt trực tiếp cả 5 điểm mở phát sinh ở (5): Model khớp `ProductionPlan.Model` không phân biệt hoa/thường + tự trim + autocomplete (US-24/AC9), Supervisor xác nhận tem trùng KHÔNG cộng thêm số lượng chỉ audit (US-25/AC8, đảo ngược giả định ban đầu của `ba`), sửa Quy cách đóng gói không hồi tố cho thùng đang dở — snapshot lúc mở thùng (US-25/AC12), lỗi in KHÔNG chặn đóng thùng kế tiếp — luôn có In lại thủ công (US-25/AC13), tiến độ theo Lot không cần trạng thái riêng — dùng lại %/nhãn Đủ-Chưa đủ của US-21/US-21a (US-26/AC2) — đồng bộ SRS mục 6 quy tắc 17-20, mục 7 AC-33→AC-35, mục 8.2 (không còn điểm mở nào cho US-24/25/26). **Vòng 3** — (7) làm rõ US-25/AC14 + SRS AC-36: các chức năng đóng thùng (bộ đếm, tự in tem, Supervisor xác nhận-đã-biết khi trùng tem) CHỈ áp dụng cho trạm cấu hình đúng công đoạn "Đóng thùng" — trạm khác vẫn chạy nguyên luồng scan tiêu chuẩn US-07/US-08, không thay đổi gì.

---

## TRẠNG THÁI TRIỂN KHAI

Quy ước cập nhật bảng này nằm ở `CLAUDE.md` (mục "Theo dõi tiến độ backlog") — agent `ba`/`dev` đọc trước khi làm việc và PHẢI tự cập nhật dòng tương ứng khi xong việc, không chờ người khác cập nhật hộ.

**Chú giải trạng thái:** ⬜ Chưa làm · 🔵 Đang làm · 🟡 Một phần (xem Ghi chú) · ✅ Xong

| US-ID | Tên | Trạng thái | Ghi chú | Cập nhật |
|---|---|---|---|---|
| [US-01](./backlog/US-01-quan-ly-danh-muc-line/README.md) | Quản lý danh mục Line | ✅ Xong | — | 2026-08-14 |
| [US-01a](./backlog/US-01a-cau-hinh-khung-gio-nghi-theo-line/README.md) | Khung giờ nghỉ theo Line | ✅ Xong | — | 2026-08-14 |
| [US-02](./backlog/US-02-quan-ly-danh-muc-cong-doan/README.md) | Quản lý danh mục Công đoạn | ✅ Xong | — | 2026-08-14 |
| [US-03](./backlog/US-03-cau-hinh-trinh-tu-cong-doan-cho-line/README.md) | Cấu hình trình tự công đoạn cho Line | ✅ Xong | — | 2026-08-17 |
| [US-04](./backlog/US-04-quan-ly-tram-lam-viec/README.md) | Quản lý trạm làm việc | ✅ Xong | — | 2026-08-14 |
| [US-04a](./backlog/US-04a-quan-ly-api-key-theo-tram/README.md) | Quản lý API Key theo trạm | ✅ Xong | — | 2026-08-14 |
| [US-05](./backlog/US-05-tao-cap-nhat-ke-hoach-san-xuat/README.md) | Tạo/cập nhật kế hoạch sản xuất | ✅ Xong | — | 2026-08-19 |
| [US-05a](./backlog/US-05a-vong-doi-trang-thai-ke-hoach/README.md) | Vòng đời trạng thái kế hoạch theo công đoạn | ✅ Xong | — | 2026-08-17 |
| [US-05b](./backlog/US-05b-chon-ap-dung-ke-hoach/README.md) | Chọn & áp dụng kế hoạch tại trạm | ✅ Xong | — | 2026-08-17 |
| [US-06](./backlog/US-06-san-luong-chuan-theo-gio/README.md) | Tính sản lượng chuẩn theo giờ | ✅ Xong | — | 2026-08-14 |
| [US-07](./backlog/US-07-scan-tem-tai-tram/README.md) | Scan tem tại trạm (luồng cơ bản) | ✅ Xong | — | 2026-08-17 |
| [US-08](./backlog/US-08-kiem-tra-hop-le-khi-scan/README.md) | Kiểm tra hợp lệ khi scan | ✅ Xong | — | 2026-08-14 |
| [US-09](./backlog/US-09-hien-thi-so-luong-chi-so-am-duong/README.md) | Hiển thị số lượng & chỉ số +/- tại trạm | 🟡 Một phần | — | 2026-08-17 |
| [US-10](./backlog/US-10-luu-tra-cuu-lich-su-scan/README.md) | Lưu & tra cứu lịch sử scan | ✅ Xong | — | 2026-08-19 |
| [US-11](./backlog/US-11-cau-hinh-bat-tat-arduino/README.md) | Bật/tắt Arduino theo trạm | ⬜ Chưa làm | — | 2026-08-14 |
| [US-12](./backlog/US-12-luong-scan-cho-ket-qua-arduino/README.md) | Luồng scan chờ Arduino | ⬜ Chưa làm | — | 2026-08-14 |
| [US-13](./backlog/US-13-timeout-kiem-tra-arduino/README.md) | Timeout xác định kết quả Arduino | ⬜ Chưa làm | — | 2026-08-14 |
| [US-14](./backlog/US-14-ket-noi-phuc-hoi-com-arduino/README.md) | Kết nối & phục hồi cổng COM | ⬜ Chưa làm | — | 2026-08-14 |
| [US-15](./backlog/US-15-khoi-phuc-trang-thai-phien-lam-viec/README.md) | Khôi phục trạng thái phiên khi mở lại | ⬜ Chưa làm | — | 2026-08-14 |
| [US-16](./backlog/US-16-hang-doi-cuc-bo-chong-mat-scan/README.md) | Hàng đợi cục bộ chống mất lượt scan | ⬜ Chưa làm | — | 2026-08-14 |
| [US-17](./backlog/US-17-hien-thi-trang-thai-dong-bo/README.md) | Hiển thị trạng thái đồng bộ trên UI | ⬜ Chưa làm | — | 2026-08-14 |
| [US-18](./backlog/US-18-scan-xac-nhan-san-pham-ng/README.md) | Scan xác nhận sản phẩm NG | 🟡 Một phần | — | 2026-08-18 |
| [US-19](./backlog/US-19-quy-trinh-rework/README.md) | Quy trình Rework | ✅ Xong | — | 2026-08-19 |
| [US-20](./backlog/US-20-bao-cao-ty-le-loi-nguyen-nhan/README.md) | Báo cáo tỷ lệ lỗi & nguyên nhân | ⬜ Chưa làm | — | 2026-08-14 |
| [US-21](./backlog/US-21-bao-cao-theo-lot/README.md) | Báo cáo theo Lot (tra cứu vòng đời sản xuất của 1 Lot) | 🟡 Một phần | — | 2026-08-19 |
| [US-21a](./backlog/US-21a-tong-so-luong-lot-nhap-tay/README.md) | "Tổng số lượng Lot" — nhập tay, không phải SUM | ✅ Xong | — | 2026-08-20 |
| [US-22](./backlog/US-22-quan-ly-nguoi-dung-phan-quyen/README.md) | Quản lý người dùng & phân quyền | ✅ Xong | — | 2026-08-14 |
| [US-23](./backlog/US-23-xuat-bao-cao-excel/README.md) | Xuất báo cáo Excel | 🟡 Một phần | — | 2026-08-19 |
| [US-24](./backlog/US-24-cau-hinh-quy-cach-dong-goi-theo-model/README.md) | Cấu hình Quy cách đóng gói theo Model (Đóng thùng) | ✅ Xong | — | 2026-08-24 |
| [US-25](./backlog/US-25-quet-tem-dong-thung/README.md) | Quét tem đóng thùng — đếm số lượng, tự động in tem thùng | 🟡 Một phần | — | 2026-08-24 |
| [US-26](./backlog/US-26-theo-doi-tien-do-dong-thung/README.md) | Theo dõi tiến độ đóng thùng ở mức quản lý | ✅ Xong | — | 2026-08-25 |
| [BUG-01](./backlog/BUG-01-lech-gio-utc-vn/README.md) | Lệch giờ 7 tiếng (UTC/VN) khi hiển thị trên web-admin | ✅ Xong | — | 2026-08-19 |
| [US-27](./backlog/US-27-xac-nhan-to-truong-truoc-khi-luu-scan-bi-tu-choi/README.md) | Xác nhận Tổ trưởng trước khi lưu lịch sử scan bị từ chối | ✅ Xong | Supersede US-25/AC8 | 2026-08-26 |

*(Chi tiết đầy đủ từng story — mô tả, Acceptance Criteria, ghi chú lịch sử triển khai — đã tách sang file riêng trong `Documents/backlog/US-XX-ten-tinh-nang/README.md`, xem quy ước tại `CLAUDE.md` mục "Theo dõi tiến độ backlog". Bảng trên chỉ giữ trạng thái tổng quan.)*

---

## 3.1 Nhóm chức năng: Quản lý danh mục Line & Công đoạn

- **US-01** — Quản lý danh mục Line (✅ Xong) → [chi tiết](./backlog/US-01-quan-ly-danh-muc-line/README.md)
- **US-01a** — Khung giờ nghỉ theo Line (✅ Xong) → [chi tiết](./backlog/US-01a-cau-hinh-khung-gio-nghi-theo-line/README.md)
- **US-02** — Quản lý danh mục Công đoạn (✅ Xong) → [chi tiết](./backlog/US-02-quan-ly-danh-muc-cong-doan/README.md)
- **US-03** — Cấu hình trình tự công đoạn cho Line (✅ Xong) → [chi tiết](./backlog/US-03-cau-hinh-trinh-tu-cong-doan-cho-line/README.md)
- **US-04** — Quản lý trạm làm việc (✅ Xong) → [chi tiết](./backlog/US-04-quan-ly-tram-lam-viec/README.md)
- **US-04a** — Quản lý API Key theo trạm (✅ Xong) → [chi tiết](./backlog/US-04a-quan-ly-api-key-theo-tram/README.md)

## 3.2 Nhóm chức năng: Kế hoạch sản xuất

- **US-05** — Tạo/cập nhật kế hoạch sản xuất (✅ Xong) → [chi tiết](./backlog/US-05-tao-cap-nhat-ke-hoach-san-xuat/README.md)
- **US-05a** — Vòng đời trạng thái kế hoạch theo công đoạn (✅ Xong) → [chi tiết](./backlog/US-05a-vong-doi-trang-thai-ke-hoach/README.md)
- **US-05b** — Chọn & áp dụng kế hoạch tại trạm (✅ Xong) → [chi tiết](./backlog/US-05b-chon-ap-dung-ke-hoach/README.md)
- **US-06** — Tính sản lượng chuẩn theo giờ (✅ Xong) → [chi tiết](./backlog/US-06-san-luong-chuan-theo-gio/README.md)

## 3.3 Nhóm chức năng: Scan tem & theo dõi sản xuất

- **US-07** — Scan tem tại trạm (luồng cơ bản) (✅ Xong) → [chi tiết](./backlog/US-07-scan-tem-tai-tram/README.md)
- **US-08** — Kiểm tra hợp lệ khi scan (✅ Xong) → [chi tiết](./backlog/US-08-kiem-tra-hop-le-khi-scan/README.md)
- **US-09** — Hiển thị số lượng & chỉ số +/- tại trạm (🟡 Một phần) → [chi tiết](./backlog/US-09-hien-thi-so-luong-chi-so-am-duong/README.md)
- **US-10** — Lưu & tra cứu lịch sử scan (✅ Xong) → [chi tiết](./backlog/US-10-luu-tra-cuu-lich-su-scan/README.md)
- **US-27** — Xác nhận Tổ trưởng trước khi lưu lịch sử scan bị từ chối (✅ Xong) → [chi tiết](./backlog/US-27-xac-nhan-to-truong-truoc-khi-luu-scan-bi-tu-choi/README.md)

## 3.4 Nhóm chức năng: Giao tiếp Arduino (kiểm tra tự động)

- **US-11** — Bật/tắt Arduino theo trạm (⬜ Chưa làm) → [chi tiết](./backlog/US-11-cau-hinh-bat-tat-arduino/README.md)
- **US-12** — Luồng scan chờ Arduino (⬜ Chưa làm) → [chi tiết](./backlog/US-12-luong-scan-cho-ket-qua-arduino/README.md)
- **US-13** — Timeout xác định kết quả Arduino (⬜ Chưa làm) → [chi tiết](./backlog/US-13-timeout-kiem-tra-arduino/README.md)
- **US-14** — Kết nối & phục hồi cổng COM (⬜ Chưa làm) → [chi tiết](./backlog/US-14-ket-noi-phuc-hoi-com-arduino/README.md)

## 3.5 Nhóm chức năng: Khôi phục trạng thái & chống mất dữ liệu

- **US-15** — Khôi phục trạng thái phiên khi mở lại (⬜ Chưa làm) → [chi tiết](./backlog/US-15-khoi-phuc-trang-thai-phien-lam-viec/README.md)
- **US-16** — Hàng đợi cục bộ chống mất lượt scan (⬜ Chưa làm) → [chi tiết](./backlog/US-16-hang-doi-cuc-bo-chong-mat-scan/README.md)
- **US-17** — Hiển thị trạng thái đồng bộ trên UI (⬜ Chưa làm) → [chi tiết](./backlog/US-17-hien-thi-trang-thai-dong-bo/README.md)

## 3.6 Nhóm chức năng: Scan NG & Quy trình Rework

- **US-18** — Scan xác nhận sản phẩm NG (🟡 Một phần) → [chi tiết](./backlog/US-18-scan-xac-nhan-san-pham-ng/README.md)
- **US-19** — Quy trình Rework (✅ Xong) → [chi tiết](./backlog/US-19-quy-trinh-rework/README.md)
- **US-20** — Báo cáo tỷ lệ lỗi & nguyên nhân (⬜ Chưa làm) → [chi tiết](./backlog/US-20-bao-cao-ty-le-loi-nguyen-nhan/README.md)

## 3.7 Nhóm chức năng: Báo cáo & quản trị

- **US-21** — Báo cáo theo Lot (tra cứu vòng đời sản xuất của 1 Lot) (🟡 Một phần) → [chi tiết](./backlog/US-21-bao-cao-theo-lot/README.md)
- **US-21a** — "Tổng số lượng Lot" — nhập tay, không phải SUM (✅ Xong) → [chi tiết](./backlog/US-21a-tong-so-luong-lot-nhap-tay/README.md)
- **US-22** — Quản lý người dùng & phân quyền (✅ Xong) → [chi tiết](./backlog/US-22-quan-ly-nguoi-dung-phan-quyen/README.md)
- **US-23** — Xuất báo cáo Excel (🟡 Một phần) → [chi tiết](./backlog/US-23-xuat-bao-cao-excel/README.md)

## 3.8 Nhóm chức năng: Đóng thùng

*(Bổ sung 20/08/2026 — do agent `ba` viết theo yêu cầu tích hợp lại nghiệp vụ từ app WinForms cũ `PrintLabel` (DB `line_andon_history1`) vào kiến trúc MES hiện tại. Xem SRS mục 1.2/8.1 (đảo ngược quyết định trước đó), FR-24/FR-25/FR-26, mục 6 quy tắc 16, mục 7 AC-29→AC-32, mục 8.2 (5 điểm mở mới). Phạm vi 3 story được chia tách vì đây là 3 mối quan tâm độc lập (danh mục cấu hình / luồng scan vận hành / báo cáo giám sát), có thể triển khai và kiểm thử riêng lẻ.)*

- **US-24** — Cấu hình Quy cách đóng gói theo Model (Đóng thùng) (✅ Xong) → [chi tiết](./backlog/US-24-cau-hinh-quy-cach-dong-goi-theo-model/README.md)
- **US-25** — Quét tem đóng thùng — đếm số lượng, tự động in tem thùng (🟡 Một phần) → [chi tiết](./backlog/US-25-quet-tem-dong-thung/README.md)
- **US-26** — Theo dõi tiến độ đóng thùng ở mức quản lý (⬜ Chưa làm) → [chi tiết](./backlog/US-26-theo-doi-tien-do-dong-thung/README.md)

---

## THỨ TỰ TRIỂN KHAI ĐỀ XUẤT

**Giai đoạn 1 — Dữ liệu nền tảng (danh mục & cấu hình)**
1. US-01 (Line)
2. US-01a (Khung giờ nghỉ theo Line) — cần US-01 xong trước; là điều kiện tiên quyết cho US-09 AC5/AC6
3. US-02 (Công đoạn master)
4. US-04 (Trạm làm việc) — cần Line + Công đoạn xong trước
5. US-04a (API Key theo trạm) — cần US-04 xong trước; là điều kiện tiên quyết bắt buộc cho US-07/US-08 (`Station.Wpf` không xác thực được nếu thiếu story này)
6. US-05 (Kế hoạch sản xuất — màn hình Cài đặt kế hoạch)
7. US-03 (Cấu hình trình tự công đoạn cho Line) — thực ra chỉ cần US-01/US-02 xong trước (không phụ thuộc US-05 nữa, xem sửa 17/08/2026), đặt sau US-05 trong danh sách này chỉ vì lý do lịch sử triển khai
8. US-05a (Vòng đời trạng thái kế hoạch theo từng công đoạn) — cần US-03 xong trước (cần biết công đoạn nào thuộc trình tự của Line để suy ra các cặp (Kế hoạch, Công đoạn) cần theo dõi)
9. US-05b (Chọn & áp dụng kế hoạch — màn hình Chọn kế hoạch) — cần US-05a xong trước
10. US-06 (Tính sản lượng chuẩn theo giờ)
11. US-22 (Quản lý người dùng & phân quyền)

*Lý do*: Đây là toàn bộ dữ liệu master mà mọi luồng nghiệp vụ phía sau (scan, Arduino, rework, báo cáo) đều phụ thuộc trực tiếp. US-01a và US-04a được chèn ngay sau story gốc của chúng (US-01/US-04) vì đây là 2 khoảng trống phát sinh sau khi US-01/US-04 đã code xong (FR-01/FR-09a bổ sung sau; ADR-005 chốt sau) — bắt buộc phải xong trước khi bước vào Giai đoạn 2, nếu không US-07/US-08/US-09 sẽ bị chặn giữa chừng. US-05a/US-05b tương tự được chèn ngay sau US-05/US-03 vì đây là khoảng trống phát sinh sau khi US-05 gốc đã lập (FR-05a bổ sung sau khi phân tích UI 13/08/2026) — US-07 (`Station.Wpf` lấy "kế hoạch active của trạm") cần US-05a đã có khái niệm `Running` theo (Line, Công đoạn) mới hoạt động đúng. Phân quyền (US-22) đặt sớm trong giai đoạn này vì US-12 (xác nhận Arduino) và US-19 (mở khóa rework) đều cần cơ chế đăng nhập/phân quyền Tổ trưởng để hoạt động đúng ngay từ khi build luồng lõi.

**Giai đoạn 2 — Luồng scan lõi (happy path)**
12. US-07 (Scan tem tại trạm)
13. US-08 (Kiểm tra hợp lệ khi scan — chống trùng tem, công đoạn liền trước)
14. US-09 (Hiển thị số lượng & chỉ số +/-)
15. US-10 (Lưu & tra cứu lịch sử scan)
16. US-15 (Khôi phục trạng thái phiên khi mở lại bình thường)

*Lý do*: Đây là lõi nghiệp vụ trung tâm của toàn hệ thống (scan → kiểm tra → cập nhật số liệu → lưu lịch sử). Cần hoàn thiện và ổn định trước khi thêm các nhánh phức tạp hơn (Arduino, offline, NG) vì các nhánh đó đều là biến thể/mở rộng của luồng scan cơ bản này.

**Giai đoạn 3 — Chống mất dữ liệu (offline/crash)**
17. US-16 (Hàng đợi cục bộ chống mất lượt scan)
18. US-17 (Hiển thị trạng thái đồng bộ trên UI)

*Lý do*: Đây là lớp bọc thêm quanh luồng scan lõi (ghi trước vào local queue, retry, idempotency) — cần scan lõi hoạt động ổn định trước, sau đó mới bọc thêm cơ chế an toàn dữ liệu này vì nó thay đổi cách trạm gửi/nhận kết quả scan.

**Giai đoạn 4 — Nhánh phụ: Scan NG & Rework**
19. US-18 (Scan xác nhận sản phẩm NG)
20. US-19 (Quy trình Rework — mở khóa & scan lại)
21. US-20 (Báo cáo tỷ lệ lỗi & nguyên nhân)

*Lý do*: NG là nhánh rẽ có điều kiện của luồng scan (không phải mọi lượt scan đều NG), và có ràng buộc dữ liệu phức tạp hơn (nhiều bản ghi tại cùng công đoạn, thay đổi ràng buộc unique) nên làm sau khi luồng OK cơ bản đã vững. Báo cáo tỷ lệ lỗi (US-20) đặt cuối nhóm này vì cần có dữ liệu NG/OK thực tế để thống kê.

**Giai đoạn 5 — Nhánh phụ: Arduino**
22. US-11 (Cấu hình bật/tắt Arduino theo trạm)
23. US-14 (Kết nối & phục hồi cổng COM)
24. US-13 (Timeout xác định kết quả kiểm tra)
25. US-12 (Luồng scan-chờ-Arduino đầy đủ, bao gồm nhánh xác nhận NG bởi Tổ trưởng)

*Lý do*: Arduino là nhánh phụ thuộc phần cứng đặc thù, chỉ áp dụng cho một số trạm/công đoạn nhất định (chưa xác định rõ theo mục 8.2). Đặt sau NG/Rework vì US-12 (bước 5) tái sử dụng trực tiếp cơ chế xác nhận NG + khóa/mở khóa rework đã xây ở Giai đoạn 4. Nên làm US-11/US-14 (cấu hình, kết nối) trước US-12/US-13 (logic state machine đầy đủ) vì đây là điều kiện kỹ thuật cần có trước khi build luồng nghiệp vụ chờ kết quả.

**Giai đoạn 6 — Báo cáo tổng hợp & xuất Excel**
26. US-21 (Báo cáo tổng hợp theo Line)
27. US-23 (Xuất báo cáo Excel)

*Lý do*: Báo cáo phụ thuộc vào dữ liệu đã tích lũy đầy đủ từ tất cả các luồng trước (scan OK, chỉ số +/-, trạng thái đồng bộ, dữ liệu NG). Xuất Excel (US-23) làm cuối cùng vì còn phụ thuộc thêm vào việc xác nhận nội dung/mẫu báo cáo cụ thể (điểm mở #4 mục 8.2 — cần hỏi lại stakeholder trước khi code phần này).

**Giai đoạn 7 — Đóng thùng** *(bổ sung 20/08/2026)*
28. US-24 (Cấu hình Quy cách đóng gói theo Model)
29. US-25 (Quét tem đóng thùng — đếm số lượng, tự động in tem thùng) — cần US-24 xong trước (AC11 chặn scan khi chưa có cấu hình)
30. US-26 (Theo dõi tiến độ đóng thùng ở mức quản lý) — cần US-25 xong trước (cần dữ liệu đóng thùng thực tế để tổng hợp), tái dùng `Lot.TotalQuantity` từ US-21a (Giai đoạn 6)

*Lý do*: "Đóng thùng" tái sử dụng gần như toàn bộ nền tảng đã có (danh mục Line/Công đoạn Giai đoạn 1, engine chống trùng + kiểm tra trình tự FR-08 của Giai đoạn 2, cơ chế re-auth Supervisor của US-18 Giai đoạn 4, `Lot.TotalQuantity` của US-21a Giai đoạn 6) nên đặt sau cùng, không chặn các luồng lõi khác. US-24 (danh mục) phải xong trước US-25 (vận hành) vì AC11 chặn cứng khi Model chưa có cấu hình; US-26 (báo cáo) đặt cuối vì cần dữ liệu đóng thùng thực tế mới có gì để tổng hợp.

---

## GHI CHÚ CHUNG VỀ CÁC ĐIỂM CÒN MỞ (mục 8.2 SRS)

9 điểm sau đây được gắn cờ cảnh báo tại từng story liên quan ở trên (điểm gạch ngang đã chốt 20/08/2026, xem chi tiết quyết định ngay sau mỗi điểm) — dev cần hỏi lại đúng các điểm CÒN MỞ trước khi triển khai phần liên quan, dù không chặn việc code phần khung/logic chung:

1. **Số lượng Line & danh sách công đoạn cụ thể từng Line** — ảnh hưởng US-01, US-03 (khi cấu hình dữ liệu thật, không ảnh hưởng thiết kế chức năng).
2. **Danh sách công đoạn cụ thể dùng Arduino** — ảnh hưởng US-02 (gắn cờ), US-04, US-11, US-12 (phạm vi trạm cần triển khai state machine Arduino).
3. **Model máy scan có đồng nhất Zebra DS2208 hay không** — ảnh hưởng US-04, US-07 (thiết kế giao tiếp HID có áp dụng chung cho mọi trạm hay cần xử lý riêng cho model khác).
4. **Nội dung cụ thể báo cáo Excel (cột dữ liệu, cách nhóm/tổng hợp)** — ảnh hưởng trực tiếp US-23, cần xác nhận trước khi thiết kế mẫu xuất file.
5. ~~Model trong cấu hình đóng gói (US-24/FR-24) khớp bằng so chuỗi tuyệt đối với `ProductionPlan.Model`~~ — **Đã chốt 20/08/2026: không phân biệt hoa/thường + tự trim + autocomplete**, chỉ áp dụng riêng bước tra cứu cấu hình đóng gói, giữ nguyên free-text (không tách danh mục Model riêng), không đổi cách dùng Model ở nơi khác trong hệ thống.
6. ~~Sau khi Supervisor xác nhận tem trùng tại "Đóng thùng" (US-25/AC8)~~ — **Đã chốt 20/08/2026: KHÔNG cộng thêm số lượng**, chỉ audit (xác nhận đã biết tình huống).
7. ~~Máy in gặp lỗi đúng lúc đủ số lượng để in tem thùng (US-25/AC4)~~ — **Đã chốt 20/08/2026: không chặn** đóng thùng kế tiếp, luôn có nút In lại thủ công (chỉ chặn khi chính lệnh gọi in thất bại, không phát hiện được lỗi vật lý sau khi đã gửi lệnh in).
8. ~~Cấu hình Quy cách đóng gói (US-24) thay đổi khi đang đóng dở 1 thùng~~ — **Đã chốt 20/08/2026: không hồi tố**, thùng đang dở giữ nguyên số lượng mục tiêu đã snapshot lúc mở thùng, quy cách mới chỉ áp dụng cho thùng mở sau.
9. ~~Có cần 1 trạng thái riêng đánh dấu "đã đóng thùng xong toàn bộ Lot"~~ — **Đã chốt 20/08/2026: không cần**, chỉ hiển thị % hoàn thành/nhãn Đủ-Chưa đủ dùng lại từ US-21/US-21a.
