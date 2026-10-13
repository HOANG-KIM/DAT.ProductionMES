# US-12: Luồng "Thông điện trước → quét QR sau" khi trạm dùng máy thông điện

> **Viết lại 12/10/2026** theo `Documents/spec-firmware-app-link.md` (mục 4 là nguồn chân lý giao thức; firmware đã code xong) và `Documents/spec-app-wpf-station.md` (mục 7 state machine). Thay thế hoàn toàn luồng cũ "scan tem trước → chờ Arduino gửi OK → timeout 45s suy luận NG → Tổ trưởng xác nhận NG" (AC4–AC6 cũ bị **bỏ**). Tên thư mục giữ nguyên để link ổn định.

**Là** Công nhân vận hành trạm Thông điện (trạm có máy, công tắc "Đang sử dụng máy thông điện" = BẬT)
**Tôi muốn** đặt dây lên jig cho máy tự thông điện, rồi quét QR (tem) trên dây để hệ thống gắn kết quả OK của máy đúng với đúng sợi dây đó và lưu lại
**Để** mọi dây qua trạm đều có bản ghi OK đúng tem, không nhầm kết quả giữa các dây, dây NG không bị lưu nhầm thành OK

## Quy trình tóm tắt
```
Đặt dây → máy tự thông điện
  NG → máy báo NG, không gửi gì, không khóa; tháo dây sửa; sửa xong thông điện lại
  OK → máy khóa, gửi #RESULT → App: "OK — QUÉT MÃ QR TRÊN DÂY"
Quét QR khi App đang giữ kết quả OK (WaitScan) → kiểm tra tem (FR-08, endpoint kiểm tra KHÔNG lưu)
  hợp lệ → #BIND → #BIND_OK → lưu Scan lên server → (server commit) → #SAVED → #UNLOCKED
  bị từ chối → KHÔNG gửi BIND, máy vẫn khóa, banner Lưu/Thoát (US-27)
Quét QR khi App KHÔNG giữ kết quả OK (Ready) → gửi #BIND,SEQ=0 NGAY, KHÔNG gọi server kiểm tra tem
  → hiển thị lý do theo phản hồi máy (DÂY NG / Chưa thông điện / Đang thông điện / BAD_SEQ...), không lưu gì
```

## State machine App (spec-app mục 7.1)
`Disconnected` · `Ready` ("Đặt dây và chờ thông điện") · `WaitScan` (chữ lớn OK xanh, "QUÉT MÃ QR TRÊN DÂY") · `Binding` ("Đang xác nhận…") · `Saving` ("Đang lưu…") · `Discarding`. Trạng thái `Disconnected` và đồng bộ lại sau khi mất kết nối thuộc US-14.

**Acceptance Criteria**
- **AC1 — Máy thông điện OK → khóa, chờ quét QR** *(AC-17 gốc, viết lại)*
  - Given trạm có máy, công tắc BẬT, App `Ready`
  - When máy thông điện dây OK và gửi `#RESULT,SEQ=n,R=OK,GM=..`
  - Then App chuyển `WaitScan`, hiển thị chữ lớn "OK — QUÉT MÃ QR TRÊN DÂY"; chưa lưu gì; App giữ kết quả `SEQ = n`
- **AC2 — Happy path: BIND → lưu → SAVED** *(AC-18 gốc, viết lại)*
  - Given App `WaitScan`, `SEQ = n`
  - When quét tem hợp lệ → App gửi `#BIND,SEQ=n` → nhận `#BIND_OK,SEQ=n` → lưu `Scan` lên server (`Result = Ok`, `TesterMode = Tester`, `TesterBootId`, `TesterSeq`, `GoldenCrc`, `FirmwareVersion`, `TestedAtUtc`)
  - Then **chỉ sau khi server commit** App mới gửi `#SAVED,SEQ=n`; nhận `#UNLOCKED` → hiển thị "ĐÃ LƯU <mã tem>" và về `Ready`; lượt scan tính vào ACTUAL, tem được qua công đoạn tiếp theo
