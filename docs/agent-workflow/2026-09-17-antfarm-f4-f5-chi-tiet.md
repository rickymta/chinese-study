# F4 Hồ sơ & quản trị vai trò + F5 Pinyin & thanh điệu — Hợp đồng thực thi chi tiết

- Ngày: 2026-09-17 · Loại: nâng cấp (bổ sung chi tiết cho hợp đồng gốc) · Service/app: `identity-service`, `chinese-backend`, `frontend/packages/{ui,auth}`, `frontend/apps/chinese`, `content/`
- Module: hồ sơ tài khoản, đồng bộ danh tính, quản trị vai trò cục bộ (F4) · pinyin, thanh điệu, sổ hoạt động học `study_events`, khung học liệu, TTS (F5)
- Hợp đồng gốc: [`2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md`](2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md) (gọi tắt **HĐG**). File này **thay thế** các mục "mức thiết kế" của F4/F5 trong HĐG (§5.1.3, §5.2.4 phần F4/F5, §5.3.3 phần F4/F5, §5.4 phần pinyin, §6.3 phần F4, §6.4, §7 F4/F5). Chỗ nào file này nói khác HĐG ⇒ **theo file này**. Chỗ nào không nói ⇒ theo HĐG.
- Người soạn: agent business-analysis (Opus). Người dùng giao chạy một mạch — mọi quyết định mở dùng mặc định BA, đánh dấu **[BA-mặc định]**, tổng hợp ở §10.2.
- Tiền đề: F2 (identity + `@af/auth`) đang code song song, F3 (JWT + phân quyền `access` + `/api/me` + trang 4xx) làm ngay sau. Thiết kế dưới đây dựa trên thiết kế F2/F3 của HĐG (§5.2.2, §5.2.3, §5.3.1, §5.3.2). Agent thực thi **phải đọc code F2/F3 thật** trước khi bắt đầu — tên lớp/hàm thật khác bảng dưới thì theo code thật, không đổi hành vi.
- **Thứ tự làm: F5 trước, F4 sau** (HĐG §8 — học pinyin sớm). Vì vậy các thành phần `@af/ui` mà HĐG giao cho F4 nhưng F5 cần (`AppDialog`, `AppDrawer`) được **chuyển sang F5** (§5.3.A).

> Khi context bị nén: đọc §3 (quy tắc), §7 (feature đang làm), rồi mục §5/§6 mà feature trỏ tới.

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

Code hiện có (kiểm 17/09/2026, commit `fbbe48a`): F0 (khung backend 19 project, `ChineseDbContext` rỗng, `Program.cs` có chỗ chờ "F3 chèn ở đây", Dockerfile chinese-backend build context `./backend` có sẵn ghi chú RK24) và F1 (`@af/tsconfig|ui|api`, `apps/chinese` chỉ có trang chủ). `@af/ui` **chưa có** `AppDialog`, `AppDrawer`, `useTabParam`, `speech`. `content/` **chưa tồn tại** (`.gitignore` gốc đã có `content/**/.raw/`, `content/node_modules/`). `AntFarm.Core/Errors` có `AppException`, `BusinessRuleException` (422, nhận `code`), `ConflictException`, `ForbiddenException`, `NotFoundException` — **chưa có lớp 503**.

### 1.2 Mục tiêu phần mềm

- **F5**: người học mở `/pinyin` thấy hướng dẫn tự soạn, bảng âm tiết bấm nghe được 4 thanh, làm bài luyện nghe-chọn thanh 20 câu, xem mình yếu thanh nào và hay nhầm thanh nào với thanh nào. Nền tảng có sổ hoạt động học `learning.study_events` (F7–F11 dùng lại), khung học liệu `content/` có kiểm tra tự động, tiện ích TTS dùng chung mọi ngôn ngữ.
- **F4**: người dùng đổi tên hiển thị, múi giờ, mật khẩu; chinese-backend thấy múi giờ mới **ngay** sau khi lưu (RK10); admin tiếng Trung tìm người dùng và gán/gỡ vai trò, không thể làm hệ thống mất admin cuối cùng.

### 1.3 Mục tiêu học tập (F5 — giai đoạn G0 "Âm & thanh")

Người học số 0, 15–20 phút/ngày trong 1–2 tuần, đi theo thứ tự:

| Bước | Nội dung | Vì sao ở vị trí này | Màn hình |
|---|---|---|---|
| 1 | **Bốn thanh + thanh nhẹ** — đường nét cao độ (1: cao bằng 55; 2: lên 35; 3: xuống-lên 214, trong lời nói thường chỉ trầm 21; 4: xuống mạnh 51; nhẹ: ngắn, nhẹ, theo thanh trước) và so sánh **gần đúng** với thanh tiếng Việt (thanh 1 ≈ thanh ngang nhưng cao hơn; thanh 2 ≈ thanh sắc; thanh 3 ≈ thanh hỏi/nặng tuỳ vị trí; thanh 4 ≈ đi từ cao xuống dứt khoát, không có trong tiếng Việt) | Thanh là thứ người Việt tưởng dễ (vì tiếng mẹ đẻ có thanh) nhưng hay nhầm nhất: **2 ↔ 3**, và đọc thanh 4 như thanh nặng. Học thanh trước để mọi âm tiết sau đều nghe kèm thanh | Tab Hướng dẫn |
| 2 | **Quy tắc đặt dấu** + cách đọc bảng pinyin (dạng số `ma3` ↔ dạng dấu `mǎ`) | Toàn bộ app ghi pinyin; người học phải đọc được cả hai dạng | Tab Hướng dẫn |
| 3 | **Thanh mẫu theo nhóm** (môi b p m f → đầu lưỡi d t n l → cuống lưỡi g k h → mặt lưỡi j q x → đầu lưỡi trước z c s → uốn lưỡi zh ch sh r) | Nhóm theo vị trí phát âm giúp so sánh cặp dễ nhầm: `z/c/s ↔ zh/ch/sh`, `j/q/x`, `b/p` (bật hơi), `zh` ≠ "tr" tiếng Việt | Hướng dẫn + Bảng (lọc theo nhóm) |
| 4 | **Vận mẫu theo nhóm** (đơn → kép → mũi `-n/-ng` → nhóm i/u/ü → đặc biệt `er`, `-i`), `ü` và quy tắc bỏ hai chấm sau j/q/x/y | Người Việt hay nhầm `-n ↔ -ng`, `ü ↔ u` | Hướng dẫn + Bảng |
| 5 | **Biến điệu** 3-3, 不, 一 — chỉ để **nhận biết khi nghe**; app vẫn lưu thanh gốc (R-C3) và chỉ hiện **gợi ý** cạnh pinyin | Gặp ngay từ bài chào hỏi (你好 `ni3 hao3` đọc ≈ `ní hǎo`); nếu không biết sẽ tưởng TTS/ghi chép sai | Hướng dẫn; tiện ích `sandhiHints` dùng lại ở F6/F9 |
| 6 | **Luyện nghe-chọn thanh** (`listen_tone`, một âm tiết) rồi **cặp thanh** (`tone_pair`, hai âm tiết) | Nghe phân biệt trước, nói sau (MVP không có nhận dạng giọng). Cặp thanh sát lời nói thật hơn | Tab Luyện |
| 7 | **Đo & tập trung chỗ yếu** — độ chính xác theo thanh (200 câu gần nhất mỗi thanh), ma trận nhầm, `recommendedFocus` ⇒ bài kế tiếp dồn 50% câu vào thanh yếu | Không đo thì người học không biết mình sai có hệ thống (vd luôn nghe 3 thành 2) | Tab Luyện (thẻ thống kê) |

**Tiêu chí "xong G0"** (hiển thị ở thẻ thống kê, không khoá gì): mỗi thanh 1–4 có ≥ 20 câu và độ chính xác ≥ 0,85 trên cửa sổ 200 câu gần nhất ⇒ hiện dòng "Bạn đã nghe thanh khá vững — có thể bắt đầu học từ vựng". **[BA-mặc định]**

---

## 2. Phạm vi

### 2.1 In-scope

**F5**
1. `content/` (dự án yarn riêng) + `content/chinese/{schemas,data/pinyin,scripts/validate.mjs,SOURCES.md,LICENSES/}` + 4 file JSON pinyin tự soạn/đối chiếu.
2. chinese-backend: migration `F5_ToneDrill` (schema `learning`: `study_events`, `tone_drill_sessions`, `tone_drill_answers`), `PinyinText` + test, nạp catalog pinyin từ file, 4 endpoint §6.4, `IStudyActivityRecorder`, `ServiceUnavailableException` (503) trong `AntFarm.Core`, Dockerfile/compose mang theo học liệu (RK24).
3. `@af/ui`: `AppDialog`, `AppDrawer`, `useTabParam`, `speech` (`listVoices`, `speak`, `useSpeech`); cập nhật comment luật `raw-dialog`.
4. `apps/chinese`: vitest + `src/lib/pinyin.ts` (số ↔ dấu, biến điệu gợi ý) ≥ 30 ca test; route `/pinyin?tab=huong-dan|bang|luyen`; bảng âm tiết; drawer âm tiết; bài luyện 2 chế độ; thẻ thống kê; hướng dẫn cài giọng.

**F4**
1. identity-service: rà/bổ sung `GET/PUT /api/account`; **đổi mật khẩu ở `POST /api/auth/password`** (lý do §4.3); refresh phải đọc lại hồ sơ từ DB.
2. chinese-backend: đồng bộ hồ sơ **ngay khi claim khác bản ghi** (bỏ qua cache 5 phút); API quản trị người dùng/vai trò (HĐG §6.3) hoàn chỉnh + `LAST_ADMIN` có khoá dòng + `isBootstrapAdmin`.
3. `@af/ui`: `ConfirmProvider/useConfirm`, `ToastProvider/useToast`, `TimeZoneAutocomplete`. `@af/auth`: `refreshSession()`, `changePassword` trỏ route mới.
4. `apps/chinese`: `/ho-so?tab=thong-tin|mat-khau`, `/quan-tri/nguoi-dung`, mục menu + menu người dùng.

### 2.2 Out-of-scope

- Nhận dạng giọng nói / chấm phát âm; file âm thanh người thật (chỉ TTS trình duyệt).
- Bài luyện thanh nhẹ đơn âm tiết (R-L1), luyện nghe phân biệt thanh mẫu/vận mẫu (`listen_initial`) — để sau MVP.
- Bảng pinyin phồn thể/chú âm (bopomofo).
- Thống kê theo cặp thanh (`byPair`) — MVP chỉ thống kê theo từng thanh. **[BA-mặc định]**
- Đổi email, xoá tài khoản, quên mật khẩu qua email, khoá/mở tài khoản, đặt lại mật khẩu hộ (F13).
- Bảng nhật ký kiểm toán thay đổi vai trò (chỉ log Serilog). **[BA-mặc định]**
- Tạo/sửa vai trò, sửa quyền của vai trò (danh mục hệ thống, seed).
- `learning.learner_settings` (F7) — tốc độ đọc TTS ở F5 lưu `localStorage`.

---

## 3. Quy tắc nghiệp vụ (đã chốt / mặc định BA)

Giữ nguyên toàn bộ R-* của HĐG §3. Bổ sung:

### 3.1 Pinyin & học liệu (F5)

- R5-1. **Khoá âm tiết** (`syllable`) = cách viết pinyin chuẩn, chữ thường, **không thanh**, `ü` viết `v` **chỉ khi chính tả chuẩn có hai chấm** (`nv`, `lv`, `nve`, `lve`); sau `j/q/x/y` chính tả chuẩn viết `u` ⇒ khoá là `ju`, `qu`, `xue`, `yuan` (vận mẫu vẫn ghi `v`, `ve`, `van`). Khoá khớp `^[a-z]+$`, `v` chỉ xuất hiện ở `nv|lv|nve|lve`. **[BA-mặc định]**
- R5-2. `syllables.json` chứa **mọi** âm tiết chuẩn của Hán ngữ pinyin phổ thông (≈ 400, không tính `r`, thán từ `m/n/ng/hm/ê`), kể cả âm tiết không có chữ minh hoạ (khi đó `tones: {}` — ô bảng hiện chữ nhưng không nghe được).
- R5-3. Chữ minh hoạ (`tones.<n>.hanzi`): **đúng một chữ Hán giản thể**, **đơn âm** (mọi mục của chữ đó trong CC-CEDICT, bỏ qua khác biệt hoa/thường, chỉ có đúng một cách đọc = âm tiết + thanh này), thông dụng, không phải 一/不 hay chữ có biến điệu. Không có chữ đạt ⇒ bỏ thanh đó. Khoá `tones` chỉ `"1".."4"` (không thanh nhẹ — R-L1).
- R5-4. `meaningVi` của chữ minh hoạ: 1–4 từ tiếng Việt do content-implement **tự viết** (nguồn `original`), không dịch máy hàng loạt ⇒ không cần nhãn `machine`. **[BA-mặc định]**
- R5-5. **Biến điệu chỉ là gợi ý hiển thị** (R-C3). Quy tắc gợi ý (hàm `sandhiHints`):
  - 3-3: âm tiết thanh 3 đứng **ngay trước** âm tiết thanh 3 ⇒ gợi ý "đọc gần thanh 2". Chuỗi ≥ 3 thanh 3 liên tiếp: gợi ý cho mọi âm tiết trừ âm tiết cuối, kèm lưu ý "tuỳ cách ngắt nhịp". Không xét ranh giới từ.
  - 不 (`bu4`) trước âm tiết thanh 4 ⇒ "đọc bú".
  - 一 (`yi1`) trước thanh 4 ⇒ "đọc yí"; trước thanh 1/2/3 ⇒ "đọc yì"; đứng cuối hoặc trước thanh nhẹ ⇒ không gợi ý. Không xử lý số thứ tự/đếm (一 trong 第一 vẫn là yī — vì đứng cuối nên tự đúng).
  - Nhận diện 不/一 bằng **chữ Hán** tại cùng vị trí, không bằng pinyin (`yi1` còn là 衣, 医...).
- R5-6. Hiển thị: pinyin dạng dấu qua `apps/chinese/src/lib/pinyin.ts`; `r5` gắn vào âm tiết trước khi hiển thị (`na3 r5` ⇒ `nǎr`). **[BA-mặc định]** Chữ Hán luôn bọc `LangText lang="zh-CN"`.

### 3.2 Luyện thanh (F5)

- R5-7. Hai chế độ:
  - `listen_tone`: mỗi câu phát **một** chữ minh hoạ; người học chọn thanh 1–4. Mỗi câu = 1 phần (`part`).
  - `tone_pair`: mỗi câu phát **hai** chữ minh hoạ liền nhau (hai âm tiết **khác nhau**); người học chọn thanh cho từng âm tiết. Mỗi câu = 2 phần. **Loại tổ hợp 3-3** (TTS sẽ biến điệu thành 2-3, gây chấm sai oan) ⇒ 15 tổ hợp. **[BA-mặc định]**
