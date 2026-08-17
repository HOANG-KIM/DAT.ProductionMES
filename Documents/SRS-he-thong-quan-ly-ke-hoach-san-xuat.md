# TÀI LIỆU ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS)
## Hệ thống Quản lý Kế hoạch Sản xuất (Production MES)

**Phiên bản:** 1.0
**Ngày:** 11/08/2026
**Trạng thái:** Dự thảo — chờ khách hàng xác nhận trước khi triển khai phát triển

---

## 1. GIỚI THIỆU

### 1.1 Mục đích
Tài liệu này đặc tả đầy đủ yêu cầu chức năng và phi chức năng của hệ thống quản lý kế hoạch sản xuất, làm cơ sở để:
- Đội phát triển thiết kế và xây dựng phần mềm.
- Khách hàng xác nhận đúng những gì sẽ được xây dựng trước khi vào giai đoạn code.
- Làm căn cứ nghiệm thu (acceptance criteria) khi bàn giao sản phẩm.

### 1.2 Phạm vi
Hệ thống quản lý việc theo dõi sản xuất tại các công đoạn (lắp ráp, thông điện, ngoại quan…) trên nhiều Line sản xuất, thông qua việc scan tem sản phẩm tại từng trạm, đối chiếu với kế hoạch sản xuất (takt time, sản lượng mục tiêu), và giao tiếp với thiết bị Arduino qua cổng COM tại một số trạm.

Phạm vi **bao gồm**:
- Web API trung tâm + cơ sở dữ liệu MySQL.
- Ứng dụng WPF tại từng trạm làm việc.
- Giao tiếp Serial (COM port) với Arduino.

Phạm vi **không bao gồm** (trừ khi có yêu cầu bổ sung riêng):
- Tích hợp ERP/MES cấp cao hơn của nhà máy.
- Ứng dụng di động cho quản lý xem báo cáo từ xa.
- Cấu hình logic bên trong firmware Arduino (chỉ quy định giao thức lệnh gửi/nhận).
- **Module Đóng thùng**: mô hình xử lý khác hẳn các công đoạn còn lại (đếm lũy kế số lượng sản phẩm theo thùng, in tem thùng khi đủ số lượng cài đặt, thay vì scan-đối-chiếu-công-đoạn-trước từng sản phẩm) — dự kiến là 1 ứng dụng/quy trình riêng biệt, sẽ đặc tả trong tài liệu riêng khi có quyết định chính thức. Không còn xếp chung vào danh mục công đoạn của hệ thống này.

### 1.3 Định nghĩa & thuật ngữ

| Thuật ngữ | Giải thích |
|---|---|
| Line / Dây chuyền | Một chuyền sản xuất vật lý, gồm nhiều trạm nối tiếp nhau |
| Công đoạn | Một bước xử lý trên sản phẩm (lắp ráp, thông điện, ngoại quan…), dùng chung danh mục cho mọi Line — không bao gồm Đóng thùng (xem mục 1.2, phạm vi không bao gồm) |
| Trạm làm việc | Vị trí vật lý (PC + màn hình + máy scan, có thể kèm Arduino) thực hiện 1 công đoạn của 1 Line cụ thể |
| Tem | Mã định danh duy nhất dán trên từng sản phẩm, được scan tại mỗi công đoạn |
| Takt time | Thời gian tiêu chuẩn (giây) để hoàn thành 1 sản phẩm tại 1 công đoạn, dùng làm cơ sở tính sản lượng/giờ |
| Kế hoạch sản xuất | Kế hoạch cho 1 lô/ca sản xuất trên 1 Line cụ thể, gồm sản phẩm, số lượng mục tiêu, takt time |
| Chỉ số âm/dương | Chênh lệch giữa sản lượng thực tế lũy kế và sản lượng kế hoạch lũy kế tại một thời điểm |
| Khung giờ nghỉ | Khoảng thời gian (giờ bắt đầu–giờ kết thúc) không sản xuất, cấu hình theo từng Line (vd nghỉ trưa 12:00–13:00, nghỉ giữa giờ) — dùng để tính đúng sản lượng kế hoạch lũy kế (xem FR-01, FR-09a) |

### 1.4 Tài liệu tham chiếu
Tài liệu thiết kế kiến trúc hệ thống (kèm sơ đồ, schema CSDL, các interface chính) đã được thống nhất trong quá trình trao đổi trước SRS này — dùng làm tài liệu kỹ thuật đi kèm, không lặp lại chi tiết trong SRS.

---

## 2. MÔ TẢ TỔNG QUAN

### 2.1 Bối cảnh hệ thống
Một server trung tâm (Web API + MySQL) phục vụ toàn bộ nhà máy, gồm nhiều Line, mỗi Line có nhiều trạm làm việc. WPF tại mỗi trạm là client, giao tiếp với server qua REST API và SignalR (real-time), một số trạm giao tiếp thêm với Arduino qua cổng COM.

### 2.2 Đối tượng người dùng

| Nhóm người dùng | Vai trò |
|---|---|
| Công nhân vận hành trạm | Scan tem, xem số lượng/chỉ số +/- tại trạm mình phụ trách |
| Tổ trưởng / Quản lý chuyền | Cấu hình kế hoạch sản xuất, công đoạn, trình tự cho Line phụ trách; xem báo cáo, lịch sử |
| Quản trị hệ thống (Admin) | Quản lý danh mục Line, công đoạn, người dùng, cấu hình thiết bị (COM port, Arduino) |
| Ban quản lý / Văn phòng | Xem báo cáo tổng hợp tất cả các Line theo thời gian thực |

### 2.3 Giả định & ràng buộc
- Mỗi sản phẩm có duy nhất 1 tem, tem không được dán lại/tái sử dụng cho sản phẩm khác trong cùng đợt sản xuất.
- Mạng LAN nội bộ nhà máy đã sẵn sàng, ổn định, kết nối được tất cả các trạm về server trung tâm.
- Máy scan tem tại các trạm sử dụng model **Zebra DS2208** — đã xác nhận hỗ trợ chế độ non-keyboard ("IBM Hand-Held USB" / "USB HID POS", ngoài chế độ mặc định "USB Keyboard HID"), cấu hình bằng cách quét mã vạch thiết lập tương ứng trong Quick Start Guide của Zebra. Thiết kế dùng HID library (mục 5.1) khả thi với model này.
- Thiết bị Arduino tại các trạm cần Arduino đã có, hoặc được cung cấp giao thức lệnh (protocol) rõ ràng trước khi phát triển mục 3.4.

---

## 3. YÊU CẦU CHỨC NĂNG

### 3.1 Nhóm chức năng: Quản lý danh mục Line & Công đoạn

**FR-01 — Quản lý danh mục Line**
- Thêm/sửa/vô hiệu hóa Line sản xuất.
- Mỗi Line có tên, mô tả, trạng thái hoạt động.
- Mỗi Line có thể cấu hình **0 hoặc nhiều khung giờ nghỉ** (giờ bắt đầu, giờ kết thúc, ghi chú — vd nghỉ trưa 12:00–13:00, nghỉ giữa giờ 15:00–15:15). Khung giờ nghỉ áp dụng chung cho **mọi kế hoạch sản xuất chạy trên Line đó** (không cấu hình riêng theo từng kế hoạch), dùng làm cơ sở tính sản lượng kế hoạch lũy kế tại FR-09a.

**FR-02 — Quản lý danh mục Công đoạn (master)**
- Thêm/sửa/vô hiệu hóa công đoạn dùng chung cho toàn hệ thống (vd: Lắp ráp, Thông điện, Ngoại quan).
- Công đoạn không gắn cố định với 1 Line — cùng 1 công đoạn có thể được áp dụng ở nhiều Line khác nhau.

**FR-03 — Cấu hình trình tự công đoạn cho từng Line**
- Trình tự công đoạn (Stage nào thuộc Line, theo thứ tự nào) là **cấu hình của Line**, thiết lập 1 lần và dùng chung cho **mọi** kế hoạch sản xuất chạy trên Line đó — **không** cấu hình lại riêng cho từng kế hoạch. Sửa trình tự (thêm/bớt/sắp xếp lại) có hiệu lực **ngay lập tức** cho mọi kế hoạch đang chạy trên Line, không cần deploy lại phần mềm.
- Với mỗi Line, người dùng có quyền (Tổ trưởng/Admin) có thể:
  - Thêm công đoạn từ danh mục master vào trình tự của Line.
  - Bớt (gỡ) công đoạn khỏi trình tự của Line — **chặn hẳn (từ chối)** nếu đang có bất kỳ kế hoạch nào của Line đó ở trạng thái `Running`/`Paused` tại đúng công đoạn sắp gỡ (xem FR-05a); phải Tạm dừng/Đóng kế hoạch tại công đoạn đó trước.
  - Sắp xếp lại **trình tự** các công đoạn (kéo-thả hoặc nhập số thứ tự).
