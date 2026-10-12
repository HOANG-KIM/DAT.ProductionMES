# Đặc tả firmware — Kết nối App WPF (thông điện trước, quét QR sau)

> Module **2.16** của `spec-firmware-wire-harness-tester.md`. File này là đặc tả chi tiết; trạng thái `[ ]`/`[~]`/`[x]` theo dõi ở cả mục 2.16 bên spec chính và checklist mục 10 bên dưới.
> Đặc tả phía app: `spec-app-wpf-station.md` — **giao thức ở mục 4 file này là nguồn chân lý chung cho cả hai bên**.

---

## 1. Mục tiêu

Cho phép App WPF lưu kết quả thông điện kèm số serial (SN) đọc từ mã QR trên dây, theo quy trình:

```
Đặt dây lên jig → máy tự thông điện (2.15)
   → NG: máy báo NG (LCD/LED/còi), tháo dây đem đi sửa — không gửi gì, không khoá
   → OK + [có App kết nối]: khoá máy, chờ quét QR
   → App quét QR → gửi BIND → máy xác nhận dây vẫn còn trên jig → App lưu → mở khoá
```

Khi **không** có App kết nối, máy chạy độc lập đúng như hiện tại (2.15), không chờ gì.

## 2. Quyết định đã chốt

| # | Quyết định |
|---|---|
| D1 | Thông điện **trước**, quét QR **sau** (dây đã đặt sẵn trên bàn thông điện) |
| D2 | **Có App kết nối ⇔ bắt buộc quét QR.** Không có lệnh `MODE` riêng — trạng thái kết nối chính là chế độ |
| D3 | "Có App kết nối" = nhận được `#PING` trong vòng `LINK_TIMEOUT_MS` = 5000 ms gần nhất |
| D4 | App có công tắc "Sử dụng máy thông điện". TẮT → App ngừng PING / đóng cổng → máy tự về chạy độc lập sau ≤ 5 s (firmware không cần biết công tắc này) |
| D5 | **Chỉ kết quả OK mới gửi lên App và khoá chờ quét QR.** NG: máy báo NG như hiện tại rồi cho tháo dây ngay (đem đi sửa, sửa xong thông điện lại) — không chủ động gửi gì cho App, không khoá |
| D11 | **Người vận hành cố tình quét QR dây NG**: App gửi `BIND` như mọi lần quét, máy trả `#BIND_FAIL,REASON=NG` (dây vừa báo NG vẫn còn trên jig) → App hiện thông báo lỗi, không lưu |
| D6 | Xử lý serial theo **cách 1**: poll không blocking trong `loop()` + gọi xen giữa các drive pin qua `ScanHook` có sẵn. Không dùng FreeRTOS task riêng |
| D7 | Lệnh `#PING` xử lý ngay cả giữa lúc scan; lệnh đổi trạng thái (`BIND`, `SAVED`, `DISCARD`, `RESULT?`) xếp hàng, thực thi khi state machine rảnh |
| D8 | **ESP32 không biết gì về mã QR / SN.** Việc đọc, kiểm tra định dạng, chống trùng SN là việc của App. ESP32 chỉ nhận lệnh, kiểm tra theo `SEQ` và trạng thái của nó rồi trả lời |
| D9 | **Nút Test (duyệt từng pin, 2.6) chỉ để chẩn đoán**, có hay không có App đều vậy: không gửi `RESULT`, không khoá chờ quét QR |
| D10 | **Learn / Lưu hoạt động như bình thường**, không cần App cho phép, **không gửi gì cho App** |

## 3. Kết nối vật lý

- **Cổng: UART0 (`Serial`)** — cổng USB ghi "UART" trên DevKitC-1 (qua chip USB-UART). Cùng cổng đang dùng cho log debug.
- **Không dùng cổng USB native (USB-CDC)**: GPIO 19/20 là USB D-/D+ nhưng đang được dùng làm nút Xoá counter / Lưu (mục 1 spec chính).
- 115200 baud, 8N1 (giữ `monitor_speed` hiện tại).
- Vì dùng chung với log, quy ước:
  - **Mọi dòng giao thức bắt đầu bằng `#`**, kết thúc bằng `\n` (chấp nhận `\r\n` khi nhận).
  - Mọi dòng khác (log debug, `[Coverage]...`, `[NVS]...`, rác ROM bootloader lúc reset) là log — App bỏ qua.
  - Không được có log nào bắt đầu bằng `#`.
  - Bản sản xuất phải để `DEBUG_SCAN_RAW_ADC` = 0 (in hàng nghìn dòng mỗi lần quét, làm chậm scan và nghẽn cổng).