- **AC3 — Tem bị từ chối: không BIND, máy vẫn khóa, banner Lưu/Thoát**
  - Given App `WaitScan` (chỉ áp dụng khi App đang giữ kết quả OK — ở `Ready` không gọi server kiểm tra tem, xem AC7/AC8)
  - When tem quét bị từ chối bởi kiểm tra FR-08 (trùng `(TagCode, Stage)` toàn hệ thống / chưa qua công đoạn liền trước / đang khóa rework / sai định dạng)
  - Then **không gửi `BIND`**; máy vẫn khóa, App ở lại `WaitScan` chờ quét tem khác hoặc Tổ trưởng Hủy (AC11); luồng banner Lưu/Thoát của US-27/FR-27 áp dụng nguyên vẹn (Thoát/Esc → không lưu gì; Lưu → Supervisor đăng nhập mới lưu)
- **AC4 — Kiểm tra tem trước, lưu sau; endpoint kiểm tra không ghi dữ liệu**
  - Given tem quét ở `WaitScan`
  - When App kiểm tra tem
  - Then bước kiểm tra chỉ đọc, **không** tạo `Scan`; `Scan` chỉ được ghi sau `BIND_OK`
- **AC5 — Rút dây trước khi quét QR**
  - Given App `WaitScan`
  - When người vận hành rút dây ra khỏi jig, máy gửi `#VOID,SEQ=n,REASON=REMOVED`
  - Then App ghi event log `VOID_REMOVED`, hiển thị "Dây đã bị rút — thông điện lại", về `Ready`; không có bản ghi `Scan`
- **AC6 — Dây rút ngay sát lúc BIND**
  - Given App `Binding`
  - When nhận `#VOID,REASON=REMOVED`, hoặc `#BIND_FAIL` với `REASON` = `NO_PENDING`
  - Then App không lưu, báo "Dây đã bị rút — thông điện lại", ghi log, về `Ready`
- **AC7 — Quét QR khi dây NG còn trên jig** *(S3b)*
  - Given dây vừa báo NG, App `Ready` (không giữ kết quả)
  - When người vận hành quét QR theo luồng thường → App gửi `#BIND,SEQ=0` **ngay, không gọi server kiểm tra tem trước** (chốt 12/10/2026: lúc này không có gì để lưu, tránh hiện banner trùng tem/Lưu-Thoát vô nghĩa cho dây NG) → máy trả `#BIND_FAIL,REASON=NG`
  - Then App hiển thị thông báo đỏ **"DÂY NG — dùng chế độ Scan NG"** (giữ vài giây hoặc tới khi bấm OK, không âm thanh); **không lưu** `Scan`; ghi event log `SCAN_ON_NG` kèm mã tem
- **AC8 — Quét QR khi chưa có kết quả** *(S3c, S5, S3d)*
  - Given App `Ready`
  - When quét QR → App gửi `#BIND,SEQ=0` ngay (không kiểm tra tem trên server, như AC7) → máy trả `BIND_FAIL,REASON=NO_PENDING`
  - Then hiển thị "Chưa thông điện", không lưu
  - And nếu máy trả `REASON=TESTING` → hiển thị "Đang thông điện — chờ kết quả rồi quét lại" (không ghi event log)
- **AC9 — Máy đang có kết quả mà App lỡ mất (BAD_SEQ)**
  - Given máy trả `BIND_FAIL,REASON=BAD_SEQ`
  - When App nhận
  - Then App ghi event log `BIND_FAIL`, gửi `#RESULT?`, nhận lại kết quả → `WaitScan`, báo người vận hành quét lại
- **AC10 — `BUSY` và không phản hồi**
  - Given App gửi `#BIND`
  - When máy trả `BIND_FAIL,REASON=BUSY` hoặc `#ERR,CODE=BUSY` (máy đã BIND rồi, đang chờ SAVED / hàng chờ lệnh đầy)
  - Then App không gửi trùng lệnh, đợi `PONG` để đồng bộ (`ST=BINDING` → tiếp tục `Saving`); hiển thị "Đang xác nhận lần trước — vui lòng chờ"
  - And nếu không có phản hồi trong 2 giây (`CommandTimeoutMs`) → gửi lại `BIND` tối đa 2 lần, sau đó về `WaitScan`, hiển thị "Không phản hồi từ máy thông điện", ghi log `BIND_FAIL`