- R5-8. Mỗi phiên **20 câu** (không đổi được trong MVP). Sinh câu **ở client** từ catalog: nếu `recommendedFocus` khác rỗng ⇒ 10 câu có (ít nhất một phần mang) thanh thuộc `recommendedFocus`, 10 câu ngẫu nhiên đều; ngược lại ngẫu nhiên đều theo thanh (mỗi thanh 5 câu với `listen_tone`). Không lặp cùng chữ minh hoạ hai lần trong một phiên nếu còn lựa chọn khác. **[BA-mặc định]**
- R5-9. Câu được chấm **đúng** khi mọi phần đúng. Người học được **nghe lại** không giới hạn (đếm `replayCount`, không trừ điểm). Sau khi trả lời hiện ngay đúng/sai + nút nghe lại **thanh đúng** và **thanh đã chọn** (nếu âm tiết có chữ minh hoạ cho thanh đó) để so sánh. Câu đúng tự sang câu kế sau 1,2 giây; câu sai phải bấm "Tiếp". **[BA-mặc định]**
- R5-10. Phiên chỉ được **nộp một lần khi làm xong đủ 20 câu**; bỏ giữa chừng ⇒ không lưu gì. Nộp theo `clientSessionId` (uuid sinh bằng `crypto.randomUUID()` lúc bắt đầu phiên) ⇒ **idempotent**: nộp lại cùng id trả kết quả đã lưu (200), không nhân đôi, không ghi thêm `study_events`.
- R5-11. Kiểm thời gian (server, `TimeProvider`): `startedAt ≤ finishedAt`; `finishedAt ≤ now + 5 phút`; `finishedAt − startedAt ≤ 3 giờ`; `startedAt ≥ now − 24 giờ` (không cho nộp bù để giả streak). Vi phạm ⇒ 422 `INVALID_SESSION_TIME`. **[BA-mặc định]**
- R5-12. Nộp xong ghi **cùng transaction**: 1 `tone_drill_sessions`, N `tone_drill_answers` (một dòng mỗi **phần**), 1 `study_events(kind='tone_drill', quantity=số câu, correct=số câu đúng, ref_id=session.id, occurred_at=finishedAt, local_date=ngày của finishedAt theo access.users.time_zone lúc ghi)`.
- R5-13. **Thống kê** (`GET /api/pinyin/tone-stats`):
  - Cửa sổ: với mỗi thanh `t` ∈ 1..4, lấy **200 phần gần nhất** có `expected_tone = t` (sắp `answered_at DESC, part_index DESC`).
  - `byTone[t] = { total, correct, accuracy }` (accuracy `null` khi total = 0; luôn trả đủ 4 khoá).
  - `accuracy` tổng = Σcorrect / Σtotal trên hợp 4 cửa sổ (`null` nếu 0).
  - `confusions`: trong hợp 4 cửa sổ, nhóm các phần sai theo `(expected, answered)`, sắp `count DESC, expected ASC, answered ASC`, tối đa 5 dòng.
  - `recommendedFocus`: các thanh có `total ≥ 10` và `accuracy < 0,8`, sắp theo accuracy tăng dần (bằng nhau ⇒ số thanh tăng dần).
  - `totalAnswered`: tổng số phần mọi thời gian. `sessionsCount`: tổng số phiên. `lastSessionAt`: `finished_at` mới nhất hoặc `null`.
  - `g0Reached`: mỗi thanh total ≥ 20 và accuracy ≥ 0,85 (§1.3).

### 3.3 Hồ sơ (F4)

- R4-1. Tên hiển thị: trim, 1–100 ký tự, không toàn khoảng trắng (đã có `displayNameSchema` F2). Email **không** đổi được trong MVP.
- R4-2. Múi giờ: ID IANA hợp lệ theo `TimeZoneInfo.TryFindSystemTimeZoneById` ở identity; sai ⇒ 422 `INVALID_TIME_ZONE`. Lưu **nguyên chuỗi** người dùng chọn (không quy đổi bí danh ở server). Frontend quy bí danh cũ phổ biến về tên hiện hành trước khi gửi (`Asia/Saigon → Asia/Ho_Chi_Minh`, `Asia/Calcutta → Asia/Kolkata`, `Asia/Katmandu → Asia/Kathmandu`, `Asia/Rangoon → Asia/Yangon`, `Europe/Kiev → Europe/Kyiv`) vì một số bản Chrome trả tên CLDR cũ từ `Intl.supportedValuesOf`. **[BA-mặc định]**
- R4-3. Đổi múi giờ **không** viết lại lịch sử: `study_events.local_date` đã ghi giữ nguyên (R-T3).
- R4-4. Lưu hồ sơ xong ⇒ frontend gọi `refreshSession()` (refresh token ⇒ access token mới mang `name`/`zoneinfo` mới ⇒ `GET /chinese/api/me`) ⇒ chinese-backend thấy claim khác bản ghi ⇒ cập nhật `access.users` **ngay trong request đó**, không chờ cache 5 phút. Refresh ở identity **phải đọc lại tài khoản từ DB** để phát claim (không chép claim từ token cũ).
- R4-5. Đổi mật khẩu: cần mật khẩu hiện tại (sai ⇒ 422 `WRONG_PASSWORD`, **không** tính vào đếm khoá đăng nhập R-A8, nhưng chịu rate limit `auth`); mật khẩu mới theo R-A2 và **khác** mật khẩu hiện tại (trùng ⇒ 422 `PASSWORD_UNCHANGED`) **[BA-mặc định]**. Thành công ⇒ cập nhật `password_changed_at`, thu hồi mọi họ refresh token **khác** họ của cookie hiện tại (R-A12, `revoke_reason='password_changed'`); không có/không nhận ra cookie ⇒ thu hồi **tất cả** họ và báo `currentSessionKept=false` (frontend đăng xuất, về trang đăng nhập).

### 3.4 Quản trị vai trò (F4)

- R4-6. Chỉ người có `users.manage` xem danh sách người dùng của service tiếng Trung và gán/gỡ vai trò. Danh sách chỉ gồm người **đã từng vào** service tiếng Trung (`access.users`) — không liệt kê tài khoản identity chưa từng vào.
- R4-7. `PUT roles` là **thay toàn bộ** tập vai trò: mảng mã vai trò (bỏ trùng, không phân biệt hoa thường ⇒ chuẩn hoá lower), cho phép **rỗng** (người dùng mất mọi quyền ⇒ vào app thấy `/403`, R-P8). Mã lạ ⇒ 422 `UNKNOWN_ROLE` (`details.roles` = danh sách mã lạ). **[BA-mặc định cho phép rỗng]**
- R4-8. `LAST_ADMIN` (R-P7): nếu thao tác làm số người dùng có vai trò `admin` về 0 ⇒ 422 `LAST_ADMIN`. Kiểm **trong transaction** sau khi khoá dòng `access.roles WHERE code='admin' FOR UPDATE` (tuần tự hoá mọi thao tác đổi vai trò — hai admin tự gỡ nhau cùng lúc không lọt). Tự gỡ admin của chính mình được phép nếu còn admin khác.
- R4-9. Sau khi đổi: `PermissionResolver.Invalidate(userId)`; log Information `{ActorId} đổi vai trò {TargetId}: {Before} → {After}`.
- R4-10. Bootstrap admin (R-P6) — **diễn giải chốt [BA-mặc định]**: seeder lúc khởi động gán `admin` cho user tồn tại có email thuộc `ChineseAdmin:BootstrapEmails` **mỗi lần khởi động** nếu đang thiếu (idempotent, cấu hình là nguồn sự thật). Muốn gỡ vĩnh viễn ⇒ xoá email khỏi cấu hình. API trả `isBootstrapAdmin` để UI cảnh báo khi gỡ admin của người đó. *(Nếu F3 đã hiện thực "chỉ một lần" có đánh dấu thì giữ theo F3 và UI vẫn hiện cảnh báo — không chặn.)*
- R4-11. Nút ẩn theo quyền phải có lời giải thích: learner không thấy menu Quản trị; vào thẳng URL ⇒ `/403` (đã có ở F3).

---

## 4. Hiện trạng liên quan

### 4.1 File hiện có sẽ bị sửa

| File | Hiện trạng | Feature sửa |
|---|---|---|
| `backend/services/chinese-backend/src/AntFarm.Chinese.Api/Program.cs` | F3 sẽ thêm auth; chưa có Content | F5 (đăng ký `Content`, warm-up catalog) |
| `.../AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj` | chưa có `None Include` học liệu | F5 |
| `.../AntFarm.Chinese.Api/Dockerfile` + `deploy/docker-compose.yml` (khối `chinese-backend`) | context `../backend`, ghi chú RK24 | F5 |
| `.../AntFarm.Chinese.Api/appsettings.json` | chưa có `Content` | F5 |
| `.../Persistence/ChineseDbContext.cs`, `.../Common/Abstractions/IChineseDbContext.cs` | F3 thêm `access.*` | F5 thêm `learning.*` |
| `.../AntFarm.Chinese.Application/DependencyInjection.cs`, `.../Infrastructure/DependencyInjection.cs` | | F5, F4 |
| `.../Application/Access/UserProvisioningService.cs` (F3) | cache 5 phút theo `sub` | F4 |
| `.../Api/Features/Admin/UsersController.cs`, `RolesController.cs`, `Application/Access/UserAdminService.cs` (F3) | list + set roles | F4 hoàn thiện |
| `backend/shared/AntFarm.Core/Errors/` | chưa có 503 | F5 thêm `ServiceUnavailableException` |
| `backend/Directory.Packages.props` | chưa có `Microsoft.Extensions.TimeProvider.Testing` | F5 thêm (kiểm version thật trên nuget.org, dòng 10.x) |
| `backend/services/identity-service/src/AntFarm.Identity.Api/Features/Account/AccountController.cs`, `Features/Auth/AuthController.cs`, `Application/Accounts/AccountService.cs`, `AuthService.cs` (F2) | | F4 |
| `frontend/packages/ui/src/index.ts` | | F5, F4 |
| `frontend/packages/auth/src/*` (F2) | | F4 |
| `frontend/scripts/check-ui-conventions.mjs` | comment ghi `AppDialog` "thêm ở F4" | F5 sửa comment thành F5 |
| `frontend/apps/chinese/{package.json,vite.config.ts,src/router.tsx,src/layout/AppShell.tsx}` | | F5, F4 |
| `frontend/yarn.lock` | | F5 (vitest) |
| `README.md` | | F5 (mục học liệu + `yarn --cwd content`), F4 (không bắt buộc) |

### 4.2 Luồng dữ liệu F5

```
content/chinese/data/pinyin/*.json ──(csproj None Include, copy vào bin/publish)──► PinyinCatalogLoader (singleton, nạp lúc khởi động)
     │                                                                                    │ lỗi ⇒ log Error, IsAvailable=false (không ném)
     └── yarn --cwd content validate:chinese (cổng kiểm tra)                               ▼
apps/chinese /pinyin ──GET /chinese/api/pinyin/chart|guide──► PinyinController ──► IPinyinCatalog
                     ──(sinh 20 câu ở client, TTS speechSynthesis đọc chữ minh hoạ)
                     ──POST /chinese/api/pinyin/tone-drills──► ToneDrillService ─┬─► learning.tone_drill_sessions/answers
                                                                                   └─► IStudyActivityRecorder ─► learning.study_events
                     ──GET /chinese/api/pinyin/tone-stats──► ToneStatsService (SQL cửa sổ 200/thanh) ─► ToneStatsCalculator
```

### 4.3 Phát hiện khi khảo sát F4 (ảnh hưởng thiết kế F2)

- HĐG §6.2 đặt đổi mật khẩu ở `POST /api/account/password`, nhưng cookie refresh `af_rt` có `Path=/api/auth` (prod) / `/identity/api/auth` (dev) ⇒ **trình duyệt không gửi cookie tới `/api/account/*`** ⇒ identity **không biết họ token hiện tại** để giữ lại khi thực hiện R-A12. **Chốt [BA-mặc định]: đổi mật khẩu chuyển sang `POST /api/auth/password`** (Bearer + cookie). Nếu F2 đã làm `/api/account/password` ⇒ F4 đổi route (không giữ bí danh — chưa có client dùng) và sửa `createIdentityClient.changePassword`. Endpoint mới tự nằm trong phạm vi kiểm `Origin` (R-A7b) và rate limit `auth`.
- `PUT /api/account` dùng Bearer trong header (không dựa cookie) ⇒ không cần kiểm `Origin`; CORS đã cho phép `PUT` (R-A7b).
- `access.users` không có cột nào mới cho F4 — **F4 không có migration** ở chinese-backend lẫn identity.

---

## 5. Thiết kế giải pháp

### 5.1 Database

#### 5.1.1 F5 — `af_chinese`, schema `learning`, migration `F5_ToneDrill` (thay HĐG §5.1.3)

Quy ước: snake_case tự động (`UseSnakeCaseNamingConvention`), PK `uuid` sinh ở ứng dụng bằng `Guid.CreateVersion7()`, mọi `timestamptz` gán `DateTime` `Kind=Utc`, ngày lịch `date` ↔ `DateOnly`. Cấu hình EF đặt ở `Persistence/Configurations/Learning/`, `builder.ToTable("...", "learning")`.

```sql
-- learning.study_events — SỔ HOẠT ĐỘNG HỌC DÙNG CHUNG (F5 ghi tone_drill; F7 srs_review; F8 writing; F9 quiz_submit, lesson_complete; F11 đọc)
id           uuid         PRIMARY KEY
user_id      uuid         NOT NULL REFERENCES access.users(id) ON DELETE CASCADE
kind         varchar(32)  NOT NULL          -- KHÔNG đặt CHECK: feature sau thêm kind không phải sửa constraint; kiểm ở code (StudyEventKinds)
occurred_at  timestamptz  NOT NULL          -- mốc UTC của hoạt động (F5: finishedAt)
local_date   date         NOT NULL          -- ngày lịch theo access.users.time_zone TẠI LÚC GHI (R-T3)
quantity     integer      NOT NULL DEFAULT 1  CHECK (quantity >= 0)
correct      integer      NULL               CHECK (correct IS NULL OR (correct >= 0 AND correct <= quantity))
ref_id       uuid         NULL               -- F5: tone_drill_sessions.id (không FK — trỏ nhiều bảng)
created_at   timestamptz  NOT NULL
INDEX ix_study_events_user_id_local_date      (user_id, local_date)
INDEX ix_study_events_user_id_kind_occurred_at (user_id, kind, occurred_at DESC)

-- learning.tone_drill_sessions
id                 uuid         PRIMARY KEY
user_id            uuid         NOT NULL REFERENCES access.users(id) ON DELETE CASCADE
client_session_id  uuid         NOT NULL
mode               varchar(16)  NOT NULL CHECK (mode IN ('listen_tone','tone_pair'))
started_at         timestamptz  NOT NULL
finished_at        timestamptz  NOT NULL CHECK (finished_at >= started_at)
total              smallint     NOT NULL CHECK (total BETWEEN 1 AND 100)      -- số CÂU
correct            smallint     NOT NULL CHECK (correct BETWEEN 0 AND total)   -- số câu đúng hết mọi phần
created_at         timestamptz  NOT NULL
UNIQUE ux_tone_drill_sessions_user_id_client_session_id (user_id, client_session_id)
INDEX  ix_tone_drill_sessions_user_id_finished_at        (user_id, finished_at DESC)

-- learning.tone_drill_answers — MỘT DÒNG MỖI PHẦN (listen_tone: 1 phần/câu; tone_pair: 2 phần/câu)
id              uuid        PRIMARY KEY
session_id      uuid        NOT NULL REFERENCES learning.tone_drill_sessions(id) ON DELETE CASCADE
user_id         uuid        NOT NULL            -- phi chuẩn hoá để thống kê không cần join; không FK (session đã CASCADE)
item_index      smallint    NOT NULL CHECK (item_index >= 0)        -- thứ tự câu trong phiên (0..)
part_index      smallint    NOT NULL CHECK (part_index IN (0,1))    -- vị trí âm tiết trong câu
syllable        varchar(8)  NOT NULL            -- khoá R5-1: 'ma', 'nv', 'zhuang'
hanzi           varchar(4)  NOT NULL            -- chữ minh hoạ đã phát (để xem lại câu sai)
expected_tone   smallint    NOT NULL CHECK (expected_tone BETWEEN 1 AND 4)
answered_tone   smallint    NOT NULL CHECK (answered_tone BETWEEN 1 AND 4)
is_correct      boolean     NOT NULL            -- = expected_tone = answered_tone (lưu để SQL đếm gọn)
response_ms     integer     NULL CHECK (response_ms IS NULL OR response_ms BETWEEN 0 AND 600000)  -- của CÂU, chép cho mọi phần
replay_count    smallint    NOT NULL DEFAULT 0 CHECK (replay_count BETWEEN 0 AND 100)          -- của CÂU
answered_at     timestamptz NOT NULL            -- = session.finished_at (thứ tự cửa sổ thống kê)
UNIQUE ux_tone_drill_answers_session_item_part (session_id, item_index, part_index)
INDEX  ix_tone_drill_answers_user_expected_answered_at (user_id, expected_tone, answered_at DESC, part_index DESC)
```