- Lưu ý mạch auto-reset của DevKitC: DTR/RTS của cổng UART nối vào EN/IO0. App phải mở cổng với DTR = RTS = false (xem spec app). Firmware vẫn phải chịu được reset bất ngờ (gửi `#HELLO` lúc boot, mục 4.3).

## 4. Giao thức

### 4.1 Định dạng chung

```
#<LỆNH>[,<KEY>=<VALUE>]...\n
```

- ASCII in được, phân tách trường bằng dấu `,`. Không có khoảng trắng thừa.
- Độ dài tối đa 1 dòng: **256 byte** kể cả `\n`. Dòng dài hơn bị bỏ (firmware xoá bộ đệm dòng, không xử lý).
- Lệnh và key viết HOA. Thứ tự key không cố định — bên nhận phải tra theo tên key.
- Số pin hiển thị **1..N** (quy ước toàn project), chuyển từ index nội bộ 0..N-1 ngay tại chỗ in.
- SN **không** đi qua giao thức (D8) — App tự giữ SN, ghép với `SEQ` khi lưu DB.

### 4.2 App → ESP32

| Lệnh | Ý nghĩa | Xử lý |
|---|---|---|
| `#PING` | Heartbeat, App gửi mỗi 1000 ms | **Ngay** (kể cả giữa scan): cập nhật `lastPingMs_`, trả `#PONG` |
| `#BIND,SEQ=<n>` | App vừa quét được QR. `SEQ` = kết quả OK App đang giữ, hoặc `0` nếu App không giữ kết quả nào. Xin xác nhận dây vẫn còn trên jig | Xếp hàng → trả `#BIND_OK` hoặc `#BIND_FAIL`. Riêng khi đang Live scan: trả ngay `BIND_FAIL,REASON=TESTING` từ `poll()` (mục 6.1) |
| `#SAVED,SEQ=<n>` | App đã lưu DB xong | Xếp hàng → mở khoá, trả `#UNLOCKED` |
| `#DISCARD,SEQ=<n>` | App huỷ kết quả đang chờ (tổ trưởng duyệt) | Xếp hàng → mở khoá, trả `#VOID,REASON=DISCARD` |
| `#RESULT?` | Xin gửi lại kết quả đang chờ (khi App lỡ mất dòng `RESULT`) | Xếp hàng → gửi lại đúng dòng `#RESULT` cũ, hoặc `#ERR,CODE=NO_PENDING` |
| `#HELLO?` | Xin thông tin máy | Xếp hàng → trả `#HELLO` |

### 4.3 ESP32 → App

| Lệnh | Khi nào | Ví dụ |
|---|---|---|
| `#HELLO` | Lúc boot; khi link chuyển từ mất → có (PING đầu tiên); trả lời `#HELLO?` | `#HELLO,FW=1.4.0,PINS=16,GM=3A7F,BOOT=8F21C04D` |
| `#PONG` | Trả lời mỗi PING | `#PONG,ST=WAIT_BIND,SEQ=105` |
| `#RESULT` | Thông điện **OK** khi đang có link (NG không gửi — D5) | `#RESULT,SEQ=105,R=OK,GM=3A7F` |
| `#BIND_OK` | BIND hợp lệ, dây vẫn còn trên jig | `#BIND_OK,SEQ=105` |
| `#BIND_FAIL` | BIND không hợp lệ | `#BIND_FAIL,SEQ=105,REASON=REMOVED` |
| `#UNLOCKED` | Sau `SAVED` hợp lệ | `#UNLOCKED,SEQ=105` |
| `#VOID` | Kết quả đang chờ bị huỷ | `#VOID,SEQ=105,REASON=REMOVED` |
| `#ERR` | Lệnh sai / không hợp lệ ở trạng thái hiện tại | `#ERR,CODE=BAD_CMD` |

**Ý nghĩa các trường:**

