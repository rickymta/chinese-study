# AntFarm Mobile (Flutter) — app học viên tiếng Trung — Hợp đồng thực thi

- Ngày: 2026-09-17 · Loại: **tạo mới** (thư mục `mobile/`) + **nâng cấp** (identity-service thêm phiên mobile) · Nhánh: `develop`
- Service/app: `mobile/apps/chinese` (package `af_chinese`) · `mobile/packages/{af_lints,af_core,af_auth,af_ui}` · `backend/services/identity-service` · `content/chinese/scripts` (sao chép dữ liệu nét)
- Module: khung monorepo Flutter, phiên đăng nhập mobile, hồ sơ, tổng quan, ôn thẻ SRS, pinyin & luyện thanh, tra từ, bài học & quiz, luyện viết
- Người soạn: agent business-analysis (Opus)
- Tài liệu nền (đọc khi cần tra): HĐG = `2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md`; HĐ45 = `2026-09-17-antfarm-f4-f5-chi-tiet.md`; HĐ67 = `2026-09-17-antfarm-f6-f7-chi-tiet.md`; HĐ811 = `2026-09-17-antfarm-f8-f11-chi-tiet.md`.
- **Nguồn sự thật về hình dạng JSON**: các file `frontend/apps/chinese/src/features/*/types.ts` (đã chạy thật với backend ở MVP, có ghi chú trường vắng do `WhenWritingNull`). Hợp đồng này tóm tắt; lệch nhau thì **theo `types.ts` + code controller thật**, và báo Orchestrator.

> Điểm tựa chống mất bộ nhớ: khi context bị nén, đọc §3 (quy tắc), §7 (feature đang làm), rồi mục §5/§6 mà feature trỏ tới.
> Ký hiệu: **[BA-mặc định]** = BA tự chốt vì người dùng giao chạy một mạch (17/09/2026), người dùng có thể phản đối (tổng hợp §10.3).

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

MVP web + backend F0–F11 đã xong (`docs/HANDOFF-2026-09-17-MVP.md`). CLAUDE.md ghi "người học ôn thẻ trên điện thoại là chính" — web đã mobile-first nhưng trải nghiệm hằng ngày (mở app, ôn 20 thẻ, nghe TTS, viết chữ bằng ngón tay) tốt hơn với app gốc. Người dùng chốt 17/09/2026: dựng **`mobile/` — monorepo Flutter nhiều app (mỗi ngôn ngữ một app, giống `frontend/`)**, bắt đầu với app tiếng Trung, **chỉ tính năng học viên**; quản trị nội dung/vai trò vẫn ở web.

Máy dev: macOS, Flutter **3.47.4** / Dart **3.13.3** (kiểm 17/09/2026). **Chưa có Android SDK; Xcode chưa `xcode-select`, chưa có CocoaPods** ⇒ agent chỉ kiểm được bằng `flutter analyze`, `flutter test`, `flutter build web` và chạy thử bản web (Chrome/web-server). Build Android/iOS **chưa verify** (giống cách Docker "chưa verify" ở MVP) — checklist ở `mobile/VERIFY-DEVICE.md`.

### 1.2 Mục tiêu phần mềm

1. Monorepo `mobile/` dùng **Dart pub workspaces**, package dùng chung `af_*` (tương đương `@af/*`), app `apps/chinese`; thêm ngôn ngữ sau = thêm `apps/<ngon-ngu>` dùng lại package.
2. identity-service có **luồng phiên riêng cho client mobile** (refresh token trong body JSON, xoay vòng + phát hiện dùng lại + ân hạn như web) **mà không làm yếu luồng cookie của web** (web vẫn bắt `Origin`).
3. App tiếng Trung có cùng hành vi nghiệp vụ với web: pinyin số↔dấu, outbox ôn thẻ idempotent, quiz, luyện thanh, heatmap, luyện viết 3 bước — **cùng API, cùng số liệu**, học trên web hay mobile đều cộng vào một tiến độ.
4. Dev chạy được bản web của app **cùng origin với gateway** (không CORS, đúng quy tắc "CORS chỉ ở identity").

### 1.3 Mục tiêu học tập (nghiệp vụ sư phạm)

Không đổi lộ trình của HĐG §1.3 (G0 âm & thanh → G1 từ vựng HSK 1 qua SRS + bài học + viết chữ; xuyên suốt: chuỗi ngày học). App mobile phục vụ **thói quen hằng ngày**:

| Tình huống trên điện thoại | Vì sao quan trọng | Màn mobile | Đo bằng |
|---|---|---|---|
| Mở app mỗi sáng thấy "việc hôm nay" | Thói quen > cường độ | Tổng quan (M5) | Streak, mục tiêu ngày (F11) |
| Ôn 10–20 thẻ lúc rảnh, mạng chập chờn (xe buýt) | SRS chỉ hiệu quả khi ôn **đúng hạn** | Ôn thẻ (M6) có outbox bền | `dueToday` về 0, thẻ vững |
| Nghe–chọn thanh bằng ngón cái | G0 cần nhiều lượt nghe ngắn | Luyện thanh (M7) | Độ chính xác từng thanh, `g0Reached` |
| Tra nhanh một chữ gặp ngoài đời | Củng cố từ trong ngữ cảnh thật | Tra từ (M8) | — (thêm vào ôn tập) |
| Học bài + quiz | Đặt từ vào ngữ cảnh | Bài học (M9) | Bài hoàn thành, điểm quiz |
| **Viết chữ bằng ngón tay** | Viết giúp nhớ mặt chữ; màn cảm ứng tự nhiên hơn chuột | Luyện viết (M10) | Chữ đã thuộc (`clean_recall_days ≥ 2`) |

Thứ tự làm feature (§8) ưu tiên **ôn thẻ** ngay sau tổng quan vì đó là việc người học làm trên điện thoại nhiều nhất; pinyin/tra từ/bài học/viết theo sau (web đã có sẵn cho những phần chưa lên mobile).

---

## 2. Phạm vi

### 2.1 In-scope

1. `mobile/` monorepo: root workspace, `af_lints`, `af_core`, `af_auth`, `af_ui`, `apps/chinese` (Android + iOS + web-dev), script kiểm tra `tool/ci.sh` + `tool/check_conventions.dart`, README, `VERIFY-DEVICE.md`.
2. identity-service: endpoint `/api/auth/mobile/{register,login,refresh,logout,password}`, cột `client_type`/`client_app`/`device_name` (migration `M1_MobileClient`), rate limit riêng, test.
3. App học viên: đăng nhập/đăng ký/đăng xuất, hồ sơ cơ bản (tên, múi giờ, mật khẩu, giao diện sáng/tối, cài đặt học tập, giọng đọc), tổng quan (F11), ôn thẻ (F7), pinyin & luyện thanh (F5), tra từ (F6), bài học & quiz (F9), luyện viết (F8).
4. Dữ liệu nét chữ đóng gói vào assets app (tập con `hanzi-writer-data@2.0.1` như web) + trang "Giấy phép & nguồn".
5. Cập nhật CLAUDE.md (mục Mobile), README, `.gitignore`, `.claude/agents/frontend-implement.md` (thêm phần Flutter), `deploy/VERIFY-DOCKER.md` (mục kiểm endpoint mobile).

### 2.2 Out-of-scope

- Quản trị nội dung / người dùng / vai trò trên mobile (F4 admin, F10) — vẫn ở web; app chỉ hiện lời giải thích.
- Phát hành store (ký release, TestFlight, Play Console), icon/splash thiết kế riêng (dùng mặc định có đổi màu), thông báo đẩy, nhắc học định giờ.
- Chế độ offline đầy đủ (cache danh sách, học không mạng) — chỉ **outbox ôn thẻ bền** + "Gửi lại" cho quiz/luyện thanh/luyện viết.
- Deep link/universal link, đăng nhập sinh trắc học, quên mật khẩu, quản lý danh sách phiên/thiết bị (cột `device_name` chỉ chuẩn bị dữ liệu).
- **Triển khai bản Flutter web lên production** — bản web chỉ là công cụ dev (web production vẫn là `frontend/apps/chinese`).
- Portal, ngôn ngữ thứ hai.

---

## 3. Quy tắc nghiệp vụ

Giữ nguyên toàn bộ R-* của HĐG, HĐ45 (R4-*, R5-*), HĐ67 (R6-*, R7-*), HĐ811 (R-LS*, R-W*, R-PG*). Bổ sung:

### 3.1 Phiên đăng nhập mobile (identity-service) — đã chốt hướng với người dùng, chi tiết [BA-mặc định]

- **RM-A1.** Client mobile dùng **endpoint riêng** dưới `/api/auth/mobile/*` (controller riêng `MobileAuthController`). Endpoint này **không đọc, không đặt, không xoá cookie** `af_rt` (kể cả khi trình duyệt tự gửi cookie kèm — cookie web `Path=/api/auth` khớp tiền tố). Luồng cookie của web (`AuthController`) **giữ nguyên**, vẫn `[ValidateOrigin]`.
- **RM-A2.** Refresh token mobile trả trong **body JSON** (`refreshToken` + `refreshTokenExpiresAt`), cùng thuật toán như web: 32 byte ngẫu nhiên hex, DB chỉ lưu SHA-256, sống `Jwt:RefreshTokenDays` (30 ngày) trượt, **xoay vòng mỗi lần dùng**, dùng lại token đã xoay **ngoài** ân hạn `Auth:RefreshReuseGraceSeconds` (30 giây) ⇒ thu hồi cả họ + 401; **trong** ân hạn ⇒ cấp token mới cùng họ.
- **RM-A3.** Mỗi refresh token mang `client_type` ∈ `web|mobile` (token mới xoay kế thừa từ token cha, cùng `client_app`, `device_name`). **Token sai kênh** (token `web` gửi tới `/mobile/refresh`, token `mobile` gửi qua cookie tới `/api/auth/refresh`) ⇒ `401 REFRESH_INVALID`, **không** thu hồi gì, log Warning `"Refresh token sai kênh {Expected}/{Actual}"` (kèm `FamilyId`, không log token).
- **RM-A4. Chặn trình duyệt**: mọi request tới `/api/auth/mobile/*` có header `Origin` ⇒ chỉ cho qua khi origin nằm trong `Auth:MobileDevOrigins` **và** môi trường là `Development`; ngược lại `403 ORIGIN_NOT_ALLOWED` ("Luồng đăng nhập của ứng dụng di động không dùng được từ trình duyệt."). Ngoài Development, `MobileDevOrigins` bị bỏ qua (log Warning lúc khởi động nếu khác rỗng). Lý do: app native (dio trên Android/iOS) không gửi `Origin`; trình duyệt luôn gửi `Origin` cho POST — chặn để SPA (kể cả khi bị XSS) không lấy được refresh token dạng đọc được bằng JS. `Referer` không xét.
- **RM-A5. Header nhận diện client** `X-AF-Client` **bắt buộc** ở mọi endpoint mobile, khớp `^[a-z0-9-]{1,32}/[0-9A-Za-z.+-]{1,32} \((android|ios|web)\)$` (vd `chinese-mobile/1.0.0+1 (android)`). Thiếu/sai ⇒ `400 VALIDATION` `details: { "X-AF-Client": ["Thiếu hoặc sai định dạng header X-AF-Client."] }`. Đây là **nhận diện để ghi log/thống kê, không phải kiểm soát an ninh** (giả được). Lưu nguyên chuỗi (cắt 64) vào `client_app`.
- **RM-A6.** `deviceName` (tuỳ chọn, trim, ≤ 100 ký tự, bỏ ký tự điều khiển) ở register/login lưu vào `device_name`.
- **RM-A7.** Đăng xuất mobile `POST /mobile/logout { refreshToken }`: token tồn tại & là `mobile` ⇒ thu hồi **cả họ** (`revoke_reason='logout'`); không tồn tại/đã thu hồi/sai kênh ⇒ vẫn `204` (không lộ thông tin). Không cần Bearer.
- **RM-A8.** Đổi mật khẩu mobile `POST /mobile/password` (Bearer audience `af-identity`) body có `refreshToken` tuỳ chọn để xác định họ hiện tại — cùng luật R4-5/D22: token hợp lệ, là `mobile`, thuộc đúng tài khoản ⇒ giữ họ đó, thu hồi họ khác (**mọi kênh**, kể cả web); không nhận ra ⇒ thu hồi tất cả, `currentSessionKept=false`.
- **RM-A9.** Rate limit policy **`auth-mobile`**: `Auth:MobileRateLimitPermitPerMinute` (mặc định **30**) yêu cầu/phút **mỗi IP** (partition theo `Connection.RemoteIpAddress` sau `UseForwardedHeaders`, giống policy `auth`), `429 RATE_LIMITED`. Cao hơn web (20) vì nhà mạng di động dùng CGNAT (nhiều máy chung IP) — RK-M14.
- **RM-A10.** Mọi response chứa token (register/login/refresh mobile) có header `Cache-Control: no-store` + `Pragma: no-cache`. Không log body request/response.
- **RM-A11.** Các luật còn lại giống web: `Auth:AllowRegistration`, khoá 10 lần sai/15 phút (`423`), tài khoản `is_active=false` ⇒ 403 `ACCOUNT_DISABLED` + thu hồi họ, email không phân biệt hoa thường, mật khẩu 8–128, `INVALID_TIME_ZONE`, dọn lười token hết hạn > 7 ngày lúc đăng nhập.
- **RM-A12.** Access token mobile **y hệt** web (RS256, 15 phút, `aud` = `Jwt:Audiences`) — chinese-backend không cần biết client là gì.
- **RM-A13.** `GET/PUT /api/account` (Bearer, không cookie, không kiểm Origin) dùng chung cho mobile — không thêm endpoint.

### 3.2 Phiên trên app (af_auth)

- **RM-S1.** Refresh token + hạn + tài khoản rút gọn lưu **chỉ** trong `flutter_secure_storage` (Keychain iOS `first_unlock_this_device` — không đồng bộ iCloud; Android Keystore). **Access token chỉ ở bộ nhớ.** Cấm lưu token vào `shared_preferences`, log, hay URL.
- **RM-S2. Ghi trước khi dùng**: nhận response refresh ⇒ **ghi refresh token mới vào secure storage trước**, rồi mới phát access token cho request đang chờ (giảm cửa sổ mất token khi app bị tắt — RK-M1).
- **RM-S3.** Làm mới **single-flight** (một `Future` dùng chung trong tiến trình); chủ động làm mới 60 giây trước hạn (Timer) và khi app trở lại foreground (`AppLifecycleState.resumed`) nếu còn ≤ 60 giây. Lỗi **mạng/5xx** khi refresh ⇒ thử lại tối đa 2 lần sau 1 s, 3 s (nằm trong ân hạn 30 giây), vẫn lỗi ⇒ giữ phiên, báo "Không kết nối được máy chủ"; chỉ `401`/`403` mới là **mất phiên** (xoá kho, về đăng nhập với `reason=expired`).
- **RM-S4.** Cài lại app trên iOS: Keychain còn token cũ. Lần chạy đầu (thiếu cờ `af.install.v1` trong `shared_preferences`) ⇒ **xoá sạch** khoá `af.auth.*` của secure storage rồi đặt cờ. Android: `android:allowBackup="false"` (khôi phục backup chứa dữ liệu mã hoá bằng khoá Keystore cũ gây lỗi giải mã). Đọc kho lỗi (giải mã hỏng) ⇒ xoá kho, coi như chưa đăng nhập, không crash.
- **RM-S5.** Quyền đọc **chỉ** từ `GET /chinese/api/me` (như web). Mọi màn học yêu cầu `study.use`; tài khoản thiếu `study.use` ⇒ trang `/403` với lời giải thích + nút Đăng xuất. **[BA-mặc định]** (web cho vào trang chủ kèm Alert; mobile không có màn nào dùng được khi thiếu `study.use`).
- **RM-S6.** Người có `content.manage`/`users.manage`: mục "Thêm" hiện dòng thông tin "Quản trị nội dung và người dùng dùng bản web: https://chinese.antfarms.xyz" (quy tắc "ẩn theo quyền phải kèm giải thích"); **không** có màn quản trị.
- **RM-S7.** Đăng xuất: nếu outbox ôn thẻ của người này còn phần tử ⇒ hỏi xác nhận "Còn N đánh giá chưa gửi — đăng xuất sẽ bỏ chúng." Đồng ý ⇒ gọi `/mobile/logout` (tối đa 5 giây, lỗi bỏ qua) ⇒ xoá kho + outbox của user ⇒ huỷ mọi provider theo người dùng ⇒ về `/dang-nhap`.

### 3.3 Hành vi học tập trên mobile (khác/bổ sung so với web)

- **RM-L1. Outbox ôn thẻ bền**: lưu `shared_preferences` khoá `af.srs.outbox.<userId>` (JSON mảng), sống qua việc tắt app; cùng luật web (`reviewOutbox.ts`): `clientReviewId` sinh **một lần** lúc chấm, giữ nguyên mọi lần gửi lại; gửi **tuần tự**; lỗi mạng/5xx/408/429 ⇒ dừng, thử lại sau 1 s, 2 s, 5 s, 10 s rồi mỗi 30 s; 4xx khác ⇒ bỏ phần tử + báo; trùng id không thêm lần hai. Kích hoạt gửi: sau mỗi lần chấm, khi mở phiên, khi app resumed, khi đăng nhập xong (đúng user). Không dùng `connectivity_plus` **[BA-mặc định]**.
- **RM-L2.** Quiz, luyện thanh, luyện viết: id idempotent (`clientAttemptId`, `clientSessionId`) sinh **lúc bắt đầu lượt**, giữ trong bộ nhớ; lỗi mạng ⇒ nút "Gửi lại" cùng id (như web). Không lưu bền **[BA-mặc định]**.
- **RM-L3.** UUID v4 sinh bằng `Random.secure()` trong `af_core` (`uuidV4()`), **không** dùng package `uuid` (tinh thần quy tắc CLAUDE.md).
- **RM-L4.** "Hôm nay"/ngày hiển thị **luôn lấy từ server** (`localDate`, `timeZone`), không tự tính bằng đồng hồ máy.
- **RM-L5.** TTS: tự đọc chỉ trong handler thao tác người dùng hoặc khi thẻ mới hiện nếu `autoPlayAudio` (native cho phép; bản web dev có thể bị Chrome chặn trước lần chạm đầu — chấp nhận). Không có giọng `zh` ⇒ nút nghe vô hiệu + hướng dẫn cài giọng (nội dung như web §5.3.D HĐ45, rút gọn cho Android/iOS).
- **RM-L6.** Tab trong trang: **tab khởi đầu lấy từ query `tab`** (để liên kết từ "Việc hôm nay" như `/pinyin?tab=luyen`), sau đó là state cục bộ, **không** ghi lại URL **[BA-mặc định — lệch có chủ đích quy tắc `useTabParam` của web: app không có thanh địa chỉ]**.
- **RM-L7.** Rời màn đang làm dở (phiên ôn có outbox, luyện thanh chưa xong, quiz đã trả lời ≥ 1 câu chưa nộp, lượt viết đang vẽ) ⇒ `PopScope` hỏi xác nhận (nội dung như web).
- **RM-L8.** Pinyin hiển thị dạng dấu qua `lib/core/pinyin/pinyin.dart` (port 1-1 từ `frontend/apps/chinese/src/lib/pinyin.ts`); mọi chữ Hán hiển thị qua `HanziText` (locale `zh-CN` + phông CJK dự phòng).

---

## 4. Hiện trạng liên quan (kiểm 17/09/2026, `develop` @ `5366aa3`)

### 4.1 Backend identity-service