- **AC11 — Hủy kết quả đang chờ (DISCARD) cần Tổ trưởng**
  - Given App `WaitScan` hoặc `Saving` (lỗi)
  - When Tổ trưởng bấm "Hủy kết quả", đăng nhập có permission `TesterResult.Discard` → App gửi `#DISCARD,SEQ=n`
  - Then máy trả `#VOID,REASON=DISCARD`; App ghi event log `VOID_DISCARD` kèm người duyệt, về `Ready`; không có bản ghi `Scan`
  - And người vận hành thường / đăng nhập sai / thiếu permission → không gửi `DISCARD`, kết quả vẫn giữ
- **AC12 — Chế độ Scan NG không đi qua máy**
  - Given Tổ trưởng/công nhân dùng Chế độ Scan NG (US-18/FR-18) tại trạm có máy
  - When quét tem sản phẩm lỗi và chọn lý do
  - Then luồng Scan NG + mở khóa rework (US-18/US-19) chạy y như cũ; **không gửi `BIND`** xuống máy; không thêm chức năng mới
- **AC13 — Lỗi lưu / mất kết nối server khi `Saving`** *(S11)*
  - Given đã `BIND_OK`, App `Saving`
  - When gọi lưu `Scan` lên server thất bại / mất kết nối server
  - Then **không gửi `SAVED`** (máy vẫn khóa, dây còn trên jig, PING vẫn chạy nên máy không tự hủy); hiển thị lỗi + nút "Thử lại"; Tổ trưởng (permission `TesterResult.Discard`) có thể Hủy; ghi event log `DB_ERROR` (nếu không ghi được server thì chỉ ghi file, không chặn thao tác)
  - And khi có kết nối lại, "Thử lại" lưu thành công → `SAVED` → `UNLOCKED`
- **AC14 — Không dùng hàng đợi offline (ngoại lệ của US-16)**
  - Given trạm có máy thông điện (công tắc BẬT)
  - When server không phản hồi lúc lưu
  - Then **không** ghi vào hàng đợi SQLite (US-16) và không mở khóa máy — đảm bảo mọi dây qua trạm đều có bản ghi trên server trước khi máy mở khóa
- **AC15 — Chống lưu trùng khi retry**
  - Given đã lưu `Scan` với `(WorkStationId, TesterBootId, TesterSeq)`
  - When App gửi lại cùng bộ khóa (retry sau mất mạng)
  - Then server trả về bản ghi cũ như thành công (idempotent), không tạo bản ghi thứ hai, không cộng ACTUAL hai lần
- **AC16 — Ghi DB xong nhưng mất kết nối máy trước `UNLOCKED`**
  - Given đã commit `Scan` và gửi `SAVED`
  - When mất kết nối máy trước khi nhận `#UNLOCKED`
  - Then bản ghi vẫn hợp lệ (dây đã được xác nhận bằng `BIND_OK`); máy tự mở khóa khi mất link quá 5 giây; xử lý đồng bộ lại theo US-14/AC5
- **AC17 — Nút Test / Learn / Lưu trên máy không ảnh hưởng App** *(S14, S15)*
  - Given App đang kết nối
  - When người dùng bấm nút Test hoặc Learn/Lưu trên máy
  - Then App không nhận kết quả nào (máy không gửi `RESULT`/gì khác), trạng thái App không đổi
- **AC18 — Mất focus cửa sổ** *(S13, spec-app 6.2)*
  - Given `AndonBoardWindow` bị `Deactivated` (click sang cửa sổ khác)
  - When đó là trạm dùng keyboard-wedge
  - Then hiển thị banner đỏ **"APP KHÔNG ĐƯỢC CHỌN — KHÔNG NHẬN MÃ QUÉT"** cho tới khi cửa sổ active lại; mã quét lúc đó không bị xử lý nhầm