| Key | Mô tả |
|---|---|
| `FW` | Phiên bản firmware (hằng `FW_VERSION`) |
| `PINS` | `HARNESS_PIN_COUNT` (16/32/48/64) |
| `GM` | CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF) của `GoldenMatrix::serialize()`, 4 ký tự hex HOA. `0000` dành riêng cho "chưa có mẫu" (golden rỗng) |
| `BOOT` | Số ngẫu nhiên 32-bit hex sinh lúc boot (`esp_random()`). App so với lần trước để biết máy đã reset |
| `ST` | Trạng thái: `IDLE` (chờ cắm) · `TESTING` (đang Live scan) · `WAIT_BIND` · `BINDING` (đã BIND_OK, chờ SAVED) · `WAIT_REMOVE` · `BROWSE` (đang duyệt kết quả nút Test) · `NO_SAMPLE` |
| `SEQ` | Số thứ tự kết quả, `uint32_t`, bắt đầu từ 1 mỗi lần boot, tăng 1 mỗi `#RESULT` mới. Trong `PONG`: SEQ đang chờ, `0` nếu không có |
| `R` | Luôn `OK` (giữ trường để mở rộng sau này) |
| `REASON` (BIND_FAIL) | `NG` (dây đang trên jig vừa bị báo NG) · `TESTING` (đang thông điện, chưa có kết quả) · `NO_PENDING` (không có kết quả nào: chưa cắm dây / đã rút / đã lưu xong) · `BAD_SEQ` (đang chờ kết quả OK khác SEQ App gửi — App nên gửi `RESULT?` để đồng bộ) · `BUSY` (đã BIND rồi, đang chờ SAVED) |
| `REASON` (VOID) | `REMOVED` (rút dây trước khi lưu xong) · `DISCARD` (App huỷ) |
| `CODE` (ERR) | `BAD_CMD` · `BAD_SEQ` · `NO_PENDING` · `TOO_LONG` |

### 4.4 Luồng điển hình

**OK:**
```
ESP → #RESULT,SEQ=105,R=OK,GM=3A7F          (LCD: "Scan QR code...", LED xanh)
App   (người vận hành quét QR — App tự kiểm tra mã, chống trùng)
App → #BIND,SEQ=105
ESP → #BIND_OK,SEQ=105                      (dây vẫn còn trên jig)
App   (ghi DB)
App → #SAVED,SEQ=105
ESP → #UNLOCKED,SEQ=105                     (LCD: "Remove harness...")
      … rút dây → IDLE → cắm sợi kế tiếp
```

**Rút dây trước khi quét:**
```
ESP → #RESULT,SEQ=106,R=OK,GM=3A7F
      (người vận hành rút dây ra trước khi quét QR)
ESP → #VOID,SEQ=106,REASON=REMOVED          (App ghi log, phải thông điện lại)
```

**NG:** máy báo NG trên LCD/LED/còi, không chủ động gửi dòng nào (SEQ không tăng). Tháo dây đem đi sửa → sửa xong cắm lại → thông điện lại như sợi mới.

**NG nhưng người vận hành vẫn quét QR:**
```
      (máy báo NG, dây vẫn trên jig)
App   (quét QR, App không giữ kết quả nào)
App → #BIND,SEQ=0
ESP → #BIND_FAIL,SEQ=0,REASON=NG            (LCD dòng 1: "NG - not saved" 1,5 s)
App   hiện thông báo lỗi "DÂY NG"
```

**App lỡ mất dòng RESULT (đồng bộ lại):**
```
ESP → #PONG,ST=WAIT_BIND,SEQ=107            (App chưa biết SEQ 107)
App → #RESULT?
ESP → #RESULT,SEQ=107,R=OK,GM=3A7F
```

## 5. Hành vi trong state machine (2.12 / 2.15)

### 5.1 Trạng thái `HarnessState` mở rộng

Hiện có `kWaitInsert`, `kWaitRemove`. Thêm:

| Trạng thái | Vào khi | Thoát khi |
|---|---|---|
| `kWaitBind` | Live scan kết thúc **Pass** **và** `isAppConnected()` | BIND hợp lệ → `kWaitSaved` · rút dây → `kWaitInsert` (gửi VOID) · `DISCARD` → `kWaitRemove` · mất link → `kWaitRemove` |
| `kWaitSaved` | `BIND_OK` đã gửi | `SAVED` → `kWaitRemove` · `DISCARD` → `kWaitRemove` · rút dây → `kWaitInsert` (gửi VOID) · mất link → `kWaitRemove` |

Live scan kết thúc kiểu **huỷ** (rút dây ở `kAuto`, Exit khi chưa có lỗi) → không có kết quả → không gửi `RESULT`, hành vi như cũ.