| File | Hiện trạng | Ảnh hưởng |
|---|---|---|
| `src/AntFarm.Identity.Api/Features/Auth/AuthController.cs` | `[Route("api/auth")]`, `[EnableRateLimiting("auth")]`, `[ValidateOrigin]` mức class; register/login/refresh/logout/password; đặt/xoá cookie theo `AuthOptions` | **Không đổi hành vi**; chỉ truyền `ClientContext.Web` vào service |
| `src/AntFarm.Identity.Api/Configuration/ValidateOriginAttribute.cs` | Kiểm `Origin` (fallback `Referer`) ∈ `AllowedOrigins`, 403 `ORIGIN_NOT_ALLOWED` | Giữ nguyên; M1 thêm `RejectBrowserOriginAttribute` riêng |
| `src/AntFarm.Identity.Api/Configuration/IdentityCorsExtensions.cs` | CORS credentials theo `AllowedOrigins` | Không đổi (mobile native không cần CORS; web-dev cùng origin) |
| `src/AntFarm.Identity.Api/Program.cs` | Rate limiter policy `auth` partition theo IP; `UseForwardedHeaders`; JSON camelCase + `WhenWritingNull` | Thêm policy `auth-mobile`, cảnh báo `MobileDevOrigins` |
| `src/AntFarm.Identity.Application/Accounts/AuthService.cs` | `RegisterAsync/LoginAsync(request, userAgent, ip)`, `RefreshAsync(cookieToken, ...)` khoá dòng `FOR UPDATE`, `LogoutAsync`, `GetActiveFamilyIdForAccountAsync` | Thêm tham số `ClientContext`, kiểm kênh, thu hồi họ khi logout mobile |
| `src/AntFarm.Identity.Application/Accounts/AuthDtos.cs` | `AuthResult(AccessToken, AccessTokenExpiresAt, RefreshTokenPlain, Account)`, `RefreshResult` | Thêm `RefreshTokenExpiresAt` |
| `src/AntFarm.Identity.Application/Common/Options/AuthOptions.cs` | `AllowedOrigins`, `RateLimitPermitPerMinute`, `RefreshReuseGraceSeconds`... | Thêm `MobileDevOrigins`, `MobileRateLimitPermitPerMinute` |
| `src/AntFarm.Identity.Domain/Accounts/RefreshToken.cs` | Không có kênh | Thêm `ClientType`, `ClientApp`, `DeviceName` |
| `src/AntFarm.Identity.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`, `Migrations/20260916175056_F2_Accounts` | Bảng `identity.refresh_tokens` | Migration `M1_MobileClient` |
| `tests/AntFarm.Identity.ApiTests/*` | Factory `IdentityDbApiFactory`, `...WithFakeClock`, `...LowRateLimit`; client test tự thêm `Origin: http://localhost:3280` | Test mobile tạo client **không** có `Origin` |

`deploy/conf/nginx.conf.example`: khối `id.antfarms.xyz` chuyển **mọi đường dẫn** → gateway `/identity/*` ⇒ `/api/auth/mobile/*` đi qua **không cần sửa nginx/gateway**. Khối `chinese.` có `location ^~ /chinese/` ⇒ app mobile gọi `https://chinese.antfarms.xyz/chinese/api/...` được ngay.

### 4.2 chinese-backend (không sửa)

Route thật (đều `[RequirePermission("study.use")]` trừ `/api/me`): `GET /api/me` · `GET|PUT /api/me/learning-settings` · `GET /api/progress/overview` · `GET /api/pinyin/{chart,guide,tone-stats}` + `POST /api/pinyin/tone-drills` · `GET /api/dictionary/search`, `/words/{id:guid}`, `/characters/{hanzi}` · `GET /api/srs/{summary,queue}`, `POST /api/srs/cards/{cardId:guid}/reviews`, `POST /api/srs/cards`, `PUT /api/srs/cards/{cardId:guid}/suspension` · `GET /api/lessons`, `GET /api/lessons/{slug}`, `POST /api/lessons/{id:guid}/start`, `POST|GET /api/lessons/{id:guid}/quiz-attempts` · `POST /api/writing/attempts`, `GET /api/writing/characters`, `/characters/{hanzi}`, `/summary` · `GET /api/system/info` (ẩn danh).

### 4.3 Frontend web — logic phải port sang Dart (kèm test tương ứng)

| Web (TS) | Số ca test web | Dart (app/package) |
|---|---|---|
| `packages/api/src/createApiClient.ts` (ApiError, thông điệp Việt, single-flight 401, điều hướng GET 403/404, `skipErrorRedirect`) | — | `af_core` `ApiClient` |
| `packages/auth/src/session.ts`, `AuthProvider.tsx`, `identityClient.ts`, `authErrors.ts`, `returnTo.ts` | — | `af_auth` |
| `packages/utils/src/{parseApiError,schemas,timeZones,relativeTime}.ts` | có | `af_core`/`af_auth` |
| `apps/chinese/src/lib/pinyin.ts` | 46 | `lib/core/pinyin/pinyin.dart` |
| `features/srs/lib/{reviewOutbox,sessionDeck,formatInterval,ratings}.ts` | 13+10+6 | `features/srs/domain/` |
| `features/pinyin/drill/{generateDrill,drillTypes}.ts`, `features/pinyin/api.ts#normalizeToneStats` | 11+6 | `features/pinyin/domain/` |
| `features/dictionary/lib/{pos,sources,characterReading}.ts` | 4+5 | `features/dictionary/domain/` |
| `features/lessons/lib/{inlineZh,quizScore,shuffle,displayPrefs}.ts` | 11+7+6 | `features/lessons/domain/` |
| `features/progress/lib/{heatmap,todayTasks,dates,labels}.ts` | 12+5+7+8 | `features/progress/domain/` |
| `features/writing/lib/{charData,setParam,nextChar,maskHanzi}.ts` | 13+9+7+5 | `features/writing/domain/` |
| `hanzi-writer@3.7.3` `dist/hanzi-writer.js` (MIT): `strokeMatches` (~dòng 625–800) + hình học (`subtract`, `magnitude`, `distance`, `length`, `cosineSimilarity`, `frechetDist`, `subdivideCurve`, `outlineCurve`, `normalizeCurve`, `rotate`, `extendStart`, lớp `Stroke`), `Positioner` | — | `features/writing/board/` |

Nguyên tắc port: **giữ nguyên hằng số, thứ tự, thông điệp tiếng Việt**; chép bộ ca test web sang Dart (ít nhất cùng số ca).

### 4.4 Dữ liệu nét chữ

`content/chinese/scripts/build-hanzi-data.mjs` chép tập con `hanzi-writer-data@2.0.1` (300 chữ, ~1,2 MB, tên file hex code point thường, `index.json`, `ARPHICPL.TXT`, `NOTICE.md`) vào `frontend/apps/chinese/public/hanzi-data/`. Định dạng mỗi file: `{ "strokes": [SVG path...], "medians": [[[x,y],...],...], "radStrokes"?: [...] }`, hệ toạ độ hộp 1024, trục y **ngược** (màn hình: `X = pad + x·s`, `Y = pad + (900 − y)·s`, `s = (size − 2·pad)/1024`).

### 4.5 Công cụ Flutter có sẵn (kiểm trong SDK 3.47.4)

`flutter_tools/lib/src/web/devfs_config.dart` đọc **`web_dev_config.yaml`** (thư mục chạy lệnh) với `server: { host, port, headers, proxy: [ { prefix|regex, target, replace? } ] }`; proxy dùng `shelf_proxy`, **giữ nguyên header** (kể cả `Origin`); lỗi kết nối tới target ⇒ **rơi xuống handler tĩnh** (trả `index.html`) — RK-M10.

---

## 5. Thiết kế giải pháp

### 5.1 Database

#### 5.1.1 identity-service — migration `M1_MobileClient` (schema `identity`)

Quy ước tên: migration của đợt mobile dùng tiền tố mã feature `M<n>_` (tương tự `F<n>_`). Chỉ identity-service có migration; `af_chinese` **không đổi**.

```sql
ALTER TABLE identity.refresh_tokens
  ADD COLUMN client_type varchar(16)  NOT NULL DEFAULT 'web',
  ADD COLUMN client_app  varchar(64)  NULL,
  ADD COLUMN device_name varchar(100) NULL;
ALTER TABLE identity.refresh_tokens
  ADD CONSTRAINT ck_refresh_tokens_client_type CHECK (client_type IN ('web', 'mobile'));
```

- Dòng cũ nhận `web` (đúng thực tế — trước M1 chỉ có web). Không thêm chỉ mục (tra cứu vẫn theo `token_hash` unique, `family_id`, `account_id`).
- Domain: `RefreshClientType` (enum `Web`, `Mobile`) lưu chuỗi `web`/`mobile` bằng `HasConversion` (hoặc hằng chuỗi — agent chọn, **giá trị DB phải đúng như trên**); `HasDefaultValue("web")` để migration sinh `DEFAULT`; `ToTable(..., t => t.HasCheckConstraint("ck_refresh_tokens_client_type", "client_type IN ('web', 'mobile')"))`.
- Lệnh (từ gốc repo):

```bash
dotnet ef migrations add M1_MobileClient \
  --project backend/services/identity-service/src/AntFarm.Identity.Infrastructure \
  --startup-project backend/services/identity-service/src/AntFarm.Identity.Api \
  --output-dir Persistence/Migrations
```

- Migration tự chạy lúc khởi động (`AutoMigrate`); kiểm trên DB dev **đang có dữ liệu F2** (dòng cũ thành `web`, đăng nhập web vẫn làm mới được).

#### 5.1.2 Lưu trữ trên thiết bị (không phải DB server)

| Nơi | Khoá | Nội dung | Ai ghi |
|---|---|---|---|
| `flutter_secure_storage` | `af.auth.session` | JSON `{ "v": 1, "refreshToken", "refreshTokenExpiresAt", "account": { id, email, displayName, timeZone } }` | `af_auth` |
| `shared_preferences` | `af.install.v1` | `true` (RM-S4) | `af_auth` |
| `shared_preferences` | `af.themeMode` | `system|light|dark` | `af_ui` |
| `shared_preferences` | `af.speech.voice.zh` | tên giọng đã chọn | `af_ui` |
| `shared_preferences` | `af.srs.outbox.<userId>` | JSON mảng `OutboxItem` (RM-L1) | app — srs |
| `shared_preferences` | `af.chinese.lesson.showPinyin` / `.showVi` | bool (mặc định true) | app — lessons |

Dùng `SharedPreferencesAsync` hoặc `SharedPreferencesWithCache` (API mới của 2.5.x; agent đọc tài liệu package trong pub cache). Mọi đọc/ghi bọc try/catch, lỗi ⇒ giá trị mặc định (không crash).

### 5.2 Backend (identity-service — agent `backend-implement`, Sonnet) — feature M1

#### 5.2.1 Danh sách file

| Lớp | File | Nội dung |
|---|---|---|
| Domain | `Accounts/RefreshToken.cs` | Thêm `ClientType`, `ClientApp`, `DeviceName`; `CreateNew(..., RefreshClientType clientType, string? clientApp, string? deviceName)`; token xoay kế thừa 3 trường từ cha |
| Domain | `Accounts/RefreshClientType.cs` | enum `Web`, `Mobile` |
| Application | `Accounts/ClientContext.cs` | `public sealed record ClientContext(RefreshClientType Type, string? ClientApp, string? DeviceName, string? UserAgent, string? Ip)` + `static ClientContext Web(string? ua, string? ip)` |
| Application | `Accounts/AuthService.cs` | `RegisterAsync(RegisterRequest, ClientContext, ct)`, `LoginAsync(LoginRequest, ClientContext, ct)`, `RefreshAsync(string? token, ClientContext, ct)` (kiểm kênh RM-A3 **sau** khi tìm thấy token, **trước** nhánh xoay/ân hạn; sai kênh ⇒ không commit thay đổi, ném 401), `LogoutAsync(string? token, RefreshClientType channel, ct)` (web và mobile: thu hồi CẢ HỌ, kể cả khi token gửi lên đã bị xoay — sửa 17/09/2026 theo review M2, tránh phiên "sống lại" do refresh song song; token đã bị thu hồi/không tồn tại/sai kênh ⇒ bỏ qua), `GetActiveFamilyIdForAccountAsync(accountId, token, RefreshClientType channel, ct)` (thêm điều kiện đúng kênh) |
| Application | `Accounts/AuthDtos.cs` | `AuthResult` và `RefreshResult` thêm `DateTime RefreshTokenExpiresAt`; `MobileRegisterRequest(Email, Password, DisplayName, TimeZone, DeviceName?)`, `MobileLoginRequest(Email, Password, DeviceName?)`, `MobileRefreshRequest(RefreshToken)`, `MobileLogoutRequest(RefreshToken)`, `MobileChangePasswordRequest(CurrentPassword, NewPassword, RefreshToken?)` |
| Application | `Accounts/AuthValidators.cs` | Validator mobile: dùng lại luật email/mật khẩu/tên/múi giờ; `DeviceName` ≤ 100; `RefreshToken` bắt buộc, đúng `^[0-9a-f]{64}$` (sai ⇒ 400 `VALIDATION`; lưu ý: ở `logout` token sai định dạng cũng chỉ 400) |
| Application | `Common/Options/AuthOptions.cs` | `string[] MobileDevOrigins = []`; `int MobileRateLimitPermitPerMinute = 30` |
| Infrastructure | `Persistence/Configurations/RefreshTokenConfiguration.cs` + migration `M1_MobileClient` | §5.1.1 |
| Api | `Features/Auth/MobileAuthController.cs` | `[ApiController] [Route("api/auth/mobile")] [EnableRateLimiting("auth-mobile")] [RejectBrowserOrigin] [RequireClientHeader]` — 5 action §6.1; **không** dùng `Request.Cookies`/`Response.Cookies`; đặt `Cache-Control: no-store`, `Pragma: no-cache` cho response có token |
| Api | `Features/Auth/MobileAuthResponses.cs` | `MobileAuthResponse(AccessToken, AccessTokenExpiresAt, RefreshToken, RefreshTokenExpiresAt, Account)`, `MobileRefreshResponse(AccessToken, AccessTokenExpiresAt, RefreshToken, RefreshTokenExpiresAt)` |
| Api | `Configuration/RejectBrowserOriginAttribute.cs` | RM-A4 (`IAsyncActionFilter`; đọc `IHostEnvironment`, `AuthOptions`) — log Warning khi chặn |
| Api | `Configuration/RequireClientHeaderAttribute.cs` | RM-A5; đặt giá trị đã kiểm vào `HttpContext.Items["af.client"]` cho controller dùng |
| Api | `Features/Auth/AuthController.cs` | Chỉ đổi lời gọi service sang `ClientContext.Web(...)` / `RefreshClientType.Web`. **Không đổi route, filter, cookie** |
| Api | `Program.cs` | `options.AddPolicy("auth-mobile", …)` giống `auth` nhưng `PermitLimit = authOptions.MobileRateLimitPermitPerMinute`; sau `Build()`: nếu `!IsDevelopment() && MobileDevOrigins.Length > 0` ⇒ `Log.Warning("Auth:MobileDevOrigins bị bỏ qua ngoài Development")` |
| Api | `appsettings.json` | `"MobileDevOrigins": []`, `"MobileRateLimitPermitPerMinute": 30` trong `Auth` |
| Api | `appsettings.Development.json.example` | `"MobileDevOrigins": ["http://localhost:3291"]` (cổng web-dev của app mobile §5.3.9) |
| Tests | `tests/AntFarm.Identity.ApiTests/Auth/MobileAuthTests.cs`, `MobileRefreshReuseTests.cs` (đồng hồ giả), `MobileChannelIsolationTests.cs`, `MobileOriginAndHeaderTests.cs`, `MobileRateLimitTests.cs`; `tests/AntFarm.Identity.UnitTests/Accounts/RefreshTokenTests.cs` (kế thừa kênh) | §5.2.3 |

Controller mỏng: map header/body → `ClientContext(Mobile, clientApp, deviceName, userAgent, ip)` → service → response. `ChangePassword` mobile gọi `accountService.ChangePasswordAsync(accountId, cur, new, familyId, ct)` như web, với `familyId = GetActiveFamilyIdForAccountAsync(accountId, body.RefreshToken, Mobile)`.

#### 5.2.2 Luồng refresh (sửa từ HĐG §5.2.2)

```
hash = sha256(token)                              # web: cookie; mobile: body.refreshToken
BEGIN; t = ... WHERE token_hash = hash FOR UPDATE
không có t / revoked / hết hạn           ⇒ 401 REFRESH_INVALID
t.client_type ≠ kênh gọi                  ⇒ ROLLBACK, log Warning sai kênh, 401 REFRESH_INVALID   (MỚI)
t.rotated_at ≠ null:
   now − rotated_at ≤ grace ⇒ cấp token mới cùng family (kế thừa client_type/app/device)
   ngược lại ⇒ thu hồi họ (reuse_detected), COMMIT, 401
account không active ⇒ thu hồi họ (account_disabled), COMMIT, 403 ACCOUNT_DISABLED
bình thường ⇒ xoay; t2 kế thừa kênh; COMMIT; trả access + t2 (+ hạn t2)
```

Web controller: nhánh `catch (AppException)` xoá cookie giữ nguyên. Mobile controller: không có gì để xoá — app tự xoá kho khi nhận 401/403.

#### 5.2.3 Test M1 (bắt buộc; `[DbFact]` dùng `af_identity_test`)

1. Mobile register ⇒ 201, body có `refreshToken` (64 hex) + `refreshTokenExpiresAt` ≈ now+30d; **không** có header `Set-Cookie`; có `Cache-Control: no-store`; DB dòng `client_type='mobile'`, `client_app` = header, `device_name` = body.
2. Mobile login đúng/sai mật khẩu (401 `INVALID_CREDENTIALS`), 10 lần sai ⇒ 423, `AllowRegistration=false` ⇒ register 403 `REGISTRATION_CLOSED`.
3. Mobile refresh xoay: token cũ dùng lại **trong** 30 giây ⇒ 200 (cùng family); **sau** 31 giây (TimeProvider giả) ⇒ 401 + mọi token active của family bị thu hồi `reuse_detected`.
4. Cô lập kênh: token web (lấy từ cookie register web) gửi tới `/mobile/refresh` ⇒ 401 và token web **vẫn** làm mới được qua cookie sau đó; token mobile gửi làm cookie `af_rt` tới `/api/auth/refresh` (kèm `Origin` hợp lệ) ⇒ 401 và token mobile **vẫn** làm mới được qua `/mobile/refresh`.
5. Cookie bị lờ: gửi `/mobile/refresh` với body token mobile hợp lệ **và** header `Cookie: af_rt=<token web>` ⇒ 200, token web không bị xoay/thu hồi.
6. Origin: request có `Origin: https://evil.example` ⇒ 403 `ORIGIN_NOT_ALLOWED` (cả 5 endpoint); `Origin: http://localhost:3291` ⇒ cho qua khi môi trường Development + có trong `MobileDevOrigins`, **bị chặn** khi môi trường `Production` (factory đặt `UseEnvironment("Production")` + cấu hình tối thiểu); không `Origin` ⇒ cho qua.
7. Header `X-AF-Client`: thiếu / `abc` / `chinese-mobile/1.0 (windows)` ⇒ 400 `VALIDATION` có khoá `X-AF-Client`; `chinese-mobile/1.0.0+1 (ios)` ⇒ qua.
8. Logout mobile ⇒ 204, mọi token của family bị thu hồi `logout`, refresh sau đó 401; logout token lạ ⇒ 204; logout token web qua mobile ⇒ 204 và token web vẫn sống.
9. Đổi mật khẩu mobile (Bearer + refreshToken đúng) ⇒ `currentSessionKept=true`, family mobile hiện tại còn sống, family web khác bị thu hồi `password_changed`; không gửi refreshToken ⇒ `currentSessionKept=false`, mọi family bị thu hồi; sai mật khẩu hiện tại ⇒ 422 `WRONG_PASSWORD`.
10. Rate limit: factory permit thấp (vd 3) ⇒ request thứ 4 trong phút ⇒ 429 `RATE_LIMITED`; policy `auth` và `auth-mobile` **đếm riêng**.
11. Hồi quy web: toàn bộ test F2/F4 hiện có vẫn xanh (đặc biệt `CookieConfigTests`, `CorsAndOriginTests`, `RefreshReuseDetectionTests`, `ChangePasswordTests`).
12. Token mobile được `AddAfJwtBearer` audience `af-chinese` chấp nhận (dùng lại mẫu `JwksAndTokenTests`).
13. Migration áp được trên DB có dữ liệu F2: dòng cũ `client_type='web'`.