- **AC19 — Giao diện tối thiểu trên `AndonBoardWindow`** (giữ mockup nền tối, ADR-007; không âm thanh)
  - Then hiển thị: chữ trạng thái lớn ("Đặt dây và chờ thông điện" / "OK — QUÉT MÃ QR TRÊN DÂY" / "Đang xác nhận…" / "Đang lưu…" / "ĐÃ LƯU <mã tem>"); thông báo cho các `BIND_FAIL` (NG/TESTING/NO_PENDING/BAD_SEQ/BUSY/timeout) như AC7–AC10; nút "Hủy kết quả" (cần Tổ trưởng); công tắc chế độ (US-11). Chỉ báo kết nối/FW/GM và các banner kết nối thuộc US-14.
- **AC20 — Trạm không có máy hoặc công tắc TẮT**
  - Given trạm `UseArduino = false`, hoặc có máy nhưng công tắc TẮT
  - When quét tem
  - Then **không** chạy luồng trên; xem US-11/AC1 và AC6 (Bypass)

## Kịch bản nghiệm thu với máy thật (map từ spec-app 12.3)
| # | Kịch bản | Kỳ vọng | AC |
|---|---|---|---|
| S1 | Mở App, chọn COM | Máy không bị reset; hiện FW, PINS, GM | US-14 |
| S2 | Dây OK → quét QR | Lưu OK (`TesterMode = Tester`), máy hiện `Remove harness...` | AC2 |
| S3 | Dây NG → Exit trên máy | App không đổi trạng thái; máy cho rút dây ngay | AC17, AC7 |
| S3b | S3 → cố quét QR khi dây NG còn trên jig | "DÂY NG — dùng chế độ Scan NG", không bản ghi, có `SCAN_ON_NG` kèm mã tem; máy hiện `NG - not saved` | AC7 |
| S3c | S3b → rút dây → quét lại | "Chưa thông điện" | AC8 |
| S3d | Quét QR khi máy đang thông điện (chưa Pass) | "Đang thông điện — chờ kết quả…" | AC8 |
| S4 | Có kết quả → rút dây → quét QR | Từ chối; log `VOID_REMOVED` | AC5, AC6 |
| S5 | Quét QR khi chưa thông điện | "Chưa thông điện" | AC8 |
| S6 | Quét tem bị kiểm tra FR-08 từ chối | Không gửi BIND, máy vẫn khóa, banner Lưu/Thoát; quét tem đúng sau đó lưu bình thường | AC3 |
| S7 | Rút cáp USB máy khi đang chờ quét | Banner đỏ, chặn quét; cắm lại → đồng bộ đúng | US-14 |
| S8 | Reset máy (EN) khi đang chờ quét | Log `VOID_REBOOT`, về Ready | US-14 |
| S9 | Tắt công tắc (Tổ trưởng) → quét QR | Lưu `Result = Ok` + `TesterMode = Bypass`; máy chạy độc lập sau ≤ 5 giây; có log đổi chế độ; không banner BYPASS | US-11/AC6 |
| S10 | Bật lại công tắc | Kết nối lại; quét QR khi chưa thông điện → "Chưa thông điện" | US-11/AC8, AC8 |
| S11 | Mất kết nối server khi đang lưu | Báo lỗi, máy vẫn khóa; có mạng lại → "Thử lại" lưu thành công, máy mở khóa | AC13 |
| S12 | Chạy 1 ca (vài trăm dây) | Không mất kết nối, không bản ghi trùng `(WorkStationId, BootId, Seq)` | AC15 |
| S13 | App mất focus rồi quét | Banner cảnh báo; mã không bị xử lý nhầm | AC18 |
| S14 | Bấm nút Test trên máy khi đang kết nối | App không nhận gì | AC17 |
| S15 | Learn + Lưu trên máy khi đang kết nối | App không nhận gì | AC17 |