- Tên chỉ mục/ràng buộc ở trên là **gợi ý**; EF sinh tên khác cũng được, miễn đủ cột/thứ tự. CHECK khai bằng `ToTable(t => t.HasCheckConstraint(...))`.
- FK chéo schema `learning → access` là bình thường (cùng DB, cùng service).
- Migration sinh bằng lệnh CLAUDE.md, tên **`F5_ToneDrill`**, cùng commit với entity. Chạy thử trên DB dev đã có `F3_Access` (có dữ liệu) và DB trống.
- Không seed gì. Pinyin chart **không** vào DB.

#### 5.1.2 F4 — không đổi schema

Không migration ở `af_identity` lẫn `af_chinese`. Truy vấn danh sách người dùng dùng chỉ mục `lower(email)` sẵn có của F3; tìm theo tên dùng `ILIKE` quét tuần tự (quy mô MVP vài chục người dùng — chấp nhận). **[BA-mặc định]**

### 5.2 Backend

#### 5.2.1 F5 — chinese-backend

**Domain (`AntFarm.Chinese.Domain`)**

| File | Nội dung |
|---|---|
| `Learning/StudyEvent.cs` | Entity; factory `StudyEvent.Create(userId, kind, occurredAtUtc, localDate, quantity, correct, refId, nowUtc)` — ném `ArgumentException` nếu `occurredAtUtc.Kind != Utc`, `quantity < 0`, `correct > quantity` |
| `Learning/StudyEventKinds.cs` | hằng `ToneDrill = "tone_drill"`, `SrsReview = "srs_review"`, `Writing = "writing"`, `QuizSubmit = "quiz_submit"`, `LessonComplete = "lesson_complete"` + `IsKnown(string)` |
| `Time/UserLocalDate.cs` | `static DateOnly From(DateTime utc, string timeZoneId)` — `TimeZoneInfo.FindSystemTimeZoneById`; ID không tìm được ⇒ dùng `Asia/Ho_Chi_Minh` (hằng `DefaultTimeZoneId`) — không ném. `static (DateTime FromUtc, DateTime ToUtcExclusive) DayRange(DateOnly, string tz)` cho F7/F11 (R-T4) |
| `Pinyin/ToneDrillMode.cs` | enum `ListenTone`, `TonePair` (JSON snake_case: `listen_tone`, `tone_pair` — converter đã đăng ký ở Program.cs); EF lưu chuỗi snake_case (`HasConversion` tường minh) |
| `Pinyin/ToneDrillSession.cs` | Entity + danh sách `Answers`; factory `Create(...)` tự tính `Total`, `Correct` từ câu |
| `Pinyin/ToneDrillAnswer.cs` | Entity; `IsCorrect` tính trong factory |
| `Pinyin/PinyinText.cs` | Tiện ích thuần (dùng ở F5 kiểm dữ liệu, F6 tra từ): `string? NormalizeNumbered(string input)` (trim, gộp khoảng trắng, `ü`/`u:`/`U:`/`Ü` ⇒ `v`, mỗi token khớp `^[A-Za-z]+[1-5]$`, giữ hoa chữ đầu, trả `null` nếu sai); `string? FromToneMarks(string marked)` (dấu ⇒ số, không dấu ⇒ `5`, tách theo khoảng trắng hoặc `'`); `string ToSearchKey(string numbered)` (bỏ số + khoảng trắng, lower: `Ni3 hao3` ⇒ `nihao`); `bool TryParseSyllable(string token, out string syllable, out int tone)`; `string ToMarked(string numbered)` (đặt dấu theo quy tắc §5.3.C) |
| `Pinyin/PinyinSyllable.cs` | record `(string Key, string Initial, string Final)` + `static bool IsValidKey(string)` theo R5-1 |

**Application (`AntFarm.Chinese.Application`)**

| File | Nội dung |
|---|---|
| `Common/Abstractions/IChineseDbContext.cs` | thêm `DbSet<StudyEvent> StudyEvents`, `DbSet<ToneDrillSession> ToneDrillSessions`, `DbSet<ToneDrillAnswer> ToneDrillAnswers`; thêm `Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken)` **nếu F3 chưa có** (Application tham chiếu `Microsoft.EntityFrameworkCore` là chấp nhận được — theo cách F3 đã làm) |
| `Learning/IStudyActivityRecorder.cs` + `StudyActivityRecorder.cs` | `Task<StudyEvent> RecordAsync(Guid userId, string kind, DateTime occurredAtUtc, int quantity, int? correct, Guid? refId, CancellationToken ct)` — đọc `access.users.time_zone` (không có user ⇒ `NotFoundException`), tính `local_date` qua `UserLocalDate.From`, `Add` vào DbContext **không** gọi `SaveChanges` (người gọi lưu trong transaction của mình). `kind` không thuộc `StudyEventKinds` ⇒ `ArgumentException` |
| `Pinyin/IPinyinCatalog.cs` | `bool IsAvailable`; `string Version` (SHA-256 16 hex đầu của nội dung 4 file — làm ETag); `PinyinChartDto Chart`; `PinyinGuideDto Guide`; `bool TryGetToneExample(string syllable, int tone, out ToneExampleDto ex)`; `bool ContainsSyllable(string)`. Truy cập `Chart`/`Guide` khi `!IsAvailable` ⇒ ném `ServiceUnavailableException("CONTENT_UNAVAILABLE", "Học liệu pinyin chưa sẵn sàng...")` |
| `Pinyin/Dtos/*.cs` | record khớp §6.4 (chart, guide, submit request/response, stats) |
| `Pinyin/SubmitToneDrillRequestValidator.cs` | FluentValidation (lỗi ⇒ 400 `VALIDATION`): `clientSessionId` ≠ empty; `mode` hợp lệ; `items` 1..100; mỗi item `parts` đúng 1 phần (`listen_tone`) hoặc 2 phần (`tone_pair`); `syllable` khớp `^[a-z]{1,6}$`; `hanzi` 1 ký tự CJK (`\p{IsCJKUnifiedIdeographs}`); `expectedTone`, `answeredTone` 1..4; `responseMs` null hoặc 0..600000; `replayCount` 0..100; `tone_pair` hai phần khác `syllable` và không cùng là thanh 3 |
| `Pinyin/ToneDrillService.cs` | `SubmitAsync(Guid userId, SubmitToneDrillRequest req, CancellationToken)` → `(SubmitToneDrillResponse Body, bool Created)`. Trình tự: (1) catalog khả dụng, (2) tìm phiên cũ theo `(userId, clientSessionId)` ⇒ có thì trả kết quả dựng từ DB, `Created=false`; (3) mỗi phần: `ContainsSyllable` sai ⇒ 422 `UNKNOWN_SYLLABLE` (`details.itemIndex`, `details.syllable`); `TryGetToneExample(syllable, expectedTone)` sai **hoặc** `hanzi` khác chữ trong catalog ⇒ 422 `TONE_NOT_AVAILABLE`; (4) R5-11 ⇒ 422 `INVALID_SESSION_TIME` (`details.reason`: `order|future|too_long|too_old`); (5) transaction: thêm session + answers (`answered_at = finishedAt`) + `RecordAsync(ToneDrill, finishedAt, total, correct, session.Id)`, `SaveChanges`, commit. Trùng khoá `ux_..._client_session_id` do hai request song song (`PostgresException.SqlState == "23505"`) ⇒ rollback, đọc lại phiên đã có, trả `Created=false` |
| `Pinyin/ToneStatsCalculator.cs` | Hàm **thuần** `Calculate(IReadOnlyList<ToneAnswerRow> windowRows, long totalAnswered, int sessionsCount, DateTime? lastSessionAt)` áp R5-13 — unit test được không cần DB |
| `Pinyin/ToneStatsService.cs` | Một truy vấn cửa sổ (raw SQL qua `Database.SqlQuery<ToneAnswerRow>` hoặc LINQ 4 lần `Where(expected==t).OrderByDescending(answered_at).ThenByDescending(part_index).Take(200)` — chọn cách nào cũng được, ưu tiên LINQ cho dễ đọc) + đếm tổng, rồi gọi `ToneStatsCalculator` |
| `DependencyInjection.cs` | đăng ký 3 service scoped + validator |

SQL tham khảo cho cửa sổ (nếu chọn raw SQL):

```sql
SELECT expected_tone, answered_tone, is_correct
FROM (
  SELECT expected_tone, answered_tone, is_correct,
         row_number() OVER (PARTITION BY expected_tone ORDER BY answered_at DESC, part_index DESC) AS rn
  FROM learning.tone_drill_answers WHERE user_id = @userId
) x WHERE rn <= 200;
```

**Shared (`AntFarm.Core/Errors/ServiceUnavailableException.cs`)**: `sealed class ServiceUnavailableException(string code, string message, object? details = null) : AppException` — `StatusCode => 503`. Thêm 1 unit test ở `AntFarm.Shared.UnitTests/Security/ErrorResponseMapperTests.cs` (503 + code).

**Infrastructure (`AntFarm.Chinese.Infrastructure`)**

| File | Nội dung |
|---|---|
| `Content/ContentOptions.cs` | `SectionName = "Content"`; `RootPath` (mặc định `"content/chinese"`). Đường dẫn tương đối ⇒ ghép với `AppContext.BaseDirectory`; tuyệt đối ⇒ dùng nguyên |
| `Content/PinyinCatalogLoader.cs` | `PinyinCatalog Load(string rootPath)` — đọc `data/pinyin/{initials,finals,syllables,guide}.json` bằng `System.Text.Json` (camelCase, `ReadCommentHandling = Skip`), kiểm tối thiểu lúc chạy: bọc `{dataset, version, items}` đúng `dataset`; khoá âm tiết hợp lệ + không trùng; `initial`/`final` của âm tiết có trong initials/finals (initial rỗng `""` = không thanh mẫu); khoá `tones` ∈ 1..4. **Mọi lỗi (thiếu file, JSON hỏng, vi phạm) ⇒ log Error kèm tên file + lý do, trả catalog `IsAvailable=false` — KHÔNG ném.** Thành công ⇒ log Information số thanh mẫu/vận mẫu/âm tiết/số cặp (âm tiết, thanh) có chữ minh hoạ |
| `Content/PinyinCatalog.cs` | `sealed class : IPinyinCatalog`, bất biến, tra `Dictionary<string, SyllableDto>` |
| `Persistence/Configurations/Learning/StudyEventConfiguration.cs`, `ToneDrillSessionConfiguration.cs`, `ToneDrillAnswerConfiguration.cs` | theo §5.1.1 |
| `Persistence/Migrations/<ts>_F5_ToneDrill.cs` | sinh bằng `dotnet ef` |
| `DependencyInjection.cs` | `services.Configure<ContentOptions>(...)`; `services.AddSingleton<IPinyinCatalog>(sp => PinyinCatalogLoader.Load(...))` — nạp **lười ở lần resolve đầu**; Program.cs resolve ngay sau `Build()` để nạp lúc khởi động |

**Api (`AntFarm.Chinese.Api`)**

- `Features/Pinyin/PinyinController.cs` — `[ApiController] [Route("api/pinyin")] [RequirePermission(PermissionCodes.StudyUse)]` (dùng hằng của F3; **không** khai `new string Policy`):
  - `GET chart`, `GET guide`: đặt `ETag: "<catalog.Version>"` + `Cache-Control: private, max-age=3600`; `If-None-Match` khớp ⇒ 304.
  - `POST tone-drills`: `Created=true` ⇒ `201` (header `Location: /api/pinyin/tone-drills/{id}` — không cần endpoint GET tương ứng; **[BA-mặc định]** bỏ header `Location`, trả `StatusCode(201, body)`); `Created=false` ⇒ `200` cùng body.
  - `GET tone-stats`.
  - `userId` lấy bằng `User.GetAccountId()` (F2 `ClaimsPrincipalExtensions`).
- `Program.cs`: sau `var app = builder.Build();` thêm `app.Services.GetRequiredService<IPinyinCatalog>();` (nạp sớm, không ném) — đặt **sau** khối AutoMigrate/seeder.
- `appsettings.json`: `"Content": { "RootPath": "content/chinese" }`.
- `AntFarm.Chinese.Api.csproj`:

```xml
<ItemGroup Label="Học liệu tiếng Trung (F5) — copy vào bin/publish, backend đọc qua Content:RootPath">
  <None Include="..\..\..\..\..\content\chinese\data\**\*.json"
        LinkBase="content\chinese\data"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

  Từ `backend/services/chinese-backend/src/AntFarm.Chinese.Api/` lên **5 cấp** là gốc repo. Kiểm sau build: `ls backend/services/chinese-backend/src/AntFarm.Chinese.Api/bin/Debug/net10.0/content/chinese/data/pinyin/` có đủ 4 file.

**Docker (RK24) — dùng `additional_contexts`, giữ context `./backend`** **[BA-mặc định]** (không đổi context sang gốc repo vì sẽ phải viết lại mọi `COPY` và `.dockerignore`):

- `deploy/docker-compose.yml`, khối `chinese-backend.build`:

```yaml
    build:
      context: ../backend
      dockerfile: services/chinese-backend/src/AntFarm.Chinese.Api/Dockerfile
      additional_contexts:
        content: ../content        # F5: học liệu content/chinese/data (RK24) — cần Docker Compose ≥ 2.17