- Hệ thống lưu công đoạn "liền trước" của mỗi công đoạn dựa trên trình tự đã cấu hình **của Line**, dùng cho FR-08.
- **Điều kiện**: không cho lưu nếu trình tự bị trùng số thứ tự hoặc 1 công đoạn xuất hiện quá 1 lần trong trình tự (gây vòng lặp khi suy "liền trước").
- **Mọi kế hoạch sản xuất tạo trên 1 Line đều đi qua đúng và đủ toàn bộ các công đoạn đã cấu hình cho Line đó, theo đúng thứ tự** — không có khái niệm 1 kế hoạch chỉ áp dụng 1 tập con công đoạn của Line.
- **Không hồi tố lịch sử scan**: khi trình tự Line thay đổi lúc đang có kế hoạch chạy dở, các lượt scan **đã ghi nhận trước đó** giữ nguyên bất biến (nhất quán với nguyên tắc snapshot ở FR-10/mục 6 quy tắc 14) — trình tự mới chỉ áp dụng cho việc kiểm tra "công đoạn liền trước" (FR-08) từ thời điểm đổi trở đi.

**FR-04 — Quản lý trạm làm việc**
- Mỗi trạm gắn với đúng 1 Line và 1 công đoạn.
- Cấu hình thông tin cổng COM (nếu trạm có kết nối Arduino): cổng, baud rate, giao thức lệnh.

### 3.2 Nhóm chức năng: Kế hoạch sản xuất

**FR-05 — Tạo/cập nhật kế hoạch sản xuất**
- Nhập: Line áp dụng, Khách hàng, Model (thay thế cặp `ProductCode`/`ProductName` cũ — gộp thành 1 cột duy nhất), Lot, Revision (có thể để trống), số lượng kế hoạch (theo Lot), takt time (giây/sản phẩm), thời gian bắt đầu (ngày + giờ), tên nhân viên vận hành tại trạm/công đoạn phụ trách lô này (có thể nhiều người — không phải người đăng nhập thao tác màn hình cấu hình, vốn đã có audit riêng theo tài khoản).
- Khi nhập takt time, hệ thống hiển thị ngay sản lượng chuẩn/giờ tính theo FR-06 để người dùng phát hiện sớm lỗi nhập liệu trước khi lưu.
- **Đơn vị lưu trữ/tính toán khác đơn vị nhập liệu/hiển thị trên UI** (chốt 17/08/2026): `TaktTimeSeconds` và `StartTime` (DateTime đầy đủ ngày+giờ) là đơn vị **lưu trữ và tính toán** ở backend, không phải format bắt buộc trên UI. Trên `Station.Wpf`: Takt time **nhập/hiển thị dạng phút:giây** (`m:ss`, vd "1:30" = 90 giây), hệ thống tự quy đổi sang giây khi gửi API/tính FR-06; Thời gian bắt đầu **nhập/hiển thị đủ Ngày + Giờ:Phút** (24h, `HH:mm`), không chỉ chọn Ngày. Xem chi tiết AC1d/AC1e ở US-05.
- Một cặp **(Line, Công đoạn)** trong một thời điểm chỉ có tối đa 1 kế hoạch ở trạng thái `Running` (xem FR-05a) — tránh nhầm lẫn khi tính sản lượng. **Không** ràng buộc theo cả Line: các công đoạn khác nhau của cùng 1 Line được phép chạy các kế hoạch khác nhau cùng lúc, vì thực tế dây chuyền luôn có WIP (bán thành phẩm tồn) giữa các công đoạn khiến công đoạn sau "đi trễ hơn" công đoạn trước.
- **Khóa tuyệt đối Khách hàng/Model/Lot/Revision khi kế hoạch đã có ít nhất 1 bản ghi scan**: một khi kế hoạch đã phát sinh scan (bất kể kết quả OK/NG/lỗi, bất kể `PlanStatus` của công đoạn đó là `Running`/`Paused`/`Completed`/`Cancelled` — xem FR-05a), hệ thống **không cho sửa Khách hàng/Model/Lot/Revision dưới bất kỳ hình thức nào**, kể cả có xác nhận (Confirm) — khác hẳn với quy tắc áp dụng cho Số lượng/Takt time ở dưới. Nếu nhập sai các trường này sau khi đã có scan, nghiệp vụ đúng là **tạo kế hoạch mới**, không sửa kế hoạch cũ, để không làm sai lệch dữ liệu snapshot đã lưu trong lịch sử scan (FR-10).
- **Số lượng kế hoạch/Takt time vẫn theo quy tắc cũ, không đổi**: khi kế hoạch đã có công đoạn `Running`/`Paused`, sửa Số lượng/Takt time vẫn cho phép nhưng yêu cầu xác nhận (Confirm) trước khi lưu — 2 quy tắc (khóa tuyệt đối Khách hàng/Model/Lot/Revision, và cảnh báo-xác nhận Số lượng/Takt time) là **độc lập nhau**, không dùng chung 1 điều kiện kích hoạt.

**FR-05a — Vòng đời trạng thái kế hoạch sản xuất (tạm dừng — chạy lại nhiều lần, đóng độc lập theo từng công đoạn)**
- Phát sinh từ tình huống thực tế: 1 kế hoạch (lot) có thể chạy dở rồi tạm ngưng (đổi Line/trạm sang việc khác), sau đó chạy tiếp lại vào thời điểm khác — cần phân biệt rõ với kế hoạch đã hoàn thành, tránh chạy nhầm lại từ đầu số lượng gốc.
- **Mỗi cặp (Kế hoạch, Công đoạn) có trạng thái riêng độc lập**: `Draft` (chưa từng chạy) → `Running` (đang active) ⇄ `Paused` (tạm dừng, chưa đủ số lượng) → `Completed` (đã đủ số lượng) hoặc `Cancelled` (đóng sớm dù chưa đủ, do quyết định nghiệp vụ) — vì các công đoạn của cùng 1 kế hoạch hoàn thành ở thời điểm khác nhau. Ví dụ: kế hoạch áp dụng cho công đoạn A, B, C — công đoạn A chạy đủ số lượng thì **A tự động đóng `Completed` ngay**, trong khi B, C vẫn tiếp tục `Running`/`Paused` độc lập cho tới khi đủ số lượng riêng của từng công đoạn.
- Tự động chuyển `Completed` khi số lượng đã chạy (tính động) tại đúng công đoạn đó đạt đủ `PlannedQuantity`; **đồng thời** cho phép Tổ trưởng đóng sớm thủ công (`→ Cancelled`) cho 1 công đoạn cụ thể dù chưa đủ số lượng — cả 2 cơ chế cùng tồn tại.
- **"Đã chạy" / "còn lại" luôn tính động** từ lịch sử scan kết quả OK theo cặp (kế hoạch, công đoạn) — không lưu số liệu tĩnh cộng dồn, tránh lệch dữ liệu khi có rework/scan hủy.
- 2 hành động tách riêng khi ngừng chạy 1 công đoạn của kế hoạch: **"Tạm dừng"** (`Running → Paused`, có thể "Áp dụng" lại bất kỳ lúc nào, tiến độ được giữ nguyên vì tính động) và **"Đóng kế hoạch"** (`→ Completed`/`Cancelled`, yêu cầu xác nhận nếu số lượng thực tế còn thấp hơn kế hoạch).
- Màn hình chọn kế hoạch (mục 5.2) hiển thị rõ tiến độ cho từng cặp (Kế hoạch, Công đoạn) đang `Paused` (vd `Đã chạy 400/1000 — còn 600`), mặc định ẩn khỏi danh sách chọn các cặp đã `Completed`/`Cancelled`.
- **Đã chốt phạm vi ràng buộc active**: theo cặp **(Line, Công đoạn)**, không theo cả Line (xem FR-05 đã cập nhật) — công đoạn A của 1 Line có thể đang `Running` kế hoạch mới trong khi công đoạn B, C của cùng Line vẫn `Running`/`Paused` kế hoạch cũ, đúng thực tế dây chuyền có WIP giữa các trạm.

**FR-06 — Tính sản lượng chuẩn theo giờ**
- Hệ thống tự tính: `Sản lượng/giờ = 3600 / Takt time`.
- Hiển thị giá trị này trên màn hình cấu hình kế hoạch và màn hình trạm.

### 3.3 Nhóm chức năng: Scan tem & theo dõi sản xuất