## Ghi chú lưu trữ (dev tham khảo)
- Thêm cột vào `Scan`: `TesterMode` (`None`/`Tester`/`Bypass`), `TesterBootId`, `TesterSeq`, `GoldenCrc`, `FirmwareVersion`, `TestedAtUtc`. Không tạo bảng `test_record` riêng.
- Unique index `(WorkStationId, TesterBootId, TesterSeq)` chống lưu trùng; MySQL cho phép nhiều dòng NULL nên các bản ghi `None`/`Bypass` không bị ảnh hưởng. Retry trùng → trả bản ghi cũ như thành công.
- Bảng event log sự kiện máy với các loại: `VOID_REMOVED`, `VOID_DISCARD`, `VOID_REBOOT`, `VOID_LINK_LOST`, `LINK_LOST`, `LINK_RESTORED`, `BIND_FAIL`, `SCAN_ON_NG`, `DB_ERROR`. Ghi lỗi event log → chỉ ghi file Serilog, không chặn thao tác.
- Một lệnh tại một thời điểm: sau `BIND`/`SAVED`/`DISCARD`/`RESULT?`/`HELLO?` chờ phản hồi tương ứng (hoặc 2 giây) mới gửi lệnh kế; `PING` độc lập theo timer.
- Thời gian lấy theo giờ PC trạm (máy thông điện không có đồng hồ thực); cân nhắc dùng giờ server để thống nhất.

## Ngoài phạm vi / để sau
- Kiểm tra số pin (`PINS`) theo Model: hiện chỉ đọc `PINS` từ `HELLO` để hiển thị/ghi log (xem US-14).
- Âm thanh cảnh báo: không làm.

**Nguồn FR:** FR-12 (viết lại 12/10/2026), mục 6 quy tắc 10, AC-17/AC-18/AC-19/AC-19b/AC-19c/AC-20
**Phụ thuộc:** US-08 (kiểm tra hợp lệ — endpoint kiểm tra không lưu), US-11 (công tắc/Bypass), US-14 (kết nối, heartbeat, đồng bộ lại), US-18/US-19 (dây NG, rework), US-27 (banner Lưu/Thoát), US-22/ADR-004 (permission `TesterResult.Discard`), ADR-005. **Không còn phụ thuộc US-13 (đã hủy).** Ngoại lệ với US-16 (không dùng hàng đợi offline).
**Cờ cảnh báo mục 8.2:** Có — điểm mở #2 (công đoạn nào dùng máy; hiện chỉ Thông điện).

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần (code + test xong, chưa nghiệm thu S1–S15 với máy thật / chưa kiểm tra trực quan đầy đủ)
- **Cập nhật:** 2026-10-12

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-12 (ba, viết lại theo spec máy thông điện ESP32)
- Viết lại toàn bộ story: luồng mới "thông điện TRƯỚC, quét QR SAU" (BIND → BIND_OK → lưu → SAVED → UNLOCKED), thứ tự kiểm tra tem → BIND → lưu → SAVED.
- **Bỏ** AC4–AC6 cũ (timeout 45s suy luận NG, Tổ trưởng xác nhận/hủy NG) và phụ thuộc US-13.
- Thêm: xử lý các `BIND_FAIL` (NG/TESTING/NO_PENDING/BAD_SEQ/BUSY/timeout), Hủy kết quả cần Tổ trưởng, Scan NG không gửi BIND, lỗi server khi `Saving` (ngoại lệ US-16), idempotency `(WorkStationId, TesterBootId, TesterSeq)`, cảnh báo mất focus, UI tối thiểu AndonBoard, bảng nghiệm thu S1–S15.
- Chưa có code triển khai.

#### 2026-10-12 (người giao việc chốt, ghi bởi agent chính)
- Quét QR ở `Ready` (App không giữ kết quả OK): gửi `#BIND,SEQ=0` ngay, **không** gọi server kiểm tra tem trước — người vận hành nhận đúng lý do từ máy (DÂY NG/Chưa thông điện/Đang thông điện), không bị banner trùng tem/Lưu-Thoát. Kiểm tra tem trên server chỉ chạy ở `WaitScan` (AC3/AC4). Sửa luồng tóm tắt, AC3, AC7, AC8 cho khớp.