```

- `Dockerfile` (stage build, **trước** `dotnet publish`, sau `COPY services/chinese-backend/src/ ...`): đường link `..\..\..\..\..\content` tính từ `/src/services/chinese-backend/src/AntFarm.Chinese.Api` là `/content` ⇒

```dockerfile
# F5: học liệu từ build context phụ "content" (compose additional_contexts; build tay: --build-context content=../content).
# Chỉ copy data/ — không kéo schemas/scripts/.raw vào ảnh.
COPY --from=content chinese/data/ /content/chinese/data/
```

  Sửa comment ⚠️ đầu file thành mô tả cách làm mới. Lệnh build tay ghi vào `deploy/VERIFY-DOCKER.md`: `docker build -f backend/services/chinese-backend/src/AntFarm.Chinese.Api/Dockerfile --build-context content=content backend`. Thêm mục kiểm: `docker compose exec chinese-backend ls /app/content/chinese/data/pinyin` có 4 file; `GET /chinese/api/pinyin/chart` ≠ 503. **Ghi rõ "CHƯA VERIFY".**
- Tạo `content/.dockerignore`? Không cần (chỉ `COPY` thư mục `chinese/data/`), nhưng `node_modules` của `content/` vẫn bị gửi vào build context phụ ⇒ **thêm `content/.dockerignore`** gồm `node_modules`, `**/.raw`. **[BA-mặc định]**

**Test F5 (backend)**

| Dự án | Test bắt buộc |
|---|---|
| `AntFarm.Chinese.UnitTests/Pinyin/PinyinTextTests.cs` | ≥ 25 ca: `NormalizeNumbered` (`"ni3  hao3"`, `"lü4"`/`"lu:4"`/`"LÜ4"` ⇒ `lv4`/`Lv4`, `"Bei3 jing1"` giữ hoa, `"ma"` ⇒ null, `"ma6"` ⇒ null, `"ma0"` ⇒ null, chuỗi rỗng ⇒ null); `FromToneMarks` (`"nǐ hǎo"`, `"lǜ"` ⇒ `lv4`, `"ma"` ⇒ `ma5`, `"Xī'ān"` ⇒ `Xi1 an1`, `"nǚ"`); `ToSearchKey`; `ToMarked` (đủ các ca §5.3.C); `PinyinSyllable.IsValidKey` (`ju` đúng, `jv` sai, `nv` đúng, `zhuang` đúng, `Ma` sai) |
| `.../Pinyin/PinyinCatalogLoaderTests.cs` | thư mục tạm: bộ file hợp lệ tối thiểu ⇒ `IsAvailable`; thiếu `guide.json` ⇒ `!IsAvailable`, không ném; JSON hỏng ⇒ không ném; khoá trùng ⇒ không khả dụng; truy cập `Chart` khi không khả dụng ⇒ `ServiceUnavailableException` code `CONTENT_UNAVAILABLE`. Thêm 1 test nạp **học liệu thật của repo** (tìm gốc repo bằng cách đi lên từ `AppContext.BaseDirectory` tới khi thấy `content/chinese`) ⇒ khả dụng, ≥ 380 âm tiết, ≥ 300 cặp (âm tiết, thanh) có chữ minh hoạ |
| `.../Pinyin/ToneStatsCalculatorTests.cs` | không dữ liệu ⇒ 4 khoá `byTone` total 0, accuracy null, focus rỗng; thanh 9 câu accuracy 0,5 ⇒ **không** vào focus; 10 câu 0,7 ⇒ vào; hai thanh cùng yếu ⇒ sắp đúng; confusions tối đa 5 và sắp đúng; `g0Reached` biên 20 câu/0,85 |
| `.../Pinyin/SubmitToneDrillRequestValidatorTests.cs` | 0 câu, 101 câu, `tone_pair` 1 phần, tone 5, `tone_pair` 3-3, hai phần cùng syllable ⇒ lỗi; hợp lệ ⇒ qua |
| `.../Learning/UserLocalDateTests.cs` | `2026-09-16T23:30Z` + `Asia/Ho_Chi_Minh` ⇒ `2026-09-17`; `2026-09-16T16:59Z` ⇒ `2026-09-16`; `17:00Z` ⇒ `2026-09-17`; tz rác ⇒ dùng mặc định không ném; `DayRange` trả `Kind=Utc` và nửa hở |
| `AntFarm.Chinese.ApiTests/Pinyin/PinyinApiTests.cs` (`[DbFact]`, token `TestTokenFactory`, `FakeTimeProvider` thay `TimeProvider` trong factory, `Content__RootPath` đặt **tuyệt đối** tới `content/chinese` của repo) | không token ⇒ 401; user bị gỡ hết vai trò ⇒ 403 `FORBIDDEN`; learner `GET chart` 200 có `syllables`, gọi lại với `If-None-Match` ⇒ 304; `POST` 20 câu hợp lệ ⇒ 201, `total=20`; **nộp lại cùng `clientSessionId` ⇒ 200, DB vẫn 1 session, 1 study_event**; syllable `xx` ⇒ 422 `UNKNOWN_SYLLABLE`; thanh không có chữ minh hoạ ⇒ 422 `TONE_NOT_AVAILABLE`; `finishedAt` > now+5 phút ⇒ 422 `INVALID_SESSION_TIME`; **đồng hồ giả `2026-09-16T23:35Z`, `finishedAt=2026-09-16T23:30Z`, user `Asia/Ho_Chi_Minh` ⇒ `study_events.local_date = 2026-09-17`** (06:30 sáng VN — tiêu chí HĐG); `tone-stats` sau 2 phiên khớp số đếm; `Content__RootPath` trỏ thư mục rỗng ⇒ `chart` 503 `CONTENT_UNAVAILABLE`, `/health/live` vẫn 200 |

Gói test mới: `Microsoft.Extensions.TimeProvider.Testing` (khai `Directory.Packages.props`, version 10.x có thật trên nuget.org — backend-implement kiểm và ghi version thật).

#### 5.2.2 F4 — identity-service

Rà code F2 trước; chỉ sửa chỗ thiếu/lệch.

| Việc | Chi tiết |
|---|---|
| `GET /api/account` | Bearer (audience `af-identity`), trả `account` §6.2 HĐG. Tài khoản `is_active=false` ⇒ 403 `ACCOUNT_DISABLED` |
| `PUT /api/account` | `UpdateProfileRequest { displayName, timeZone }` — validator: displayName trim 1–100, không rỗng; timeZone 1–64 ký tự. `TimeZoneInfo.TryFindSystemTimeZoneById` sai ⇒ 422 `INVALID_TIME_ZONE`. Lưu trim; `updated_at = now`. Trả 200 `account`. Không đổi token (frontend tự refresh) |
| `POST /api/auth/password` | **Route mới thay `/api/account/password`** (§4.3). `[Authorize]` (Bearer) + đọc cookie `af_rt` (không bắt buộc). Validator: `currentPassword` không rỗng; `newPassword` 8–128. Kiểm: mật khẩu hiện tại sai ⇒ 422 `WRONG_PASSWORD` (không tăng `failed_login_count`); `VerifyHashedPassword(newPassword)` khớp hash hiện tại ⇒ 422 `PASSWORD_UNCHANGED`. Thành công (một transaction): băm mới, `password_changed_at=now`; xác định `currentFamilyId` = family của token cookie nếu cookie hợp lệ, active và **thuộc chính tài khoản trong Bearer**; thu hồi mọi token active của tài khoản có `family_id ≠ currentFamilyId` (`revoke_reason='password_changed'`). Trả 200 `{ "otherSessionsRevoked": <số họ bị thu hồi>, "currentSessionKept": <bool> }`. Filter kiểm `Origin` + rate limit `auth` áp như mọi `POST /api/auth/*` |
| Refresh đọc lại hồ sơ | `AuthService.RefreshAsync` phát access token từ **bản ghi `accounts` vừa đọc** (`display_name`, `time_zone`, `email`). Nếu F2 đã làm vậy thì chỉ thêm test |
| Test (ApiTests `[DbFact]`) | PUT tên + múi giờ ⇒ 200; refresh ngay ⇒ access token mới có `name`, `zoneinfo` mới; `timeZone="Mars/Olympus"` ⇒ 422 `INVALID_TIME_ZONE`; `displayName="   "` ⇒ 400; đổi mật khẩu đúng với cookie phiên A, đang có phiên B ⇒ 200 `currentSessionKept=true`, refresh bằng cookie B ⇒ 401, bằng cookie A ⇒ 200; đổi mật khẩu **không** cookie ⇒ `currentSessionKept=false`, mọi phiên bị thu hồi; sai mật khẩu hiện tại ⇒ 422 `WRONG_PASSWORD` và `failed_login_count` không đổi; trùng mật khẩu cũ ⇒ 422 `PASSWORD_UNCHANGED`; `POST /api/auth/password` với `Origin` lạ ⇒ 403 `ORIGIN_NOT_ALLOWED`; đăng nhập bằng mật khẩu mới OK, cũ ⇒ 401 |

#### 5.2.3 F4 — chinese-backend

| Việc | Chi tiết |
|---|---|
| Đồng bộ hồ sơ ngay (R4-4, RK10) | `UserProvisioningService.EnsureAsync(principal)`: cache bộ nhớ theo `sub` lưu **ảnh chụp** `(Email, DisplayName, TimeZone, CheckedAt)`. Quy tắc: (a) không có trong cache ⇒ đọc DB, tạo/đồng bộ, ghi cache; (b) claim `email`/`name`/`zoneinfo` **khác** ảnh chụp ⇒ đồng bộ DB **ngay** (bỏ qua 5 phút), ghi cache; (c) giống và `now − CheckedAt < 5 phút` ⇒ bỏ qua; (d) giống và ≥ 5 phút ⇒ cập nhật `last_seen_at`, ghi cache. `zoneinfo` không hợp lệ (`TryFindSystemTimeZoneById` sai) ⇒ giữ múi giờ cũ, log Warning, **không** chặn request. Claim `zoneinfo` rỗng ⇒ giữ cũ. Đồng bộ đồng thời hai request: dùng `ExecuteUpdateAsync` (không đọc-sửa-ghi), không cần khoá |
| `GET /api/admin/users` | `users.manage`. Query `q` (trim, ≤ 100; so khớp `lower(email) LIKE %q%` hoặc `display_name ILIKE %q%` — escape `%`/`_`), `page` ≥ 1, `pageSize` 1..100 (mặc định 20). Sắp `last_seen_at DESC, id`. Mỗi item thêm `isBootstrapAdmin` (email ∈ `ChineseAdmin:BootstrapEmails`, so lower-trim). Tham số sai ⇒ 400 `VALIDATION` |
| `GET /api/admin/users/{id}` | `users.manage` — **[BA-mặc định]** thêm để dialog tải lại bản mới nhất; 404 `NOT_FOUND` |
| `PUT /api/admin/users/{id}/roles` | `users.manage`. `UserAdminService.SetRolesAsync(actorId, targetId, roles)`: chuẩn hoá (trim, lower, distinct); mã lạ ⇒ 422 `UNKNOWN_ROLE` `details.roles`; transaction: `SELECT ... FROM access.roles WHERE code='admin' FOR UPDATE` (raw SQL `FromSql`/`ExecuteSql`); target không có ⇒ 404; tính `adminCountAfter` = số user có admin trừ target (nếu target mất admin) — `0` ⇒ 422 `LAST_ADMIN`; xoá/thêm `user_roles` chênh lệch (`assigned_at = now`), commit; `PermissionResolver.Invalidate(targetId)`; log R4-9. Trả 200 item như danh sách. Tập mới = tập cũ ⇒ 200, không ghi |
| `GET /api/admin/roles` | `users.manage` — `[{ code, name, description, permissions: [code] }]`, sắp `admin`, `learner`. `description` tiếng Việt: admin "Toàn quyền: học, soạn nội dung, quản lý người dùng", learner "Dùng các chức năng học" — **[BA-mặc định]** nếu F3 chưa có cột `description` trong `access.roles` thì lấy từ hằng trong code (`RoleCodes.Describe`), **không** thêm migration |
| Test | UnitTests: `SetRolesAsync` gỡ admin cuối ⇒ `LAST_ADMIN`; tự gỡ khi còn admin khác ⇒ OK; mã lạ; rỗng OK; provisioning bỏ qua cache khi `zoneinfo` khác (dùng `FakeTimeProvider`, chưa qua 5 phút). ApiTests `[DbFact]`: token tz `Asia/Ho_Chi_Minh` gọi `/api/me` ⇒ rồi token tz `Europe/Berlin` ngay sau ⇒ `/api/me.timeZone = Europe/Berlin`; learner `GET /api/admin/users` ⇒ 403; admin duy nhất tự gỡ admin ⇒ 422 `LAST_ADMIN`; có 2 admin ⇒ gỡ một ⇒ 200 và người bị gỡ gọi `/api/admin/ping` ⇒ 403 **ngay** (cache quyền đã xoá); `q` theo một phần email ⇒ đúng 1 kết quả; `pageSize=101` ⇒ 400; `roles=["superman"]` ⇒ 422 `UNKNOWN_ROLE`; id lạ ⇒ 404 |

### 5.3 Frontend (agent `frontend-implement`, model **Fable**)

#### 5.3.A `@af/ui` — thêm ở F5

| File | API |
|---|---|
| `src/components/dialog/AppDialog.tsx` | `AppDialogProps extends Omit<DialogProps, 'onClose' \| 'title'> { open; onClose: () => void; title?: ReactNode; actions?: ReactNode; closeOnBackdrop?: boolean /* mặc định false */; hideCloseButton?: boolean; fullScreenBelow?: 'sm' \| 'md' \| false /* mặc định 'sm' */ }`. Bọc `Dialog` MUI: `onClose` MUI lọc `reason === 'backdropClick' \| 'escapeKeyDown'` khi `!closeOnBackdrop`; nút X ở tiêu đề (aria-label "Đóng"); `DialogTitle`/`DialogContent`/`DialogActions` |
| `src/components/dialog/AppDrawer.tsx` | `AppDrawerProps { open; onClose; title?; anchor?: 'right' \| 'bottom' \| 'left' \| 'responsive' /* mặc định 'responsive' = bottom ở xs–sm, right ở md+ */; width?: number /* 420 */; closeOnBackdrop?: boolean; children; actions? }`. Bottom: bo góc trên, `maxHeight: '85dvh'`, nội dung cuộn, `pb: env(safe-area-inset-bottom)` |
| `src/hooks/useTabParam.ts` | `useTabParam<T extends string>(allowed: readonly T[], defaultValue: T, key = 'tab'): [T, (v: T) => void]` — đọc `useSearchParams`; giá trị lạ ⇒ `defaultValue`; set dùng `setSearchParams(prev => …, { replace: true })`, **giữ** các tham số khác; set về default ⇒ xoá khoá khỏi URL |
| `src/speech/speech.ts` | `isSpeechSupported(): boolean` (`'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window`); `listVoices(langPrefix: string, timeoutMs = 1500): Promise<SpeechSynthesisVoice[]>` — lấy `getVoices()`, rỗng thì chờ sự kiện `voiceschanged` tới hết timeout; lọc `voice.lang.replace('_','-').toLowerCase().startsWith(langPrefix.toLowerCase())`; sắp: `lang` khớp đúng (vd `zh-cn`) trước, rồi tên chứa `Natural`/`Online`, rồi `localService`; `pickVoice(voices, preferredUri?)`; `speak(text, { lang, rate = 1, pitch = 1, voice? }): Promise<void>` — `speechSynthesis.cancel()` trước, resolve ở `onend`, **cũng resolve** ở `onerror` với `error` = `canceled`/`interrupted`, reject lỗi khác; `cancelSpeech()` |
| `src/speech/useSpeech.ts` | `useSpeech(langPrefix: string, opts?: { storageKey?: string })` → `{ status: 'loading' \| 'ready' \| 'unsupported' \| 'no-voice'; voices; voice; setVoiceUri(uri); speak(text, { rate? }): Promise<void>; cancel(); speaking: boolean }`. Giọng người dùng chọn lưu `localStorage['af.speech.voice.' + langPrefix]` (try/catch). Huỷ đọc khi unmount. Ghi chú trong code: iOS Safari chỉ phát khi `speak` được gọi **trong** handler thao tác người dùng — không gọi trong `useEffect` |
| `src/index.ts` | export tất cả ở trên (+ kiểu) |

`frontend/scripts/check-ui-conventions.mjs`: sửa comment "(thêm ở F4)" ⇒ "(có từ F5)". Không đổi luật.

#### 5.3.B `apps/chinese` — vitest (F5)

- `package.json`: `devDependencies.vitest` = **`^5.0.1`** (npm 17/09/2026: peer `vite ^6.4 \|\| ^7 \|\| ^8` — khớp vite 8.3.0 đang dùng; agent kiểm lại khi cài); script `"test": "vitest run"`, `"test:watch": "vitest"`.
- Cấu hình: thêm khối `test` vào `vite.config.ts` (`/// <reference types="vitest/config" />`, `test: { environment: 'node', include: ['src/**/*.test.ts'] }`). Chỉ test hàm thuần — **không** cần jsdom/testing-library ở F5. **[BA-mặc định]**
- `tsconfig.app.json` đang `include: ["src"]` ⇒ file test cũng được `tsc -b` kiểm (tốt); nếu kiểu `vitest` không thấy ⇒ import tường minh `import { describe, it, expect } from 'vitest'` (không dùng globals).
- Chạy: `yarn workspace @af/chinese test`. Turbo: thêm task `test` (`cache: false`) vào `frontend/turbo.json` và script `"test": "turbo test"` ở `frontend/package.json`. Dockerfile frontend **không** chạy test.

#### 5.3.C `apps/chinese/src/lib/pinyin.ts` (F5) — nguồn sự thật hiển thị

```ts
export type Tone = 1 | 2 | 3 | 4 | 5
export interface ParsedSyllable { letters: string; tone: Tone; capitalized: boolean } // letters: chữ thường, ü = 'v'
export function normalizeNumbered(input: string): string | null      // như PinyinText.NormalizeNumbered (C#)
export function parseSyllable(token: string): ParsedSyllable | null   // 'Lv4' → { letters: 'lv', tone: 4, capitalized: true }
export function syllableToMarked(token: string): string                // 'lve4' → 'lüè'; token sai → trả nguyên văn
export function numberedToMarked(pinyin: string, opts?: { join?: boolean }): string
//   'ni3 hao3' → 'nǐ hǎo'; 'na3 r5' → 'nǎr' (R5-6); join: 'Xi1 an1' → "Xī'ān" (dấu nháy khi âm tiết sau bắt đầu bằng a/o/e)
export function markedToNumbered(marked: string): string | null        // 'nǐ hǎo' → 'ni3 hao3'; 'ma' → 'ma5'; "Xī'ān" → 'Xi1 an1'
export function toneOf(token: string): Tone | null
export function stripTone(token: string): string                       // 'lv4' → 'lv'
export function displaySyllableKey(key: string): string                // 'nv' → 'nü', 'lve' → 'lüe', 'ju' → 'ju'
export interface SandhiHint { index: number; kind: 'third_tone' | 'bu' | 'yi'; suggestedTone: 2 | 4; text: string }
export function sandhiHints(pinyin: string, hanzi?: string): SandhiHint[] // R5-5; hanzi dùng nhận diện 不/一 (so theo vị trí ký tự ↔ âm tiết, bỏ qua r5)
export const TONE_MARKS: Record<'a'|'e'|'i'|'o'|'u'|'v', [string, string, string, string]>
```