### 5.3 Mobile — kiến trúc chung (agent `frontend-implement`, **model Fable**)

> **Phân công**: mọi việc trong `mobile/` do agent `frontend-implement` làm — Orchestrator gọi công cụ `Agent` với `subagent_type: "frontend-implement"` và **luôn truyền `model: "fable"`**; lời giao việc phải nói rõ "**đây là Flutter/Dart trong `mobile/`** — bỏ qua quy tắc React/MUI/yarn, theo §5.3 hợp đồng mobile". Việc `content/` (M10.1) do `content-implement` (Sonnet); việc identity (M1) do `backend-implement` (Sonnet).

#### 5.3.1 Monorepo: pub workspaces, **không melos** [BA-mặc định]

Đánh giá: Dart pub workspaces (Dart ≥ 3.6) đã giải quyết phần cốt lõi — **một** `flutter pub get` ở gốc giải phụ thuộc cho mọi package, một `pubspec.lock`, package nội bộ tham chiếu bằng tên (không `path:` lặp lại, không `pubspec_overrides.yaml`). Melos 8.7.0 hỗ trợ workspaces và thêm chạy lệnh theo package/lọc thay đổi, nhưng phải cài global (`dart pub global activate`) hoặc làm dev dependency, thêm một lớp cấu hình để agent phải học. Với 1 app + 4 package, **script shell `tool/ci.sh` là đủ**. Xem lại khi có ≥ 2 app hoặc cần publish (ghi quyết định mới).

```
mobile/
  pubspec.yaml                # workspace root (name: antfarm_mobile, publish_to: none)
  pubspec.lock                # COMMIT
  analysis_options.yaml       # include: package:af_lints/analysis_options.yaml
  README.md                   # cách chạy dev, cổng, lệnh kiểm
  VERIFY-DEVICE.md            # checklist chạy trên Android/iOS thật (chưa verify)
  tool/
    ci.sh                     # cổng kiểm (§9.1)
    check_conventions.dart    # luật dự án (§5.3.4) — chỉ dùng dart:io
  packages/
    af_lints/   pubspec.yaml · lib/analysis_options.yaml
    af_core/    pubspec.yaml · lib/af_core.dart · lib/src/... · test/...
    af_auth/    pubspec.yaml · lib/af_auth.dart · lib/src/... · test/...
    af_ui/      pubspec.yaml · lib/af_ui.dart · lib/src/... · test/...
  apps/
    chinese/    pubspec.yaml (name: af_chinese) · web_dev_config.yaml · config/*.json
                android/ ios/ web/ · assets/ · lib/ · test/
```

Root `mobile/pubspec.yaml`:

```yaml
name: antfarm_mobile
publish_to: none
environment:
  sdk: ^3.13.0
workspace:
  - packages/af_lints
  - packages/af_core
  - packages/af_auth
  - packages/af_ui
  - apps/chinese
```

Mỗi package con có `resolution: workspace` và `environment: sdk: ^3.13.0` (app thêm `flutter: ">=3.47.0"`). **Không** thư mục `bin/` trong `mobile/` (gốc repo `.gitignore` có `bin/` của .NET — RK-M12); script để ở `tool/`.

**Tạo app** (M0, chạy trong `mobile/apps/`):

```bash
flutter create --platforms=android,ios,web --org xyz.antfarms --project-name af_chinese --description "AntFarm — học tiếng Trung" chinese
```

Sau đó sửa tay: Android `namespace` + `applicationId` = **`xyz.antfarms.chinese`** (`android/app/build.gradle.kts`), chuyển `MainActivity.kt` sang `xyz/antfarms/chinese/`; iOS `PRODUCT_BUNDLE_IDENTIFIER` = **`xyz.antfarms.chinese`** (mọi cấu hình trong `project.pbxproj`, test target `xyz.antfarms.chinese.RunnerTests`); xoá `test/widget_test.dart` mẫu; thêm `resolution: workspace`.

| Mục | Giá trị |
|---|---|
| Tên package Dart | `af_<ten>` cho package dùng chung; app `af_<ngon-ngu>` (`af_chinese`) |
| Bundle id / applicationId | `xyz.antfarms.<ngon-ngu>` (`xyz.antfarms.chinese`) |
| Tên hiển thị (launcher) | **"AntFarm Trung"** [BA-mặc định] — `CFBundleDisplayName`/`CFBundleName`, `android:label`; web `<title>AntFarm · Tiếng Trung (dev)</title>` |
| Tiêu đề trong app | "AntFarm · Tiếng Trung" |
| Phiên bản | `version: 0.1.0+1` trong `apps/chinese/pubspec.yaml` |
| minSdk / iOS target | giữ mặc định của Flutter 3.47 (không hạ thấp); nếu plugin đòi cao hơn thì nâng và ghi lý do |
| Android manifest (main) | `<uses-permission android:name="android.permission.INTERNET"/>` (**bắt buộc** — `flutter create` chỉ đặt ở debug/profile, RK-M4); `android:allowBackup="false"`; `<queries><intent><action android:name="android.intent.action.TTS_SERVICE"/></intent></queries>` (RK-M6) |
| Android debug | `src/debug/res/xml/network_security_config.xml` cho phép cleartext **chỉ** `10.0.2.2`, `localhost`, `127.0.0.1`; khai trong `src/debug/AndroidManifest.xml` (RK-M5). Release không cleartext |
| iOS Info.plist | `NSAppTransportSecurity` → `NSAllowsLocalNetworking = true` (chỉ localhost dev); `CFBundleLocalizations` = `vi`, `en`; `UIRequiresFullScreen` không bắt buộc |

#### 5.3.2 Phiên bản package (ghim chính xác, kiểm pub.dev 17/09/2026)

| Package | Version | Dùng ở | Ghi chú |
|---|---|---|---|
| `flutter_riverpod` | **3.4.3** | af_auth, af_ui, app | Không dùng codegen (`riverpod_annotation`/`riverpod_generator`) **[BA-mặc định]** — tránh build_runner; dùng `Provider`, `FutureProvider(.autoDispose/.family)`, `Notifier`, `AsyncNotifier` viết tay. API 3.x khác 2.x (vd `Ref` không generic, `autoDispose` mặc định cho provider có tham số?) ⇒ **agent đọc CHANGELOG/README trong pub cache trước khi viết** (RK-M16) |
| `go_router` | **18.0.1** | af_auth (redirect helper), app | `StatefulShellRoute.indexedStack` cho bottom nav |
| `dio` | **5.11.1** | af_core | |
| `flutter_secure_storage` | **11.2.0** | af_auth | iOS `KeychainAccessibility.first_unlock_this_device`; Android dùng mặc định của v11 (đọc README v11 — v10 đã bỏ `encryptedSharedPreferences`) |
| `shared_preferences` | **2.5.5** | af_core (`KeyValueStore`), af_ui | |
| `flutter_tts` | **4.2.5** | af_ui | Android/iOS/web |
| `flutter_timezone` | **5.1.0** | af_auth (đăng ký), app (hồ sơ) | Lấy IANA của máy + danh sách múi giờ. Apache-2.0 |
| `package_info_plus` | **10.2.1** | af_core | Phiên bản cho `X-AF-Client` |
| `path_parsing` | **1.1.0** | app (luyện viết) | Parse SVG path → `Path` (gói của nhóm Flutter, BSD) |
| `intl` | `any` | af_ui, app | SDK `flutter_localizations` ghim `^0.20.3` — để `any` cho resolver chọn đúng bản SDK |
| `flutter_localizations` | `sdk: flutter` | app | `vi`, `en` |
| `flutter_lints` | **6.0.0** | af_lints | Chọn thay `very_good_analysis` 11.0.0 **[BA-mặc định]** — VGA bắt `public_member_api_docs`… quá chặt, làm chậm agent; bù bằng luật bổ sung §5.3.3 |
| `mocktail` | **1.0.5** | dev mọi package | |
| `flutter_test` | `sdk: flutter` | dev | |

**Không dùng**: `uuid`, `http`, `connectivity_plus`, `stroke_order_animator`, `hive*`, `drift`, `sqflite`, `json_serializable`/`freezed` (model viết tay `fromJson` — **[BA-mặc định]**: ~40 lớp, không cần build_runner trong workspace; bù bằng test parse JSON mẫu cho **mọi** model), `google_fonts` (tải mạng lúc chạy).

Giấy phép đều thân thiện (MIT/BSD/Apache-2.0); agent xác nhận trong pub cache khi thêm và ghi vào `mobile/README.md` mục "Phụ thuộc".

#### 5.3.3 `af_lints`

`lib/analysis_options.yaml`:

```yaml
include: package:flutter_lints/flutter.yaml
analyzer:
  language:
    strict-casts: true
    strict-inference: true
    strict-raw-types: true
  errors:
    unawaited_futures: warning
  exclude: [build/**, "**/*.g.dart"]
linter:
  rules:
    - always_declare_return_types
    - avoid_dynamic_calls
    - avoid_print
    - avoid_relative_lib_imports
    - cancel_subscriptions
    - close_sinks
    - directives_ordering
    - prefer_const_constructors
    - prefer_final_locals
    - prefer_single_quotes
    - require_trailing_commas
    - unawaited_futures
    - use_build_context_synchronously
```

Mọi package và app: `analysis_options.yaml` = `include: package:af_lints/analysis_options.yaml` (thêm `af_lints` vào `dev_dependencies`).

#### 5.3.4 `tool/check_conventions.dart` (tương đương `lint:ui`)

Quét `packages/*/lib/**.dart`, `apps/*/lib/**.dart`; exit 1 khi có FAIL.

| Luật | Mức | Phát hiện |
|---|---|---|
| `raw-dialog` | FAIL | `showDialog(`, `showModalBottomSheet(`, `showCupertinoDialog(` trong `apps/` (miễn `packages/af_ui`) — phải dùng `showAfDialog`/`showAfBottomSheet` |
| `uuid-import` | FAIL | `package:uuid/` |
| `token-in-prefs` | FAIL | file vừa chứa `SharedPreferences` vừa chứa `refreshToken`/`accessToken` |
| `hardcoded-url` | FAIL | chuỗi `http://` hoặc `https://` trong `lib/` ngoài `lib/**/config/**` và ngoài hằng giấy phép/nguồn (`sources.dart`, `licenses.dart`) |
| `print-call` | FAIL | `print(` / `debugPrint(` ngoài `af_core/lib/src/log/` |
| `cjk-without-hanzitext` | WARN | chuỗi literal chứa ký tự CJK (`一-鿿`) trong file không import `HanziText`/`LangText` |

Kèm test nhanh trong `ci.sh`: tạo file tạm có `showDialog(` trong `apps/chinese/lib/` ⇒ script exit 1 ⇒ xoá (làm trong M0, không để trong ci).

#### 5.3.5 `af_core`

```
lib/af_core.dart                         # export
lib/src/config/app_config.dart           # AppConfig
lib/src/http/api_error.dart              # ApiError (+ isApiError)
lib/src/http/api_client.dart             # createApiClient(...) → Dio
lib/src/http/interceptors.dart           # auth/refresh, lỗi, JSON guard
lib/src/http/request_options_ext.dart    # skipErrorRedirect, skipAuthRefresh qua Options.extra
lib/src/json/json_read.dart              # readString/readInt/readDouble/readBool/readList/readMap (thiếu ⇒ null/mặc định)
lib/src/storage/key_value_store.dart     # bọc shared_preferences, try/catch, có InMemoryKeyValueStore cho test
lib/src/util/uuid_v4.dart                # uuidV4() — Random.secure()
lib/src/util/client_header.dart          # buildClientHeader(appSlug, version, platform) → 'chinese-mobile/0.1.0+1 (android)'
lib/src/log/af_log.dart                  # afLog(message) — debugPrint chỉ ở debug
test/  api_client_test.dart · api_error_test.dart · app_config_test.dart · uuid_v4_test.dart · json_read_test.dart
```

**`AppConfig`** (đọc `String.fromEnvironment`, truyền bằng `--dart-define-from-file`):

| Khoá | Ý nghĩa |
|---|---|
| `AF_ENV` | `dev` \| `prod` |
| `AF_GATEWAY_URL` | Dev: gốc gateway (`http://10.0.2.2:5280` Android emulator, `http://localhost:5280` iOS simulator / `adb reverse`). Rỗng + đang chạy web ⇒ `Uri.base.origin` (cùng origin qua proxy dev) |
| `AF_IDENTITY_API_URL` | Ưu tiên nếu có; prod `https://id.antfarms.xyz/api` |
| `AF_CHINESE_API_URL` | Ưu tiên nếu có; prod `https://chinese.antfarms.xyz/chinese/api` |

Suy ra: `identityApiUrl = AF_IDENTITY_API_URL ?: '$gateway/identity/api'`; `chineseApiUrl = AF_CHINESE_API_URL ?: '$gateway/chinese/api'`. Thiếu hết (native mà không có gateway) ⇒ app hiện màn "Thiếu cấu hình máy chủ — chạy với --dart-define-from-file=config/<...>.json". `AF_ENV=prod` mà URL không phải `https://` ⇒ màn lỗi cấu hình (không chạy tiếp). Hàm thuần `AppConfig.resolve(Map<String,String> env, {required bool isWeb, Uri? base})` để test.

**`ApiError`**: `message` (tiếng Việt), `status?`, `code?`, `details?` (`Map<String, Object?>`), `data?`. Thông điệp **y hệt** `toApiError` web: body có `error` ⇒ dùng; timeout ⇒ "Máy chủ phản hồi quá lâu, vui lòng thử lại."; không có response ⇒ "Không kết nối được máy chủ. Kiểm tra mạng hoặc dịch vụ chưa chạy."; 502/503/504 ⇒ "Dịch vụ chưa sẵn sàng (gateway không tới được service)."; 429, 404, 403, 401 như web. `fieldErrors(String field)` tiện đọc `details`.

**`createApiClient`**:

```dart
Dio createApiClient({
  required String baseUrl,
  Duration timeout = const Duration(seconds: 15),
  Map<String, String> headers = const {},        // vd X-AF-Client cho identity
  String? Function()? getAccessToken,
  Future<String> Function()? refresh,            // single-flight nằm ở af_auth; client vẫn gộp trong phạm vi client
  void Function()? onAuthLost,
  void Function(int status, RequestOptions req)? onErrorRedirect, // GET 403/404 (không skipErrorRedirect) ⇒ app điều hướng
});
```

- `Accept: application/json`; body map ⇒ JSON.
- Interceptor (1) auth: gắn `Authorization: Bearer`; 401 (trừ `skipAuthRefresh` và URL khớp `(^|/)auth/(mobile/)?(login|register|refresh|logout)(\?|$)`) ⇒ `refresh()` single-flight ⇒ gửi lại **một** lần với `skipAuthRefresh`; refresh lỗi `ApiError` 401/403 ⇒ `onAuthLost()`; lỗi khác ⇒ trả lỗi 401 gốc; lần gửi lại vẫn 401 ⇒ `onAuthLost()`. **Đăng ký interceptor refresh TRƯỚC interceptor chuyển lỗi** (bài học web).
- Interceptor (2) JSON guard: response 2xx của request mong JSON (mặc định) mà `content-type` không chứa `json` và body khác rỗng ⇒ lỗi `ApiError('Phản hồi máy chủ không hợp lệ — kiểm tra cấu hình AF_*_API_URL hoặc proxy dev.')` (RK-M10). 204 bỏ qua.
- Interceptor (3) lỗi: chuyển `DioException` → `ApiError`; GET 403/404 và không `skipErrorRedirect` ⇒ `onErrorRedirect(status, req)`.
- Test bằng `HttpClientAdapter` giả tự viết (không thêm `http_mock_adapter`): 401→refresh→retry thành công; hai request 401 đồng thời ⇒ `refresh` gọi **đúng 1 lần**; refresh 401 ⇒ `onAuthLost` 1 lần; URL `/auth/mobile/login` 401 ⇒ không refresh; GET 404 ⇒ `onErrorRedirect(404)`; POST 404 ⇒ không; `skipErrorRedirect` ⇒ không; body HTML 200 ⇒ ApiError JSON guard; thông điệp tiếng Việt từng loại.

#### 5.3.6 `af_auth`

```
lib/af_auth.dart
lib/src/models.dart               # Account, MobileAuthResponse, MobileRefreshResponse, ChangePasswordResult, MeInfo (permissions)
lib/src/token_store.dart          # abstract TokenStore { read(); write(StoredSession); clear(); } + SecureTokenStore + InMemoryTokenStore
lib/src/install_guard.dart        # RM-S4
lib/src/identity_client.dart      # IdentityClient(Dio): register, login, refresh, logout, changePassword (mobile), getAccount, updateAccount
lib/src/auth_session.dart         # AuthSession: token bộ nhớ, refresh single-flight + retry mạng (RM-S3), hẹn giờ, lifecycle, write-before-use (RM-S2)
lib/src/auth_controller.dart      # AuthController (Notifier<AuthState>) + authControllerProvider
lib/src/auth_state.dart           # sealed: AuthLoading | AuthAnonymous(reason?) | AuthAuthenticated(account, me) | AuthUnreachable(message)
lib/src/router/auth_redirect.dart # authRedirect(state, location) + refreshListenable
lib/src/validators.dart           # validateEmail/Password/DisplayName — thông điệp như @af/utils schemas.ts
lib/src/time_zones.dart           # alias (Asia/Saigon→Asia/Ho_Chi_Minh, Asia/Calcutta→Asia/Kolkata, Asia/Katmandu→Asia/Kathmandu, Asia/Rangoon→Asia/Yangon, Europe/Kiev→Europe/Kyiv), deviceTimeZone()
lib/src/auth_errors.dart          # map code → thông điệp màn đăng nhập (port authErrors.ts)
lib/src/pages/login_page.dart     # LoginPage(brand, onRegisterTap)
lib/src/pages/register_page.dart  # RegisterPage(brand)
lib/src/widgets/password_field.dart
test/ auth_session_test.dart · identity_client_test.dart · auth_controller_test.dart · auth_redirect_test.dart · validators_test.dart · time_zones_test.dart · token_store_test.dart
```

- `AuthController` nhận `loadMe: Future<MeInfo> Function()` do **app** cung cấp (app tiếng Trung gọi `GET /me`, fail-closed như `loadMe.ts`), `onSignedOut` hook (app xoá outbox, huỷ provider theo user).
- Khởi động: `install_guard` → đọc kho → không có ⇒ `AuthAnonymous`; có ⇒ `refresh` ⇒ `loadMe` ⇒ `AuthAuthenticated`. Refresh/`loadMe` lỗi mạng ⇒ `AuthUnreachable` (màn "Không kết nối được máy chủ" + Thử lại + Đăng xuất). `loadMe` 403 ⇒ vẫn `AuthAuthenticated` với `permissions` rỗng (router đưa `/403`).
- `login/register` ⇒ lưu kho (write-before-use) ⇒ `loadMe` ⇒ authenticated. Lỗi hiển thị tại chỗ: 401 "Email hoặc mật khẩu không đúng." · 423 kèm `lockedUntil` (giờ địa phương) · 403 `ACCOUNT_DISABLED`/`REGISTRATION_CLOSED` · 409 `EMAIL_TAKEN` (dưới ô email) · 422 `INVALID_TIME_ZONE` · 429 · lỗi mạng.
- `refreshSession()`: refresh ⇒ cập nhật `account` từ claim JWT (`sub`, `email`, `name`, `zoneinfo` — port `accountFromToken`, không kiểm chữ ký) ⇒ `reloadMe()` (R4-4).
- `hasPermission(code)`.
- `RegisterPage` gửi `timeZone` = `deviceTimeZone()` (flutter_timezone, quy alias, lỗi ⇒ `Asia/Ho_Chi_Minh`), `deviceName` = model máy **không** (tránh thêm plugin) ⇒ `null`; **[BA-mặc định]** gửi `deviceName` = `"<platform> app"` (`Android app`/`iOS app`/`Web dev`).
- Màn đăng nhập/đăng ký: một cột, `AutofillGroup` + `autofillHints` (email, password, newPassword), bàn phím email, nút hiện/ẩn mật khẩu, nút chính cao ≥ 48, `reason=expired` ⇒ banner "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại."; `reason=password-changed` ⇒ banner tương ứng.
- Test `AuthSession`: 3 lời gọi đồng thời ⇒ 1 refresh; lỗi mạng 2 lần rồi thành công ⇒ không mất phiên; 401 ⇒ kho bị xoá + sự kiện lost; thứ tự ghi kho trước khi trả token (store giả ghi log thứ tự); hẹn giờ refresh 60 s trước hạn (dùng `fake_async` có sẵn qua `flutter_test`/`package:fake_async` — nếu cần thêm dependency thì ghim bản hiện hành và ghi vào bảng §5.3.2).