Live scan kết thúc **Fail** (Exit khi còn lỗi) → báo NG như hiện tại (2.15), vào thẳng `kWaitRemove`, **không gửi gì, không khoá** — có hay không có App đều vậy (D5). Đồng thời đặt cờ `lastFail_ = true` để trả lời `BIND` bằng `REASON=NG` (D11). Xoá cờ khi: xác nhận đã rút dây (`kWaitRemove` → `kWaitInsert`), hoặc bắt đầu Live scan mới, hoặc Learn.

Không có link → sau Pass vào thẳng `kWaitRemove` như hiện tại.

### 5.2 Quy tắc khi đang khoá (`kWaitBind` / `kWaitSaved`)

- **Không** tự khởi động Live scan, kể cả khi dò thấy dây (dây đang cắm sẵn là chuyện bình thường ở trạng thái này).
- Nút **Start, Test, Learn, Lưu**: bỏ qua, LCD dòng 1 hiện `Scan QR first` ~1,5 s. (Ngoài lúc khoá, Test và Learn/Lưu chạy bình thường — D9, D10. Riêng lúc khoá phải chặn: duyệt Test làm ngừng dò rút dây (2.15), còn Learn sẽ đổi mẫu trong khi kết quả theo mẫu cũ đang chờ lưu.)
- Nút **Exit**: bỏ qua — chỉ App (`DISCARD`) hoặc rút dây mới huỷ được kết quả, để mọi lần huỷ đều để lại dấu vết bên App.
- Nút **Xoá counter**: vẫn cho phép (không ảnh hưởng truy vết).
- LED Pass giữ sáng như hiện tại (tắt khi đã rút dây).
- **Dò rút dây vẫn chạy**: `pollHarness()` tiếp tục gọi `isHarnessPresent()` mỗi `kPresencePollMs`; không thấy dây `kRemoveConfirmPolls` lần liên tiếp → gửi `#VOID,REASON=REMOVED`, về `kWaitInsert`. Đây là cơ sở để `BIND_OK` khẳng định "đúng sợi dây vừa đo" — máy theo dõi liên tục từ lúc có kết quả đến lúc lưu xong.
- Mất link (quá `LINK_TIMEOUT_MS` không có PING): huỷ kết quả tại chỗ (không gửi được VOID), LCD dòng 1 `App lost-result void` → `kWaitRemove`. Khi link có lại, `#HELLO` + `PONG ST=...,SEQ=0` cho App biết kết quả cũ không còn.

### 5.3 Xử lý từng lệnh

| Lệnh | Điều kiện hợp lệ | Hành động |
|---|---|---|
| `BIND` | `kWaitBind`, `SEQ` = SEQ đang chờ | Gửi `BIND_OK` → `kWaitSaved`. Các trường hợp còn lại xét theo thứ tự: |
| | | 1. Đang Live scan → `TESTING` (trả ngay trong `poll()`) |
| | | 2. `kWaitBind` nhưng SEQ khác (kể cả 0) → `BAD_SEQ` |
| | | 3. `kWaitSaved` → `BUSY` |
| | | 4. `lastFail_` → `NG`, LCD dòng 1 `NG - not saved` ~1,5 s |
| | | 5. Còn lại → `NO_PENDING` |
| | | Không lệnh nào ở bước 1–5 làm đổi trạng thái máy |
| `SAVED` | `kWaitSaved`, SEQ khớp | Gửi `UNLOCKED`, LCD dòng 1 `Saved OK`, → `kWaitRemove` |
| `DISCARD` | `kWaitBind` hoặc `kWaitSaved`, SEQ khớp | Gửi `VOID,REASON=DISCARD`, → `kWaitRemove` |
| `RESULT?` | Có kết quả đang chờ | Gửi lại nguyên dòng `RESULT` đã lưu (cùng SEQ) |

Firmware không nhận, không lưu, không hiển thị SN (D8).

### 5.4 Counter Pass/Fail cục bộ

Giữ nguyên: counter tăng ngay lúc Live scan kết luận Pass/Fail (2.11), không phụ thuộc việc App có lưu hay không. Counter là số liệu tại máy; số liệu chính thức nằm trong DB của App.

### 5.5 Các nguồn kết quả