Quy tắc đặt dấu (kiểm bằng test): có `a` ⇒ trên `a`; không `a` mà có `e` ⇒ trên `e`; có `ou` ⇒ trên `o`; còn lại ⇒ nguyên âm **cuối** (`gui4 → guì`, `liu2 → liú`, `huo3 → huǒ`); `v` luôn hiển thị `ü` (`lv4 → lǜ`, `nv3 → nǚ`, `lve4 → lüè`); thanh 5 không dấu nhưng vẫn đổi `v → ü` (`lv5 → lü`); chữ hoa giữ (`Ou1 zhou1 → Ōu zhōu`, `A1 → Ā`); `er2 → ér`, `r5 → r`; `m`/`n`/`ng` không có nguyên âm ⇒ trả không dấu (không ném).

`pinyin.test.ts` **≥ 30 ca**, tối thiểu gồm: `lve4→lüè`, `gui4→guì`, `liu2→liú`, `er2→ér`, `r5→r`, `ni3 hao3→nǐ hǎo`, `na3 r5→nǎr`, `zhuang4→zhuàng`, `xue2→xué`, `jiong3→jiǒng`, `ou1→ōu`, `Ou1 zhou1`, `lv5→lü`, `nv3→nǚ`, `A1→Ā`, `Xi1 an1` join ⇒ `Xī'ān`, token sai giữ nguyên, `normalizeNumbered('lü4')`, `normalizeNumbered('lu:4')`, `normalizeNumbered('ni3  hao3 ')`, `normalizeNumbered('ma')===null`, `normalizeNumbered('ma6')===null`, `markedToNumbered('nǐ hǎo')`, `markedToNumbered('lǜ')==='lv4'`, `markedToNumbered('ma')==='ma5'`, round-trip 10 âm tiết, `sandhiHints('ni3 hao3')` ⇒ index 0 thanh 2, `sandhiHints('wo3 hen3 hao3')` ⇒ index 0 và 1, `sandhiHints('bu4 shi4','不是')` ⇒ bú, `sandhiHints('bu4 hao3','不好')` ⇒ rỗng, `sandhiHints('yi1 ge4','一个')` ⇒ yí, `sandhiHints('yi1 tian1','一天')` ⇒ yì, `sandhiHints('yi1 fu5','衣服')` ⇒ rỗng (không phải 一), `sandhiHints('tong3 yi1','统一')` ⇒ rỗng.

`src/features/pinyin/drill/generateDrill.ts` + `generateDrill.test.ts`: `generateDrill({ mode, chart, focus, count = 20, random = Math.random }): DrillItem[]` (R5-7, R5-8; nhận `random` để test tất định). Test: đủ 20 câu; `listen_tone` không focus ⇒ mỗi thanh 5 câu; focus `[2]` ⇒ ≥ 10 câu thanh 2; `tone_pair` không có cặp 3-3, hai âm tiết khác nhau; mọi phần đều có chữ minh hoạ trong chart; không lặp chữ khi còn lựa chọn.

#### 5.3.D `apps/chinese` — màn hình F5

Cây file:

```
src/lib/pinyin.ts  pinyin.test.ts
src/components/Hanzi.tsx              # <LangText lang="zh-CN"> + size prop
src/components/Pinyin.tsx             # <Pinyin value="ni3 hao3" hanzi? showSandhi?> — hiển thị dạng dấu + gợi ý biến điệu (Typography caption)
src/features/pinyin/api.ts            # getChart, getGuide, submitToneDrill, getToneStats (chineseApi)
src/features/pinyin/hooks.ts          # usePinyinChart (staleTime Infinity), usePinyinGuide, useToneStats, useSubmitToneDrill (invalidate tone-stats)
src/features/pinyin/types.ts          # khớp §6.4
src/features/pinyin/useTtsRate.ts     # tốc độ đọc localStorage 'af.chinese.ttsRate' (mặc định 0.8; 0.5–1.2)
src/features/pinyin/pages/PinyinPage.tsx
src/features/pinyin/components/VoiceMissingAlert.tsx
src/features/pinyin/components/VoiceSettings.tsx     # chọn giọng + tốc độ (AppDialog)
src/features/pinyin/components/GuideView.tsx
src/features/pinyin/components/PinyinChart.tsx
src/features/pinyin/components/SyllableDrawer.tsx
src/features/pinyin/components/ToneStatsCard.tsx
src/features/pinyin/components/drill/DrillSetup.tsx
src/features/pinyin/components/drill/DrillRunner.tsx
src/features/pinyin/components/drill/ToneButtons.tsx
src/features/pinyin/components/drill/DrillResult.tsx
src/features/pinyin/drill/generateDrill.ts  generateDrill.test.ts
```

Route & menu:
- `router.tsx`: `/pinyin` là con của `AppShell`, bọc `RequireAuth` + `RequirePermission permission="study.use"` (theo cách F3 bọc route học).
- `AppShell.tsx`: thêm `{ label: 'Pinyin', to: '/pinyin', icon: <RecordVoiceOverOutlinedIcon />, requiredPermission: 'study.use' }` sau Trang chủ.
- Tab cấp trang: `useTabParam(['huong-dan','bang','luyen'] as const, 'huong-dan')` **[BA-mặc định: mặc định Hướng dẫn cho người số 0]**. `Tabs` `variant="fullWidth"` ở xs.

`PinyinPage`:
- Đầu trang: tiêu đề "Pinyin & thanh điệu", nút biểu tượng "Cài đặt giọng đọc" mở `VoiceSettings`.
- `useSpeech('zh')` dùng chung cho cả trang (truyền xuống). `status='unsupported'` hoặc `'no-voice'` ⇒ `VoiceMissingAlert` (severity `warning`) luôn hiện ở đầu cả ba tab; nút nghe bị vô hiệu kèm tooltip "Chưa có giọng tiếng Trung".
- `VoiceMissingAlert` nội dung (tiếng Việt, rút gọn theo hệ điều hành đoán từ `navigator.userAgent`, có nút "Xem cho hệ điều hành khác" mở rộng):
  - Windows 10/11: Cài đặt → Thời gian và ngôn ngữ → Ngôn ngữ và vùng → Thêm ngôn ngữ → "中文(中华人民共和国)" → tích **Chuyển văn bản thành giọng nói** → khởi động lại trình duyệt.
  - macOS: Cài đặt hệ thống → Trợ năng → Nội dung được đọc → Giọng hệ thống → Quản lý giọng → Tiếng Trung (Trung Quốc đại lục), vd Tingting.
  - iPhone/iPad: Cài đặt → Trợ năng → Nội dung được đọc → Giọng nói → Tiếng Trung.
  - Android: Cài đặt → Hệ thống → Ngôn ngữ → Đầu ra chuyển văn bản sang lời nói → Dịch vụ của Google → cài dữ liệu giọng Tiếng Trung.
  - Mẹo: **Microsoft Edge** có sẵn giọng tiếng Trung trực tuyến chất lượng cao; Firefox trên Linux thường không có giọng.
  - `unsupported`: "Trình duyệt này không hỗ trợ đọc văn bản. Hãy dùng Chrome, Edge hoặc Safari bản mới."

Tab **Hướng dẫn** (`GuideView`): danh sách chủ đề từ `guide.items` dạng `Accordion` (mở sẵn chủ đề đầu); block `paragraph` ⇒ `Typography`; `examples` ⇒ danh sách dòng (Hanzi lớn, `Pinyin`, nghĩa, nút nghe); `tone_contour` ⇒ ô SVG nhỏ vẽ đường nét 5 mức (thanh 1–4 theo số 55/35/214/51) — **[BA-mặc định]** vẽ bằng SVG `polyline`, không thư viện; `compare` ⇒ bảng 2 cột (cặp dễ nhầm) có nút nghe từng bên. Cuối tab: nút "Sang bảng âm tiết".

Tab **Bảng** (`PinyinChart`):
- Hàng = vận mẫu (theo `finals` thứ tự file, có tiêu đề nhóm), cột = thanh mẫu (cột đầu "Ø" = không thanh mẫu, rồi theo `initials`). Ô có âm tiết ⇒ nút hiển thị `displaySyllableKey`; ô trống ⇒ để trống. Âm tiết `tones` rỗng ⇒ chữ màu `text.disabled`, vẫn bấm được (drawer báo "Chưa có chữ minh hoạ đọc đúng cho âm tiết này").
- Chip lọc nhóm thanh mẫu (`Tất cả` + 6 nhóm) và nhóm vận mẫu (`Tất cả` + nhóm) — lọc lên URL `?tab=bang&tm=<nhom>&vm=<nhom>` (replace). **[BA-mặc định]** mặc định mobile (<600px) `tm=moi` để bảng hẹp; desktop `Tất cả`.
- Vùng bảng: `Box sx={{ overflow: 'auto', maxHeight: 'calc(100dvh - 220px)' }}` — **chỉ vùng này cuộn ngang**, trang không cuộn ngang ở 375px; hàng tiêu đề và cột đầu `position: sticky`; ô ≥ 44×40 px.
- Bấm ô ⇒ `SyllableDrawer` (`AppDrawer anchor="responsive"`, **`closeOnBackdrop` — hộp chỉ đọc, đóng nhanh; ghi comment lý do**): tiêu đề âm tiết; dòng thanh mẫu + vận mẫu kèm `noteVi`; 4 dòng thanh (1–4): số thanh, pinyin dạng dấu, `Hanzi`, nghĩa, nút nghe; thanh không có chữ ⇒ dòng mờ "không có chữ minh hoạ". Nút "Nghe lần lượt" phát các thanh có chữ cách nhau 600 ms (dừng khi đóng drawer).

Tab **Luyện**:
- Trạng thái `setup` (`DrillSetup` + `ToneStatsCard`): chọn chế độ (`ToggleButtonGroup`: "Một âm tiết" / "Cặp thanh"), dòng "Bài này sẽ tập trung vào thanh 2, 3" khi có focus, nút **Bắt đầu (20 câu)**. Chế độ lưu URL `?tab=luyen&che-do=mot|cap` (replace).
- `ToneStatsCard`: 4 thanh dạng `LinearProgress` + `a/b câu`, accuracy `null` ⇒ "chưa có dữ liệu"; nhầm lẫn diễn đạt: "Bạn hay nghe **thanh 2** thành **thanh 3** (14 lần)" (tối đa 3 dòng); `g0Reached` ⇒ `Alert success` §1.3; chưa có phiên ⇒ "Làm bài đầu tiên để xem bạn yếu thanh nào". Lỗi 503 ⇒ `Alert` "Học liệu pinyin chưa sẵn sàng — báo quản trị viên" (không điều hướng).
- Trạng thái `running` (`DrillRunner`): thanh tiến độ `n/20`; nút lớn "Nghe" (tự phát khi sang câu mới **chỉ nếu** câu trước được chuyển bằng thao tác người dùng — tuân iOS); `ToneButtons` 4 nút lớn (≥ 64px cao, nhãn "1 ˉ", "2 ˊ", "3 ˇ", "4 ˋ"), `tone_pair` hai hàng nút (âm tiết 1, âm tiết 2) — chấm khi đủ cả hai; phím tắt: `1–4` chọn (với cặp: gõ lần lượt hai số), `Space` nghe lại, `Enter` sang câu kế. Sau khi trả lời: hiện đúng/sai, `Hanzi` + pinyin dạng dấu + nghĩa, nút "Nghe thanh đúng" / "Nghe thanh bạn chọn" (R5-9). Đo `responseMs` từ lúc phát xong lần đầu tới lúc chọn đủ.
- Rời trang/tab khi đang làm: `useBlocker` (react-router) + `AppDialog` xác nhận "Bỏ bài đang làm? Kết quả sẽ không được lưu." (F5 chưa có `useConfirm`). Đổi tab trong trang cũng hỏi.
- Làm xong câu 20 ⇒ `useSubmitToneDrill` (gửi `clientSessionId` sinh lúc bắt đầu bằng `crypto.randomUUID()`) ⇒ `DrillResult`: điểm `x/20`, theo thanh, danh sách câu sai (nghe lại), nút "Làm bài mới" và "Xem thống kê". Gửi lỗi (mạng/5xx) ⇒ `Alert error` + nút "Gửi lại" (cùng `clientSessionId`); kết quả hiển thị tạm từ client trong lúc chờ. 422 ⇒ hiện thông điệp server, nút "Làm bài mới".
- 375px: không cuộn ngang; nút trả lời nằm trong tầm ngón cái (dưới nửa màn hình); `BottomNavigation` không che nút (đã có padding của `AppLayout`).

#### 5.3.E `@af/ui` + `@af/auth` — thêm ở F4

| File | API |
|---|---|
| `packages/ui/src/feedback/ConfirmProvider.tsx` | `ConfirmProvider` + `useConfirm(): (opts: { title; message: ReactNode; confirmText?; cancelText?; tone?: 'default' \| 'danger' }) => Promise<boolean>` — dùng `AppDialog` |
| `packages/ui/src/feedback/ToastProvider.tsx` | `ToastProvider` + `useToast(): { success(msg); error(msg); info(msg) }` — một `Snackbar` + `Alert`, 4 giây, hàng đợi đơn giản; ở xs đặt `anchorOrigin` trên-giữa để không đè bottom nav |
| `packages/ui/src/inputs/AppAutocomplete.tsx` | Bọc `Autocomplete` với `label`, `helperText`, `error`, `textFieldProps` — `renderInput` **trải `params.slotProps` trước** rồi mới ghi đè slot con (quy tắc CLAUDE.md); đây là mẫu chuẩn cho mọi Autocomplete sau |
| `packages/ui/src/inputs/TimeZoneAutocomplete.tsx` | `TimeZoneAutocomplete({ value, onChange, label = 'Múi giờ', error?, helperText? })` dựa trên `AppAutocomplete`. Danh sách = `Intl.supportedValuesOf('timeZone')` (thiếu hàm ⇒ danh sách dự phòng ~30 múi giờ phổ biến, có `Asia/Ho_Chi_Minh`) → quy bí danh R4-2 → hợp với `value` hiện tại → bỏ trùng. Nhãn `(UTC+07:00) Asia/Ho_Chi_Minh` (offset lấy từ `Intl.DateTimeFormat('en-US', { timeZone, timeZoneName: 'longOffset' })`, `GMT` ⇒ `UTC+00:00`), sắp theo offset rồi tên. `filterOptions`: so khớp không phân biệt hoa thường, coi `_` như khoảng trắng (gõ "Ho_Chi", "ho chi", "saigon"→ không bắt buộc) |
| `packages/ui/src/index.ts` | export thêm |
| `packages/auth` | `useAuth()` thêm `refreshSession(): Promise<void>` (refresh token ⇒ `getAccount` ⇒ `reloadMe`, cập nhật `account`); `createIdentityClient.changePassword` gọi `POST /auth/password`, trả `{ otherSessionsRevoked, currentSessionKept }` |

App: `App.tsx` bọc `ConfirmProvider` + `ToastProvider` (trong `ThemeProvider`, ngoài `RouterProvider`). Nếu `useConfirm`/`ToastProvider` cần `AppDialog` ⇒ đã có từ F5.

#### 5.3.F `apps/chinese` — màn hình F4

```
src/features/profile/pages/ProfilePage.tsx          # /ho-so?tab=thong-tin|mat-khau (F7 thêm hoc-tap)
src/features/profile/components/ProfileForm.tsx
src/features/profile/components/ChangePasswordForm.tsx
src/features/admin-users/api.ts  hooks.ts  types.ts
src/features/admin-users/pages/AdminUsersPage.tsx   # /quan-tri/nguoi-dung
src/features/admin-users/components/UserRolesDialog.tsx
src/features/admin-users/components/UserList.tsx     # bảng (md+) / thẻ (xs–sm)
```