**FR-07 — Scan tem tại trạm**
- Người vận hành scan tem sản phẩm; hệ thống ghi nhận thời gian, trạm, công đoạn, Line, kế hoạch tương ứng.
- Sau khi scan hợp lệ, số lượng đã scan tại trạm được cập nhật ngay trên màn hình (real-time).
- Khi scan thành công (kết quả OK), hiển thị **thông báo xác nhận đã lưu** (dạng popup/toast) cho công nhân biết lượt scan đã được ghi nhận, kèm mã tem vừa scan.
  - Thông báo phải **tự động biến mất sau 1–2 giây** (không chặn thao tác), để không làm chậm nhịp scan liên tục trên dây chuyền.
  - Thông báo không được chèn giữa màn hình che số liệu chính (số lượng, chỉ số +/-) — nên hiển thị ở góc màn hình hoặc dạng banner nhỏ.
  - Khi scan lỗi (Trùng tem / Chưa qua công đoạn trước / NG), thông báo phải có màu sắc và âm thanh khác biệt rõ ràng với thông báo OK, đồng thời **cần người vận hành xác nhận đã đọc** (không tự tắt) vì đây là tình huống cần được xử lý, không nên bỏ lỡ.

**FR-08 — Kiểm tra hợp lệ khi scan (business rule đã chốt)**
Khi nhận 1 lượt scan, hệ thống thực hiện tuần tự:
1. **Kiểm tra trùng tem theo công đoạn — phạm vi toàn hệ thống, không phân biệt Line**: nếu tem đã được scan OK tại **cùng loại công đoạn này** ở bất kỳ Line nào (kể cả Line hiện tại) → từ chối, báo lỗi "Trùng tem tại công đoạn này".
2. **Kiểm tra đã qua công đoạn liền trước hay chưa** (theo trình tự cấu hình **của Line** ở FR-03 — "liền trước" suy ra từ trình tự của đúng Line nơi đang scan), việc tra cứu "đã qua chưa" thì trên toàn hệ thống (không giới hạn theo Line, vì công đoạn trước có thể đã thực hiện ở Line khác) → nếu chưa, từ chối và nêu rõ tên công đoạn còn thiếu.
3. Nếu qua cả 2 bước kiểm tra → ghi nhận lượt scan là hợp lệ (OK).

> **Ví dụ minh họa rule đã thống nhất**: Tem A đã "Lắp ráp" ở Line 1 → không thể "Lắp ráp" lại ở Line 2/3 (kể cả Line 1 lần 2). Nhưng tem A có thể "Thông điện" ở Line 2 hoặc Line 3 bình thường, dù trước đó lắp ráp ở Line 1.

**FR-09 — Hiển thị số lượng và chỉ số âm/dương**
- Màn hình trạm hiển thị: số lượng đã scan OK (lũy kế theo ca/kế hoạch hiện tại), sản lượng kế hoạch lũy kế đến thời điểm hiện tại, và **chênh lệch (Thực tế − Kế hoạch)**.
- Chênh lệch dương → hiển thị màu xanh (vượt tiến độ); âm → hiển thị màu đỏ (trễ tiến độ).
- Giá trị cập nhật ngay khi có lượt scan mới (real-time), không cần thao tác làm mới thủ công.

**FR-09a — Trừ thời gian nghỉ khi tính sản lượng kế hoạch lũy kế**
- Sản lượng kế hoạch lũy kế tại FR-09 = Sản lượng chuẩn/giờ (FR-06, `3600 / Takt time`) × **thời gian làm việc thực tế** đã trôi qua kể từ giờ bắt đầu ca, sau khi **trừ đi phần giao với các khung giờ nghỉ đã cấu hình cho Line** (FR-01) tính đến thời điểm hiện tại.
- Trong lúc đang ở trong 1 khung giờ nghỉ, sản lượng kế hoạch lũy kế **dừng tăng**, giữ nguyên giá trị tại thời điểm bắt đầu khung giờ nghỉ đó; sau khi hết nghỉ, tính tiếp bình thường theo thời gian làm việc thực tế.
- Áp dụng cho cả bảng theo dõi theo mốc giờ tại màn hình trạm: nếu 1 mốc giờ hiển thị rơi vào khung giờ nghỉ, mốc đó **vẫn hiển thị** trên bảng nhưng cột PLAN lũy kế giữ nguyên bằng giá trị tại thời điểm bắt đầu nghỉ (không cộng thêm cho tới khi hết khung giờ nghỉ).
- Quy tắc này **không thay đổi công thức sản lượng chuẩn/giờ gốc tại FR-06** — chỉ ảnh hưởng cách tính lũy kế theo thời gian thực tế đã trôi qua.

**FR-10 — Lưu lịch sử scan**
- Mọi lượt scan (kể cả lượt bị từ chối/lỗi) được lưu lại: mã tem, thời gian, trạm, công đoạn, Line, kế hoạch, kết quả (OK/Trùng tem/Chưa qua công đoạn trước), người thao tác.
- **Lưu kèm snapshot thông tin kế hoạch tại đúng thời điểm scan**: Khách hàng, Model, Lot, Revision, số lượng kế hoạch (`PlannedQuantity`), takt time (`TaktTimeSeconds`) — ghi lại đúng giá trị của kế hoạch (`ProductionPlan`) tại thời điểm tạo bản ghi scan, **không suy ra bằng cách tra cứu (join) tới kế hoạch hiện tại**. Mục đích: nếu sau này kế hoạch bị sửa (chỉ có thể xảy ra với Số lượng/Takt time, xem quy tắc khóa tuyệt đối tại FR-05), lịch sử scan cũ vẫn phản ánh đúng thông tin tại thời điểm sản phẩm thực sự đi qua công đoạn — đảm bảo tính bất biến (immutability) của bằng chứng truy vết.
- Cho phép tra cứu lịch sử theo tem, theo trạm, theo khoảng thời gian, theo Line.

### 3.4 Nhóm chức năng: Giao tiếp Arduino (kiểm tra tự động — vd Thông điện)

**Mô hình nghiệp vụ**: khác với giả định ban đầu (phần mềm ra lệnh cho Arduino bật đèn theo kết quả scan), thực tế tại công đoạn như Thông điện, **Arduino chính là nguồn xác định kết quả kiểm tra thật** — thiết bị đo bên ngoài (máy test điện) gửi tín hiệu vào Arduino, Arduino xử lý và báo kết quả về phần mềm **trước khi** tem được scan. Phần mềm chỉ cần chờ đúng kết quả rồi gán chính xác cho tem được scan ngay sau đó.

**FR-11 — Cấu hình bật/tắt sử dụng Arduino theo từng trạm**
- Mỗi trạm (`TramLamViec`) có cờ cấu hình **`SuDungArduino` (bật/tắt)**, độc lập với các trạm khác — công đoạn nào không dùng Arduino thì hoạt động hoàn toàn theo luồng scan thủ công bình thường (FR-07/FR-08/FR-18), không bị ảnh hưởng.
- Khi `SuDungArduino = false`: bỏ qua toàn bộ các bước ở FR-12 đến FR-14 bên dưới.
- Khi `SuDungArduino = true`: trạm hoạt động theo state machine mô tả tại FR-12.

**FR-12 — Luồng "Scan tem trước → chờ kết quả kiểm tra từ Arduino → xử lý theo kết quả" (khi có Arduino)**
Trạm có Arduino hoạt động theo trạng thái sau, để đảm bảo **không lấy nhầm kết quả kiểm tra gán cho sai tem** và **không tự động ghi NG oan khi chưa được xác nhận**. **Quy ước đã chốt**: Arduino **chỉ chủ động gửi tín hiệu khi kiểm tra ĐẠT** (`println("OK")`); khi không đạt, Arduino **không gửi gì** — hệ thống suy luận NG thông qua timeout (FR-13). Về mặt vật lý, quy trình đảm bảo tại 1 thời điểm chỉ có **tối đa 1 sản phẩm** đang chờ kết quả (sản phẩm tiếp theo chỉ được đưa vào kiểm tra sau khi sản phẩm hiện tại đã có kết quả Thông điện OK), nên không cần thiết kế hàng đợi nhiều kết quả:

1. **Trạng thái mặc định: "Sẵn sàng scan"** — công nhân scan tem sản phẩm chuẩn bị đưa vào kiểm tra, như luồng scan bình thường (FR-07). Hệ thống vẫn thực hiện đầy đủ kiểm tra trùng tem/công đoạn trước (FR-08) ngay lúc scan, nhưng **chưa ghi nhận kết quả cuối cùng OK/NG** — chuyển sang trạng thái tạm **"Đang chờ kết quả kiểm tra Arduino cho tem [mã tem]"** và bắt đầu đếm thời gian timeout (FR-13).
2. Trong lúc chờ, giao diện hiển thị rõ mã tem đang chờ kết quả. **Không cho phép scan tem khác** cho tới khi có kết quả hoặc hủy — tránh 2 tem cùng chờ gây nhầm lẫn khi gán kết quả.
3. Công nhân đưa sản phẩm vào thiết bị kiểm tra thông điện (bên ngoài) → nếu đạt, thiết bị gửi tín hiệu vào chân input của Arduino → Arduino gửi `println("OK")` về PC qua Serial.
4. **Nếu nhận được "OK" trước khi hết timeout**: hệ thống **tự động lưu lịch sử ngay** với kết quả OK (theo FR-08), tem được lưu thông sang công đoạn tiếp theo, trạng thái quay lại "Sẵn sàng scan" cho sản phẩm tiếp theo.
5. **Nếu hết thời gian timeout mà không nhận được "OK"** (xem FR-13): hệ thống **suy luận đây là kết quả NG**, nhưng **không tự động lưu ngay**. Hiển thị popup **ngay tại màn hình trạm**, yêu cầu **Tổ trưởng xác nhận** (cần đăng nhập/xác thực riêng bằng tài khoản Tổ trưởng tại trạm — không cho công nhân tự xác nhận) với 2 lựa chọn:
   - **"Xác nhận lưu NG"** → ghi nhận lịch sử với kết quả NG (tự động gán lý do lỗi mặc định "Lỗi thông điện (tự động từ thiết bị — không nhận phản hồi OK)"), tem bị khóa tại công đoạn này, cần **Mở khóa rework** theo FR-19 để được scan lại.
   - **"Hủy — kiểm tra lại"** → **không ghi nhận NG vào lịch sử** (tránh tạo bản ghi NG oan nếu nghi ngờ lỗi thiết bị đo/kết nối), tem **không bị khóa**, trạng thái quay về **"Đang chờ kết quả kiểm tra"** cho đúng tem đó (đếm lại timeout từ đầu) — công nhân đưa lại sản phẩm vào máy test mà **không cần scan lại tem**.

**FR-13 — Timeout xác định kết quả kiểm tra (đóng vai trò suy luận NG khi Arduino không phản hồi)**
- Kể từ lúc scan tem (bước 1 ở FR-12), nếu không nhận được `"OK"` từ Arduino trong khoảng thời gian quy định → hết timeout, hệ thống chuyển sang bước xác nhận NG (bước 5, FR-12).
- **Thời gian timeout mặc định 45 giây, cấu hình được qua file cấu hình cục bộ tại từng trạm** (ví dụ `appsettings.json`) — cho phép mỗi trạm có giá trị khác nhau tùy đặc thù thiết bị/quy trình, không cần sửa code hay deploy lại.

**FR-14 — Kết nối & phục hồi cổng COM**
- Ứng dụng WPF tự động mở kết nối cổng COM khi khởi động (với trạm có `SuDungArduino = true`), tự kết nối lại nếu mất kết nối; trong lúc mất kết nối, trạm hiển thị rõ trạng thái lỗi thiết bị, không cho scan tem (vì không thể tin cậy kết quả OK/NG lúc này).
- Log lại toàn bộ dữ liệu gửi/nhận qua Serial (bảng `LichSuLenhArduino`) để phục vụ truy vết khi có sự cố phần cứng.

### 3.5 Nhóm chức năng: Khôi phục trạng thái & chống mất dữ liệu khi tắt/mở lại

Yêu cầu này phát sinh từ tình huống thực tế: trạm làm việc có thể bị **tắt/mở lại bình thường**, hoặc gặp **mất điện/crash đột ngột**. Hai tình huống cần xử lý khác nhau:

**FR-15 — Khôi phục trạng thái phiên làm việc khi khởi động lại (trường hợp tắt/mở bình thường)**
- Khi WPF trạm khởi động, tự động gọi API lấy lại: kế hoạch sản xuất đang active của trạm, số lượng đã scan lũy kế, chỉ số âm/dương hiện tại — hiển thị đúng ngay, không yêu cầu người dùng chọn lại từ đầu.
- Dữ liệu này vốn đã an toàn vì lưu tại server (MySQL); yêu cầu này chỉ đảm bảo **giao diện trạm phản ánh đúng trạng thái server** ngay khi mở lại.

**FR-16 — Hàng đợi cục bộ chống mất lượt scan khi mất mạng/crash đột ngột (đã rút gọn)**
- Mỗi trạm WPF duy trì một **hàng đợi lưu cục bộ** (local storage nhẹ, ví dụ SQLite) độc lập với server.
- Luồng xử lý 1 lượt scan:
  1. Ngay khi scan, ghi lượt scan vào hàng đợi cục bộ **trước tiên** (trạng thái `Chờ gửi`), kèm 1 mã định danh duy nhất (GUID) sinh tại client.
  2. Đồng thời gửi lượt scan (kèm GUID) lên server để kiểm tra & ghi nhận theo FR-08.
  3. Nếu server phản hồi thành công → cập nhật trạng thái cục bộ thành `Đã đồng bộ`, hiển thị kết quả OK/NG chính thức.
  4. Nếu không gửi được (mất mạng/server tạm ngưng/timeout) → giữ nguyên `Chờ gửi`, hiển thị trạng thái **"Chờ đồng bộ"** (không hiển thị OK/NG cho tới khi có xác nhận từ server) — vì kết quả kiểm tra trùng tem/công đoạn trước phụ thuộc dữ liệu server.
- **Cơ chế thử lại tự động**: một tiến trình nền (background worker) định kỳ (ví dụ mỗi 5 giây) quét các bản ghi `Chờ gửi` trong hàng đợi cục bộ và gửi lại lên server, theo đúng thứ tự thời gian đã scan, cho tới khi thành công.
- **Khi khởi động lại sau crash/mất điện**: app đọc hàng đợi cục bộ, phát hiện các bản ghi còn ở trạng thái `Chờ gửi` (chưa kịp xác nhận trước khi tắt), tự động gửi lại — không cần công nhân scan lại thủ công.
- **Chống gửi trùng (idempotency)**: server lưu kèm GUID của client trong `LichSuScan`. Khi nhận lại 1 lượt gửi có GUID đã tồn tại (do client gửi lặp vì crash giữa lúc chờ phản hồi), server trả về **kết quả đã ghi nhận trước đó** thay vì báo lỗi "trùng tem" — tránh hiểu nhầm giữa "trùng vì gửi lại của chính lượt scan này" với "trùng tem thật do quét 2 lần khác nhau".
- **Phạm vi áp dụng**: cơ chế này áp dụng đồng nhất cho **mọi trạm, mọi công đoạn** trong hệ thống — không phân biệt trạm nào đang là "công đoạn cuối cùng" của chuỗi hay không, vì trình tự công đoạn có thể được cấu hình lại theo từng kế hoạch (FR-03), nên vị trí "cuối chuỗi" không cố định theo trạm. Áp dụng đồng nhất giúp đơn giản hóa triển khai, đồng thời tránh trường hợp công đoạn đang tạm thời là cuối chuỗi bị mất dữ liệu mà không có công đoạn kế tiếp nào phát hiện hộ.
- **Không thiết kế cơ chế cảnh báo theo ngưỡng số bản ghi tồn đọng/thời gian mất kết nối, và không tạo báo cáo đối soát riêng cho giai đoạn mất kết nối** — do hạ tầng mạng LAN nội bộ nhà máy được đánh giá ổn định, rủi ro mất mạng kéo dài diện rộng là thấp (xem quyết định tại mục 8.1); phần retry tự động ở trên đã đủ xử lý các tình huống gián đoạn ngắn hạn (crash, restart server, mất mạng tạm thời).

**FR-17 — Hiển thị rõ ràng trạng thái đồng bộ trên UI**
- Danh sách/số lượng tại màn hình trạm cần phân biệt rõ 3 trạng thái: `Đã xác nhận OK` (xanh), `Đã xác nhận NG` (đỏ), `Chờ đồng bộ` (vàng/xám) — để công nhân/tổ trưởng biết những lượt nào chưa được server xác nhận cuối cùng, tránh nhầm là đã tính vào sản lượng chính thức.

### 3.6 Nhóm chức năng: Scan NG & Quy trình xử lý sản phẩm lỗi (Rework)

Phân biệt rõ 2 loại NG trong hệ thống:
- **NG hệ thống tự phát hiện** (đã có ở FR-08): trùng tem, chưa qua công đoạn trước — do lỗi thao tác/quy trình, không phải lỗi chất lượng sản phẩm.
- **NG chất lượng sản phẩm** (mục này): do người vận hành/Tổ trưởng đánh giá sản phẩm không đạt tại công đoạn đó (vd: lỗi ngoại quan, lỗi thông điện) — cần được **chủ động ghi nhận**, không phải hệ thống tự suy ra.