| Nguồn | Gửi `RESULT`? |
|---|---|
| Live scan `kAuto` (cắm dây tự chạy, 2.15) | Có, chỉ khi Pass |
| Live scan `kManual` (nút Start) | Có, chỉ khi Pass |
| Nút Test (duyệt từng pin, 2.6) | **Không** — chỉ chẩn đoán, có hay không có App (D9). Counter cục bộ vẫn tăng như hiện tại (2.6), chỉ là số liệu tại máy |

## 6. Cấu trúc code

### 6.1 Module mới `src/link/serial_link.h` / `.cpp` (2.16)

```cpp
namespace SerialLink {

enum class CommandType : uint8_t { kNone, kBind, kSaved, kDiscard, kResultQuery, kHelloQuery };

struct Command {
  CommandType type = CommandType::kNone;
  uint32_t seq = 0;
};

void begin();                    // dùng Serial đã begin() trong main.cpp; sinh BOOT id
void poll();                     // KHÔNG blocking: đọc hết byte có sẵn, ghép dòng,
                                 // PING xử lý ngay, lệnh khác vào hàng chờ 1 phần tử
bool isAppConnected();           // millis() - lastPingMs < LINK_TIMEOUT_MS
bool linkJustCameUp();           // true đúng 1 lần khi link mất -> có (để gửi HELLO)
bool takeCommand(Command& out);  // lấy lệnh đang chờ (nếu có)

// State machine cập nhật để PONG trả đúng ST/SEQ
void setReportedState(const char* st, uint32_t pendingSeq);

void sendHello(uint16_t gmCrc);
uint32_t sendResultOk(uint16_t gmCrc);  // tự đánh SEQ mới, trả SEQ; lưu dòng để RESULT? gửi lại
void resendResult();
void sendBindOk(uint32_t seq);
void sendBindFail(uint32_t seq, const char* reason);
void sendUnlocked(uint32_t seq);
void sendVoid(uint32_t seq, const char* reason);
void sendError(const char* code);

}  // namespace SerialLink
```

Yêu cầu cài đặt:

- **Không dùng** `readStringUntil()` / `readString()` / `parseInt()` (blocking tới `Serial.setTimeout`, mặc định 1000 ms). Đọc từng byte trong `while (Serial.available())` vào `char lineBuf_[256]`.
- Byte đầu dòng khác `#` → bỏ cả dòng (không phải lệnh).
- Lệnh `BIND` đến khi `setReportedState()` đang là `TESTING`: trả ngay `#BIND_FAIL,SEQ=<n>,REASON=TESTING` trong `poll()`, không xếp hàng — Live scan có thể kéo dài tới khi Pass/Exit, xếp hàng sẽ làm App timeout. An toàn vì không đổi trạng thái.
- Hàng chờ lệnh: 1 phần tử. Lệnh mới đến khi phần tử cũ chưa được lấy → trả `#ERR,CODE=BUSY` cho lệnh mới (App luôn chờ phản hồi trước khi gửi lệnh kế, nên bình thường không xảy ra).
- Ghi: trước mỗi dòng kiểm tra `Serial.availableForWrite()` ≥ độ dài dòng; không đủ thì bỏ dòng đó (với `PONG`) — không được chặn scan. Riêng `RESULT` / `BIND_OK` / `VOID`: vẫn ghi (UART0 115200 ≈ 11,5 KB/s, 1 dòng < 25 ms) vì App cần chắc chắn nhận được.
- Gửi bằng 1 lần `Serial.write(buf, len)` cho cả dòng (đã `snprintf` sẵn) để log debug không chen vào giữa 1 dòng giao thức.
- Hằng số đặt ở đầu `serial_link.cpp`: `LINK_TIMEOUT_MS` = 5000, `kMaxLineLen` = 256. `FW_VERSION` đặt trong `src/config/version.h` (mới).

### 6.2 Thay đổi ở module khác (ngoài 2.16 nhưng bắt buộc)