- **`/ho-so`** (`RequireAuth`, không cần quyền riêng — người 0 quyền vẫn đã bị `RequireAuth` đưa tới `/403`): `useTabParam(['thong-tin','mat-khau'], 'thong-tin')`.
  - *Thông tin*: email (chỉ đọc, kèm dòng "Không đổi được email"), tên hiển thị, `TimeZoneAutocomplete`; nếu múi giờ trình duyệt ≠ giá trị đang chọn ⇒ `Alert info` "Trình duyệt của bạn đang ở múi giờ X" + nút "Dùng múi giờ này". Dòng giải thích: "Múi giờ quyết định lúc nào sang ngày học mới (chuỗi ngày học, thẻ đến hạn). Đổi múi giờ không làm thay đổi lịch sử đã ghi." RHF + zod (`displayNameSchema` của `@af/utils`, timeZone bắt buộc). Nút Lưu vô hiệu khi form không đổi. Lưu: `updateAccount` ⇒ `refreshSession()` ⇒ toast "Đã lưu hồ sơ". 422 `INVALID_TIME_ZONE` ⇒ lỗi dưới ô múi giờ; lỗi khác ⇒ `Alert` tại chỗ (`parseApiError`).
  - *Mật khẩu*: mật khẩu hiện tại, mới, nhập lại (khớp — zod `refine`), nút hiện/ẩn mật khẩu, `autoComplete` đúng (`current-password`, `new-password`). 422 `WRONG_PASSWORD` ⇒ lỗi ô hiện tại; `PASSWORD_UNCHANGED` ⇒ lỗi ô mới. Thành công: `currentSessionKept=true` ⇒ reset form + toast "Đã đổi mật khẩu. Các thiết bị khác đã bị đăng xuất."; `false` ⇒ `logout()` + chuyển `/dang-nhap?reason=password-changed` (trang đăng nhập hiện thông báo tương ứng nếu `@af/auth` hỗ trợ `reason`; chưa hỗ trợ thì thêm).
- **Menu người dùng** (`userMenu` của F2): thêm mục "Hồ sơ" → `/ho-so` trước "Đăng xuất".
- **`/quan-tri/nguoi-dung`** (`RequirePermission permission="users.manage"`); menu `{ label: 'Người dùng', to: '/quan-tri/nguoi-dung', icon: <ManageAccountsOutlinedIcon />, requiredPermission: 'users.manage' }` (nằm cuối, ở mobile sẽ vào "Thêm" khi quá 5 mục).
  - Ô tìm `?q=` (debounce 300 ms, `replace`), `?page=`; `useQuery(['admin-users', q, page])` giữ dữ liệu cũ khi chuyển trang (`placeholderData: keepPreviousData`).
  - Mỗi người: tên, email, chip vai trò (`admin` màu primary), "Lần cuối: <thời gian tương đối tiếng Việt>", nhãn "(bạn)" nếu là chính mình, nhãn "Admin theo cấu hình" nếu `isBootstrapAdmin`. Nút "Đổi vai trò". Rỗng ⇒ "Không tìm thấy người dùng nào". Phân trang `Pagination` (mobile `size="small"`).
  - `UserRolesDialog` (`AppDialog`, **không** `closeOnBackdrop` — có thao tác ghi): checkbox mỗi vai trò (tên + mô tả + danh sách quyền dạng chip nhỏ). Cảnh báo động: bỏ hết vai trò ⇒ `Alert warning` "Người này sẽ không vào được ứng dụng tiếng Trung"; tự gỡ `admin` ⇒ `Alert warning` "Bạn sẽ mất quyền quản trị ngay sau khi lưu"; gỡ `admin` của người `isBootstrapAdmin` ⇒ `Alert info` "Email này nằm trong cấu hình quản trị — khởi động lại dịch vụ sẽ gán lại quyền admin". Lưu ⇒ nếu tự gỡ admin thì `useConfirm` (tone danger) trước. 422 `LAST_ADMIN` ⇒ `Alert error` **trong dialog** "Không thể gỡ quản trị viên cuối cùng", dialog không đóng. Thành công ⇒ đóng, toast, invalidate `admin-users`; nếu sửa chính mình ⇒ `reloadMe()`, mất `users.manage` ⇒ `navigate('/')`.
- Không có nút nào ẩn theo quyền trong hai màn này ngoài mục menu (R4-11).

### 5.4 Học liệu (F5 — agent `content-implement`, Sonnet)

#### 5.4.1 Cây thư mục

```
content/
  package.json          # { "name": "antfarm-content", "private": true, "type": "module",
                        #   "scripts": { "validate:chinese": "node chinese/scripts/validate.mjs" },
                        #   "devDependencies": { "ajv": "^8.17.1", "ajv-formats": "^3.0.1" },   ← kiểm version thật khi cài
                        #   "packageManager": "yarn@1.22.22", "engines": { "node": ">=22.12" } }
  yarn.lock             # COMMIT
  .dockerignore         # node_modules  **/.raw   (§5.2.1 Docker)
  README.md             # quy ước chung: mỗi ngôn ngữ một thư mục, bản quyền trước tiên, cách chạy validate, không import JSON từ frontend
  chinese/
    SOURCES.md
    LICENSES/README.md  # F5 chưa phân phối lại dữ liệu có giấy phép bên ngoài ⇒ chỉ ghi chú; F6 thêm CC-BY-SA-4.0.txt, Unicode-3.0.txt
    schemas/
      pinyin-initials.schema.json
      pinyin-finals.schema.json
      pinyin-syllables.schema.json
      pinyin-guide.schema.json
    data/pinyin/
      initials.json  finals.json  syllables.json  guide.json
    scripts/
      validate.mjs
      lib/cedict.mjs    # đọc .raw/cedict_ts.u8 (nếu có) ⇒ Map<giản thể, Set<pinyin số lower>>
    .raw/               # gitignore — cedict_ts.u8 tải từ MDBG (CC BY-SA 4.0), chỉ để KIỂM TRA, không phân phối
```

`content/` **không** thuộc workspace `frontend/`. Lệnh: `yarn --cwd content install` rồi `yarn --cwd content validate:chinese` (từ gốc repo).

#### 5.4.2 Định dạng JSON (bọc chung) **[BA-mặc định: bọc `{dataset, version, items}` thay mảng trần của HĐG §5.4.2, thống nhất với `hsk-words.json`]**

```json
{ "dataset": "pinyin-initials", "version": "2026-09-17", "items": [ ... ] }
```

`dataset` ∈ `pinyin-initials | pinyin-finals | pinyin-syllables | pinyin-guide` (khớp tên file); `version` dạng `YYYY-MM-DD`.

**`initials.json`** — 21 thanh mẫu + mục không thanh mẫu (`code: ""`):

```json
{ "code": "zh", "group": "uon-luoi", "display": "zh", "ipa": "ʈʂ", "aspirated": false,
  "noteVi": "Uốn đầu lưỡi lên, không bật hơi. Đừng đọc thành 'tr' tiếng Việt.",
  "examples": [{ "pinyin": "zhong1", "hanzi": "中" }] }
```

`group` ∈ `khong | moi | dau-luoi | cuong-luoi | mat-luoi | dau-luoi-truoc | uon-luoi`; thứ tự trong file = thứ tự cột bảng: `"" b p m f d t n l g k h j q x zh ch sh r z c s`. `y`, `w` **không** là thanh mẫu (chỉ là chính tả) — ghi trong `guide.json`. `examples[].pinyin` là pinyin số hợp lệ, `hanzi` đơn âm theo R5-3 (được kiểm như chữ minh hoạ).

**`finals.json`**:

```json
{ "code": "ian", "group": "i", "display": "ian", "standaloneSpelling": "yan", "noteVi": "Đọc gần 'iên', không phải 'ian'." }
```

`code` dùng `v` cho ü (`v`, `ve`, `van`, `vn`); `display` dùng `ü`; `group` ∈ `don | kep | mui | i | u | v | dac-biet`; `standaloneSpelling` = cách viết khi không có thanh mẫu (`yan`, `wu`, `yu`...) hoặc `null` nếu không tự đứng (vd `-i` sau zh/z). Vận mẫu `-i` (sau z/c/s/zh/ch/sh/r) dùng `code: "-i"`, `display: "-i"`. Thứ tự file = thứ tự hàng bảng.

**`syllables.json`**:

```json
{ "syllable": "nv", "initial": "n", "final": "v",
  "tones": { "3": { "hanzi": "女", "meaningVi": "nữ, con gái" } } }
```

Ví dụ khác: `{ "syllable": "ju", "initial": "j", "final": "v", "tones": {...} }`, `{ "syllable": "yan", "initial": "", "final": "ian", ... }`, `{ "syllable": "zhi", "initial": "zh", "final": "-i", ... }`, `{ "syllable": "er", "initial": "", "final": "er", ... }`.

**`guide.json`**:

```json
{ "id": "bon-thanh", "title": "Bốn thanh điệu và thanh nhẹ", "order": 1,
  "blocks": [
    { "type": "paragraph", "text": "..." },
    { "type": "tone_contour", "tones": [1, 2, 3, 4] },
    { "type": "examples", "items": [{ "pinyin": "ma1", "hanzi": "妈", "meaningVi": "mẹ" }] },
    { "type": "compare", "title": "Thanh 2 và thanh 3", "pairs": [
        { "left":  { "pinyin": "ma2", "hanzi": "麻", "meaningVi": "tê" },
          "right": { "pinyin": "ma3", "hanzi": "马", "meaningVi": "ngựa" }, "noteVi": "..." } ] },
    { "type": "tip", "text": "..." }
  ] }
```

`type` ∈ `paragraph | tone_contour | examples | compare | tip`. Mọi `hanzi` trong `examples`/`compare` dùng để nghe ⇒ đơn âm và khớp `pinyin` (kiểm bằng CEDICT như R5-3). **Được phép** ví dụ nhiều âm tiết (`"pinyin": "ni3 hao3", "hanzi": "你好"`) trong chủ đề biến điệu — khi đó kiểm từng chữ khớp một cách đọc trong CEDICT (không đòi đơn âm) và TTS sẽ đọc theo biến điệu tự nhiên (đúng mục đích minh hoạ).

Chủ đề bắt buộc (theo thứ tự §1.3, `id` cố định): `bon-thanh`, `dat-dau`, `thanh-mau`, `van-mau`, `u-hai-cham` (ü và j/q/x/y, y/w), `cap-de-nham` (z/c/s–zh/ch/sh, j/q/x, b/p–d/t–g/k bật hơi, -n/-ng, 2↔3), `bien-dieu` (3-3, 不, 一 — ghi rõ "app lưu thanh gốc, chỉ gợi ý"), `cach-luyen` (cách dùng tab Luyện, 15–20 phút/ngày). Văn phong: tiếng Việt có dấu, câu ngắn, mọi so sánh với tiếng Việt ghi rõ **"gần đúng"**. **Tự soạn** (nguồn `original`), không chép giáo trình.

#### 5.4.3 Schema (JSON Schema 2020-12, `additionalProperties: false` mọi cấp)

Mỗi schema kiểm khung bọc (`dataset` const, `version` pattern `^\d{4}-\d{2}-\d{2}$`, `items` minItems ≥ 1) + trường mục. Pattern: pinyin số `^[A-Za-z]+[1-5]( [A-Za-z]+[1-5])*$`; khoá âm tiết `^[a-z]{1,6}$`; khoá `tones` `propertyNames: { enum: ["1","2","3","4"] }`; `hanzi` minLength 1 maxLength 8; `meaningVi` 1..60 ký tự.

#### 5.4.4 `validate.mjs` — kiểm tra (FAIL ⇒ exit 1; WARN ⇒ in ra, exit 0)

| # | Kiểm | Mức |
|---|---|---|
| 1 | 4 file tồn tại, parse được, đúng schema (ajv + ajv-formats, `allErrors`) | FAIL |
| 2 | `initials.code` duy nhất, đủ 22 mục (gồm `""`); `finals.code` duy nhất | FAIL |
| 3 | `syllable` duy nhất; khớp R5-1 (`v` chỉ ở `nv lv nve lve`; `j/q/x/y` + vận mẫu `v*` ⇒ khoá viết `u`); `initial` có trong initials, `final` có trong finals; số âm tiết **≥ 380** | FAIL |
| 4 | Ghép `initial + final` theo quy tắc chính tả ra đúng `syllable` (hàm `spell(initial, final)` trong script: xử lý `y/w`, `iu/ui/un` rút gọn, ü sau j/q/x/y, `-i`) | FAIL |
| 5 | Chữ minh hoạ: đúng 1 ký tự CJK; không thuộc danh sách cấm `一 不 了 的 着 地 得 和 行 长 重 还 为 都 要 好 啊 吧 呢 吗`; một chữ không làm minh hoạ cho hai cặp (âm tiết, thanh) khác nhau | FAIL |
| 6 | Có `.raw/cedict_ts.u8`: mỗi chữ minh hoạ có **đúng một** cách đọc (lower, `u:` ⇒ `v`) trong CEDICT và cách đọc đó = `syllable + tone` | FAIL |
| 6b | Không có `.raw/` | WARN "Bỏ qua kiểm tra đơn âm — tải CC-CEDICT vào content/chinese/.raw/ (xem SOURCES.md)" |
| 7 | Ví dụ trong `initials.examples`, `guide` (examples, compare): pinyin số hợp lệ, số âm tiết = số chữ Hán, âm tiết (bỏ thanh) có trong syllables; có CEDICT ⇒ mỗi chữ có cách đọc tương ứng; ví dụ một chữ ⇒ đơn âm như #6 | FAIL |
| 8 | `guide` có đủ 8 `id` bắt buộc, `order` duy nhất | FAIL |
| 9 | Độ phủ: số cặp (âm tiết, thanh) có chữ **≥ 300**; mỗi thanh 1–4 có **≥ 60** âm tiết; **≥ 100** âm tiết có ≥ 3 thanh — thiếu ⇒ WARN kèm số liệu (không FAIL để không chặn vì dữ liệu thực tế) | WARN |
| 10 | Mọi khoá nguồn nhắc trong `SOURCES.md` có mục; in thống kê cuối: số thanh mẫu/vận mẫu/âm tiết/cặp có chữ/phân bố theo thanh | — |

Script chỉ dùng Node chuẩn + ajv; viết thông điệp tiếng Việt kèm đường dẫn JSON (`syllables.json › items[12] › tones.3`).

**Quy trình soạn `syllables.json`** (content-implement): (1) dựng danh sách âm tiết chuẩn từ bảng pinyin (dữ kiện — không bảo hộ) và đối chiếu với tập âm tiết xuất hiện trong CC-CEDICT; (2) với mỗi (âm tiết, thanh), lọc các chữ đơn âm trong CEDICT có đúng cách đọc đó, chọn chữ **thông dụng** (ưu tiên chữ có trong danh sách HSK 1–3 nếu content-implement có sẵn nguồn MIT đã xác minh; không thì theo hiểu biết, tránh chữ hiếm/nghĩa xấu); (3) tự viết `meaningVi`; (4) chạy validate tới khi sạch; báo số liệu độ phủ trong bàn giao.

#### 5.4.5 `SOURCES.md` (F5)

Bảng cột: `Khoá | Tên | URL | Giấy phép | Ngày lấy | Phiên bản/commit | Phần đã dùng | Nghĩa vụ | File bị ảnh hưởng`. Dòng F5:

- `pinyin-table` — Bảng âm tiết Hán ngữ pinyin (dữ kiện ngôn ngữ, không bảo hộ) — dùng cho danh sách âm tiết, thanh mẫu, vận mẫu — nghĩa vụ: không — file `initials.json`, `finals.json`, `syllables.json`.
- `cc-cedict` — CC-CEDICT (MDBG), `https://www.mdbg.net/chinese/dictionary?page=cc-cedict` — CC BY-SA 4.0 — ngày tải + dòng `#! date=` của file — **chỉ dùng để kiểm tra/lựa chọn chữ đơn âm, không sao chép nội dung mục từ (nghĩa, định nghĩa) vào dữ liệu** — nghĩa vụ: ghi công; file bị ảnh hưởng: không có dữ liệu dẫn xuất ở F5 (F6 sẽ khác). *Ghi chú: việc chọn chữ + cách đọc là dữ kiện; nếu review cho rằng đây là dẫn xuất thì áp CC BY-SA 4.0 cho `data/pinyin/syllables.json` và thêm toàn văn giấy phép vào `LICENSES/` — D6 của HĐG mặc định đã chấp nhận.*
- `original` — Tự soạn bởi dự án — `noteVi`, `meaningVi`, toàn bộ `guide.json` — thuộc dự án.