**FR-18 — Scan xác nhận sản phẩm NG**
- Trạm hoạt động theo 2 chế độ: **"Chế độ Scan OK"** (mặc định, luôn ở trạng thái này khi rảnh) và **"Chế độ Scan NG"** (chỉ kích hoạt tạm thời khi có sản phẩm lỗi cần ghi nhận).
- **Kích hoạt Chế độ Scan NG** — chọn 1 trong 2 cách tùy hạ tầng của trạm (xác nhận theo khảo sát thực tế từng trạm):
  - *Cách 1*: bấm nút **"NG"** hiển thị rõ ràng (màu đỏ, kích thước lớn) trên màn hình — áp dụng cho trạm có màn hình cảm ứng/chuột.
  - *Cách 2*: quét **1 mã vạch "NG" cố định** dán sẵn tại bàn thao tác (không phải tem sản phẩm, chỉ đóng vai trò lệnh chuyển chế độ) — áp dụng cho trạm chỉ có đầu đọc mã vạch, không có chuột/bàn phím.
- Sau khi kích hoạt Chế độ Scan NG: giao diện phải **thay đổi rõ rệt** (đổi nền màu đỏ, hiển thị dòng chữ lớn "ĐANG Ở CHẾ ĐỘ NG — Vui lòng scan tem sản phẩm lỗi") để người vận hành không nhầm lẫn với chế độ scan OK bình thường.
- Người vận hành quét tem sản phẩm lỗi → hệ thống **bắt buộc nhập lý do lỗi dạng tự do (free text)** — Tổ trưởng gõ trực tiếp mô tả lỗi. Hệ thống **gợi ý (autocomplete)** các lý do lỗi đã từng được nhập trước đó cho công đoạn này (dựa trên lịch sử), giúp dần hình thành danh mục lỗi thực tế theo thời gian mà không cần cấu hình trước — có thể chuẩn hóa thành danh mục cố định ở giai đoạn sau khi đã đủ dữ liệu thực tế.
- Ghi nhận vào lịch sử: mã tem, công đoạn, trạm, thời gian, người xác nhận, lý do lỗi, kết quả = NG.
- **Sau khi bị NG, tem đó bị khóa tại công đoạn này**: không được tiếp tục scan sang công đoạn kế tiếp (áp dụng đúng rule FR-08 — "chưa qua công đoạn trước" chỉ được thỏa khi kết quả là OK).
- **Tự động quay về Chế độ Scan OK** sau khi: (a) đã hoàn tất 1 lượt Scan NG, hoặc (b) hết thời gian chờ (timeout mặc định **30 giây, cấu hình được qua file cấu hình cục bộ tại từng trạm**) mà không có tem nào được quét kể từ lúc kích hoạt Chế độ Scan NG — tránh trường hợp công nhân kích hoạt NG rồi quên, khiến lượt scan sản phẩm bình thường tiếp theo bị ghi nhầm thành NG.

**FR-19 — Quy trình Rework (sửa lỗi & scan lại)**
- Sản phẩm bị NG **không được tự động scan lại** tại công đoạn đó. Cần **Tổ trưởng (vai trò có phân quyền riêng)** thực hiện thao tác **"Mở khóa rework"** cho đúng tem đó tại đúng công đoạn đó, sau khi xác nhận sản phẩm đã được sửa lỗi.
- Thao tác mở khóa được ghi log: ai duyệt, thời điểm, ghi chú (nếu có).
- Sau khi được mở khóa, người vận hành scan lại tem tại công đoạn đó như bình thường (theo FR-07). Nếu đạt → ghi nhận OK, tem được tiếp tục lưu thông sang công đoạn sau; nếu vẫn không đạt → tiếp tục quy trình NG/mở khóa như trên, không giới hạn số lần lặp lại.
- **Giữ lại toàn bộ lịch sử tất cả các lần scan** (NG lẫn OK) tại cùng 1 công đoạn — không ghi đè — để phục vụ báo cáo tỷ lệ lỗi (PPM), thống kê loại lỗi thường gặp theo FR-20.

**FR-20 — Báo cáo tỷ lệ lỗi & nguyên nhân**
- Thống kê số lượng/tỷ lệ NG theo công đoạn, theo Line, theo loại lỗi, theo khoảng thời gian — phục vụ phân tích chất lượng (không thuộc phạm vi màn hình trạm, mà ở màn hình báo cáo/Ban quản lý).

> **Lưu ý kỹ thuật quan trọng**: quy tắc này làm thay đổi ràng buộc `UNIQUE (MaTem, CongDoanId)` đã đặt ra trước đó ở tài liệu kiến trúc — vì giờ đây 1 tem có thể có **nhiều bản ghi** tại cùng 1 công đoạn (các lần NG + lần OK cuối). Ràng buộc unique cần đổi thành: **tối đa 1 bản ghi có `KetQua = OK`** cho mỗi `(MaTem, CongDoanId)` (không giới hạn số bản ghi NG). MySQL không hỗ trợ trực tiếp unique index có điều kiện, nên ràng buộc này cần xử lý ở tầng Business Logic (`ScanService`) kết hợp cột trạng thái tem (`Khóa`/`Mở khóa chờ rework`/`Đã qua`) thay vì chỉ dựa vào unique constraint ở DB như thiết kế ban đầu.

### 3.7 Nhóm chức năng: Báo cáo & quản trị

**FR-21 — Báo cáo tổng hợp theo Line**
- Xem sản lượng thực tế, kế hoạch, chỉ số âm/dương của từng Line, từng công đoạn theo thời gian thực hoặc theo khoảng thời gian đã qua.
- Báo cáo chỉ tính các lượt scan ở trạng thái `Đã xác nhận OK` (không tính lượt `Chờ đồng bộ` cho tới khi được server xác nhận, theo FR-16/FR-17).

**FR-22 — Quản lý người dùng & phân quyền**
- Phân quyền theo nhóm người dùng ở mục 2.2 (Công nhân / Tổ trưởng / Admin / Ban quản lý), bổ sung quyền riêng cho thao tác "Mở khóa rework" (FR-19) — chỉ Tổ trưởng mới thực hiện được.

**FR-23 — Xuất báo cáo Excel**
- Cho phép xuất báo cáo tổng hợp (FR-21) và báo cáo tỷ lệ lỗi (FR-20) ra file Excel (.xlsx), theo bộ lọc đã chọn trên màn hình báo cáo (Line, khoảng thời gian, công đoạn).

---

## 4. YÊU CẦU PHI CHỨC NĂNG

| Hạng mục | Yêu cầu |
|---|---|
| Hiệu năng | Xử lý 1 lượt scan (kiểm tra + ghi nhận + phản hồi UI) trong ≤ 500ms trong điều kiện mạng LAN nội bộ bình thường |
| Real-time | Độ trễ cập nhật số liệu qua SignalR tới các màn hình liên quan ≤ 1 giây kể từ khi có scan mới |
| Độ tin cậy | Không được để mất dữ liệu lượt scan trong mọi tình huống — kể cả mất mạng tạm thời, mất điện, hoặc crash ứng dụng đột ngột — thông qua cơ chế hàng đợi cục bộ ghi trước (write-ahead) tại FR-16. Không cho phép scan "treo" không rõ trạng thái; mọi lượt scan luôn ở 1 trong 3 trạng thái rõ ràng: OK / NG / Chờ đồng bộ |
| Bảo mật | Đăng nhập theo tài khoản/phân quyền; toàn bộ giao tiếp API qua HTTPS |
| Khả năng mở rộng | Thêm Line mới, trạm mới, công đoạn mới chỉ bằng cấu hình, không cần sửa/deploy lại code |
| Khả năng phục hồi | Ứng dụng WPF tự kết nối lại (HTTP, SignalR, COM) sau khi mất kết nối, không cần khởi động lại thủ công |
| Khả năng bảo trì | Tuân thủ 3-layer, SOLID, Unit of Work để dễ viết unit test, dễ thay đổi từng phần độc lập |

---

## 5. YÊU CẦU GIAO DIỆN

### 5.1 Giao diện phần cứng
- **Máy scan tem**: giao tiếp qua **USB HID (chế độ HID POS/HID thô)**, sử dụng thư viện HID (vd: HidLibrary/hidapi trong .NET) để đọc trực tiếp dữ liệu từ thiết bị theo VendorID/ProductID — không dùng chế độ keyboard-wedge (HID Keyboard giả lập bàn phím) để tránh phụ thuộc vào cửa sổ đang active (focus), đặc biệt quan trọng với trạm gộp nhiều màn hình/công đoạn trên 1 PC (mục 6, quy tắc 7). Với trạm chỉ có 1 máy scan cho 1 công đoạn, Serial (COM) vẫn là phương án dự phòng hợp lệ nếu máy scan không hỗ trợ chế độ HID POS.
- **Arduino**: giao tiếp Serial (COM) qua USB CDC, giao thức lệnh dạng text đơn giản (vd: "OK\n" / "NG\n"), baud rate cấu hình theo từng trạm.