#### 5.3.7 `af_ui`

```
lib/af_ui.dart
lib/src/theme/af_theme.dart          # buildAfTheme({required Brightness brightness, Color accent})
lib/src/theme/theme_mode_controller.dart   # Notifier<ThemeMode>, lưu af.themeMode
lib/src/text/lang_text.dart          # LangText(text, locale, style) ; HanziText = LangText zh-CN + fallback CJK
lib/src/layout/af_shell_scaffold.dart# bottom NavigationBar ≤ 5 đích + badge; AppBar tiêu đề
lib/src/layout/sticky_action_bar.dart# thanh đáy dính, SafeArea
lib/src/feedback/dialogs.dart        # showAfDialog (barrierDismissible mặc định false), showAfConfirm → Future<bool>, showAfBottomSheet (isDismissible mặc định false; tham số closeOnBarrier tường minh)
lib/src/feedback/toast.dart          # showAfToast(context, msg, {kind})
lib/src/feedback/error_view.dart     # ErrorView(kind: unauthorized|forbidden|notFound|network|unknown, message, onRetry, actions) — tương đương ErrorPage
lib/src/feedback/async_view.dart     # AsyncValueView<T>(value, data:, onRetry:) — loading skeleton/lỗi ApiError chuẩn
lib/src/feedback/empty_state.dart
lib/src/cards/section_card.dart
lib/src/speech/af_tts.dart           # AfTts (bọc flutter_tts)
lib/src/speech/speech_controller.dart# SpeechState {status: loading|ready|noVoice|unsupported, voices, voice, speaking} + speak/cancel/setVoice
lib/src/speech/voice_missing_notice.dart   # hướng dẫn cài giọng (Android/iOS/web)
test/ theme_test.dart · lang_text_test.dart · dialogs_test.dart · speech_controller_test.dart (FlutterTts giả) · error_view_test.dart
```

**Theme** (bám `frontend/packages/ui/src/theme/buildTheme.ts`), Material 3, `ColorScheme.fromSeed(seedColor: primary, brightness)` rồi `copyWith` ép đúng màu:

| | Sáng | Tối |
|---|---|---|
| primary | `#2E7D32` (onPrimary `#FFFFFF`) | `#66BB6A` (onPrimary `#0B1410`) |
| secondary (accent app) | tiếng Trung `#C62828` | làm sáng accent (vd `#EF5350`) |
| surface / nền scaffold | `#FFFFFF` / `#FAFAF7` | `#1A201C` / `#101412` |
| bo góc | 10 | 10 |

Nút cao tối thiểu 48; `CardTheme` viền mảnh (outlined), không đổ bóng. Chế độ `system|light|dark` chọn ở Hồ sơ.

**Chữ Hán**: `HanziText` đặt `locale: Locale('zh','CN')` trong `TextStyle` **và** `fontFamilyFallback: ['PingFang SC', 'Noto Sans CJK SC', 'Noto Sans SC', 'Source Han Sans SC', 'Microsoft YaHei']`; không đóng gói phông **[BA-mặc định]** (RK-M7). Ngôn ngữ ứng dụng `locale: Locale('vi')`, `supportedLocales: [vi, en]`, `GlobalMaterialLocalizations/Widgets/Cupertino` delegates. Chuỗi giao diện viết thẳng tiếng Việt trong code (như web), **không** ARB ở đợt này **[BA-mặc định]**.

**TTS (`AfTts`)**:
- `init()`: `awaitSpeakCompletion(true)`; iOS `setSharedInstance(true)` + `setIosAudioCategory(IosTextToSpeechAudioCategory.playback, [mixWithOthers], IosTextToSpeechAudioMode.defaultMode)` (phát cả khi gạt im lặng — [BA-mặc định]); lấy `getVoices` (list map `name`, `locale`), lọc `locale` chuẩn hoá (`_`→`-`, lower) bắt đầu bằng `zh`; sắp: khớp đúng `zh-cn` trước, rồi tên chứa `Enhanced|Premium|Natural`, rồi còn lại. Rỗng ⇒ thử `isLanguageAvailable('zh-CN')` ⇒ `true` thì `ready` với giọng mặc định engine, ngược lại `noVoice`. Plugin ném `MissingPluginException` ⇒ `unsupported`.
- `speak(text, {double rate = 0.8})`: `stop()` trước; `setLanguage('zh-CN')`, `setVoice` nếu có; **quy đổi tốc độ** theo nền tảng: web `rate` giữ nguyên (1.0 = bình thường); **Android và iOS** `clamp(0.5 × rate, 0.1, 1.0)` — iOS vì AVSpeech 0.5 = bình thường, Android vì flutter_tts 4.2.5 tự nhân 2 (`FlutterTtsPlugin.kt` `setSpeechRate(rate * 2.0f)`) — sửa 17/09/2026 theo M3, đã đối chiếu mã plugin [BA-mặc định, kiểm trên máy thật — VERIFY-DEVICE]. Lỗi phát ⇒ nuốt, trả về (không ném lên UI).
- Giọng đã chọn lưu `af.speech.voice.zh`. `ttsRate` lấy từ `learning-settings` (app truyền vào).
- `VoiceMissingNotice`: Android "Cài đặt → Hệ thống → Ngôn ngữ → Đầu ra chuyển văn bản sang lời nói → Dịch vụ của Google → Cài dữ liệu giọng nói → Tiếng Trung"; iOS "Cài đặt → Trợ năng → Nội dung được đọc → Giọng nói → Tiếng Trung"; web dev "Dùng Chrome/Edge có giọng tiếng Trung".

#### 5.3.8 App `apps/chinese` — cấu trúc, điều hướng, quyền, lỗi

```
lib/main.dart                  # WidgetsFlutterBinding; AppConfig.resolve; runApp(ProviderScope(child: ChineseApp()))
lib/app.dart                   # MaterialApp.router: theme sáng/tối, locale vi, router
lib/config/                    # (chỉ nơi được có URL) giải thích dart-define
lib/api/clients.dart           # identityDioProvider (header X-AF-Client), chineseDioProvider — gắn AuthSession
lib/router/router.dart         # GoRouter + redirect + routes
lib/router/routes.dart         # hằng đường dẫn
lib/shell/app_shell.dart       # StatefulShellRoute: 5 nhánh
lib/shell/more_page.dart       # "Thêm": Pinyin, Từ điển, Hồ sơ, Giấy phép & nguồn, dòng giải thích quản trị (RM-S6), Đăng xuất
lib/shell/coming_soon_page.dart# "Tính năng này sắp có trên ứng dụng — dùng bản web chinese.antfarms.xyz" (được thay dần ở M5–M10)
lib/core/pinyin/pinyin.dart    # port pinyin.ts (M3)
lib/core/widgets/              # PinyinText, SpeakButton, MeaningStatusChip, ... (M3+)
lib/core/session_scope.dart    # userScopeProvider (id user hiện tại) — provider theo user `ref.watch` để tự huỷ khi đổi người
lib/features/<module>/
    data/models.dart           # model + fromJson/toJson (viết tay, dùng json_read)
    data/<module>_api.dart     # lời gọi Dio
    domain/*.dart              # logic thuần (port từ web lib/) — có test
    application/providers.dart # Riverpod
    presentation/pages/*.dart
    presentation/widgets/*.dart
test/<module>/...              # test domain + parse model + widget test chính
test/fixtures/*.json           # JSON mẫu lấy đúng từ HĐ §6 / types.ts
```

**Route** (go_router, **hash URL mặc định** trên web dev [BA-mặc định]; không route nào bắt đầu bằng `/chinese` hoặc `/identity` — proxy dev nuốt):

| Đường dẫn | Màn | Nằm trong shell | Feature |
|---|---|---|---|
| `/dang-nhap?returnTo=&reason=` · `/dang-ky` | Đăng nhập / đăng ký (af_auth) | không | M2 |
| `/` | Tổng quan | nhánh 1 "Trang chủ" | M5 |
| `/on-tap` | Trang ôn tập | nhánh 2 "Ôn tập" (badge `dueNow + newAvailableToday`, tối đa 99) | M6 |
| `/on-tap/phien` | Phiên ôn (toàn màn hình, ẩn bottom nav) | không (root navigator) | M6 |
| `/bai-hoc` · `/bai-hoc/:slug?tab=noi-dung\|tu-vung\|quiz` | Bài học | nhánh 3 "Bài học" | M9 |
| `/luyen-viet?tab=hsk1\|bai-hoc\|can-luyen\|da-luyen&bai=` | Luyện viết | nhánh 4 "Luyện viết" | M10 |
| `/luyen-viet/:hanzi?tab=xem\|to-theo\|tu-viet&tu=` | Bảng viết (toàn màn hình) | không | M10 |
| `/them` | Thêm | nhánh 5 "Thêm" | M0 |
| `/pinyin?tab=huong-dan\|bang\|luyen&che-do=mot\|cap` | Pinyin | trong nhánh 5 (push) | M7 |
| `/tu-dien?q=` · `/tu-dien/:id` · `/tu-dien/chu/:hanzi` | Tra từ | trong nhánh 5 (push) | M8 |
| `/ho-so?tab=thong-tin\|mat-khau\|hoc-tap\|giao-dien` | Hồ sơ | trong nhánh 5 (push) | M4 |
| `/giay-phep` | Giấy phép & nguồn | trong nhánh 5 | M4 (bổ sung ở M8, M10) |
| `/401` `/403` `/404` · `errorBuilder` ⇒ 404 | `ErrorView` | không | M2 |

Thứ tự nav giống web (Trang chủ, Ôn tập, Bài học, Luyện viết, còn lại vào "Thêm"). Tham số `:hanzi` luôn `Uri.encodeComponent`.

**Redirect** (`authRedirect`, nghe `AuthController` qua `refreshListenable`):
- `AuthLoading` ⇒ màn splash (không điều hướng).
- `AuthUnreachable` ⇒ màn "Không kết nối được máy chủ" (Thử lại / Đăng xuất).
- `AuthAnonymous` & đích không phải `/dang-nhap|/dang-ky|/401|/404` ⇒ `/dang-nhap?returnTo=<đích>` (+ `reason=expired` nếu mất phiên).
- `AuthAuthenticated` & đang ở `/dang-nhap|/dang-ky` ⇒ `returnTo` (chỉ nhận đường dẫn nội bộ bắt đầu `/`, không `//` — port `returnTo.ts`) hoặc `/`.
- `AuthAuthenticated` thiếu `study.use` & đích ≠ `/403|/them|/ho-so|/giay-phep` ⇒ `/403` (trang có nút Đăng xuất + lời giải thích RM-S5).

**Lỗi 4xx** (`onErrorRedirect` của `chineseDio`): GET 403 ⇒ `router.go('/403')`; GET 404 ⇒ `router.push('/404')` (giữ nút quay lại) [BA-mặc định]; lời gọi nền (`/me`, badge ôn tập, trạng thái hệ thống) dùng `skipErrorRedirect`. Lời gọi ghi (POST/PUT) giữ lỗi để báo tại chỗ (`showAfToast` hoặc banner trong màn). 503 `CONTENT_UNAVAILABLE` ⇒ `ErrorView` "Học liệu chưa sẵn sàng — báo quản trị viên" (không điều hướng).

**Tải dữ liệu**: `FutureProvider.autoDispose` cho GET (tương đương React Query), `ref.keepAlive()` cho dữ liệu tĩnh (`pinyin chart/guide`, manifest nét); sau mutation ⇒ `ref.invalidate(...)` đúng danh sách như web (vd nộp quiz ⇒ lessons, lesson(slug), srs summary, progress overview, attempts). Danh sách có kéo-để-làm-mới. Tổng quan + badge làm mới khi app `resumed` và khi chọn lại tab. Mọi provider theo người dùng `watch(userScopeProvider)` ⇒ đăng xuất/đổi người tự huỷ cache.

**Mobile-first**: bố cục một cột, rộng tối đa 600 ở tablet/web (căn giữa); vùng chạm ≥ 48; nút hành động chính ở `StickyActionBar`; không cuộn ngang ngoài vùng chủ ý; hỗ trợ `textScaler` tới 1.3 không vỡ bố cục (kiểm widget test ở 360×740 và 1.3×).

#### 5.3.9 Chạy dev — bản web cùng origin với gateway (giải bài toán CORS)

Vấn đề: bản Flutter web chạy ở origin riêng; gọi `http://localhost:5280/chinese/api` sẽ bị CORS, mà quy tắc dự án cấm thêm CORS ở gateway/chinese-backend. **Giải pháp [BA-mặc định]: dùng proxy có sẵn của dev server Flutter (`web_dev_config.yaml`, §4.5)** — trình duyệt chỉ thấy một origin `http://localhost:3291`, dev server chuyển `/identity/*` và `/chinese/*` sang gateway (giống Vite proxy của web). Không cần script proxy riêng, giữ được hot reload.

`mobile/apps/chinese/web_dev_config.yaml` (commit):

```yaml
# Dev server Flutter web — CÙNG ORIGIN với API như Vite của frontend/apps/chinese.
# Trình duyệt → http://localhost:3291 → (proxy) gateway :5280 → service. Không thêm CORS ở gateway/service.
# Lưu ý: gateway tắt ⇒ proxy rơi về index.html (200 text/html) — af_core chặn bằng JSON guard.
server:
  host: localhost
  port: 3291
  proxy:
    - prefix: "/identity/"
      target: "http://localhost:5280/"
    - prefix: "/chinese/"
      target: "http://localhost:5280/"
```