#### 5.4.6 Cách nạp

Backend đọc 4 file lúc khởi động (§5.2.1). Đổi học liệu ⇒ khởi động lại chinese-backend (dev: `dotnet run` tự copy file mới vào `bin` khi build lại). Frontend **không** import JSON.

---

## 6. Hợp đồng API

Đường dẫn service (trình duyệt thêm `/chinese` hoặc `/identity` phía trước — HĐG §6). Lỗi theo HĐG §6.0; mã mới trong file này: `UNKNOWN_SYLLABLE`, `TONE_NOT_AVAILABLE`, `INVALID_SESSION_TIME`, `PASSWORD_UNCHANGED` (đều 422), `CONTENT_UNAVAILABLE` (503, đã có ở HĐG).

### 6.1 F5 — chinese-backend, mọi endpoint yêu cầu quyền `study.use`

Chung: 401 `UNAUTHENTICATED` (không/hỏng token) · 403 `FORBIDDEN` (thiếu `study.use`) · 503 `CONTENT_UNAVAILABLE` (catalog không khả dụng — chỉ `chart`, `guide`, `tone-drills`).

**`GET /api/pinyin/chart`** → 200, header `ETag`, `Cache-Control: private, max-age=3600`; `If-None-Match` khớp ⇒ 304.

```json
{
  "version": "3f9a1c0d2b7e4a55",
  "initials": [ { "code": "b", "group": "moi", "display": "b", "ipa": "p", "aspirated": false, "noteVi": "...", "examples": [ { "pinyin": "ba4", "hanzi": "爸" } ] } ],
  "finals":   [ { "code": "ian", "group": "i", "display": "ian", "standaloneSpelling": "yan", "noteVi": "..." } ],
  "syllables": [
    { "syllable": "ma", "initial": "m", "final": "a",
      "tones": { "1": { "hanzi": "妈", "meaningVi": "mẹ" }, "3": { "hanzi": "马", "meaningVi": "ngựa" } } },
    { "syllable": "zhei", "initial": "zh", "final": "ei", "tones": {} }
  ]
}
```

**`GET /api/pinyin/guide`** → 200 (ETag như trên) `{ "version": "...", "topics": [ { "id", "title", "order", "blocks": [ ... §5.4.2 ] } ] }` (sắp `order`).

**`POST /api/pinyin/tone-drills`**

```json
// request — listen_tone
{ "clientSessionId": "0f8e5b1a-7c2d-4e3f-9a10-5b6c7d8e9f00",
  "mode": "listen_tone",
  "startedAt": "2026-09-16T23:26:00Z", "finishedAt": "2026-09-16T23:30:00Z",
  "items": [
    { "parts": [ { "syllable": "ma", "hanzi": "马", "expectedTone": 3, "answeredTone": 2 } ], "responseMs": 1800, "replayCount": 1 }
  ] }
// request — tone_pair (mỗi item đúng 2 phần, khác âm tiết, không cùng thanh 3)
{ "clientSessionId": "...", "mode": "tone_pair", "startedAt": "...", "finishedAt": "...",
  "items": [ { "parts": [ { "syllable": "ma", "hanzi": "妈", "expectedTone": 1, "answeredTone": 1 },
                          { "syllable": "cha", "hanzi": "茶", "expectedTone": 2, "answeredTone": 3 } ],
               "responseMs": 3200, "replayCount": 0 } ] }

// 201 (tạo mới) | 200 (clientSessionId đã nộp — trả lại kết quả cũ)
{ "id": "0192f0aa-...", "clientSessionId": "0f8e5b1a-...", "mode": "listen_tone",
  "total": 20, "correct": 16, "localDate": "2026-09-17",
  "byTone": { "1": { "total": 5, "correct": 5 }, "2": { "total": 5, "correct": 3 },
              "3": { "total": 5, "correct": 4 }, "4": { "total": 5, "correct": 4 } } }
```

- `byTone` đếm theo **phần** (`tone_pair` 20 câu ⇒ tổng 40), luôn đủ 4 khoá; `total`/`correct` đếm theo **câu**.
- Lỗi: 400 `VALIDATION` (hình dạng — §5.2.1 validator; `details` theo đường dẫn trường, vd `"items[3].parts"`) · 422 `UNKNOWN_SYLLABLE` `details: { itemIndex, partIndex, syllable }` · 422 `TONE_NOT_AVAILABLE` `details: { itemIndex, partIndex, syllable, tone }` · 422 `INVALID_SESSION_TIME` `details: { reason: "order" | "future" | "too_long" | "too_old" }` · 503.
- Thông điệp mẫu: `"Âm tiết 'xx' không có trong bảng pinyin."`, `"Âm tiết 'zhei' thanh 4 không có chữ minh hoạ."`, `"Thời gian bài luyện không hợp lệ."`.

**`GET /api/pinyin/tone-stats`** → 200 (không cache)

```json
{ "totalAnswered": 240, "sessionsCount": 12, "lastSessionAt": "2026-09-16T23:30:00Z",
  "windowSize": 200,
  "accuracy": 0.82,
  "byTone": { "1": { "total": 60, "correct": 57, "accuracy": 0.95 },
              "2": { "total": 60, "correct": 41, "accuracy": 0.68 },
              "3": { "total": 60, "correct": 45, "accuracy": 0.75 },
              "4": { "total": 60, "correct": 54, "accuracy": 0.9 } },
  "confusions": [ { "expected": 2, "answered": 3, "count": 14 }, { "expected": 3, "answered": 2, "count": 11 } ],
  "recommendedFocus": [2, 3],
  "g0Reached": false }
```

Người chưa luyện: `totalAnswered: 0`, `sessionsCount: 0`, `lastSessionAt: null`, `accuracy: null`, `byTone` 4 khoá `{ total: 0, correct: 0, accuracy: null }`, `confusions: []`, `recommendedFocus: []`, `g0Reached: false`. **Lưu ý serializer** `WhenWritingNull` đang bật ở Program.cs ⇒ trường `null` sẽ **bị bỏ**; DTO stats đánh `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` cho `accuracy`, `lastSessionAt` để frontend luôn nhận khoá. Kiểu TS khai `accuracy: number | null`.

### 6.2 F4 — identity-service (Bearer audience `af-identity`)

```
GET  /api/account                → 200 { id, email, displayName, timeZone, createdAt }
PUT  /api/account                { "displayName": "Quân", "timeZone": "Asia/Ho_Chi_Minh" }
                                 → 200 account · 400 VALIDATION · 422 INVALID_TIME_ZONE · 401 · 403 ACCOUNT_DISABLED
POST /api/auth/password          (Bearer + cookie af_rt nếu có; kiểm Origin; rate limit auth)
                                 { "currentPassword": "...", "newPassword": "..." }
                                 → 200 { "otherSessionsRevoked": 2, "currentSessionKept": true }
                                 · 400 VALIDATION · 401 · 403 ORIGIN_NOT_ALLOWED · 422 WRONG_PASSWORD | PASSWORD_UNCHANGED · 429 RATE_LIMITED
```

`POST /api/account/password` của HĐG §6.2 **bị thay** bởi `POST /api/auth/password`.

### 6.3 F4 — chinese-backend (quyền `users.manage`, trừ `/api/me`)

```
GET /api/me                                 ([Authorize]) → như HĐG §6.3; timeZone phản ánh claim mới NGAY (R4-4)

GET /api/admin/users?q=quan&page=1&pageSize=20
→ 200 { "items": [ { "id": "0192...", "email": "quandh@...", "displayName": "Quân",
                     "roles": ["admin"], "isBootstrapAdmin": true,
                     "firstSeenAt": "2026-09-16T08:00:00Z", "lastSeenAt": "2026-09-17T01:00:00Z" } ],
        "page": 1, "pageSize": 20, "totalCount": 1 }
  · 400 VALIDATION (page < 1, pageSize ∉ 1..100, q > 100 ký tự)

GET /api/admin/users/{id}                   → 200 item · 404 NOT_FOUND

PUT /api/admin/users/{id}/roles             { "roles": ["learner"] }     // [] hợp lệ
→ 200 item (sau khi đổi)
  · 400 VALIDATION (thiếu roles / không phải mảng / phần tử rỗng)
  · 404 NOT_FOUND
  · 422 UNKNOWN_ROLE  { "error": "Vai trò không tồn tại: superman", "code": "UNKNOWN_ROLE", "details": { "roles": ["superman"] } }
  · 422 LAST_ADMIN    { "error": "Không thể gỡ quyền của quản trị viên cuối cùng.", "code": "LAST_ADMIN" }

GET /api/admin/roles
→ 200 [ { "code": "admin", "name": "Quản trị viên", "description": "Toàn quyền: học, soạn nội dung, quản lý người dùng",
          "permissions": ["content.manage", "study.use", "users.manage"] },
        { "code": "learner", "name": "Học viên", "description": "Dùng các chức năng học", "permissions": ["study.use"] } ]
```

---

## 7. Phân rã feature

> Mỗi feature: code → cổng build/test (§9.1) → review (Opus) → integration (Opus, **commit local riêng**, không push) → dừng/hoặc chạy tiếp theo lệnh người dùng. Trong một feature: **DB/Backend ‖ Content ‖ Frontend** song song sau khi §5.1 và §6 đã chốt (đã chốt trong file này).

### Feature F5: Pinyin & thanh điệu

- **Mục tiêu:** người số 0 học âm & thanh, luyện nghe-chọn thanh, biết mình yếu thanh nào; nền `study_events` + khung `content/` + TTS dùng chung.
- **Phụ thuộc:** F3 (JWT, `access.users`, `RequirePermission`, `PermissionCodes.StudyUse`, `RequireAuth/RequirePermission` FE, `TestTokenFactory`).
- **Phân công & file dự kiến:**

| Agent | Phạm vi | File |
|---|---|---|
| **content-implement** (Sonnet) | §5.4 | `content/{package.json,yarn.lock,.dockerignore,README.md}`, `content/chinese/{SOURCES.md,LICENSES/README.md}`, `content/chinese/schemas/pinyin-*.schema.json` (4), `content/chinese/data/pinyin/{initials,finals,syllables,guide}.json`, `content/chinese/scripts/{validate.mjs,lib/cedict.mjs}` |
| **database-implement** (Sonnet) | §5.1.1 | `AntFarm.Chinese.Domain/Learning/StudyEvent.cs`, `Pinyin/ToneDrillSession.cs`, `Pinyin/ToneDrillAnswer.cs`, `Pinyin/ToneDrillMode.cs`; `AntFarm.Chinese.Infrastructure/Persistence/Configurations/Learning/*.cs` (3); `ChineseDbContext.cs`, `IChineseDbContext.cs`; migration `F5_ToneDrill` |
| **backend-implement** (Sonnet) | §5.2.1 | `AntFarm.Core/Errors/ServiceUnavailableException.cs` (+ test shared); Domain `Learning/StudyEventKinds.cs`, `Time/UserLocalDate.cs`, `Pinyin/PinyinText.cs`, `Pinyin/PinyinSyllable.cs`; Application `Learning/{IStudyActivityRecorder,StudyActivityRecorder}.cs`, `Pinyin/{IPinyinCatalog,SubmitToneDrillRequestValidator,ToneDrillService,ToneStatsCalculator,ToneStatsService}.cs`, `Pinyin/Dtos/*.cs`, `DependencyInjection.cs`; Infrastructure `Content/{ContentOptions,PinyinCatalogLoader,PinyinCatalog}.cs`, `DependencyInjection.cs`; Api `Features/Pinyin/PinyinController.cs`, `Program.cs`, `appsettings.json`, `.csproj`, `Dockerfile`; `deploy/docker-compose.yml`, `deploy/VERIFY-DOCKER.md`; `backend/Directory.Packages.props`; test §5.2.1; `README.md` (mục học liệu) |
| **frontend-implement** (**Fable** — `model: "fable"`) | §5.3.A–D | `frontend/packages/ui/src/{components/dialog/AppDialog.tsx,components/dialog/AppDrawer.tsx,hooks/useTabParam.ts,speech/speech.ts,speech/useSpeech.ts,index.ts}`; `frontend/scripts/check-ui-conventions.mjs` (comment); `frontend/{package.json,turbo.json,yarn.lock}`; `frontend/apps/chinese/{package.json,vite.config.ts}`; `apps/chinese/src/{lib/pinyin.ts,lib/pinyin.test.ts,components/Hanzi.tsx,components/Pinyin.tsx,router.tsx,layout/AppShell.tsx}`; `apps/chinese/src/features/pinyin/**` (§5.3.D) |