### 5.2 Giao diện phần mềm
- **Màn hình trạm (WPF)**: hiển thị số lượng scan hiện tại, chỉ số âm/dương (màu sắc rõ ràng, chữ lớn, dễ nhìn từ xa), ô nhập/nhận tín hiệu scan, thông báo lỗi khi scan không hợp lệ.
- **Màn hình cấu hình (Admin/Tổ trưởng)**: quản lý danh mục Line, công đoạn, kế hoạch, trình tự công đoạn (kéo-thả).
- **Màn hình báo cáo**: bảng/biểu đồ sản lượng theo Line, theo thời gian.
- **API**: RESTful, trả JSON, có tài liệu Swagger đi kèm.

---

## 6. QUY TẮC NGHIỆP VỤ CHỐT (tổng hợp)

Đây là các quyết định nghiệp vụ đã thống nhất qua quá trình trao đổi, liệt kê lại để tránh hiểu sai:

1. Một server trung tâm phục vụ nhiều Line; mỗi Line có bộ công đoạn và trình tự cấu hình độc lập — **trình tự này thuộc về Line (FR-03), cấu hình 1 lần, dùng chung cho mọi kế hoạch sản xuất chạy trên Line đó, KHÔNG cấu hình lại riêng theo từng kế hoạch** (chốt lại 17/08/2026, sửa hiểu sai trước đó khi FR-03/US-03 bản đầu viết nhầm thành "áp dụng cho từng kế hoạch").
2. Công đoạn là danh mục dùng chung (master) cho toàn hệ thống, không thuộc riêng 1 Line.
3. **Chống trùng tem xét theo `(Mã tem, Công đoạn)` trên toàn hệ thống** — không theo Line/kế hoạch. Cùng 1 công đoạn không được scan 2 lần dù ở Line khác nhau; khác công đoạn thì Line nào cũng được.
4. Kiểm tra "đã qua công đoạn liền trước" cũng tra cứu toàn hệ thống, không giới hạn theo Line.
5. Sản lượng/giờ = `3600 / Takt time`; chỉ số âm/dương = Sản lượng thực tế lũy kế − Sản lượng kế hoạch lũy kế, tính riêng theo từng Line/kế hoạch/công đoạn.
6. Mọi lượt scan (kể cả lỗi) đều được lưu lịch sử.
7. Với trạm gộp nhiều công đoạn trên 1 PC (nhiều màn hình): bắt buộc mỗi màn hình chạy 1 tiến trình riêng, mỗi công đoạn có máy scan/Arduino riêng biệt. Máy scan giao tiếp qua **USB HID (đọc trực tiếp theo VendorID/ProductID)** thay vì keyboard-wedge, để không phụ thuộc cửa sổ đang active — mỗi process chỉ lắng nghe đúng thiết bị scan tương ứng với công đoạn đó, tránh ghi nhầm dữ liệu giữa các công đoạn khi nhiều người thao tác gần như đồng thời.
8. Khi tắt/mở lại ứng dụng (bình thường hoặc do crash/mất điện), phải khôi phục lại đúng trạng thái phiên làm việc (kế hoạch, số lượng, chỉ số +/-) và **không được mất bất kỳ lượt scan nào** đã thực hiện trước khi tắt, kể cả lượt chưa kịp gửi lên server — xử lý bằng hàng đợi cục bộ ghi trước (FR-16), hiển thị rõ trạng thái "Chờ đồng bộ" cho các lượt chưa được server xác nhận.
9. Sản phẩm bị đánh giá NG về chất lượng phải được **chủ động scan NG kèm lý do lỗi** (không phải hệ thống tự suy luận), bị khóa lại tại công đoạn đó, và chỉ được scan lại sau khi **Tổ trưởng xác nhận mở khóa rework** — không giới hạn số lần lặp lại. Toàn bộ lịch sử các lần NG/OK tại cùng công đoạn đều được lưu lại, không ghi đè.
10. Với công đoạn có Arduino làm thiết bị kiểm tra tự động (bật/tắt theo từng trạm): công nhân **scan tem trước**, sau đó chờ Arduino gửi tín hiệu "OK" (Arduino không chủ động gửi NG). Nếu hết thời gian timeout (mặc định 45 giây) mà không nhận được OK, hệ thống **suy luận là NG**, nhưng **không tự động lưu** — cần **Tổ trưởng xác nhận ngay tại trạm** để lưu NG hoặc hủy kiểm tra lại. Về vật lý, chỉ có tối đa 1 sản phẩm được kiểm tra tại 1 thời điểm (không có hàng đợi nhiều kết quả).
11. Mỗi Line có thể cấu hình **khung giờ nghỉ riêng** (nghỉ trưa, nghỉ giữa giờ…), áp dụng chung cho mọi kế hoạch chạy trên Line đó (FR-01). Sản lượng kế hoạch lũy kế hiển thị tại màn hình trạm **dừng tăng trong lúc nghỉ**, tính tiếp sau khi hết nghỉ (FR-09a) — không ảnh hưởng đến công thức sản lượng chuẩn/giờ gốc (FR-06).
12. Mỗi cặp (Kế hoạch, Công đoạn) có vòng đời trạng thái riêng `Draft/Running/Paused/Completed/Cancelled` (FR-05a) thay vì 1 cờ bật/tắt chung cho cả kế hoạch — cho phép tạm dừng và chạy lại nhiều lần mà không mất/nhầm tiến độ (tính động từ lịch sử scan OK), đồng thời cho phép từng công đoạn của cùng 1 kế hoạch **đóng độc lập** (vd công đoạn A chạy đủ số lượng thì tự đóng `Completed` dù công đoạn B, C của cùng kế hoạch chưa đủ). Ràng buộc "1 kế hoạch `Running` tại 1 thời điểm" (FR-05) áp dụng theo cặp **(Line, Công đoạn)**, không theo cả Line — công đoạn A của 1 Line được phép chuyển sang kế hoạch mới trong khi công đoạn B, C của cùng Line vẫn đang chạy/tạm dừng kế hoạch cũ, đúng thực tế dây chuyền có WIP giữa các trạm.
13. Tổ trưởng có thể cấu hình/áp dụng kế hoạch cho **bất kỳ công đoạn nào cùng Line**, không giới hạn theo công đoạn vật lý của trạm đang thao tác — màn hình "Chọn kế hoạch" (mục 5.2) cho chọn Công đoạn độc lập với công đoạn của trạm hiện tại, phục vụ Tổ trưởng quản lý nhiều công đoạn từ 1 vị trí.
14. Lịch sử scan (`Scan`) lưu **snapshot** Khách hàng/Model/Lot/Revision/Số lượng kế hoạch/Takt time tại đúng thời điểm scan (FR-10), không tra cứu động qua kế hoạch hiện tại. Để snapshot này luôn đúng, khi kế hoạch đã có ít nhất 1 bản ghi scan, hệ thống **khóa tuyệt đối** Khách hàng/Model/Lot/Revision của kế hoạch đó — không cho sửa dưới bất kỳ hình thức nào (kể cả Confirm), khác với Số lượng/Takt time vẫn cho sửa kèm xác nhận (FR-05). Nhập sai các trường bị khóa sau khi đã có scan → tạo kế hoạch mới, không sửa kế hoạch cũ.

---

## 7. TIÊU CHÍ NGHIỆM THU (Acceptance Criteria — trích các mục trọng yếu)