Cổng **3291** = dải 32xx của app, dành cho "web dev của app mobile tiếng Trung" [BA-mặc định]; thêm vào bảng cổng CLAUDE.md. identity-service dev: `Auth:MobileDevOrigins = ["http://localhost:3291"]` (trình duyệt gửi `Origin` cả khi cùng origin). Cookie web `af_rt` (host-only `localhost`, không phân biệt cổng) có thể bị gửi kèm tới `/identity/api/auth/mobile/*` — endpoint mobile bỏ qua (RM-A1, test M1 #5).

`config/*.json` (commit, không bí mật):

```jsonc
// config/dev-web.json      — URL rỗng ⇒ dùng Uri.base.origin (cùng origin qua proxy)
{ "AF_ENV": "dev" }
// config/dev-android.json  — emulator: 10.0.2.2 = localhost của máy Mac
{ "AF_ENV": "dev", "AF_GATEWAY_URL": "http://10.0.2.2:5280" }
// config/dev-ios.json      — simulator dùng chung mạng máy Mac; máy Android thật: `adb reverse tcp:5280 tcp:5280` rồi dùng file này
{ "AF_ENV": "dev", "AF_GATEWAY_URL": "http://localhost:5280" }
// config/prod.json         — native gọi thẳng, không CORS
{ "AF_ENV": "prod", "AF_IDENTITY_API_URL": "https://id.antfarms.xyz/api", "AF_CHINESE_API_URL": "https://chinese.antfarms.xyz/chinese/api" }
```

Lệnh (sau khi chạy identity → chinese → gateway theo README gốc):

```bash
cd mobile && flutter pub get
cd apps/chinese
# Chrome có giao diện (người dùng/Orchestrator xem):
flutter run -d chrome --web-port 3291 --dart-define-from-file=config/dev-web.json
# Không giao diện (agent tự kiểm proxy):
flutter run -d web-server --web-port 3291 --dart-define-from-file=config/dev-web.json &
curl -s http://localhost:3291/chinese/api/system/info     # ⇒ JSON service=chinese-backend
curl -s http://localhost:3291/identity/api/system/info    # ⇒ JSON service=identity-service
curl -s -X POST http://localhost:3291/identity/api/auth/mobile/refresh \
  -H 'Origin: http://localhost:3291' -H 'X-AF-Client: chinese-mobile/0.1.0+1 (web)' \
  -H 'Content-Type: application/json' -d '{"refreshToken":"'"$(printf 'a%.0s' {1..64})"'"}'   # ⇒ 401 REFRESH_INVALID (M1 xong)
# Thiết bị (khi người dùng đã cài SDK — §10.5):
flutter run -d emulator-5554 --dart-define-from-file=config/dev-android.json
flutter run -d "iPhone 17" --dart-define-from-file=config/dev-ios.json
# Build kiểm biên dịch (không phát hành):
flutter build web --release --dart-define-from-file=config/dev-web.json
```

Nếu `flutter run -d web-server` không nạp `web_dev_config.yaml` (khác phiên bản) ⇒ agent **dừng và báo** (không tự thêm CORS vào gateway/service); phương án dự phòng: script `mobile/tool/dev_proxy.dart` (`shelf` + `shelf_proxy`, thêm vào dev_dependencies root) phục vụ `build/web` ở cổng 3291 và chuyển `/identity`, `/chinese` sang 5280.

`web/index.html`: `lang="vi"`, title "AntFarm · Tiếng Trung (dev)". Bản web **không bao giờ** được đóng gói Docker/đưa lên server (RK-M19).

### 5.4 Học liệu & giấy phép

#### 5.4.1 Dữ liệu nét chữ trên mobile — **đóng gói vào assets app** [BA-mặc định]

Phương án đã cân nhắc:

| Phương án | Ưu | Nhược | Chọn |
|---|---|---|---|
| A. `build-hanzi-data.mjs` ghi thêm bản sao vào `mobile/apps/chinese/assets/hanzi-data/` (commit, ~1,2 MB, nén trong APK/IPA còn ~350 KB) | Học offline, không phụ thuộc web/CDN, cùng phiên bản với web, nghĩa vụ Arphic rõ (kèm `ARPHICPL.TXT` trong mỗi bản sao) | Trùng dữ liệu trong git (chấp nhận, có kiểm đồng nhất) | **Chọn** |
| B. Tải từ `https://chinese.antfarms.xyz/hanzi-data/` lúc chạy + cache | Không trùng | Cần mạng lần đầu, dev phải proxy thêm, xử lý cache/phiên bản | Không |
| C. Symlink `assets` → `frontend/.../public/hanzi-data` | Không trùng | Flutter/Windows/git symlink không ổn định | Không |

Việc cho `content-implement` (M10.1):
- `content/chinese/scripts/build-hanzi-data.mjs`: thêm đích thứ hai `MOBILE_DEST_DIR = mobile/apps/chinese/assets/hanzi-data/` — **cùng nội dung từng byte** với đích web (mọi `<hex>.json`, `index.json`, `ARPHICPL.TXT`, `NOTICE.md`), xoá `*.json` cũ trước khi chép; nếu thư mục `mobile/apps/chinese` không tồn tại ⇒ bỏ qua đích mobile kèm WARN (không exit 1).
- `validate.mjs`: nếu `mobile/apps/chinese/assets/hanzi-data/` tồn tại ⇒ so **từng file** với bản web (tên + byte), lệch/thiếu/thừa ⇒ FAIL "chạy lại build:hanzi-data:chinese".
- `content/chinese/SOURCES.md` dòng `hanzi-writer-data`: cột File thêm `mobile/apps/chinese/assets/hanzi-data/*`; thêm dòng `hanzi-writer (thuật toán chấm nét — port sang Dart)` MIT, `mobile/apps/chinese/lib/features/writing/board/stroke_matcher.dart`, `mobile/apps/chinese/assets/licenses/hanzi-writer.LICENSE.txt`.
- Chép nguyên văn `frontend/node_modules/hanzi-writer/LICENSE` ⇒ `mobile/apps/chinese/assets/licenses/hanzi-writer.LICENSE.txt` (Fable làm trong M10.2 nếu content chưa làm).

App: `pubspec.yaml` khai `assets: [assets/hanzi-data/, assets/licenses/]`; đọc `rootBundle.loadString('assets/hanzi-data/<hex>.json')`, manifest `index.json` (cache module). `main.dart` đăng ký `LicenseRegistry.addLicense` cho: Arphic Public License (nội dung `ARPHICPL.TXT`, gói "hanzi-writer-data (Make Me a Hanzi, Arphic)"), MIT hanzi-writer (gói "hanzi-writer — thuật toán chấm nét"). Trang `/giay-phep`: danh sách nguồn học liệu giống `SourceAttribution` web (HSK 3.0, CC-CEDICT CC BY-SA 4.0, CVDICT – Phong Phan CC BY-SA 4.0, Unicode Unihan, Hán Việt AntFarm CC BY-SA 4.0, "Dữ liệu đã được chỉnh sửa", Make Me a Hanzi/Arphic) + nút "Giấy phép phần mềm" (`showLicensePage`).

#### 5.4.2 Học liệu khác

Không có học liệu mới. Pinyin/từ vựng/bài học lấy qua API (backend nạp từ `content/`). Nghĩa `machine` hiện chip "Chưa duyệt" như web (R-C4).

#### 5.4.3 Luyện viết — phương án kỹ thuật [BA-mặc định]

`hanzi-writer` là JS nên không dùng được trực tiếp. So sánh:

| Phương án | Đánh giá |
|---|---|
| `stroke_order_animator` 3.3.1 (pub.dev, **BSD-3-Clause**, dữ liệu cùng định dạng Make Me a Hanzi) | Giấy phép dùng được; nhưng bản cuối 25/05/2025, điểm pub 125/160, kéo theo `http` + `svg_path_parser` 1.1.2 (SDK `<3.0.0`, cũ), cơ chế chấm nét và ngưỡng khác hanzi-writer ⇒ **số lỗi/gợi ý lệch với web**, trong khi `masteryStatus` (R-W5) dựa trên `totalMistakes`/`hintsUsed` từ cả hai nền tảng |
| **Tự viết bằng `CustomPainter` + `path_parsing`, port thuật toán `strokeMatches` của hanzi-writer 3.7.3 (MIT)** | Cùng ngưỡng (`averageDistanceThreshold 350`, `COSINE 0`, `START_AND_END 250`, `FRECHET 0.4`, `MIN_LEN 0.35`, `leniency 1`, xoay `±π/16, ±π/32, 0`) ⇒ **hành vi và số liệu khớp web**; đọc assets cục bộ; không phụ thuộc gói cũ. Chi phí ~2 ngày, có test |

**Chọn phương án tự viết.** Phương án B (dự phòng, chỉ khi port thất bại sau khi review): `stroke_order_animator` 3.3.1 với dữ liệu nạp từ assets (không dùng tải mạng), ghi lệch số liệu vào HANDOFF. Chi tiết engine ở M10.2, màn hình ở M10.3 (§7); dữ liệu ở M10.1.

---

## 6. Hợp đồng API

Đường dẫn **service** (app ghép: identity `AF_IDENTITY_API_URL` = prod `https://id.antfarms.xyz/api`, dev `<gateway>/identity/api`; tiếng Trung `AF_CHINESE_API_URL` = prod `https://chinese.antfarms.xyz/chinese/api`, dev `<gateway>/chinese/api`). Lỗi `{ "error", "code", "details"? }` (HĐG §6.0). Trường `null` **bị lược** khỏi JSON (`WhenWritingNull`) ⇒ `fromJson` coi thiếu = `null`.

### 6.1 M1 — identity-service, endpoint mobile (MỚI)

Chung cho mọi endpoint dưới `/api/auth/mobile`: header **bắt buộc** `X-AF-Client` (RM-A5); không có `Origin` (hoặc origin dev hợp lệ ở Development — RM-A4); rate limit `auth-mobile`; không cookie.

Lỗi chung: `400 VALIDATION` (kể cả header) · `403 ORIGIN_NOT_ALLOWED` · `429 RATE_LIMITED`.

**`POST /api/auth/mobile/register`** (ẩn danh)

```json
// request
{ "email": "ban@vidu.com", "password": "matkhau-dai-it-nhat-8", "displayName": "Quân",
  "timeZone": "Asia/Ho_Chi_Minh", "deviceName": "Android app" }
// 201   (Cache-Control: no-store — KHÔNG có Set-Cookie)
{ "accessToken": "eyJ...", "accessTokenExpiresAt": "2026-09-17T08:15:00Z",
  "refreshToken": "9f2c…(64 hex)", "refreshTokenExpiresAt": "2026-10-17T08:00:00Z",
  "account": { "id": "0192…", "email": "ban@vidu.com", "displayName": "Quân", "timeZone": "Asia/Ho_Chi_Minh", "createdAt": "2026-09-17T08:00:00Z" } }
```

Lỗi riêng: `403 REGISTRATION_CLOSED` · `409 EMAIL_TAKEN` · `422 INVALID_TIME_ZONE`.

**`POST /api/auth/mobile/login`** `{ "email", "password", "deviceName"? }` → `200` (như register) · `401 INVALID_CREDENTIALS` · `403 ACCOUNT_DISABLED` · `423 ACCOUNT_LOCKED` (`details.lockedUntil`).

**`POST /api/auth/mobile/refresh`**

```json
// request
{ "refreshToken": "9f2c…" }
// 200 (no-store)
{ "accessToken": "eyJ...", "accessTokenExpiresAt": "2026-09-17T08:30:00Z",
  "refreshToken": "41ab…(token MỚI — token cũ hết dùng sau ân hạn 30 s)", "refreshTokenExpiresAt": "2026-10-17T08:15:00Z" }
```

Lỗi: `401 REFRESH_INVALID` (không có / hết hạn / thu hồi / **sai kênh** / dùng lại ngoài ân hạn — app xoá kho) · `403 ACCOUNT_DISABLED` (app xoá kho).

**`POST /api/auth/mobile/logout`** `{ "refreshToken": "…" }` → `204` luôn (trừ 400/403/429 chung). Không cần Bearer.

**`POST /api/auth/mobile/password`** (Bearer audience `af-identity`)

```json
// request
{ "currentPassword": "…", "newPassword": "…", "refreshToken": "41ab…" }   // refreshToken tuỳ chọn
// 200
{ "otherSessionsRevoked": 2, "currentSessionKept": true }
```

Lỗi: `401 UNAUTHENTICATED` · `422 WRONG_PASSWORD` · `422 PASSWORD_UNCHANGED`. `currentSessionKept=false` ⇒ app đăng xuất cục bộ, về `/dang-nhap?reason=password-changed`.

**Không đổi** (mobile dùng lại): `GET /api/account` → `{ id, email, displayName, timeZone, createdAt }`; `PUT /api/account` `{ displayName, timeZone }` → account · `422 INVALID_TIME_ZONE` (Bearer, HĐ45 §6.2). `GET /api/system/info` (ẩn danh).

**Không đổi** luồng web: `POST /api/auth/{register,login,refresh,logout,password}` (cookie + `Origin`).

### 6.2 chinese-backend — API app dùng (không đổi, tóm tắt + nơi tra đầy đủ)

| Feature | Endpoint | Tra đầy đủ | Model Dart (port từ) |
|---|---|---|---|
| M2 | `GET /me` → `{ id, email, displayName, timeZone, roles[], permissions[], firstSeenAt }` | HĐG §6.3 | `features/auth/types.ts`, `loadMe.ts` (fail-closed) |
| M0 | `GET /system/info` → `{ service, version, environment, serverTimeUtc }` | HĐG §6.1 | `features/system/types.ts` |
| M4 | `GET|PUT /me/learning-settings` `{ dailyNewCards 0..50, dailyReviewLimit 10..1000, desiredRetention 0.80..0.97, ttsRate 0.5..1.2, autoPlayAudio }` (+ `isDefault` khi GET) | HĐ67 §6.2 | `features/srs/types.ts` |
| M5 | `GET /progress/overview` (khối `srs`, `dailyGoal`, `vocabulary`, `lessons`, `writing`, `tone` có thể vắng) | HĐ811 §6.4 | `features/progress/types.ts` |
| M6 | `GET /srs/summary` · `GET /srs/queue?limit=20` (1..50) · `POST /srs/cards/{cardId}/reviews` `{ clientReviewId, rating: again\|hard\|good\|easy, durationMs 0..600000 }` → `{ reviewId, duplicate, card, summary }` · lỗi `404`, `409 CLIENT_REVIEW_ID_CONFLICT`, `422 CARD_SUSPENDED\|NEW_CARD_LIMIT_REACHED` | HĐ67 §6.2 | `features/srs/types.ts` |
| M6 | `GET /dictionary/words/{id}` (sheet "Xem chi tiết") | HĐ67 §6.1 | `features/dictionary/types.ts` |
| M7 | `GET /pinyin/chart` · `GET /pinyin/guide` · `GET /pinyin/tone-stats` · `POST /pinyin/tone-drills` `{ clientSessionId, mode, startedAt, finishedAt (ISO có Z), items[{ parts[{syllable, hanzi, expectedTone, answeredTone}], responseMs, replayCount }] }` → 201/200 · lỗi `422 UNKNOWN_SYLLABLE\|TONE_NOT_AVAILABLE\|INVALID_SESSION_TIME`, `503` | HĐ45 §6.1 | `features/pinyin/types.ts`, `api.ts#normalizeToneStats` |
| M8 | `GET /dictionary/search?q=&hsk=&page=&pageSize=` · `GET /dictionary/words/{id}` (có `srs`) · `GET /dictionary/characters/{hanzi}` · `POST /srs/cards` `{ wordIds }` → `{ added, skipped, cards[] }` · `PUT /srs/cards/{cardId}/suspension` `{ suspended }` | HĐ67 §6.1–6.2 | `features/dictionary/types.ts` |
| M9 | `GET /lessons` → `{ items[], nextLessonSlug? }` · `GET /lessons/{slug}` (quiz **không** có đáp án) · `POST /lessons/{id}/start` · `POST /lessons/{id}/quiz-attempts` `{ clientAttemptId, startedAt, answers[{questionId, optionId}] }` → 201/200 `{ attemptId, submittedAt, total, correct, scorePercent, passed, passThresholdPercent, firstCompletion, srsCardsAdded, results[], progress }` · `GET /lessons/{id}/quiz-attempts?limit=5` · lỗi `409 DUPLICATE_ATTEMPT_ID`, `422 QUIZ_EMPTY\|QUIZ_CHANGED` | HĐ811 §6.1 | `features/lessons/types.ts` |
| M10 | `GET /writing/characters?set=hsk1\|lesson:<slug>\|weak\|practiced&page=&pageSize=60` · `GET /writing/characters/{hanzi}` · `GET /writing/summary` · `POST /writing/attempts` `{ clientAttemptId, hanzi, mode: guided\|recall, totalStrokes 1..64, totalMistakes 0..500, hintsUsed 0..200, durationMs 0..3600000 }` → `{ attemptId, completedAt, isClean, stats, becameMastered }` · lỗi `409 DUPLICATE_ATTEMPT_ID`, `422 UNKNOWN_CHARACTER` | HĐ811 §6.2 | `features/writing/types.ts` |

Thời điểm gửi lên: `DateTime.toUtc().toIso8601String()` (luôn có `Z`) — thiếu `Z` bị 400 (D38).

---

## 7. Phân rã feature

> Mỗi feature: code → cổng build/test (§9.1) → review (Opus) → integration (Opus, **commit local riêng** trên `develop`, **không push**) → dừng/hoặc chạy tiếp theo lệnh người dùng. Tiền tố commit: `feat(mobile): M<n> — …`, `feat(identity): M1 — …`, `feat(content): M10.1 — …`.
> Phân công: **Flutter = `frontend-implement` với `model: "fable"`** (ghi rõ "Flutter trong mobile/" khi giao); **backend = `backend-implement` (Sonnet)**; **content = `content-implement` (Sonnet)**; review/integration = Opus.

### Feature M0: Khung monorepo mobile + app chạy được

- **Mục tiêu:** `mobile/` dựng xong, app `af_chinese` chạy trên web-dev cùng origin với gateway, có theme sáng/tối, bottom nav 5 nhánh (trang tạm "Sắp có"), màn trạng thái hệ thống; cổng kiểm `tool/ci.sh` sạch.
- **Phạm vi (Fable):**
  - §5.3.1 toàn bộ (root workspace, `flutter create`, bundle id, tên hiển thị, manifest/Info.plist, `web_dev_config.yaml`, `config/*.json`).
  - `af_lints` (§5.3.3); `tool/check_conventions.dart` (§5.3.4) + `tool/ci.sh` (§9.1).
  - `af_core` **đầy đủ** (§5.3.5) — chưa có refresh thật (tham số để trống).
  - `af_ui` phần: theme + `ThemeModeController`, `LangText`/`HanziText`, `AfShellScaffold`, `StickyActionBar`, dialogs/toast/`ErrorView`/`AsyncValueView`/`EmptyState`/`SectionCard` (TTS để M3).
  - `af_auth`: **không** tạo ở M0 — tạo ở M2 (quy tắc "package chỉ tạo khi feature đầu tiên cần"); danh sách `workspace:` của root ở M0 chưa có `af_auth` (M2 thêm).
  - App: `main.dart`, `app.dart`, `router.dart` (chưa redirect auth), `app_shell.dart`, `more_page.dart` (mục chế độ tối tạm ở đây), `coming_soon_page.dart`, trang chủ tạm = **thẻ "Trạng thái hệ thống"**: gọi `GET <identity>/system/info` và `GET <chinese>/system/info` (`skipErrorRedirect`), chip "Tiếng Trung: đang chạy"/"Tài khoản: đang chạy" hoặc lỗi (tương đương F1).
  - `mobile/README.md` (yêu cầu, cài đặt, chạy dev web/thiết bị, cổng, cổng kiểm, cấu trúc, phụ thuộc + giấy phép), `mobile/VERIFY-DEVICE.md` (§9.3).
  - Gốc repo: `.gitignore` (§10.4), `CLAUDE.md` mục Mobile + bảng cổng (§10.4), `README.md` mục "Mobile (Flutter)" trỏ `mobile/README.md`, `.claude/agents/frontend-implement.md` thêm phần "Flutter (`mobile/`)" (§10.4), `docs/agents/AGENT-WORKFLOW.md` thêm dòng mobile.
- **API:** `GET /system/info` cả hai service.
- **Phụ thuộc:** không (song song với M1).
- **Tiêu chí hoàn thành + tự test:**
  1. `cd mobile && ./tool/ci.sh` exit 0 (format, analyze `--fatal-infos`, check_conventions, test mọi package, `flutter build web`).
  2. Thêm tạm `showDialog(` vào `apps/chinese/lib/` ⇒ `dart run tool/check_conventions.dart` exit 1 (rồi gỡ).
  3. Chạy 3 backend + `flutter run -d web-server --web-port 3291 …` ⇒ `curl` 2 `system/info` qua 3291 ra JSON; tắt chinese-backend ⇒ thẻ trạng thái báo lỗi, app không trắng.
  4. Widget test: shell hiện 5 nhãn nav đúng thứ tự; chuyển chế độ tối đổi `Theme.of(context).brightness`; màn 360×740 không overflow.
  5. `git status` không có `build/`, `.dart_tool/`, `local.properties`, `Pods/`, `Generated.xcconfig`.
  6. `applicationId`/`PRODUCT_BUNDLE_IDENTIFIER` = `xyz.antfarms.chinese` (grep). Build Android/iOS: **CHƯA VERIFY** (ghi trong commit + VERIFY-DEVICE).

### Feature M1: identity-service — phiên mobile

- **Mục tiêu:** client mobile đăng ký/đăng nhập/làm mới/đăng xuất/đổi mật khẩu bằng token trong body, an toàn, không ảnh hưởng web.
- **Phạm vi (backend-implement, Sonnet):** §3.1, §5.1.1, §5.2, §6.1. Cập nhật `deploy/VERIFY-DOCKER.md` (§10.4) và mục "5b. Xác thực" của `README.md` gốc (bảng endpoint mobile + `MobileDevOrigins`). Không đổi compose/nginx/gateway (đã kiểm §4.1) — ghi rõ trong commit.
- **Phụ thuộc:** không (song song M0).
- **Tiêu chí hoàn thành + tự test:**
  1. `dotnet build backend/backend.slnx -v q` 0 error; `AF_TEST_PG=... dotnet test backend/backend.slnx` xanh, báo số test chạy/skip; 13 nhóm test §5.2.3 có mặt.
  2. Chạy thật: `curl` login mobile (không Origin, có `X-AF-Client`) ⇒ 200 có `refreshToken`, không `Set-Cookie`; refresh 2 lần liên tiếp cách > 30 s bằng token cũ ⇒ 401; đăng nhập web ở `http://localhost:3280` vẫn F5 giữ phiên.
  3. `grep -rn "Cookies" …/MobileAuthController.cs` rỗng.
  4. Migration áp trên DB dev hiện có không lỗi; `SELECT client_type, count(*) FROM identity.refresh_tokens GROUP BY 1` ⇒ dòng cũ `web`.

### Feature M2: Đăng nhập, phiên, quyền, trang lỗi

- **Mục tiêu:** người học đăng ký/đăng nhập trên app, mở lại app vẫn giữ phiên (30 ngày trượt), mất phiên thì về đăng nhập đúng lý do; quyền từ `/me`; 401/403/404 thống nhất.
- **Phạm vi (Fable):** `af_auth` đầy đủ (§5.3.6); app: `api/clients.dart` gắn `AuthSession` (`refresh`, `onAuthLost`, `onErrorRedirect`), `loadMe` fail-closed, `router` redirect (§5.3.8), trang `/401` `/403` `/404`, splash, màn "Không kết nối được", `userScopeProvider`, mục "Đăng xuất" ở "Thêm" (RM-S7 — hook outbox để M6 điền), dòng giải thích quản trị (RM-S6), header `X-AF-Client` = `chinese-mobile/<version> (<android|ios|web>)`.
- **Màn & điều hướng:** `/dang-nhap` (email, mật khẩu, "Đăng nhập", liên kết "Tạo tài khoản") → `returnTo`/`/`; `/dang-ky` (tên hiển thị, email, mật khẩu, nhập lại; dòng "Múi giờ: Asia/Ho_Chi_Minh (theo máy)") → `/`.
- **API:** §6.1 (register/login/refresh/logout), `GET /me`.
- **Lỗi/offline:** §5.3.6 (RM-S3: mạng lỗi không đăng xuất; 401/403 mới mất phiên).
- **Phụ thuộc:** M0, M1.
- **Tiêu chí + tự test:**
  1. Unit test `af_auth` (§5.3.6) + test redirect (ẩn danh → `/dang-nhap?returnTo=/on-tap`; đăng nhập xong về `/on-tap`; `returnTo=//evil` ⇒ `/`; thiếu `study.use` ⇒ `/403`).
  2. Widget test LoginPage: lỗi 401 hiện "Email hoặc mật khẩu không đúng."; 423 hiện giờ mở khoá.
  3. Web-dev: đăng ký tài khoản mới ⇒ vào app; tải lại trang ⇒ vẫn đăng nhập (secure storage web); DevTools: không có token trong URL; `identity.refresh_tokens` có dòng `mobile`, `client_app` = `chinese-mobile/0.1.0+1 (web)`.
  4. Xoá dòng token trong DB (hoặc revoke) rồi chờ access hết hạn / gọi API ⇒ về `/dang-nhap` có banner "Phiên đăng nhập đã hết hạn".
  5. Tắt identity-service khi đang mở app, tải lại ⇒ màn "Không kết nối được máy chủ" (không đăng xuất); bật lại + Thử lại ⇒ vào được.
  6. Gỡ vai trò của user trong `af_chinese` ⇒ app hiện `/403` có nút Đăng xuất.
  7. Đăng xuất ⇒ `/mobile/logout` gọi, kho trống, token family bị thu hồi.

### Feature M3: Nền tiếng Trung — pinyin, chữ Hán, giọng đọc

- **Mục tiêu:** các màn học sau dùng chung: hiển thị pinyin dạng dấu đúng quy tắc web, chữ Hán đúng glyph giản thể, đọc TTS tiếng Trung với phát hiện giọng.
- **Phạm vi (Fable):**
  - `apps/chinese/lib/core/pinyin/pinyin.dart` — port **toàn bộ** `pinyin.ts` (`normalizeNumbered`, `parseSyllable`, `toneOf`, `stripTone`, `syllableToMarked`, `numberedToMarked({join})`, `splitPunctuation`, `stripPunctuation`, `markedToNumbered`, `displaySyllableKey`, `sandhiHints`, `TONE_MARKS`) + `test/core/pinyin_test.dart` chép **đủ 46 ca** của `pinyin.test.ts` (tối thiểu các ca HĐ45 §5.3.C).
  - `lib/core/widgets/pinyin_text.dart` (`PinyinText(value, {hanzi, showSandhi})`), `speak_button.dart` (dùng `speechControllerProvider`; không giọng ⇒ disabled + tooltip "Chưa có giọng tiếng Trung"), `meaning_status_chip.dart` ("Chưa duyệt" khi `machine`, tooltip như web), `hanzi_big.dart` (cỡ chữ theo ngữ cảnh).
  - `af_ui` speech (§5.3.7): `AfTts`, `SpeechController`, `VoiceMissingNotice`.
  - Màn thử nhanh trong "Thêm" → "Giọng đọc" (tạm, M4 chuyển vào Hồ sơ tab Giao diện): danh sách giọng `zh`, chọn giọng, thanh tốc độ 0,5–1,2, nút nghe thử "你好".
- **API:** không.
- **Phụ thuộc:** M0 (không cần M2 — làm song song M2 được).
- **Tiêu chí + tự test:** 46+ ca pinyin xanh; test `SpeechController` với `FlutterTts` giả (không giọng ⇒ `noVoice`; có `zh_CN` ⇒ chọn đúng; `MissingPluginException` ⇒ `unsupported`; quy đổi tốc độ iOS); widget test `HanziText` có `locale` `zh-CN`; web-dev Chrome trên macOS nghe được "你好" (hoặc hiện hướng dẫn).

### Feature M4: Hồ sơ & cài đặt

- **Mục tiêu:** người học đổi tên, múi giờ (quyết định "hôm nay"), mật khẩu; chỉnh cài đặt học tập, giọng đọc, giao diện; xem giấy phép.
- **Phạm vi (Fable):** `features/profile/` — `/ho-so` 4 tab (RM-L6):
  - *Thông tin*: email chỉ đọc ("Không đổi được email"); tên hiển thị (validator); múi giờ = ô chọn mở `showAfBottomSheet` có ô tìm (khớp không phân biệt hoa thường, `_` ≡ khoảng trắng) trên danh sách `FlutterTimezone.getAvailableTimezones()` (lỗi ⇒ danh sách dự phòng ~30 múi giờ phổ biến có `Asia/Ho_Chi_Minh`) đã quy alias, ghim đầu "Múi giờ của máy: X"; nhãn chỉ là ID (không tính offset) [BA-mặc định]; máy ≠ hồ sơ ⇒ banner + nút "Dùng múi giờ này"; dòng giải thích như web. Lưu: `PUT /account` ⇒ `refreshSession()` ⇒ toast "Đã lưu hồ sơ". 422 ⇒ lỗi dưới ô múi giờ.
  - *Mật khẩu*: 3 ô, `POST /mobile/password` kèm `refreshToken` hiện tại; kết quả như HĐ45 §5.3.F; `currentSessionKept=false` ⇒ đăng xuất cục bộ + `/dang-nhap?reason=password-changed`.
  - *Học tập*: port `LearningSettingsTab` (slider/ô số, switch, nút nghe thử 你好 với tốc độ đang chọn, "Lưu", lỗi 400 theo `details`); lưu xong invalidate `srs summary` + `learningSettingsProvider` (TTS đọc `ttsRate` từ đây).
  - *Giao diện*: chế độ Hệ thống/Sáng/Tối; giọng đọc (chuyển từ M3).
  - `/giay-phep` (§5.4.1, phần nguồn từ điển; M10 thêm Arphic/hanzi-writer).
- **API:** `GET/PUT /account`, `POST /mobile/password`, `GET/PUT /me/learning-settings`.
- **Phụ thuộc:** M2, M3.
- **Tiêu chí + tự test:** đổi múi giờ ⇒ `GET /me` trả múi giờ mới ngay (R4-4); đổi mật khẩu ⇒ phiên web khác bị thu hồi, app vẫn đăng nhập; test parse/serialize `LearningSettings`; widget test form mật khẩu (nhập lại không khớp ⇒ lỗi); gõ "ho chi" ra `Asia/Ho_Chi_Minh`.

### Feature M5: Tổng quan (trang chủ)

- **Mục tiêu:** mở app thấy chuỗi ngày học, mục tiêu ngày, việc hôm nay, lịch 90 ngày, tiến độ từ vựng/bài/viết/thanh (F11).
- **Phạm vi (Fable):** `features/progress/`:
  - `domain/`: port `heatmap.ts` (`levelOf` ngưỡng `[0, 1–9, 10–29, 30–59, ≥60]`, `buildHeatmap` tuần bắt đầu Thứ Hai, nhãn tháng), `todayTasks.ts` (R-PG9, `PINYIN_MIN_ANSWERED = 40`, đường dẫn đích giống web), `dates.ts`, `labels.ts` + test (≥ 12 + 5 + 7 + 8 ca như web).
  - `presentation/pages/dashboard_page.dart` thay trang chủ tạm; thẻ: `StreakCard`, `DailyGoalCard`, `TodayTasks`, `ActivityHeatmap` (lưới ô 16 px, gap 3; chạm ô ⇒ tooltip "dd/MM: N lượt"; cuộn ngang tới tuần hiện tại nếu hẹp), `VocabularyCard`, `LessonProgressCard`, `WritingProgressCard`, `ToneAccuracyCard`; thẻ "Trạng thái hệ thống" (từ M0) chỉ hiện khi có `users.manage`, thu gọn cuối trang.
  - Tiêu đề "Hôm nay, {thứ} {dd/MM}" theo `localDate`; múi giờ máy ≠ `overview.timeZone` ⇒ dòng nhỏ "Ngày học tính theo múi giờ hồ sơ ({timeZone}) — đổi ở Hồ sơ" (chạm ⇒ `/ho-so`).
  - Việc hôm nay trỏ tới route chưa làm ⇒ `ComingSoonPage` (đã có từ M0).
  - Badge nhánh "Ôn tập" chưa làm ở đây (M6).
- **API:** `GET /progress/overview`.
- **Trạng thái:** tải ⇒ skeleton; lỗi ⇒ `ErrorView` + Thử lại; khối vắng ⇒ ẩn; số 0 ⇒ lời mời (CTA); kéo để làm mới; tự làm mới khi app resumed/chọn lại tab.
- **Phụ thuộc:** M2 (M3 không bắt buộc).
- **Tiêu chí + tự test:** test domain; test parse `fixtures/progress_overview_full.json` và `_minimal.json` (chỉ `localDate`, `timeZone`, `streak`, `today`, `activity`); widget test: khối vắng không hiện, người mới thấy CTA; web-dev với tài khoản MVP có dữ liệu ⇒ số liệu **trùng** trang chủ web `http://localhost:3280/`.

### Feature M6: Ôn thẻ SRS (FSRS-6)

- **Mục tiêu:** ôn thẻ đến hạn + thẻ mới trên điện thoại, chịu được mạng chập chờn và tắt app, không mất/không nhân đôi đánh giá.
- **Phạm vi (Fable):** `features/srs/`:
  - `domain/`: port `formatInterval.ts` (ca bắt buộc HĐ67 §5.3.2), `sessionDeck.ts` (`mergeIncoming`, `shouldLoadMore` ngưỡng 5, `countRatings`, `formatSessionDuration`, `clampDurationMs`), `ratings.ts` (Quên/Khó/Được/Dễ, màu error/warning/success/info), `review_outbox.dart` (port `reviewOutbox.ts` thuần: `enqueue`, `isRetryableError`, `retryDelayMs`, `flushOutbox` tuần tự) + test (≥ 6 + 10 + 13 ca như web).
  - `application/outbox_controller.dart`: giữ danh sách trong bộ nhớ + ghi `KeyValueStore` khoá `af.srs.outbox.<userId>` sau **mỗi** thay đổi (RM-L1); hẹn giờ thử lại; flush khi enqueue / mở phiên / `resumed` / đăng nhập; phần tử bị bỏ (4xx) ⇒ toast (409/422 thông điệp server); đăng ký hook cho RM-S7 (số phần tử + xoá khi đăng xuất). Phản hồi thành công ⇒ cập nhật `srsSummaryProvider` bằng `summary` trả về.
  - `pages/review_home_page.dart` `/on-tap`: 3 thẻ số (Đến hạn hôm nay, Từ mới còn học được `x/dailyNewCards`, Đã ôn hôm nay); nút chính cao 56 "Bắt đầu ôn ({dueNow + newAvailableToday})"; bằng 0 ⇒ "Hôm nay xong rồi!" + "Lượt ôn kế tiếp: HH:mm dd/MM" — hiển thị theo **giờ máy** (`nextDueAt.toLocal()`), kèm dòng nhỏ "(giờ trên máy)" khi múi giờ máy ≠ `summary.timeZone` [BA-mặc định — Dart không có CSDL múi giờ sẵn; web dùng múi giờ hồ sơ; RK-M23]; "Từ vững: {matureCards}/500"; liên kết "Cài đặt học tập" ⇒ `/ho-so?tab=hoc-tap`; hết lượt ôn ⇒ banner như web; `PendingReviewsBanner` khi outbox > 0.
  - `pages/review_session_page.dart` `/on-tap/phien` (toàn màn hình): `SessionHeader` (X đóng, `LinearProgress`, "đã ôn/tổng"); mặt trước `HanziText` 72–96 + `SpeakButton` (tự đọc khi thẻ hiện nếu `autoPlayAudio`, không đọc lại khi lật), chip "Mới"/"Học lại"; nút "Hiện đáp án" (cao 56) ở `StickyActionBar`; chạm vào thẻ cũng lật [BA-mặc định]; mặt sau: `PinyinText` 24, Hán Việt in hoa, ≤ 3 nghĩa, `MeaningStatusChip`, "Xem chi tiết" ⇒ `showAfBottomSheet(closeOnBarrier: true /* chỉ đọc */)` hiện chi tiết từ (`GET /dictionary/words/{id}` — widget `WordDetailView` tái dùng ở M8); `RatingBar` 4 cột bằng nhau, mỗi nút cao ≥ 56, hai dòng (nhãn + `formatInterval`), khoá 300 ms sau khi chấm, `HapticFeedback.selectionClick()`.
  - Chấm: `durationMs` từ lúc thẻ hiện; `clientReviewId = uuidV4()`; lạc quan (sang thẻ kế ngay) + outbox. Bộ bài: `GET queue?limit=20`, còn ≤ 5 ⇒ tải thêm theo `mergeIncoming` (`skipNew` khi outbox còn phần tử lúc gọi); hết ⇒ `SessionSummary` (tổng thẻ, 4 mức dạng thanh, thời gian, "Về trang ôn tập", "Ôn tiếp" khi server còn thẻ).
  - Rời phiên khi outbox > 0 ⇒ `PopScope` + `showAfConfirm` "Còn N đánh giá chưa gửi. Rời đi vẫn giữ để gửi lại sau?" (outbox bền nên an toàn).
  - Badge nhánh "Ôn tập" (`skipErrorRedirect`, làm mới khi resumed và sau mỗi flush).
  - Điền hook đăng xuất RM-S7.
- **API:** `GET /srs/summary`, `GET /srs/queue`, `POST /srs/cards/{id}/reviews`, `GET /me/learning-settings`, `GET /dictionary/words/{id}`.
- **Lỗi/offline:** tải hàng đợi lỗi ⇒ `ErrorView` + Thử lại; 503 ⇒ "Học liệu chưa sẵn sàng"; mất mạng giữa phiên ⇒ vẫn chấm được các thẻ đã tải, banner chờ gửi; tắt app ⇒ mở lại, outbox tự gửi với **cùng** `clientReviewId`; `422 NEW_CARD_LIMIT_REACHED` ⇒ bỏ phần tử + toast + tải lại hàng đợi khi outbox trống.
- **Phụ thuộc:** M2, M3 (M4 để có cài đặt học tập — nếu chưa có, dùng `DEFAULT_LEARNING_SETTINGS`).
- **Tiêu chí + tự test:**
  1. Test domain + `OutboxController` với `InMemoryKeyValueStore` và API giả: mất mạng 3 lần rồi thành công ⇒ **cùng** `clientReviewId`, thứ tự giữ nguyên; khởi tạo controller mới đọc lại phần tử đã lưu (mô phỏng tắt app); outbox user A không gửi khi đăng nhập user B.
  2. Widget test phiên: lật ⇒ hiện RatingBar; chấm ⇒ thẻ kế; đóng khi outbox > 0 ⇒ hỏi.
  3. Web-dev: ôn 20 thẻ ⇒ `learning.srs_review_logs` tăng đúng 20, `study_events` 20; DevTools offline giữa phiên ⇒ chấm 3 thẻ ⇒ banner "Đang chờ gửi 3" ⇒ online ⇒ gửi, DB không trùng; tải lại trang khi còn outbox ⇒ vẫn gửi.
  4. Số "Đến hạn hôm nay" khớp web `/on-tap`.

### Feature M7: Pinyin & luyện thanh

- **Mục tiêu:** người số 0 học âm & thanh trên điện thoại, luyện nghe–chọn thanh bằng ngón cái, biết thanh yếu (G0).
- **Phạm vi (Fable):** `features/pinyin/`:
  - `domain/`: port `generateDrill.ts` (`buildTonePools`, `TONE_PAIR_COMBOS` loại 3-3, `generateDrill({mode, chart, focus, count = 20, random})` nhận `Random` để test tất định), `drillTypes.ts` (`computeResponseMs`, `summarizeByTone`, `RESPONSE_MS_MAX`), `normalizeToneStats` + test (≥ 11 + 6 ca).
  - `/pinyin` 3 tab (mặc định **Hướng dẫn**; RM-L6), nút "Cài đặt giọng đọc" ⇒ `/ho-so?tab=giao-dien`; `VoiceMissingNotice` ở đầu cả ba tab khi `noVoice/unsupported`.
  - *Hướng dẫn*: `ExpansionTile` theo `topics` (mở sẵn chủ đề đầu); block `paragraph`/`examples`/`tone_contour` (vẽ `CustomPainter` đường 5 mức 55/35/214/51 như `ToneContour.tsx`)/`compare` (2 cột, nút nghe từng bên); cuối tab nút "Sang bảng âm tiết".
  - *Bảng*: lưới thanh mẫu × vận mẫu trong vùng cuộn **hai chiều riêng** (hàng tiêu đề + cột đầu dính — dùng `TableView` của `two_dimensional_scrollables` **không** thêm; tự dựng bằng 2 `ScrollController` đồng bộ [BA-mặc định]); ô ≥ 48×44; chip lọc nhóm thanh mẫu/vận mẫu (mặc định điện thoại `tm=moi`); ô không có chữ minh hoạ màu mờ; chạm ô ⇒ bottom sheet chỉ đọc (`closeOnBarrier: true`, bình luận lý do) với 4 dòng thanh + "Nghe lần lượt" (cách 600 ms, dừng khi đóng).
  - *Luyện*: `DrillSetup` (SegmentedButton "Một âm tiết"/"Cặp thanh", dòng "tập trung thanh …", nút "Bắt đầu (20 câu)") + `ToneStatsCard` (4 thanh `LinearProgressIndicator` + `a/b câu`, nhầm lẫn tối đa 3 dòng, `g0Reached` ⇒ banner thành công); `DrillRunner` (tiến độ n/20, nút "Nghe" lớn, `ToneButtons` 4 nút cao ≥ 64 "1 ˉ"…"4 ˋ" ở nửa dưới màn hình, `tone_pair` hai hàng; sau trả lời hiện đúng/sai + chữ + pinyin + nghĩa + "Nghe thanh đúng"/"Nghe thanh bạn chọn"; đúng tự sang câu sau 1,2 s, sai bấm "Tiếp"; tự phát câu mới chỉ khi chuyển bằng thao tác); rời giữa chừng ⇒ hỏi (RM-L7); xong 20 câu ⇒ `POST tone-drills` với `clientSessionId = uuidV4()` sinh lúc bắt đầu ⇒ `DrillResult` (điểm, theo thanh, câu sai nghe lại, "Làm bài mới", "Xem thống kê"); lỗi mạng ⇒ "Gửi lại" cùng id (kết quả tạm tính ở client hiển thị trong lúc chờ); 422 ⇒ thông điệp server + "Làm bài mới". Xong ⇒ invalidate `tone-stats`, `progress overview`.
- **API:** `GET /pinyin/chart`, `/pinyin/guide` (keepAlive), `/pinyin/tone-stats`, `POST /pinyin/tone-drills`, `GET /me/learning-settings` (tốc độ).
- **Phụ thuộc:** M2, M3.
- **Tiêu chí + tự test:** test domain; parse fixture chart/guide/stats (kể cả stats người mới với `accuracy` vắng); widget test DrillRunner chọn thanh ⇒ hiện đúng/sai; web-dev: làm 1 phiên ⇒ `tone_drill_sessions` +1, `study_events` kind `tone_drill`; bảng không làm tràn ngang trang ở 360 px.

### Feature M8: Tra từ

- **Mục tiêu:** tra nhanh bằng chữ Hán, pinyin (số/dấu/không thanh), tiếng Việt, Hán Việt; thêm từ vào ôn tập.
- **Phạm vi (Fable):** `features/dictionary/`:
  - `domain/`: port `pos.ts`, `sources.ts`, `characterReading.ts` + test.
  - `/tu-dien?q=`: ô tìm dính đầu (`TextInputAction.search`, placeholder như web), debounce 300 ms (`Timer`), `q` rỗng ⇒ "Lộ trình HSK 1 (500 từ)"; chip "HSK 1"; danh sách **cuộn vô hạn** (tải trang kế khi còn 5 dòng cuối) thay `Pagination` [BA-mặc định — hợp điện thoại hơn]; dòng kết quả cao ≥ 64 (chữ 28, pinyin dấu + Hán Việt in hoa, ≤ 2 nghĩa một dòng, chip chưa duyệt); tải/lỗi/rỗng/503 như web; giữ vị trí cuộn khi quay lại (provider giữ state trong nhánh).
  - `/tu-dien/:id`: widget `WordDetailView` (tái dùng sheet M6): chữ 56–72 + nghe (tốc độ `ttsRate`), pinyin 20, phồn thể, dạng khác, `usageNote`, Hán Việt + chip "Hán Việt suy ra", chip cấp HSK, từ loại, nghĩa Việt đánh số + nguồn, nghĩa Anh (`ExpansionTile` đóng), lưới "Chữ trong từ" (72×88, chạm ⇒ `/tu-dien/chu/:hanzi`), `AddToSrsButton` (`srs == null` ⇒ "Thêm vào ôn tập" ⇒ toast "Đã thêm — thẻ sẽ xuất hiện trong lượt từ mới"; có thẻ ⇒ chip "Đang ôn · đến hạn dd/MM" / "Chờ học"; tạm dừng ⇒ chip + "Tiếp tục ôn"), dòng nguồn cuối trang (chạm ⇒ `/giay-phep`).
  - `/tu-dien/chu/:hanzi`: chữ 96 + nghe, cách đọc, Hán Việt (đầu đậm), số nét, bộ thủ "爪 (bộ số 87)", phồn thể, "Từ có chữ này" (≤ 20); nút "Luyện viết" chỉ hiện sau M10.
  - "Thêm" → mục "Từ điển".
- **API:** `GET /dictionary/search|words/{id}|characters/{hanzi}`, `POST /srs/cards`, `PUT /srs/cards/{id}/suspension`.
- **Phụ thuộc:** M2, M3 (M6 nếu muốn tái dùng sheet — nếu M8 làm trước thì M8 tạo `WordDetailView`, M6 dùng lại).
- **Tiêu chí + tự test:** test domain + parse fixture (tìm, chi tiết có/không `srs`, chữ); web-dev: `爱`, `ai4`, `ài`, `ai`, `yêu`, `yeu`, `ái` đều ra 爱; thêm vào ôn tập ⇒ chip đổi, `/on-tap` số từ mới thay đổi theo luật server; `/tu-dien/chu/%E7%88%B1` mở đúng.

### Feature M9: Bài học & quiz

- **Mục tiêu:** học bài theo chủ đề (hội thoại, ngữ pháp, mẹo phát âm), làm quiz, hoàn thành ≥ 80% thì từ vào ôn tập.
- **Phạm vi (Fable):** `features/lessons/`:
  - `domain/`: port `inlineZh.ts` (`[[hanzi|pinyin]]`, token lỗi giữ nguyên văn), `quizScore.ts` (`scorePercent` floor, `isPassed` bằng số nguyên, `minCorrectToPass`, `countUnanswered`, `firstUnansweredIndex`, `buildAnswers`), `shuffle.ts` (Fisher–Yates, `seededRng`) + test (≥ 11 + 7 + 6 ca); `display_prefs.dart` (khoá `af.chinese.lesson.showPinyin|showVi`).
  - Model khối bài dạng `sealed class LessonBlock` (`TextBlock`, `DialogueBlock`, `GrammarBlock`, `TipBlock`, `UnknownBlock` — loại lạ bỏ qua không crash).
  - `/bai-hoc`: thẻ "Bài tiếp theo" nổi bật (`nextLessonSlug`); thẻ bài (tiêu đề, tóm tắt, số từ, số câu, ~phút, chip trạng thái "Chưa học/Đang học/Hoàn thành {best}%", chip "Nội dung chưa được duyệt"); rỗng ⇒ "Chưa có bài học nào được xuất bản".
  - `/bai-hoc/:slug` tab Nội dung / Từ vựng / Quiz: gọi `start` **một lần** khi `progress == null`; banner "Nên học xong phần Pinyin trước…" khi `tone-stats.totalAnswered < 40` (lỗi bỏ qua); công tắc "Pinyin"/"Nghĩa tiếng Việt"; `InlineZh` hiển thị **ruby** (pinyin dấu nhỏ **phía trên** chữ Hán, tắt công tắc ⇒ bỏ pinyin) — tự dựng bằng `WidgetSpan` + `Column` [BA-mặc định, khớp chốt tích hợp F9]; chạm token ⇒ đọc; hội thoại mỗi dòng (người nói, chữ 22–24, pinyin, nghĩa, nút nghe) + "Nghe cả đoạn" (hàng đợi, dừng khi rời màn); từ vựng (`LessonWordList` + "Từ bổ sung (không vào ôn tập)", chip "Đang ôn", chạm ⇒ `/tu-dien/:id` nếu M8 đã có, ngược lại sheet `WordDetailView`); nút "Luyện viết chữ của bài" (hiện sau M10).
  - `QuizRunner`: intro → câu i/n (lựa chọn nút to xếp dọc, xáo một lần khi bắt đầu lượt, "Trước"/"Tiếp") → nộp (nút disabled khi còn câu chưa trả lời + dòng "Còn N câu chưa trả lời") → kết quả (điểm lớn, Đạt/Chưa đạt "ngưỡng 80%", `firstCompletion` ⇒ "Hoàn thành bài! Đã thêm {srsCardsAdded} từ vào ôn tập" khi > 0 + "Ôn tập ngay"; từng câu: đã chọn, đúng, lời giải có `InlineZh`; "Làm lại"; lịch sử 5 lần). `listen_choice`: loa lớn, `autoPlayAudio` ⇒ đọc trong handler nút dẫn tới câu; không giọng ⇒ "Hiện chữ". `clientAttemptId` + `startedAt` sinh lúc bắt đầu lượt; lỗi mạng ⇒ "Thử lại" cùng id; `422 QUIZ_CHANGED` ⇒ "Bài vừa được cập nhật — tải lại quiz", refetch, về intro. Rời khi đã trả lời ≥ 1 câu ⇒ hỏi. Sau nộp invalidate: lessons, lesson(slug), srs summary + badge, progress overview, attempts.
- **API:** `GET /lessons`, `GET /lessons/{slug}`, `POST /lessons/{id}/start`, `POST|GET /lessons/{id}/quiz-attempts`, `GET /pinyin/tone-stats`.
- **Phụ thuộc:** M2, M3 (M6/M8 tuỳ chọn cho liên kết).
- **Tiêu chí + tự test:** test domain + parse fixture bài `chao-hoi` (đủ 4 loại khối) và kết quả quiz (lần đầu + phát lại `srsCardsAdded = 0`); widget test QuizRunner: chưa đủ câu ⇒ nút nộp disabled; web-dev: làm bài 01 đạt ⇒ `/on-tap` có thêm từ mới, trang chủ "Bài hoàn thành" +1; nộp lại cùng id (giả lập) không tạo lần làm thứ hai.

### Feature M10.1: Dữ liệu nét chữ cho mobile (content)

- **Mục tiêu:** app có tập con dữ liệu nét giống hệt web, đúng nghĩa vụ giấy phép.
- **Phạm vi (content-implement, Sonnet):** §5.4.1 (script 2 đích, `validate.mjs` so khớp, `SOURCES.md`), chạy `yarn --cwd content build:hanzi-data:chinese` sinh `mobile/apps/chinese/assets/hanzi-data/` (commit), chép `hanzi-writer.LICENSE.txt` vào `mobile/apps/chinese/assets/licenses/`.
- **Phụ thuộc:** M0 (có thư mục app).
- **Tiêu chí:** `yarn --cwd content validate:chinese` sạch; sửa 1 byte trong bản mobile ⇒ validate FAIL (rồi hoàn tác); chạy lại build không tạo diff git.

### Feature M10.2: Luyện viết — engine bảng viết

- **Mục tiêu:** bảng viết Flutter tương đương `hanzi-writer` (xem nét, tô theo, tự viết) với cùng cách chấm và đếm lỗi/gợi ý.
- **Phạm vi (Fable):** `apps/chinese/lib/features/writing/board/` (đặc thù chữ Hán ⇒ đặt trong app, không làm package chung [BA-mặc định]):
  - `char_data.dart`: `codePointHex` (dùng `runes.first`, chữ Ext B ra 5 hex), `loadManifest()` (asset `index.json`, cache), `hasStrokeData`, `loadCharData(ch)` ⇒ `CharacterStrokes { List<Path> strokePaths; List<List<Offset>> medians; }` (parse bằng `path_parsing` → `Path` trong hệ toạ độ gốc), lỗi ⇒ ném `CharDataException`.
  - `positioner.dart`: hộp gốc `from (0, −124)` → `to (1024, 900)`; `size`, `padding = 12`; `toScreen(p) = (pad + x·s, pad + (900 − y)·s)`, `toData(screen)` ngược lại; `s = (size − 2·pad)/1024` (port `Positioner` hanzi-writer, căn giữa khi không vuông).
  - `geometry.dart` + `stroke_matcher.dart`: port **nguyên văn logic** từ `frontend/node_modules/hanzi-writer/dist/hanzi-writer.js` 3.7.3 (MIT — đầu file ghi nguồn, phiên bản, "port sang Dart bởi AntFarm", trỏ tới `assets/licenses/hanzi-writer.LICENSE.txt`): `subtract`, `magnitude`, `distance`, `equals`, `length`, `cosineSimilarity`, `frechetDist`, `subdivideCurve(maxLen 0.05)`, `outlineCurve(numPoints 30)`, `normalizeCurve`, `rotate`, `extendStart`, lớp `StrokeGeom` (`getStartingPoint`, `getEndingPoint`, `getLength`, `getVectors`, `getAverageDistance`), `stripDuplicates`, `getEdgeVectors`, `directionMatches`, `startAndEndMatches`, `lengthMatches`, `shapeFit` (xoay `[π/16, π/32, 0, −π/32, −π/16]`), `getMatchData`, `strokeMatches(userPoints, strokes, strokeNum, {leniency = 1, isOutlineVisible, averageDistanceThreshold = 350})` gồm nhánh "nét sau khớp hơn ⇒ giảm leniency" (hệ số `0.6 * (closest + avg) / (2 * avg)`), trả `{ isMatch, isStrokeBackwards }`. Hằng số giữ đúng: `COSINE_SIMILARITY_THRESHOLD = 0`, `START_AND_END_DIST_THRESHOLD = 250`, `FRECHET_THRESHOLD = 0.4`, `MIN_LEN_THRESHOLD = 0.35`. Nét ngược tính là **sai** (`acceptBackwardsStrokes = false` như mặc định web).
  - `quiz_engine.dart` (thuần, test được): trạng thái `currentStroke`, `mistakesOnStroke`, `totalMistakes`, `hintsUsed`; `submit(userPoints)` ⇒ `correct(strokeNum)` / `mistake(strokeNum, mistakesOnStroke)`; `mistakesOnStroke == threshold` (guided 2, recall 3) ⇒ `hintsUsed++` + phát sự kiện "hiện gợi ý nét"; `hint()` ⇒ `hintsUsed++` + sự kiện tô sáng; xong nét cuối ⇒ `complete(AttemptSummary{totalMistakes, hintsUsed, totalStrokes = strokes.length, durationMs ≤ 3 600 000})`. Nét người vẽ < 2 điểm sau `stripDuplicates` ⇒ bỏ qua (không tính lỗi).
  - `hanzi_board.dart` (`StatefulWidget` + `CustomPainter`): lớp vẽ — lưới 米字格 nét đứt; `view`: hoạt hình từng nét (clip bằng `strokePath`, vẽ đường median dày `≈ 200·s` phần `t` qua `PathMetric.extractPath`, thời lượng mỗi nét `(length + 600) / (3 · speed)` ms như hanzi-writer, nghỉ giữa nét 1000 ms/speed), "Phát lại", "Lặp", tốc độ 0,5×/1×; `guided`: viền mờ toàn chữ, nét đúng tô đầy; `recall`: không viền, không chữ, nét đúng tô đầy, hoàn tất ⇒ nháy tô toàn chữ; gợi ý ⇒ nháy nét hiện tại. Nhập: `GestureDetector` `onPanStart/Update/End` (điểm đổi sang toạ độ gốc), vẽ nét tạm theo ngón tay (mờ dần 300 ms); vùng bảng không cuộn trang (bảng nằm trong màn không cuộn hoặc chặn gesture arena). Màu lấy từ theme (nét: `onSurface`, viền: `outlineVariant`, tô sáng: `secondary`, nét người vẽ: `primary`). `size = min(width·0.9, 360)`. Cleanup timer/animation khi dispose/đổi chữ.
  - Test: `test/writing/stroke_matcher_test.dart` dùng file thật `assets/hanzi-data/7231.json` (爱) và `4e00.json` (一): median của nét i (đảo trục nếu cần — median đã ở toạ độ gốc) ⇒ khớp nét i; median đảo chiều ⇒ `isMatch=false` (backwards); median nét j ≠ i ⇒ không khớp nét i; đường ngẫu nhiên ⇒ không khớp; median dịch 40 đơn vị ⇒ vẫn khớp; `quiz_engine_test.dart`: 2 lần sai ở guided ⇒ `hintsUsed = 1`; bấm gợi ý ⇒ +1; hoàn tất đúng `totalStrokes`; widget test `hanzi_board_test.dart`: kéo lần lượt theo median mọi nét (đổi sang toạ độ màn) ⇒ `onComplete` 0 lỗi.
  - `char_data_test.dart`: `codePointHex('爱') == '7231'`, chữ Ext B 5 hex, `hasStrokeData` theo manifest giả, parse đủ số nét = `medians.length`.
- **Phụ thuộc:** M0, M10.1.
- **Tiêu chí:** test xanh; web-dev màn thử nội bộ (route ẩn `/dev/bang-viet` **chỉ khi `AF_ENV=dev`**) viết được 爱 bằng chuột; review đối chiếu port với dist hanzi-writer từng hàm.

### Feature M10.3: Luyện viết — màn hình

- **Mục tiêu:** luyện viết 3 bước với danh sách chữ theo bộ, ghi kết quả và trạng thái thuộc chữ như web.
- **Phạm vi (Fable):** `features/writing/`:
  - `domain/`: port `setParam.ts` (tab ⇄ set, `lesson:<slug>` kiểm slug), `nextChar.ts`, `maskHanzi.ts` + test (≥ 9 + 7 + 5 ca).
  - `/luyen-viet` tab `hsk1 | bai-hoc | can-luyen | da-luyen` (+ `bai` = slug, chọn bài bằng dropdown từ `GET /lessons`); đầu trang tóm tắt (đã luyện/đã thuộc/cần luyện); `CharacterGrid` ô 64 (4–5 cột ở 360 px), chữ + pinyin nhỏ + chấm trạng thái (new xám, practicing cam, mastered xanh), ô mờ + nhãn "Chưa có dữ liệu nét" khi thiếu (R-W9); cuộn vô hạn 60 chữ/trang; rỗng `can-luyen` ⇒ "Chưa có chữ nào cần luyện thêm — tiếp tục giữ nhịp nhé".
  - `/luyen-viet/:hanzi?tab=&tu=` (toàn màn hình): `CharacterInfoCard` (chữ lớn, cách đọc, Hán Việt, số nét, bộ thủ, ≤ 5 từ — chữ đang luyện trong từ che bằng `maskHanzi` ở bước Tự viết, nghe); tab Xem / Tô theo / Tự viết (mặc định: chưa viết lần nào ⇒ Xem, đã viết ⇒ Tự viết); bước Tự viết có nút "Gợi ý nét"; hoàn tất lượt ⇒ `POST /writing/attempts` (`clientAttemptId` sinh lúc bắt đầu lượt / "Viết lại") ⇒ `AttemptResult` (lỗi, gợi ý, trạng thái; `becameMastered` ⇒ "Đã thuộc chữ 爱!"; "Viết lại", "Bước kế", "Chữ tiếp" theo `tu` và `nextChar`); lỗi mạng ⇒ "Gửi lại" cùng id; rời khi đang vẽ dở ⇒ hỏi. Invalidate writing + progress overview.
  - Nối: M9 thêm nút "Luyện viết chữ của bài" (kết quả quiz + tab từ vựng) ⇒ `/luyen-viet?tab=bai-hoc&bai=<slug>`; M8 trang chữ thêm "Luyện viết"; `/giay-phep` thêm Arphic + hanzi-writer; `LicenseRegistry` (§5.4.1); dòng `LicenseNote` cuối trang luyện viết.
- **API:** §6.2 dòng M10 + `GET /lessons`.
- **Phụ thuộc:** M10.2, M2 (M9 cho tab bài học).
- **Tiêu chí + tự test:** test domain + parse fixture; web-dev: viết 爱 ở Tự viết không lỗi ⇒ `learning.writing_attempts` +1 `mode=recall`, response `isClean=true`; chữ không có dữ liệu nét hiện mờ và không vào được Tô/Tự viết; trang chủ app "Chữ đã luyện" tăng và khớp web; `showLicensePage` có mục Arphic + hanzi-writer; luật "đã thuộc sau 2 ngày" do backend đảm bảo (test F8 sẵn có) — không kiểm tay.

---

## 8. Thứ tự thực thi & phụ thuộc

```
M0 (Fable) ─┬─► M2 (Fable) ─┬─► M4 ─┐
M1 (Sonnet)─┘               ├─► M5  │
M0 ─► M3 (Fable) ───────────┴─► M6 ─┼─► M7 ─► M8 ─► M9 ─► M10.3
M0 ─► M10.1 (content, Sonnet) ─► M10.2 (Fable) ────────────┘
```

Thứ tự commit đề xuất: **M0 ‖ M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8 → M9 → M10.1 → M10.2 → M10.3**.

- **Song song được:** M0 và M1 (hai agent khác nhau, không đụng file chung — M1 chỉ `backend/`, `README.md` mục 5b, `deploy/VERIFY-DOCKER.md`; M0 sửa `README.md` mục mới — integration gộp cẩn thận). M3 làm song song M2 được (không phụ thuộc phiên) nhưng commit sau M2. M10.1 (content) chạy song song bất kỳ lúc nào sau M0. M10.2 (engine) song song M7–M9 nếu có hai lượt Fable.
- **Lý do thứ tự:** phiên đăng nhập là nền; M3 cung cấp pinyin/TTS cho mọi màn học; hồ sơ + cài đặt học tập (M4) cần cho SRS; tổng quan (M5) là trang chủ; **ôn thẻ (M6) ưu tiên cao nhất trong các màn học** vì là việc làm trên điện thoại nhiều nhất; pinyin (M7) phục vụ người mới G0; tra từ (M8) trước bài học để liên kết từ; viết (M10) cuối vì phức tạp nhất và phụ thuộc bài học cho bộ chữ.
- Mỗi feature dừng chờ người dùng OK **trừ khi** người dùng đã giao chạy một mạch (17/09/2026: có — chạy một mạch, quyết định nhỏ dùng mặc định BA).
- Trước khi bắt đầu mỗi feature Flutter: agent đọc README/CHANGELOG của package liên quan trong `~/.pub-cache/hosted/pub.dev/<pkg>-<version>/` (RK-M16).

---

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng bắt buộc

**Mobile** (mọi feature đụng `mobile/`) — `mobile/tool/ci.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
flutter pub get
dart format --output=none --set-exit-if-changed .
flutter analyze --fatal-infos
dart run tool/check_conventions.dart
for p in packages/af_core packages/af_auth packages/af_ui apps/chinese; do
  [ -d "$p/test" ] && (cd "$p" && flutter test)
done
(cd apps/chinese && flutter build web --release --dart-define-from-file=config/dev-web.json)
```

(Package chưa tồn tại — vd `af_auth` trước M2 — thì vòng lặp bỏ qua; `dart format` dùng độ rộng mặc định 80 **hoặc** khai `formatter: page_width: 120` trong `analysis_options.yaml` gốc [BA-mặc định: 120].)

**Backend** (M1): `dotnet build backend/backend.slnx -v q` (0 error) · `AF_TEST_PG=… dotnet test backend/backend.slnx` (báo số chạy/skip).
**Học liệu** (M10.1): `yarn --cwd content validate:chinese`.
**Web không hồi quy** (khi M1 đụng identity): `cd frontend && yarn workspace @af/chinese tsc -b && yarn workspace @af/chinese test`; đăng nhập web thủ công ở 3280 vẫn giữ phiên sau F5.
Kèm: `git status` không có file sinh (§10.4 `.gitignore`), không bí mật.

### 9.2 Nghiệm thu end-to-end (sau M10.3, trên web-dev 390×844 trong Chrome DevTools)

Đăng ký tài khoản mới trên app → học Hướng dẫn pinyin + 1 phiên luyện thanh → tra `ni3hao3`/`ai` → thêm 1 từ vào ôn tập → ôn 10 thẻ (tắt mạng giữa chừng 3 thẻ, bật lại) → học bài 01 + quiz đạt → viết 3 chữ của bài → trang chủ app **và** trang chủ web (cùng tài khoản) hiện số liệu giống nhau; chuỗi ngày học = 1. Đổi mật khẩu trên app ⇒ phiên web bị đăng xuất, app vẫn dùng được.

### 9.3 `mobile/VERIFY-DEVICE.md` — chưa verify (cần SDK), người dùng/agent chạy khi có máy

1. `flutter doctor` xanh mục Android + Xcode.
2. `flutter build apk --debug --dart-define-from-file=config/dev-android.json` và `flutter build ios --simulator --dart-define-from-file=config/dev-ios.json` thành công (CocoaPods cài được plugin).
3. Emulator: đăng nhập qua `10.0.2.2:5280` (cleartext debug OK); release build **không** gọi được http (đúng thiết kế).
4. Tắt app khi đang có outbox ⇒ mở lại ⇒ đánh giá tự gửi, DB không trùng.
5. Gỡ app iOS rồi cài lại ⇒ phải đăng nhập lại (RM-S4).
6. TTS: Android có giọng Google tiếng Trung đọc được; iOS gạt im lặng vẫn nghe; tốc độ 0,8 nghe tự nhiên trên cả hai (chỉnh hệ số iOS nếu cần — ghi lại).
7. Chữ Hán hiển thị glyph giản thể (so 直, 骨, 角 với web) trên máy đặt ngôn ngữ tiếng Việt.
8. Viết chữ bằng ngón tay: không cuộn trang khi vẽ; 爱 viết đúng thứ tự không bị báo sai oan (so cảm giác với web trên cùng máy).
9. Xoay ngang/kích thước chữ hệ thống lớn: không vỡ bố cục.
10. Bản release với `config/prod.json` (khi server F12 lên): đăng nhập `https://id.antfarms.xyz/api/auth/mobile/login` OK; `identity.refresh_tokens.client_app` ghi `(android)`/`(ios)`.
11. Kiểm `adb reverse tcp:5280 tcp:5280` + `config/dev-ios.json` trên máy Android thật.

---

## 10. Rủi ro / quyết định mở / ràng buộc

### 10.1 Rủi ro

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RK-M1 | Server đã xoay refresh token nhưng response mất (mạng rớt / app bị tắt giữa chừng) ⇒ app giữ token cũ ⇒ lần sau ngoài 30 s bị coi là dùng lại | Cả họ bị thu hồi, người học phải đăng nhập lại (không mất dữ liệu học) | RM-S2 ghi trước khi dùng; RM-S3 thử lại nhanh trong ân hạn; theo dõi log `reuse_detected` có `client_type=mobile`; nếu xảy ra thường xuyên ⇒ quyết định DM3 |
| RK-M2 | Keychain iOS còn token sau khi gỡ app | Cài lại vẫn đăng nhập tài khoản cũ | RM-S4 cờ cài đặt |
| RK-M3 | Android khôi phục backup chứa dữ liệu mã hoá bằng khoá Keystore cũ | Crash/giải mã lỗi | `allowBackup=false`; đọc lỗi ⇒ xoá kho |
| RK-M4 | Manifest main thiếu quyền `INTERNET` | Bản release không gọi được API, debug vẫn chạy | §5.3.1; VERIFY-DEVICE |
| RK-M5 | Android chặn HTTP cleartext; iOS ATS | Dev không gọi được `10.0.2.2:5280` | Network security config chỉ ở debug; `NSAllowsLocalNetworking` |
| RK-M6 | TTS: Android 11+ thiếu `<queries>` TTS_SERVICE; máy không có giọng `zh`; iOS gạt im lặng; thang tốc độ khác nhau | Không nghe được / đọc quá nhanh | §5.3.1, §5.3.7, `VoiceMissingNotice`, VERIFY-DEVICE #6 |
| RK-M7 | Han unification: không đặt locale ⇒ glyph kiểu Nhật/phồn thể | Người học nhớ sai mặt chữ | `HanziText` bắt buộc + luật WARN; VERIFY-DEVICE #7 |
| RK-M8 | Port thuật toán chấm nét sai | Báo sai oan, số liệu thuộc chữ lệch web | Port từng hàm, giữ hằng số, test bằng dữ liệu thật, review đối chiếu; phương án B §5.4.3 |
| RK-M9 | Không build được Android/iOS trên máy hiện tại | Lỗi cấu hình native chỉ lộ khi người dùng cài SDK | Ghi "CHƯA VERIFY", `VERIFY-DEVICE.md`, giữ cấu hình tối thiểu, plugin phổ biến |
| RK-M10 | Proxy dev Flutter rơi về `index.html` khi gateway tắt | Lỗi parse JSON khó hiểu | JSON guard trong `af_core` với thông điệp rõ |
| RK-M11 | Cookie web `af_rt` (host-only localhost) bị gửi tới endpoint mobile ở web-dev | Nhầm lẫn phiên | Endpoint mobile không đọc cookie; test M1 #5 |
| RK-M12 | `.gitignore` gốc có `bin/` (.NET) | Script Dart để trong `bin/` bị bỏ khỏi git âm thầm | Quy ước dùng `tool/`; README ghi chú |
| RK-M13 | Client hợp lệ nào đó gửi `Origin` tới endpoint mobile | 403 | dio native không gửi `Origin`; web-dev dùng `MobileDevOrigins`; lỗi có thông điệp rõ |
| RK-M14 | CGNAT: nhiều người dùng chung IP | Rate limit chặn oan | Policy riêng 30/phút, cấu hình được; theo dõi 429 |
| RK-M15 | Hai bản dữ liệu nét (web + mobile) lệch nhau | App và web chấm khác nhau | Script ghi cả hai; `validate.mjs` so từng byte |
| RK-M16 | Riverpod 3.x, go_router 18, flutter_secure_storage 11 mới hơn hiểu biết mặc định của agent | Code sai API, analyze đỏ, mất thời gian | Đọc README/CHANGELOG trong pub cache trước khi code; ghim version |
| RK-M17 | Outbox của người dùng A gửi dưới phiên người dùng B | Ghi sai người (server từ chối 404 vì thẻ không thuộc B — nhưng mất đánh giá của A) | Khoá outbox theo `userId`; đăng xuất hỏi + xoá |
| RK-M18 | `flutter_timezone` trả alias cũ (`Asia/Saigon`) | 422 `INVALID_TIME_ZONE` khi đăng ký | Quy alias (§5.3.6); fallback `Asia/Ho_Chi_Minh` |
| RK-M19 | Ai đó đóng gói bản Flutter web lên production | Refresh token nằm trong localStorage trình duyệt; RM-A4 sẽ chặn ở production nhưng vẫn là cấu hình sai | Out-of-scope ghi rõ; không Dockerfile; README cảnh báo |
| RK-M20 | Thiếu khoá ký release / quy trình phát hành | Không phát hành được | Ngoài phạm vi; `key.properties`, `*.jks` gitignore sẵn |
| RK-M21 | `WhenWritingNull` lược trường | `fromJson` ném khi thiếu khoá | `json_read` coi thiếu = null; fixture tối thiểu cho mọi model |
| RK-M22 | Thiếu nghĩa vụ giấy phép khi phân phối app (Arphic, CC BY-SA) | Vi phạm giấy phép | `ARPHICPL.TXT` trong assets, `LicenseRegistry`, trang `/giay-phep` |
| RK-M23 | Giờ "lượt ôn kế tiếp" hiển thị theo giờ máy thay vì múi giờ hồ sơ | Lệch khi người học đi nước ngoài | Ghi chú khi múi giờ máy ≠ hồ sơ (M6); cân nhắc `timezone` package sau |

### 10.2 Quyết định mở (không chặn — đã có mặc định)

| Mã | Câu hỏi | Mặc định BA | Ảnh hưởng |
|---|---|---|---|
| DM1 | Tên hiển thị dưới icon | "AntFarm Trung" | M0 (đổi 2 dòng) |
| DM2 | Phát hành thử (TestFlight / Play internal testing), tài khoản nhà phát triển | Chưa làm | Sau M10 |
| DM3 | Nếu RK-M1 hay xảy ra: nới ân hạn riêng mobile (vd 120 s) hoặc luật "token kế nhiệm chưa từng dùng ⇒ cho dùng lại token cũ một lần" | Giữ 30 s như web (an toàn hơn) | M1 (cấu hình thêm `Auth:MobileRefreshReuseGraceSeconds`) |
| DM4 | Đóng gói phông Noto Sans SC (tập con HSK) | Không — dùng phông hệ thống | M3 |
| DM5 | Offline sâu hơn (cache hàng đợi SRS để ôn không mạng từ đầu) | Không | Sau M10 |
| DM6 | Quản lý phiên/thiết bị (liệt kê, đăng xuất từ xa) dùng `device_name` | Không | Sau |
| DM7 | Nhắc học hằng ngày (thông báo cục bộ) | Không | Sau |

### 10.3 Mặc định BA đã dùng (người dùng có thể phản đối)

| Mã | Quyết định | Lý do |
|---|---|---|
| DB-M1 | Pub workspaces, **không melos**; script `tool/ci.sh` | 1 app + 4 package, bớt công cụ |
| DB-M2 | `flutter_lints` + luật bổ sung, không `very_good_analysis` | Đủ chặt, không làm chậm agent |
| DB-M3 | Riverpod **không codegen**; model **viết tay** (không json_serializable/freezed) | Tránh build_runner trong workspace; bù bằng test parse |
| DB-M4 | Endpoint mobile riêng `/api/auth/mobile/*`, cột `client_type`/`client_app`/`device_name`, token sai kênh ⇒ 401 không thu hồi | Cô lập hai luồng, không đụng web |
| DB-M5 | Chặn mọi request có `Origin` ở endpoint mobile (trừ dev) | Trình duyệt không được cầm refresh token đọc được bằng JS |
| DB-M6 | Header `X-AF-Client` bắt buộc (nhận diện, không phải bảo mật) | Log/thống kê theo nền tảng |
| DB-M7 | Rate limit mobile 30/phút/IP | CGNAT |
| DB-M8 | Đăng xuất (mobile và web) thu hồi cả họ, kể cả token đã xoay | Một thiết bị/trình duyệt = một họ; refresh song song không làm phiên sống lại (sửa 17/09/2026) |
| DB-M9 | Đổi mật khẩu mobile ở `/api/auth/mobile/password` với `refreshToken` trong body | Không có cookie để biết phiên hiện tại |
| DB-M10 | Outbox SRS bền bằng `shared_preferences` theo `userId`; không `connectivity_plus` | Dữ liệu nhỏ, đủ bền; bớt plugin |
| DB-M11 | Quiz/luyện thanh/luyện viết: "Gửi lại" trong bộ nhớ, không lưu bền | Giống web; phạm vi nhỏ |
| DB-M12 | UUID v4 tự viết bằng `Random.secure()` | Không dùng package `uuid` |
| DB-M13 | Tab trong trang: khởi đầu từ query, không ghi lại URL | App không có thanh địa chỉ |
| DB-M14 | GET 403 ⇒ `go('/403')`; GET 404 ⇒ `push('/404')` | Giữ nút quay lại |
| DB-M15 | Dev web cùng origin bằng `web_dev_config.yaml` cổng 3291 | Proxy có sẵn của Flutter, không thêm CORS |
| DB-M16 | Hash URL trên web-dev | Tránh phụ thuộc fallback của dev server |
| DB-M17 | Dữ liệu nét đóng gói vào assets (bản sao byte-byte của web) | Offline, cùng phiên bản, giấy phép rõ |
| DB-M18 | Luyện viết tự viết engine + port `strokeMatches` (MIT) thay `stroke_order_animator` | Số liệu khớp web, không phụ thuộc gói cũ |
| DB-M19 | Không đóng gói phông CJK; `HanziText` đặt locale + fallback | APK nhỏ; phông hệ thống đủ |
| DB-M20 | Chuỗi giao diện tiếng Việt viết thẳng, không ARB | Giống web; một ngôn ngữ UI |
| DB-M21 | Nhãn múi giờ chỉ hiện ID, không offset | Không cần CSDL múi giờ |
| DB-M22 | Tra từ cuộn vô hạn thay phân trang | Hợp điện thoại |
| DB-M23 | Thiếu `study.use` ⇒ `/403` (không vào trang chủ như web) | Không có màn nào dùng được |
| DB-M24 | Phiên ôn và bảng viết là màn toàn màn hình (ẩn bottom nav) | Tập trung, tránh chạm nhầm |
| DB-M25 | iOS TTS: category playback (phát cả khi gạt im lặng); tốc độ ×0,5 trên iOS **và Android** (plugin Android nhân 2) | Học viên cần nghe; thang AVSpeech khác |
| DB-M26 | Tên hiển thị "AntFarm Trung", bundle `xyz.antfarms.chinese`, cổng web-dev 3291 | Quy ước theo domain |
| DB-M27 | Engine viết đặt trong app, không tạo package chung | Đặc thù chữ Hán (quy tắc tiện ích đặc thù ngôn ngữ ở app) |
| DB-M28 | `deviceName` = "Android app" / "iOS app" / "Web dev" | Không thêm plugin thông tin thiết bị |

### 10.4 Cập nhật tài liệu/cấu hình ngoài `mobile/`

**`.gitignore` gốc** (M0) — thêm khối:

```gitignore
# ── Flutter / Dart (mobile/) ────────────────────────────
mobile/**/.dart_tool/
mobile/**/build/
mobile/**/.flutter-plugins
mobile/**/.flutter-plugins-dependencies
mobile/**/*.iml
mobile/**/android/.gradle/
mobile/**/android/.kotlin/
mobile/**/android/local.properties
mobile/**/android/key.properties
mobile/**/*.jks
mobile/**/*.keystore
mobile/**/ios/Pods/
mobile/**/ios/.symlinks/
mobile/**/ios/Flutter/Generated.xcconfig
mobile/**/ios/Flutter/flutter_export_environment.sh
mobile/**/ios/Flutter/ephemeral/
mobile/**/xcuserdata/
mobile/**/*.xcworkspace/xcshareddata/swiftpm/
```

(Giữ `.gitignore` do `flutter create` sinh trong `apps/chinese/` và `android/`, `ios/`. **Commit** `pubspec.lock`, `.metadata`, `ios/Podfile` (khi có), `Podfile.lock` (khi có).)

**`CLAUDE.md`** (M0; M1 bổ sung phần xác thực):
- Mục lục + sơ đồ: thêm "Mobile (Flutter) — `mobile/`" và trỏ hợp đồng này.
- Mục mới **"Mobile — `mobile/` (Flutter 3.47 + Dart pub workspaces + Riverpod 3 + go_router)"**: cấu trúc `packages/af_*` + `apps/<ngon-ngu>` (`af_<ngon-ngu>`, bundle `xyz.antfarms.<ngon-ngu>`); lệnh `cd mobile && ./tool/ci.sh`; chạy dev web `flutter run -d chrome --web-port 3291 --dart-define-from-file=config/dev-web.json`; quy tắc: **agent Flutter = `frontend-implement` + `model: "fable"`**; `showAfDialog/showAfBottomSheet` thay dialog trần; chữ Hán qua `HanziText`; không `uuid`; token chỉ ở secure storage, access token chỉ bộ nhớ; không script trong `bin/`; build Android/iOS "chưa verify" tới khi có SDK; **bản Flutter web không bao giờ triển khai**.
- Quy tắc xác thực: thêm câu "Client mobile dùng `/api/auth/mobile/*` (refresh token trong body, header `X-AF-Client`, bị chặn nếu có `Origin` ngoài `Auth:MobileDevOrigins` ở Development); refresh token có `client_type` — dùng sai kênh ⇒ 401. Luồng cookie web giữ nguyên, vẫn kiểm `Origin`."
- Bảng cổng: thêm `apps/chinese (mobile, web-dev) | http://localhost:3291 (proxy /identity, /chinese → 5280) | — (không Docker)`.
- "Thêm một ngôn ngữ mới": thêm bước `mobile/apps/<ngon-ngu>` (tuỳ chọn).

**`README.md` gốc**: mục "Mobile (Flutter)" ngắn trỏ `mobile/README.md` (M0); mục "5b. Xác thực" thêm bảng endpoint mobile + `Auth:MobileDevOrigins`, `Auth:MobileRateLimitPermitPerMinute` (M1).

**`.claude/agents/frontend-implement.md`** (M0): thêm phần "Flutter (`mobile/`)": đọc hợp đồng mobile §5.3; cổng `mobile/tool/ci.sh`; không áp quy tắc MUI/yarn cho mobile; đọc tài liệu package trong pub cache; không tự thêm CORS; báo "chưa verify Android/iOS".

**`docs/agents/AGENT-WORKFLOW.md`** (M0): dòng "mobile/ do frontend-implement (Fable)".

**`deploy/`** (M1): **không** cần container cho app mobile; **không** đổi `docker-compose.yml`, nginx, gateway (đã kiểm: `id.` chuyển mọi đường dẫn, `chinese.` có `/chinese/`). Không đặt `Auth__MobileDevOrigins` ở production. `deploy/VERIFY-DOCKER.md` thêm mục:
- `curl -s -X POST https://id.antfarms.xyz/api/auth/mobile/refresh -H 'X-AF-Client: chinese-mobile/0.1.0+1 (android)' -H 'Content-Type: application/json' -d '{"refreshToken":"<64 ký tự a>"}'` ⇒ `401 REFRESH_INVALID` JSON.
- Cùng lệnh thêm `-H 'Origin: https://chinese.antfarms.xyz'` ⇒ `403 ORIGIN_NOT_ALLOWED`.
- Thiếu `X-AF-Client` ⇒ `400 VALIDATION`.
- Nếu Cloudflare/WAF có luật chặn request không `Origin`/`User-Agent` lạ ⇒ phải cho phép `/api/auth/mobile/*`.

`deploy/.env.example`: không đổi.

### 10.5 Người dùng tự làm để chạy trên thiết bị (cần quyền quản trị — agent không làm)

```bash
# iOS (cần Xcode đầy đủ từ App Store)
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -runFirstLaunch
sudo xcodebuild -license accept
brew install cocoapods                      # hoặc: sudo gem install cocoapods
xcodebuild -downloadPlatform iOS            # nếu chưa có runtime simulator
open -a Simulator
cd mobile/apps/chinese && flutter run -d ios --dart-define-from-file=config/dev-ios.json

# Android
brew install --cask android-studio          # mở Android Studio → SDK Manager: SDK Platform + Build-Tools + Command-line Tools + Emulator
flutter config --android-sdk ~/Library/Android/sdk
flutter doctor --android-licenses
# Android Studio → Device Manager → tạo AVD (Pixel, API mới nhất) → chạy
cd mobile/apps/chinese && flutter run -d emulator-5554 --dart-define-from-file=config/dev-android.json
# Máy Android thật qua USB (bật Gỡ lỗi USB):
adb reverse tcp:5280 tcp:5280 && flutter run -d <id-máy> --dart-define-from-file=config/dev-ios.json

flutter doctor                              # kiểm lại cả hai mục
```

Sau đó chạy checklist `mobile/VERIFY-DEVICE.md` (§9.3).

### 10.6 Ràng buộc dự án phải nhắc agent thực thi

- Trả lời/comment/tài liệu **tiếng Việt có dấu**; commit local riêng từng feature trên `develop`, **không push**.
- Backend: `dotnet build backend/backend.slnx -v q` 0 error, `dotnet test` xanh (đặt `AF_TEST_PG`, báo skip); DDD 4 lớp, controller mỏng; Npgsql `timestamptz` chỉ `Kind=Utc`; migration tên `M1_MobileClient`, tự chạy theo `AutoMigrate`; `RequirePermissionAttribute` không đổi; identity **chỉ xác thực**; CORS chỉ ở identity, **không** thêm CORS/`Access-Control-*` ở gateway/nginx/chinese-backend; không commit `appsettings.Development.json`, `.secrets/`.
- Phân quyền cục bộ: app đọc quyền từ `GET /chinese/api/me`, không suy từ JWT.
- Mobile: cổng `mobile/tool/ci.sh`; không `uuid`; không dialog/bottom sheet trần; chữ Hán qua `HanziText` (`zh-CN`); pinyin lưu số, hiển thị dấu; "hôm nay" từ server; token chỉ ở secure storage (refresh) / bộ nhớ (access); mobile-first 360–390 px; build Android/iOS ghi "CHƯA VERIFY".
- Frontend web (nếu đụng): `yarn workspace @af/chinese tsc -b` (bắt buộc `-b`), MUI v9, `AppDialog`, không `uuid`.
- Học liệu: chỉ nguồn có giấy phép rõ; ghi `content/chinese/SOURCES.md`; giữ `ARPHICPL.TXT` trong mọi bản sao dữ liệu nét; seed/import không ném lỗi.