| Module | Thay đổi |
|---|---|
| 2.4 `GoldenMatrix` | Thêm `uint16_t crc16() const` (CRC-16/CCITT-FALSE trên `serialize()`; trả `0x0000` nếu `isEmpty()`, nếu CRC thật ra 0x0000 thì trả 0x0001) |
| 2.13 `LiveScanMode::run()` | Trả về kết quả thay vì `void`: `enum class Outcome { kPass, kFail, kCancel }` (state machine chỉ cần biết Pass để gửi `RESULT`/khoá). Thêm tham số `TestMode::ScanHook extraHook, void* context` (mặc định `nullptr`) để state machine chèn `SerialLink::poll()` vào hook nội bộ (đang poll nút Exit) |
| 2.9 `LcdDisplay` | `HarnessPrompt::kWaitQr` → dòng 2 `Scan QR code...`; thông báo dòng 1: `Scan QR first`, `Saved OK`, `App lost-result void`, `Result void` |
| 2.12 `StateMachine` | Sở hữu vòng đời link: gọi `SerialLink::poll()` đầu `update()` và trong `pollButtons()` (đã được gọi xen giữa lần dò dây); thêm `kWaitBind`/`kWaitSaved`; `handleLinkCommand()` thực thi lệnh từ `takeCommand()` **chỉ khi không đang scan** (tự đúng vì kiến trúc đơn luồng: chỉ gọi trong `update()`); gửi `#HELLO` khi `linkJustCameUp()`; gọi `setReportedState()` mỗi lần đổi trạng thái |
| `main.cpp` | Giữ `Serial.begin(115200)`; thêm `SerialLink::begin()` (hoặc gọi trong `StateMachine::begin()`) |
| `config/debug.h` | Thêm `DEBUG_SERIAL_LINK` (mặc định 0): in `[LINK] rx: ...` / `[LINK] tx: ...` — tiền tố `[LINK]` (không phải `#`) để App bỏ qua |

### 6.3 Thời gian đáp ứng (cách 1)

| Tình huống | Độ trễ xử lý PING tối đa |
|---|---|
| Idle / chờ khoá | 1 vòng `update()` (< 1 ms) hoặc 1 drive pin trong lần dò dây |
| Live scan (hook mỗi drive pin) | ~2 ms (64 pin) |
| Learn / nút Test (single-shot, không hook) | 1 lần quét đủ: ~0,1–0,2 s ở 64 pin |
| Lưu NVS | vài chục ms |

Tất cả ≪ `LINK_TIMEOUT_MS` = 5 s. Số đo thực tế ghi vào mục 10 khi kiểm thử.

## 7. Hiển thị LCD (20x4, chỉ ASCII)

| Trạng thái | Dòng 0 | Dòng 1 | Dòng 2 | Dòng 3 |
|---|---|---|---|---|
| Chờ quét QR (chỉ sau OK) | trạng thái hệ thống | kết quả `PASS` | `Scan QR code...` | counter |
| NG (có hay không có App) | như hiện tại (2.15) | như hiện tại | `Remove harness...` | counter |
| Bấm nút khi đang khoá | — | `Scan QR first` (1,5 s) | — | — |
| Đã lưu | — | `Saved OK` | `Remove harness...` | — |
| Huỷ do rút dây | — | `Result void` | `Insert harness...` | — |
| Quét QR dây NG (nhận BIND khi `lastFail_`) | — | `NG - not saved` (1,5 s) | `Remove harness...` | — |
| Mất App khi đang chờ | — | `App lost-result void` | `Remove harness...` | — |

Góc phải dòng 0 có thể thêm ký hiệu link `[A]` khi `isAppConnected()` để người vận hành biết lần đo này sẽ phải quét QR (tuỳ chọn, chỉ làm nếu dòng 0 còn đủ chỗ ở mọi trạng thái).

## 8. Kiểm thử

Kiểm thử bằng terminal serial (gõ tay) trước khi có App: PuTTY / `pio device monitor` với DTR/RTS tắt (`pio device monitor --dtr 0 --rts 0`), hoặc script Python `pyserial` gửi PING mỗi 1 s.