| # | Kịch bản kiểm thử | Kết quả mong đợi |
|---|---|---|
| AC-01 | Scan tem A tại công đoạn "Lắp ráp" Line 1, sau đó scan tem A tại công đoạn "Lắp ráp" Line 2 | Lần 2 bị từ chối, báo "Trùng tem tại công đoạn này" |
| AC-02 | Scan tem A tại "Lắp ráp" Line 1, sau đó scan tem A tại "Thông điện" Line 2 | Lần 2 được chấp nhận (khác công đoạn, khác Line hợp lệ) |
| AC-03 | Scan tem B tại "Thông điện" trong khi chưa từng scan "Lắp ráp" ở bất kỳ Line nào | Bị từ chối, báo rõ "Chưa qua công đoạn: Lắp ráp" |
| AC-04 | Nhập takt time = 30 giây | Hệ thống hiển thị sản lượng chuẩn = 120 sản phẩm/giờ |
| AC-05 | Sản lượng thực tế cao hơn kế hoạch lũy kế | Chỉ số hiển thị dương, màu xanh |
| AC-06 | Mất kết nối mạng giữa trạm và server trong lúc scan | Ứng dụng báo lỗi rõ ràng, không ghi nhận lượt scan "treo"; tự kết nối lại khi mạng phục hồi |
| AC-07 | Thêm 1 công đoạn mới vào kế hoạch đang chạy | Trình tự công đoạn cập nhật ngay, không cần deploy lại phần mềm |
| AC-08 | 2 trạm scan gần như đồng thời cho 2 tem khác nhau | Cả 2 được ghi nhận đúng, không lẫn dữ liệu giữa 2 trạm |
| AC-09 | Tắt ứng dụng bình thường rồi mở lại | Hiển thị lại đúng kế hoạch, số lượng, chỉ số +/- như trước khi tắt, không cần chọn lại cấu hình |
| AC-10 | Ngắt mạng/rút điện đột ngột ngay sau khi công nhân scan 1 tem (trước khi có phản hồi từ server), sau đó khởi động lại ứng dụng | Lượt scan đó vẫn còn trong hàng đợi cục bộ ở trạng thái "Chờ đồng bộ", tự động gửi lại lên server khi có mạng, không yêu cầu scan lại tem, và không bị ghi nhận trùng 2 lần trên server |
| AC-11 | Scan tem hợp lệ (kết quả OK) | Hiển thị thông báo xác nhận đã lưu kèm mã tem, tự động biến mất sau 1–2 giây, không che số liệu chính, không chặn thao tác scan tiếp theo |
| AC-12 | Scan tem bị từ chối (trùng tem hoặc chưa qua công đoạn trước) | Hiển thị thông báo lỗi rõ ràng, khác biệt màu sắc/âm thanh so với thông báo OK, yêu cầu người vận hành xác nhận đã đọc trước khi tắt |
| AC-13 | Người vận hành chọn "Scan NG", quét tem, chọn lý do lỗi | Ghi nhận NG kèm lý do; tem bị khóa, không thể scan sang công đoạn kế tiếp |
| AC-13b | Bấm nút NG (hoặc quét mã NG cố định) để kích hoạt Chế độ Scan NG | Giao diện đổi rõ rệt (nền đỏ, thông báo lớn), chờ quét tem sản phẩm lỗi |
| AC-13c | Kích hoạt Chế độ Scan NG nhưng không quét tem nào trong 30 giây | Tự động quay về Chế độ Scan OK mặc định, không ảnh hưởng lượt scan bình thường tiếp theo |
| AC-14 | Công nhân cố gắng tự scan lại tem vừa bị NG (chưa được Tổ trưởng mở khóa) | Hệ thống từ chối, báo "Sản phẩm đang chờ mở khóa rework" |
| AC-15 | Tổ trưởng mở khóa rework cho tem, công nhân scan lại và đạt | Ghi nhận thêm 1 bản ghi OK mới (không ghi đè bản ghi NG cũ), tem được phép sang công đoạn kế tiếp |
| AC-17 | Trạm có `SuDungArduino = true`, công nhân scan tem | Chuyển sang trạng thái "Đang chờ kết quả kiểm tra" cho đúng tem đó; không cho scan tem khác trong lúc chờ |
| AC-18 | Arduino gửi "OK" sau khi đã scan tem, trước khi hết timeout | Tự động lưu lịch sử OK ngay, không cần xác nhận thêm; trạng thái quay lại "Sẵn sàng scan" |
| AC-19 | Hết 45 giây kể từ lúc scan tem mà không nhận được "OK" từ Arduino | Hệ thống suy luận NG, hiển thị popup ngay tại trạm yêu cầu Tổ trưởng xác nhận "Lưu NG" hoặc "Hủy — kiểm tra lại" (cần đăng nhập Tổ trưởng) |
| AC-19b | Tổ trưởng chọn "Xác nhận lưu NG" | Ghi nhận lịch sử NG kèm lý do mặc định, tem bị khóa, cần mở khóa rework (FR-19) để scan lại |
| AC-19c | Tổ trưởng chọn "Hủy — kiểm tra lại" | Không ghi nhận NG vào lịch sử, tem không bị khóa, trạng thái quay về "Đang chờ kết quả kiểm tra" cho đúng tem đó (đếm lại timeout), không cần scan lại tem |
| AC-20 | Trạm có `SuDungArduino = false` | Hoạt động hoàn toàn theo luồng scan thủ công bình thường (FR-07/FR-08/FR-18), không có bước chờ Arduino |
| AC-21 | Mất mạng/server tạm ngưng trong lúc trạm đang có nhiều bản ghi ở trạng thái "Chờ gửi" | Không chặn scan; tiến trình nền tiếp tục thử gửi lại định kỳ; khi kết nối phục hồi, toàn bộ bản ghi tồn đọng tự động đồng bộ theo đúng thứ tự thời gian, không cần thao tác thủ công |
| AC-22 | Xuất báo cáo tổng hợp theo bộ lọc đã chọn | Tải về file Excel (.xlsx) đúng dữ liệu đã lọc |
| AC-24 | Line có cấu hình nghỉ trưa 12:00–13:00, thời điểm hiện tại đang trong khung giờ này | Sản lượng kế hoạch lũy kế trên màn hình trạm giữ nguyên giá trị tại 12:00, không tăng thêm cho tới 13:00 |
| AC-25 | Bảng theo dõi theo giờ tại màn hình trạm có mốc 12:35 (nằm trong khung nghỉ trưa 12:00–13:00) | Dòng 12:35 vẫn hiển thị bình thường; cột PLAN = giá trị lũy kế tại 12:00 (không cộng thêm phần rơi vào giờ nghỉ) |
| AC-26 | Kế hoạch Lot A (SL 1000) đang `Running`, đã scan OK 400 sản phẩm tại công đoạn X, Tổ trưởng bấm "Tạm dừng" | Kế hoạch chuyển `Paused`, không mất tiến độ; Line có thể Áp dụng kế hoạch khác |
| AC-27 | Vài ngày sau, Tổ trưởng mở màn hình "Chọn kế hoạch", chọn lại Lot A (đang `Paused`) | Hiển thị rõ "Đã chạy 400/1000 — còn 600" (tính động từ lịch sử scan OK), không hiển thị nhầm như còn nguyên 1000 |
| AC-28 | Tổ trưởng bấm "Đóng kế hoạch" cho 1 lot đang `Paused` chưa đủ số lượng | Hệ thống yêu cầu xác nhận trước khi chuyển sang `Cancelled`, nêu rõ số lượng còn thiếu |

---

## 8. CÁC QUYẾT ĐỊNH ĐÃ CHỐT & MỤC CÒN CẦN XÁC NHẬN

### 8.1 Đã chốt qua trao đổi với khách hàng