#### 2026-10-12 (dev, triển khai luồng "thông điện trước → quét QR sau") — commit `86fdfb3` (backend), `90df2ad` (state machine), `102cb15` (Station.Wpf)
- **Domain:** `Scan` thêm `TesterBootId`, `TesterSeq`, `GoldenCrc`, `FirmwareVersion`, `TestedAtUtc` (nullable; `TesterMode` đã thêm ở US-11).
- **Application:** `ScanService` tách lõi dùng chung `EvaluateAndMaybeSaveAsync` (đúng 1 đường code FR-08/US-19/US-25 cho scan thường, kiểm tra không lưu và lưu theo máy — không nhân bản rule): `CheckAsync` (KHÔNG ghi `Scan`, KHÔNG bắn real-time — AC4), `CreateTesterScanAsync` (lưu `Result = Ok`, `TesterMode = Tester` + trường máy, idempotent theo `(WorkStationId, TesterBootId, TesterSeq)`: gửi lại cùng bộ khóa trả bản ghi cũ, cùng bộ khóa nhưng khác tem => 409, không cộng ACTUAL hai lần — AC15; chặn khi trạm không có máy / công tắc TẮT). Validator `CheckScanRequest`, `CreateTesterScanRequest`.
- **Infrastructure:** migration `AddScanTesterResultColumns` (5 cột + unique index `(WorkStationId, TesterBootId, TesterSeq)`; MySQL cho phép nhiều NULL nên bản ghi `None`/`Bypass` không bị ảnh hưởng).
- **Api:** `POST api/v1/scans/check` (200, StationApiKey), `POST api/v1/scans/tester` (201, StationApiKey).
- **Thư viện `Station.Tester` — `TesterStationController`** (state machine `Stopped/Disconnected/Ready/WaitScan/Binding/Saving/Discarding`, mọi thay đổi trạng thái trên 1 luồng qua `ITesterDispatcher`, 1 lệnh chờ phản hồi tại 1 thời điểm, timeout `CommandTimeoutMs` gửi lại tối đa 2 lần): `RESULT` => `WaitScan` (AC1); quét ở `WaitScan` => `CheckTag` (FR-08) => hợp lệ `#BIND,SEQ=n` => `BIND_OK` => lưu server => CHỈ SAU commit mới `#SAVED` => `UNLOCKED` => "ĐÃ LƯU <mã tem>" (AC2); tem bị từ chối => KHÔNG BIND, máy vẫn khóa, banner Lưu/Thoát US-27 (AC3); `VOID REMOVED` ở `WaitScan`/`Binding`/`Saving`, `BIND_FAIL NO_PENDING/REMOVED` ở `Binding` => không lưu, log, về `Ready` (AC5/AC6); quét ở `Ready` => `#BIND,SEQ=0` NGAY, KHÔNG gọi server kiểm tra tem: `NG` => "DÂY NG — dùng chế độ Scan NG" + event `ScanOnNg` kèm mã tem, `NO_PENDING` => "Chưa thông điện", `TESTING` => "Đang thông điện — chờ kết quả rồi quét lại", `BAD_SEQ` => event `BindFail` + `#RESULT?` => `WaitScan` + báo quét lại (AC7/AC8/AC9); `BUSY`/`ERR BUSY` ở `Binding` => không gửi trùng, đợi PONG (`BINDING` => `Saving`), "Đang xác nhận lần trước — vui lòng chờ"; không phản hồi => gửi lại tối đa 2 lần rồi `WaitScan` + "Không phản hồi từ máy thông điện" + event `BindFail` (AC10); `DISCARD` (Tổ trưởng) => `VOID DISCARD` => event `VoidDiscard` kèm người duyệt (AC11); lưu lỗi/mất server => KHÔNG SAVED, event `DbError`, nút "Thử lại", Tổ trưởng Hủy được, không hàng đợi offline (AC13/AC14); mất kết nối máy sau khi commit => bản ghi vẫn hợp lệ, đồng bộ lại theo US-14 (AC16).
- **Station.Wpf:** `HandleScanAsync` định tuyến: Chế độ Scan NG (không BIND — AC12) / chế độ `Active` => `TesterStationController` / còn lại luồng thường. `AndonBoardWindow` giữ nền tối (ADR-007), KHÔNG âm thanh: chữ trạng thái lớn ("Đặt dây và chờ thông điện" / "OK — QUÉT MÃ QR TRÊN DÂY" / "Đang xác nhận…" / "Đang lưu…" / "ĐÃ LƯU …") + thông báo BIND_FAIL (lỗi giữ 12 giây hoặc tới khi bấm OK) + nút "HỦY KẾT QUẢ" (đăng nhập Tổ trưởng, permission `TesterResult.Discard`) + "THỬ LẠI" + công tắc chế độ (US-11) + banner đỏ "APP KHÔNG ĐƯỢC CHỌN — KHÔNG NHẬN MÃ QUÉT" khi `Deactivated` (mọi trạm — AC18; mã quét lúc đó bị bỏ, không xử lý nhầm).
- **Test:** `ScanServiceTesterModeTests` (check không lưu / lưu đủ trường / retry cùng bộ khóa / khác tem / BOOT khác / chặn khi TẮT), `TesterScanValidatorTests`; `TesterStationControllerTests` (69 kịch bản bám bảng 7.1/7.3/7.5) + `TesterIntegrationTests` (S1, S2, S3b/S3c/S3d, S4, S5, S6, S7, S7b, S8, S11, Hủy kết quả, mất PONG, thiếu mẫu, tắt/bật công tắc) chạy với `MachineSimulator`. Kết quả: `Application.Tests` 424/424, `Station.Tester.Tests` 160/160 pass.
- **Build:** `dotnet build ProductionMES.sln` 0 lỗi 0 cảnh báo.
- **Quyết định tự chọn:**
  - Server chạy lại kiểm tra FR-08 đầy đủ ở bước lưu (có khoảng thời gian giữa kiểm tra và lưu); nếu bị từ chối lúc lưu thì Station.Wpf xử lý như lỗi lưu (máy vẫn khóa, "Thử lại"/"Hủy").
  - Sau `BIND_OK` mà `SAVED` không nhận `UNLOCKED` sau 3 lần gửi: bản ghi đã commit nên "Thử lại" chỉ gửi lại `SAVED` (không lưu lại), thông báo "Đã lưu nhưng chưa nhận xác nhận mở khóa từ máy".
  - `BIND_OK` đến muộn (sau khi đã hết lượt gửi lại, App về `WaitScan`) vẫn được chấp nhận miễn đã có tem.
  - `BUSY` ở `Ready` (App tưởng không giữ kết quả nhưng máy đang chờ SAVED): gửi lại tối đa 2 lần rồi báo "Máy thông điện đang bận".
  - Hủy kết quả chỉ cho ở `WaitScan` hoặc `Saving` khi lưu lỗi (chưa commit); không cho khi đang lưu dở hoặc đã commit.
  - Banner mất focus bỏ qua trường hợp `Deactivated` do mở hộp thoại con (đăng nhập Tổ trưởng) của chính cửa sổ.
  - Nếu trạm Đóng thùng cũng có máy: `ScanSaved` vẫn gọi `ApplyPackingScanResult` để bộ đếm thùng đúng (chưa kiểm thử tổ hợp này).
- **Còn thiếu / lý do giữ 🟡:** chưa nghiệm thu S1–S15 với máy thật (đặc biệt S2, S3b, S7, S8, S11–S15) và chưa kiểm tra trực quan đầy đủ các trạng thái trên màn hình trạm (đã chụp thử: mất kết nối, công tắc BẬT/TẮT, panel máy với server giả); chưa có kiểm tra PINS theo Model (ngoài phạm vi); US-12 phụ thuộc chạy migration `AddScanTesterResultColumns`/`AddTesterEventLog`/`AddTesterModeSwitchAndScanTesterMode` trên DB thật.