| # | Kịch bản | Kỳ vọng |
|---|---|---|
| T1 | Không PING, cắm dây OK | Chạy như 2.15, không gửi gì, không khoá |
| T2 | PING đều, cắm dây OK | `RESULT R=OK`, LCD `Scan QR code...`, không tự test lại |
| T3 | T2 → BIND đúng → SAVED | `BIND_OK`, `UNLOCKED`, `Remove harness...` |
| T4 | T2 → rút dây | `VOID REASON=REMOVED` sau ~450 ms, về chờ cắm |
| T5 | T2 → BIND sai SEQ / chưa có kết quả | `BIND_FAIL` với `BAD_SEQ` / `NO_PENDING` |
| T6 | T2 → ngừng PING | sau 5 s: `App lost-result void`, mở khoá |
| T7 | T2 → bấm Start/Test/Learn/Lưu/Exit | bị bỏ qua, `Scan QR first` |
| T8 | Dây NG + Exit (đang PING) | Báo NG như hiện tại, **không** gửi dòng nào, không khoá, rút dây được ngay; `PONG` vẫn `SEQ=0` |
| T9 | T8 → sửa xong cắm lại, OK | `RESULT R=OK` với SEQ kế tiếp, chờ QR như T2 |
| T15 | Learn + Lưu khi đang PING | Chạy bình thường, không gửi dòng nào |
| T16 | T8 (dây NG còn trên jig) → `#BIND,SEQ=0` | `BIND_FAIL,SEQ=0,REASON=NG`, LCD `NG - not saved`, máy không đổi trạng thái |
| T17 | T16 → rút dây → `#BIND,SEQ=0` | `BIND_FAIL,REASON=NO_PENDING` |
| T18 | Đang Live scan (dây cắm hờ, chưa Pass) → `#BIND,SEQ=0` | `BIND_FAIL,REASON=TESTING` trả về ngay (< 50 ms), scan không bị gián đoạn |
| T10 | PING liên tục trong lúc Live scan 64 pin | `PONG ST=TESTING` đều đặn, không mất link |
| T11 | Reset ESP32 khi App đang mở cổng | `#HELLO` với `BOOT` mới, `SEQ` bắt đầu lại từ 1 |
| T12 | `RESULT?` khi đang chờ / không chờ | gửi lại đúng dòng cũ / `ERR NO_PENDING` |
| T13 | Bật `DEBUG_TEST` + PING | log in xen kẽ nhưng mọi dòng `#` nguyên vẹn |
| T14 | Đo thời gian 1 chu kỳ thông điện trước/sau khi thêm link | không chậm hơn đáng kể (ghi số đo) |

## 9. Vấn đề còn mở

Đã chốt (D5, D8–D11 ở mục 2): chỉ OK mới gửi/khoá, NG tháo ra sửa; cố quét QR dây NG → `BIND_FAIL,REASON=NG`; nút Test chỉ chẩn đoán; Learn/Lưu không gửi gì; ESP32 không xử lý SN.

Còn lại (không chặn việc code, quyết khi làm LCD):

1. **Hiển thị `[A]` trên LCD** khi có App: có làm không (phụ thuộc chỗ trống dòng 0).

## 10. Checklist triển khai

- `[x]` `config/version.h` (`FW_VERSION` = "1.0.0"), `DEBUG_SERIAL_LINK` trong `debug.h`
- `[x]` `GoldenMatrix::crc16()` (2.4) — tính thẳng trên `rows_` theo thứ tự byte của `serialize()`, không cần buffer 512 byte
- `[x]` `src/link/serial_link.h/.cpp`: parser không blocking, PING/PONG, BIND lúc `TESTING` trả ngay, hàng chờ 1 lệnh, các hàm `send*`. `LinkState` là enum (không phải chuỗi) cho `setReportedState()`
- `[x]` `LiveScanMode::run()` trả `LiveScanMode::Outcome` (enum chuyển từ `.cpp` ra header), thêm `extraHook`/`extraContext` (2.13)
- `[x]` LCD: `HarnessPrompt::kWaitQr`, `LinkMessage` + `showLinkMessage()` / `clearLinkMessage()` (chỉ xoá nếu dòng 1 vẫn đúng thông báo đó) (2.9)
- `[x]` State machine: `kWaitBind`/`kWaitSaved`, chặn nút, dò rút dây khi khoá, `handleLinkCommand()`/`handleBind()`, `lastFail_`, thông báo tạm không blocking, HELLO lúc boot + khi link có lại (2.12). Khác spec 6.2 một chút: `SerialLink::poll()` gọi trong hook của `pollHarness()` (cạnh `pollButtons()`) thay vì bên trong `pollButtons()`; thêm poll trong vòng chờ 1,5 s của `handleSave()`. `SAVED`/`DISCARD` sai trạng thái/SEQ → `#ERR,CODE=BAD_SEQ` (đang khoá) hoặc `NO_PENDING` (không khoá)
- `[x]` Build OK cả `tester16` và `tester64`, không cảnh báo; build thử `DEBUG_SERIAL_LINK=1` OK. RAM: 22,3 KB (16 pin) / 50,1 KB (64 pin)
- `[ ]` Kiểm thử T1–T18 bằng terminal/script
- `[ ]` Kiểm thử với App WPF thật