- Số lượng Line thực tế và số công đoạn từng Line: **chưa xác định tại thời điểm viết SRS** — sẽ khảo sát cụ thể trong giai đoạn triển khai. Kiến trúc đã thiết kế linh hoạt (cấu hình được, không hard-code), nên không ảnh hưởng đến thiết kế hệ thống.
- Arduino **chỉ gửi tín hiệu khi kiểm tra đạt** (`println("OK")`), không gửi tín hiệu khi không đạt — hệ thống suy luận NG qua timeout (FR-13). Về vật lý, chỉ 1 sản phẩm được kiểm tra tại 1 thời điểm tại mỗi trạm (không có trường hợp nhiều sản phẩm chờ kết quả cùng lúc).
- Tổ trưởng xác nhận Lưu NG hoặc Hủy — kiểm tra lại (FR-12) thực hiện **ngay tại màn hình trạm** (cần đăng nhập/xác thực riêng), không phải hàng đợi duyệt từ xa.
- **"QC" chỉ là cách gọi khác của vai trò Tổ trưởng, không phải 1 role riêng biệt** — toàn bộ các mục nhắc đến "Tổ trưởng/QC" trong tài liệu trước đây đã thống nhất chỉ còn 1 vai trò **Tổ trưởng**, đúng theo bảng vai trò người dùng ở mục 2.2 (không bổ sung role mới).
- **Không có tình huống 1 tem bị scan đồng thời tại 2 Line khác nhau** — vì tem là vật lý duy nhất, tại 1 thời điểm chỉ tồn tại ở đúng 1 vị trí, nên rule chống trùng tem (mục 6, quy tắc 3 / FR-08) không cần thiết kế riêng cho race condition giữa 2 Line. Tầng code (`ScanService`) vẫn xử lý theo transaction thông thường khi ghi nhận lượt scan, không cần cơ chế khóa/đồng bộ đặc biệt cho tình huống này.
- Máy scan sử dụng model **Zebra DS2208** — đã xác nhận hỗ trợ chế độ HID phù hợp (mục 2.3).
- Trạm gộp nhiều công đoạn trên 1 PC: **dự kiến không có**, nếu phát sinh thì **tối đa 2 màn hình/2 công đoạn** trên 1 PC — thiết kế theo mục 6, quy tắc 7 vẫn áp dụng đúng cho trường hợp này.
- Cơ chế xử lý khi lượt scan bị từ chối: giữ **mặc định từ chối cứng**, không cho ghi đè/xử lý ngoại lệ.
- Có yêu cầu **xuất báo cáo ra Excel** (đã bổ sung FR-23), không yêu cầu xuất PDF ở giai đoạn này.
- **Hạ tầng mạng LAN nội bộ nhà máy được đánh giá ổn định**, rủi ro mất mạng kéo dài diện rộng là thấp → **rút gọn FR-16**: bỏ cơ chế cảnh báo theo ngưỡng (200 bản ghi/15 phút) và báo cáo đối soát riêng; chỉ giữ lại hàng đợi cục bộ ghi trước + retry tự động + idempotency (GUID) để chống mất dữ liệu do crash/mất điện/server tạm ngưng — áp dụng đồng nhất cho mọi trạm, không phân biệt theo vị trí trong chuỗi công đoạn (xem FR-16 đã cập nhật).
- **Module Đóng thùng tách khỏi phạm vi SRS này** — do mô hình xử lý khác hẳn (đếm lũy kế theo thùng, in tem khi đủ số lượng cài đặt, không phải scan-đối-chiếu-công-đoạn từng sản phẩm), dự kiến phát triển thành ứng dụng riêng, sẽ đặc tả trong tài liệu riêng khi có quyết định chính thức (xem mục 1.2).
- Danh mục lý do lỗi khi Scan NG: dùng **nhập tự do (free text)** kèm gợi ý autocomplete từ lịch sử, không cấu hình danh mục cố định trước (đã cập nhật FR-18).
- Cách kích hoạt Chế độ Scan NG: giữ **cả 2 phương án** (nút bấm cho trạm có màn hình cảm ứng/chuột, mã vạch NG cố định cho trạm chỉ có đầu đọc mã vạch) — Admin cấu hình theo từng trạm tùy hạ tầng thực tế.
- Các giá trị timeout (Chế độ Scan NG 30s, chờ Arduino 45s) **cấu hình qua file cấu hình cục bộ tại từng trạm** (ví dụ `appsettings.json`), không phải cấu hình tập trung ở Admin/server — cho phép chỉnh riêng theo từng trạm mà không cần deploy lại hay phụ thuộc kết nối server.
- **Nền tảng UI cho ứng dụng trạm làm việc: WPF (kèm mẫu kiến trúc MVVM), không dùng WinForms** — quyết định kiến trúc chính thức, xem chi tiết bối cảnh và lý do tại `ADR-001-lua-chon-wpf-hay-winforms.md`.
- **Thời gian nghỉ cấu hình theo từng Line** (không theo từng kế hoạch/ca) — áp dụng chung cho mọi kế hoạch chạy trên Line đó. Chỉ ảnh hưởng cách tính sản lượng kế hoạch lũy kế hiển thị tại màn hình trạm (dừng tăng trong lúc nghỉ, tính tiếp sau khi hết nghỉ), không thay đổi công thức sản lượng chuẩn/giờ gốc (FR-06) — xem FR-01, FR-09a, mục 6 quy tắc 11.
- Trường "Ca làm việc" (Shift) tại màn hình Cài đặt kế hoạch: **không cần** — Lot + Thời gian bắt đầu đã đủ xác định phiên chạy, đã bỏ khỏi FR-05.
- Trường "Tên nhân viên" tại màn hình Cài đặt kế hoạch: là **danh sách nhân viên vận hành tại trạm/công đoạn phụ trách lô đó** (có thể nhiều người), không phải người đăng nhập thao tác màn hình cấu hình — audit thao tác cấu hình vẫn theo tài khoản đăng nhập riêng (ADR-005).
- Màn hình Cài đặt kế hoạch hiển thị **ngay sản lượng chuẩn/giờ** (FR-06) khi nhập takt time, không cần đợi qua màn hình trạm mới thấy — giúp phát hiện sớm lỗi nhập liệu.
- Kế hoạch sản xuất có **vòng đời trạng thái** `Draft/Running/Paused/Completed/Cancelled` (không phải cờ bật/tắt đơn giản), cho phép tạm dừng và chạy lại nhiều lần; "đã chạy/còn lại" luôn tính động từ lịch sử scan OK theo cặp (kế hoạch, công đoạn) — xem FR-05a, mục 6 quy tắc 12.
- Combobox "Công đoạn" tại màn hình Chọn kế hoạch **giữ nguyên** (không lược bỏ) — mục đích để Tổ trưởng cấu hình/áp dụng kế hoạch cho các công đoạn khác cùng Line, không giới hạn theo công đoạn vật lý của trạm đang thao tác — xem mục 6 quy tắc 13.
- **Số lượng Line thực tế: nhiều hơn 2 Line** — chưa có con số/danh sách công đoạn chính xác từng Line (vẫn cần khảo sát tại xưởng, xem mục 8.2), nhưng đã đủ để loại trừ giả định "2 Line" khi ước lượng hạ tầng.
- **Công đoạn dùng Arduino hiện tại: chỉ có Thông điện**, chưa có công đoạn nào khác — chỉ cần cấu hình `SuDungArduino = true` cho các trạm Thông điện ở giai đoạn triển khai đầu.
- **Không cần toàn bộ máy scan cùng 1 model** — thiết kế đã dựa vào VendorID/ProductID đọc trực tiếp qua HID (mục 5.1), không phụ thuộc model cụ thể; chỉ cần xác định đúng VID/PID của từng thiết bị Zebra thực tế khi cấu hình từng trạm, khác model không ảnh hưởng thiết kế.
- **"Model" ở màn hình Cài đặt kế hoạch thay thế hẳn cặp `ProductCode`/`ProductName`** cũ (dư thừa) — gộp thành 1 cột duy nhất (đã cập nhật FR-05).
- Kế hoạch chuyển `Completed` **theo cả 2 cơ chế**: tự động khi đủ số lượng, **và** cho phép Tổ trưởng đóng sớm thủ công — áp dụng **độc lập theo từng công đoạn** trong cùng 1 kế hoạch, không chờ tất cả công đoạn cùng hoàn thành (đã cập nhật FR-05a, mục 6 quy tắc 12).
- **Lịch sử scan phải bất biến (snapshot) và kế hoạch đã có scan bị khóa tuyệt đối Khách hàng/Model/Lot/Revision** — quyết định chốt ngày 14/08/2026, xử lý gap phát hiện giữa FR-05 (cho sửa tự do Khách hàng/Model/Lot/Revision kể cả khi đã có scan) và FR-10/US-10 (tra cứu lịch sử scan qua join động tới kế hoạch, không có dữ liệu độc lập) — nếu không xử lý, sửa kế hoạch sau khi đã scan sẽ làm lịch sử scan hiển thị sai thông tin lô hàng đã thực sự đi qua công đoạn. Giải pháp 2 phần: (a) `Scan` lưu snapshot 6 field (Khách hàng/Model/Lot/Revision/Số lượng kế hoạch/Takt time) tại thời điểm scan; (b) `ProductionPlanService.UpdateAsync` chặn tuyệt đối sửa Khách hàng/Model/Lot/Revision khi kế hoạch đã có ≥1 bản ghi scan, không có đường Confirm override như Số lượng/Takt time. Xem FR-05, FR-10, mục 6 quy tắc 14.
- Ràng buộc "1 kế hoạch `Running` tại 1 thời điểm" áp dụng theo cặp **(Line, Công đoạn)**, không theo cả Line — các công đoạn khác nhau của cùng 1 Line được chạy kế hoạch khác nhau cùng lúc, đúng thực tế dây chuyền có WIP giữa các trạm (đã cập nhật FR-05, FR-05a). Đây là thay đổi so với ràng buộc gốc (chỉ theo `LineId`) — cần sửa ràng buộc unique trong `ProductionPlanService` sang theo `(LineId, StageId)` khi triển khai.

### 8.2 Còn cần xác nhận trước khi phát triển

- [ ] Danh sách công đoạn cụ thể + số lượng chính xác từng Line (đã biết có nhiều hơn 2 Line) — cần khảo sát tại xưởng trước khi cấu hình hệ thống lần đầu.
- [ ] Nội dung cụ thể cần có trong báo cáo Excel (FR-23) — các cột dữ liệu, cách nhóm/tổng hợp — khách hàng sẽ chốt sau, để thiết kế đúng mẫu báo cáo khi phát triển.