- Database và backend có thể là **một** agent Sonnet nếu Orchestrator muốn gọn (cùng project); content và frontend chạy song song với backend. Frontend dựng theo §6.1 — chưa có backend thì dùng dữ liệu giả **chỉ trong lúc phát triển**, không commit mock.
- **Tiêu chí hoàn thành + cách tự test:**
  1. `yarn --cwd content install && yarn --cwd content validate:chinese` exit 0 (có `.raw/cedict_ts.u8` ⇒ không WARN #6b); in số liệu độ phủ; ≥ 380 âm tiết. Sửa tạm một chữ minh hoạ thành 好 ⇒ FAIL (rồi hoàn tác).
  2. `dotnet build backend/backend.slnx -v q` 0 error; `dotnet test backend/backend.slnx` xanh **với `AF_TEST_PG`** (báo số test chạy/skip); ≥ 25 test `PinyinText`; test local_date 06:30 sáng VN xanh.
  3. `bin/Debug/net10.0/content/chinese/data/pinyin/` có 4 file; `dotnet publish` ra thư mục có 4 file.
  4. Migration `F5_ToneDrill` chạy được trên DB dev đang có dữ liệu F3 và DB trống (xoá DB dev tạm hoặc DB test).
  5. `cd frontend && yarn install && yarn workspace @af/chinese tsc -b && yarn workspace @af/chinese build && yarn workspace @af/chinese test && yarn lint:ui` sạch; `test` ≥ 30 ca pinyin + test `generateDrill`.
  6. Chạy thật 4 tiến trình: đăng nhập learner ⇒ menu "Pinyin" ⇒ `/pinyin` mở tab Hướng dẫn; đổi tab ⇒ URL `?tab=...` đổi bằng replace (Back không đi qua từng tab).
  7. Tab Bảng: bấm `ma` ⇒ drawer 4 thanh, nghe đủ 4 thanh trên máy người dùng; máy không có giọng `zh` (hoặc giả lập bằng cách chặn `getVoices`) ⇒ `VoiceMissingAlert` đúng hệ điều hành, nút nghe vô hiệu có tooltip.
  8. Tab Luyện: làm 20 câu `listen_tone` bằng phím `1–4`/`Space`/`Enter` và bằng chạm ở 375px; nộp ⇒ kết quả + thống kê cập nhật; tắt mạng lúc nộp ⇒ "Gửi lại" hoạt động và DB chỉ có **1** session. Làm 1 phiên `tone_pair` ⇒ không có câu 3-3.
  9. Làm bài lúc 06:30 sáng giờ VN (hoặc đổi giờ máy/`FakeTimeProvider` ở test) ⇒ `learning.study_events.local_date` = ngày VN.
  10. Tắt tạm `content/chinese/data/pinyin/guide.json` (đổi tên) + khởi động lại backend ⇒ log Error rõ, `/health/live` 200, `/chinese/api/pinyin/chart` 503 JSON, trang Pinyin hiện Alert (không trắng); hoàn tác.
  11. Learner bị gỡ hết vai trò (sửa DB) ⇒ `/pinyin` ⇒ `/403`.
  12. Dockerfile + compose có `additional_contexts` + `COPY --from=content`, ghi **"CHƯA VERIFY"**; `VERIFY-DOCKER.md` có mục kiểm học liệu.
  13. `SOURCES.md` đủ 3 dòng §5.4.5; không có JSON học liệu nào bị frontend import (`grep -rn "content/chinese" frontend/apps` rỗng).
- **Học thử ngay:** đọc hết tab Hướng dẫn, làm 3 phiên luyện thanh (2 `listen_tone`, 1 `tone_pair`), ghi lại thanh bị gợi ý tập trung.

### Feature F4: Hồ sơ & quản trị vai trò

- **Mục tiêu:** người học đổi tên/múi giờ/mật khẩu, chinese-backend thấy múi giờ mới ngay; admin tiếng Trung gán/gỡ vai trò an toàn.
- **Phụ thuộc:** F3 (bắt buộc), F5 (dùng `AppDialog`/`AppDrawer`/`useTabParam` — nếu người dùng đổi thứ tự làm F4 trước F5 thì F4 tạo ba thành phần này theo §5.3.A và F5 bỏ qua phần đó).
- **Phân công & file dự kiến:**

| Agent | Phạm vi | File |
|---|---|---|
| **backend-implement** (Sonnet) — identity | §5.2.2 | `AntFarm.Identity.Api/Features/Auth/AuthController.cs` (thêm `password`), `Features/Account/AccountController.cs` (gỡ `password`, rà PUT), `AntFarm.Identity.Application/Accounts/{AccountService,AuthService}.cs`, validator `ChangePasswordRequestValidator.cs`, `UpdateProfileRequestValidator.cs`; test `AntFarm.Identity.ApiTests/Account/*`, `Auth/ChangePasswordTests.cs` |
| **backend-implement** (Sonnet) — chinese | §5.2.3 | `AntFarm.Chinese.Application/Access/{UserProvisioningService,UserAdminService}.cs`, `Access/Dtos/*`, validator `AdminUsersQueryValidator.cs`, `SetUserRolesRequestValidator.cs`; `AntFarm.Chinese.Domain/Access/RoleCodes.cs` (`Describe`); `AntFarm.Chinese.Api/Features/Admin/{UsersController,RolesController}.cs`; test UnitTests + ApiTests §5.2.3 |
| database-implement | — | không có (không migration) |
| content-implement | — | không có |
| **frontend-implement** (**Fable** — `model: "fable"`) | §5.3.E–F | `frontend/packages/ui/src/{feedback/ConfirmProvider.tsx,feedback/ToastProvider.tsx,inputs/AppAutocomplete.tsx,inputs/TimeZoneAutocomplete.tsx,index.ts}`; `frontend/packages/auth/src/*` (`refreshSession`, `changePassword`, `reason=password-changed`); `apps/chinese/src/{App.tsx,router.tsx,layout/AppShell.tsx}` + menu người dùng; `apps/chinese/src/features/profile/**`, `apps/chinese/src/features/admin-users/**` (§5.3.F). Package mới trong `@af/ui` không kéo thư viện mới (chỉ MUI) ⇒ không đổi peerDependencies |

- **Tiêu chí hoàn thành + cách tự test:**
  1. Cổng build/test §9.1 sạch (ApiTests identity + chinese với `AF_TEST_PG`).
  2. `/ho-so`: đổi tên ⇒ menu người dùng hiện tên mới ngay; đổi múi giờ sang `Europe/Berlin` ⇒ tab Network thấy `POST .../auth/refresh` rồi `GET /chinese/api/me` trả `timeZone: "Europe/Berlin"` **trong cùng lượt lưu** (không chờ 5 phút); đổi lại `Asia/Ho_Chi_Minh`.
  3. Ô múi giờ gõ "Ho_Chi" và "ho chi" đều ra gợi ý `(UTC+07:00) Asia/Ho_Chi_Minh`; chọn được bằng bàn phím; `yarn lint:ui` không báo `autocomplete-slotprops-override`.
  4. Đổi mật khẩu với 2 trình duyệt đang đăng nhập (A đổi) ⇒ A vẫn dùng tiếp, B bị đăng xuất ở lần refresh kế; sai mật khẩu hiện tại ⇒ lỗi ngay dưới ô; đăng nhập lại bằng mật khẩu mới OK.
  5. Admin vào `/quan-tri/nguoi-dung`: tìm theo một phần email (URL có `?q=`), F5 trang giữ kết quả; mở dialog, gỡ `admin` của chính mình khi là admin duy nhất ⇒ `Alert` "Không thể gỡ quản trị viên cuối cùng" **trong dialog**, dialog không đóng; bấm ra ngoài/ESC không đóng dialog.
  6. Gán `admin` cho tài khoản thứ hai ⇒ tài khoản đó tải lại app thấy menu "Người dùng"; gỡ lại ⇒ lần gọi kế `/chinese/api/admin/ping` 403.
  7. Learner không thấy menu "Người dùng"; gõ thẳng `/quan-tri/nguoi-dung` ⇒ `/403`.
  8. Gỡ hết vai trò một người ⇒ dialog cảnh báo trước; người đó vào app ⇒ `/403` có nút Đăng xuất.
  9. 375px: danh sách dạng thẻ, dialog toàn màn hình, form hồ sơ không cuộn ngang.
- **Học thử ngay:** đặt múi giờ đúng nơi đang sống (quyết định ranh giới ngày học của F7/F11), đặt mật khẩu mạnh.

---

## 8. Thứ tự thực thi & phụ thuộc

```
F3 ─► F5 ─► F4 ─► F6 ...
      │
      ├─ (song song) content-implement: §5.4  ────────────┐
      ├─ (song song) database+backend-implement: §5.1.1, §5.2.1 ─┼─► review ─► integration (commit "feat(chinese): F5 — ...")
      └─ (song song) frontend-implement (Fable): §5.3.A–D ─┘
F4:   backend identity ‖ backend chinese ‖ frontend (Fable) ─► review ─► integration (commit "feat(platform): F4 — ...")
```

- Trong F5: backend cần **dữ liệu thật** cho test nạp catalog (§5.2.1) ⇒ nếu content chưa xong, backend viết test đó với cờ skip tạm và integration **bắt buộc** bật lại trước commit. Frontend cần `chart` thật để kiểm tay mục 7–8.
- Trong F4: backend identity phải xong trước khi frontend kiểm tay mục 4 (route `/auth/password`).
- Commit: mỗi feature **một** commit local (có thể kèm commit `fix(...)` sau review). Không push.

---

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng bắt buộc (macOS — máy dev hiện tại)

```bash
# gốc repo
dotnet build backend/backend.slnx -v q                       # 0 error
AF_TEST_PG="Host=localhost;Port=5432;Username=...;Password=..." dotnet test backend/backend.slnx   # xanh, báo số skip
yarn --cwd content install && yarn --cwd content validate:chinese   # F5
cd frontend
yarn install
yarn workspace @af/chinese tsc -b                            # bắt buộc -b
yarn workspace @af/chinese build                             # F5 đổi dependency + @af/ui; F4 đổi @af/ui, @af/auth
yarn workspace @af/chinese test                              # từ F5
yarn lint:ui                                                  # exit 0
git status                                                    # không appsettings.Development.json, .secrets/, content/node_modules, content/**/.raw
```

### 9.2 Kiểm tay

Chạy identity (5281) → chinese (5282) → gateway (5280) → app (3280); thao tác ở 1366px và 375px (DevTools) theo tiêu chí §7 từng feature. Kiểm TTS trên **máy và trình duyệt người dùng dùng thật** (macOS Safari/Chrome, điện thoại).

### 9.3 Review phải soát riêng

- `grep -rn "new string Policy" backend` rỗng; controller mới dùng `[RequirePermission]` đúng mã.
- Mọi `DateTime` ghi DB `Kind=Utc` (`startedAt`/`finishedAt` bind từ JSON có `Z` ⇒ Utc; chuỗi không `Z` ⇒ validator **từ chối** hoặc quy đổi `ToUniversalTime()` — backend chọn một, ghi comment; **[BA-mặc định] từ chối 400** vì client luôn gửi `toISOString()`).
- `study_events` ghi cùng transaction với session; nộp lại không nhân đôi.
- Không Dialog/Drawer MUI trần trong `apps/`; `SyllableDrawer` có comment lý do `closeOnBackdrop`.
- Chữ Hán đều trong `LangText lang="zh-CN"`/`Hanzi`.
- Không `uuid` package; `crypto.randomUUID()`.
- `speak` chỉ gọi từ handler người dùng (iOS).

---

## 10. Rủi ro / quyết định mở / ràng buộc

### 10.1 Rủi ro mới (bổ sung HĐG §10.1)

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RK34 | Cookie `af_rt` có `Path=/api/auth` nên không tới `/api/account/password` (thiết kế HĐG) | Đổi mật khẩu không giữ được phiên hiện tại hoặc thu hồi sai | Chuyển sang `POST /api/auth/password` (§4.3); test hai phiên |
| RK35 | TTS đọc cặp hai chữ theo ngữ điệu từ (biến điệu 3-3, thanh nhẹ nếu vô tình thành từ có thật, vd hai chữ ghép thành từ có âm tiết sau đọc nhẹ) | Chấm sai oan ở `tone_pair` | Loại 3-3; hai âm tiết khác nhau; content ưu tiên chữ không tạo từ phổ biến khi ghép; người dùng báo cặp sai ⇒ thêm danh sách cặp cấm trong `generateDrill` |
| RK36 | Chrome cũ trả tên múi giờ CLDR (`Asia/Saigon`) từ `Intl.supportedValuesOf`; ảnh Docker `aspnet` thiếu `tzdata` ⇒ `TryFindSystemTimeZoneById` sai | Người dùng không tìm thấy/không lưu được múi giờ; production từ chối mọi múi giờ | Quy bí danh ở frontend (R4-2); VERIFY-DOCKER thêm mục `docker compose exec identity-service ls /usr/share/zoneinfo/Asia/Ho_Chi_Minh` — thiếu thì cài `tzdata` trong Dockerfile. `UserLocalDate` có fallback không ném |
| RK37 | Test nạp catalog tìm gốc repo bằng đường dẫn tương đối | Test đỏ trên CI/Docker, xanh trên máy dev | Đi ngược thư mục tới khi thấy `content/chinese`; không thấy ⇒ skip có thông điệp (không fail âm thầm — integration báo) |
| RK38 | `additional_contexts` cần Docker Compose ≥ 2.17 + BuildKit | Build ảnh chinese-backend lỗi `COPY --from=content` trên máy cũ | Ghi yêu cầu version trong VERIFY-DOCKER; build tay dùng `--build-context` |
| RK39 | Độ phủ chữ minh hoạ đơn âm thấp với một số thanh | Bài luyện lặp chữ, phân bố thanh lệch | WARN độ phủ (#9); `generateDrill` cho lặp khi hết lựa chọn |
| RK40 | Bootstrap admin gán lại mỗi lần khởi động (R4-10) trái kỳ vọng "gỡ được" | Admin gỡ xong thấy quyền quay lại sau khi deploy | Cảnh báo trong dialog; tài liệu README: gỡ vĩnh viễn = xoá email khỏi cấu hình |
| RK41 | `WhenWritingNull` bỏ trường `null` | Frontend đọc `undefined` thay `null`, hiển thị "NaN%" | DTO đánh `JsonIgnore(Never)` cho trường có thể null (§6.1); TS xử lý cả `undefined` |
| RK42 | Thiết kế F4/F5 dựa trên thiết kế F2/F3 chưa code xong | Tên lớp/route lệch | Dòng "Tiền đề" đầu tài liệu: agent đọc code thật trước; lệch tên thì theo code, lệch hành vi thì báo Orchestrator |

### 10.2 Quyết định mặc định BA dùng trong file này (người dùng có thể phản đối)

| Mã | Quyết định | Lý do |
|---|---|---|
| D21 | Đổi mật khẩu ở `POST /api/auth/password` thay `/api/account/password` | Cookie refresh chỉ gửi tới `/api/auth` (RK34) |
| D22 | Đổi mật khẩu: sai mật khẩu hiện tại không tính vào khoá đăng nhập; mật khẩu mới trùng cũ ⇒ 422 `PASSWORD_UNCHANGED`; không nhận ra phiên ⇒ thu hồi mọi phiên | Đơn giản, an toàn |
| D23 | `PUT roles` cho phép mảng rỗng; khoá dòng vai trò `admin` để kiểm `LAST_ADMIN` | Cho phép khoá quyền một người mà không cần tính năng khoá tài khoản |
| D24 | Bootstrap admin gán lại mỗi lần khởi động; API trả `isBootstrapAdmin` | Cấu hình là nguồn sự thật, dễ hiểu |
| D25 | Không bảng nhật ký vai trò, chỉ log | MVP một người dùng chính |
| D26 | Khoá âm tiết theo chính tả chuẩn (`ju`, `nv`) | Khớp cách người học đọc bảng |
| D27 | Bọc JSON pinyin `{dataset, version, items}` | Thống nhất với `hsk-words.json`, có version làm ETag |
| D28 | `tone_pair` loại 3-3, hai âm tiết khác nhau; 20 câu cố định; 50% câu dồn vào thanh yếu; sinh câu ở client | Tránh chấm oan; đơn giản |
| D29 | Nộp phiên idempotent theo `clientSessionId`; chỉ nộp khi xong; cửa sổ thời gian 24 giờ/3 giờ/+5 phút | Mạng di động chập chờn; chống nộp bù giả streak |
| D30 | Thống kê: cửa sổ 200 phần/thanh; focus = total ≥ 10 & accuracy < 0,8; "xong G0" = mỗi thanh ≥ 20 câu & ≥ 0,85 | HĐG + tiêu chí đo tiến bộ rõ |
| D31 | `study_events.kind` không CHECK ở DB | Feature sau thêm loại không phải sửa constraint |
| D32 | Docker: giữ context `./backend`, thêm `additional_contexts: content` | Không phải viết lại mọi `COPY`/`.dockerignore` |
| D33 | Tab mặc định `/pinyin` là Hướng dẫn; mobile bảng mặc định lọc nhóm "môi" | Người số 0 cần đọc trước; bảng đầy đủ quá rộng ở 375px |
| D34 | Tốc độ đọc TTS lưu `localStorage` ở F5 (chuyển sang `learner_settings` ở F7) | Chưa có bảng cài đặt |
| D35 | vitest `^5.0.1`, môi trường `node`, chỉ test hàm thuần | Nhẹ, đủ cho tiện ích pinyin |
| D36 | Nghĩa Việt của chữ minh hoạ tự viết (`original`), không nhãn `machine` | Ngắn, do content-implement viết và người dùng thấy ngay |
| D37 | CC-CEDICT chỉ dùng để kiểm chữ đơn âm, không phân phối lại ở F5 | Tránh nghĩa vụ CC BY-SA cho F5; D6 của HĐG vẫn mặc định chấp nhận nếu review coi là dẫn xuất |
| D38 | Chuỗi thời gian không có `Z` ở `tone-drills` ⇒ 400 | Tránh `Kind=Unspecified` vào Npgsql |
| D39 | Thêm `GET /api/admin/users/{id}` và `description` vai trò (từ hằng code, không migration) | Dialog cần mô tả; không đổi schema F3 |

Các quyết định mở của HĐG §10.2 (D3–D8, D19, D20) **không** chặn F4/F5.

### 10.3 Ràng buộc dự án phải nhắc agent thực thi

- Build/test sạch §9.1; `tsc -b` (không `--noEmit`); commit local riêng từng feature, **không push**, nhánh `develop`.
- DDD 4 lớp; Application không phụ thuộc Infrastructure; controller mỏng.
- Phân quyền cục bộ trong DB `af_chinese`; frontend đọc quyền từ `/chinese/api/me`; `RequirePermissionAttribute` gán `Policy` trong constructor.
- Npgsql `timestamptz` chỉ `Kind=Utc`; "hôm nay" theo `access.users.time_zone`; `local_date` tính lúc ghi.
- MUI v9 (`slotProps`, shorthand trong `sx`); `AppDialog`/`AppDrawer` thay Dialog/Drawer trần; `useTabParam` cho tab cấp trang; Autocomplete trải `params.slotProps` trước; không `uuid` (dùng `crypto.randomUUID()`); `@af/*` import TS source, peer dependency khai ở app; yarn Classic.
- Mobile-first 375px; chữ Hán `lang="zh-CN"`; pinyin lưu số, hiển thị dấu.
- Học liệu: chỉ nguồn có giấy phép rõ, ghi `content/chinese/SOURCES.md`; `.raw/` không commit; loader không ném lỗi.
- File Docker cập nhật cùng feature, ghi "CHƯA VERIFY"; không commit bí mật.
