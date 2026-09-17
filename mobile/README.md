# AntFarm Mobile — monorepo Flutter (`mobile/`)

App học viên gốc (Android/iOS) cho các ngôn ngữ của AntFarm; tiếng Trung là app đầu tiên (`apps/chinese`, package `af_chinese`).
**Chỉ tính năng học viên** — quản trị nội dung/người dùng vẫn ở web (`frontend/apps/chinese`).
Hợp đồng thực thi: `docs/agent-workflow/2026-09-17-antfarm-mobile-flutter-hop-dong-thuc-thi.md`.

> Bản Flutter **web** chỉ là công cụ dev (chạy cùng origin với gateway qua proxy) — **không bao giờ** đóng gói Docker hay đưa lên server (RK-M19). Web production vẫn là `frontend/apps/chinese`.

## Yêu cầu

- Flutter **3.47.x** / Dart **3.13.x** (`flutter --version`). Cài theo hướng dẫn chính thức; không cần melos.
- Chạy app cần backend local: identity-service (5281) → chinese-backend (5282) → gateway (5280) — xem README gốc.
- Android/iOS: cần Android SDK / Xcode + CocoaPods (xem `VERIFY-DEVICE.md` — **CHƯA VERIFY** trên máy hiện tại).

## Cài đặt & chạy

```bash
cd mobile
flutter pub get                       # một lần cho cả workspace (pub workspaces) — sinh pubspec.lock duy nhất ở đây

cd apps/chinese
# Web dev có giao diện (Chrome) — cùng origin với gateway qua proxy web_dev_config.yaml, cổng 3291
flutter run -d chrome --web-port 3291 --dart-define-from-file=config/dev-web.json
# Web dev không giao diện (kiểm proxy bằng curl)
flutter run -d web-server --web-port 3291 --dart-define-from-file=config/dev-web.json
curl -s http://localhost:3291/chinese/api/system/info      # ⇒ JSON service=chinese-backend
curl -s http://localhost:3291/identity/api/system/info     # ⇒ JSON service=identity-service

# Thiết bị (khi đã có SDK)
flutter run -d emulator-5554 --dart-define-from-file=config/dev-android.json     # 10.0.2.2 = localhost máy Mac
flutter run -d "iPhone 17"  --dart-define-from-file=config/dev-ios.json           # simulator dùng mạng máy Mac
adb reverse tcp:5280 tcp:5280 && flutter run -d <id-máy> --dart-define-from-file=config/dev-ios.json   # Android thật
```

`config/*.json` (không bí mật, commit) truyền bằng `--dart-define-from-file`: `AF_ENV`, `AF_GATEWAY_URL`, `AF_IDENTITY_API_URL`, `AF_CHINESE_API_URL` — xem `apps/chinese/config/README.md`. Thiếu cấu hình ⇒ app hiện màn "Thiếu cấu hình máy chủ"; `AF_ENV=prod` bắt buộc `https://`.

### Cổng

| Thành phần | Cổng |
|---|---|
| gateway / identity / chinese-backend | 5280 / 5281 / 5282 |
| `frontend/apps/chinese` (web production, Vite) | 3280 |
| **`mobile/apps/chinese` web-dev** (proxy `/identity`, `/chinese` → 5280) | **3291** |

identity-service dev cần `Auth:MobileDevOrigins = ["http://localhost:3291"]` (trình duyệt gửi `Origin` cả khi cùng origin) — M1.

## Cổng kiểm (bắt buộc trước khi bàn giao/commit)

```bash
cd mobile && ./tool/ci.sh
```

= `flutter pub get` → `dart format --set-exit-if-changed` (độ rộng 120) → `flutter analyze --fatal-infos` → `dart run tool/check_conventions.dart` → `flutter test` từng package/app → `flutter build web` (chỉ kiểm biên dịch).

`tool/check_conventions.dart` (tương đương `yarn lint:ui` của frontend):

| Luật | Mức | Ý nghĩa |
|---|---|---|
| `raw-dialog` | FAIL | `showDialog(`/`showModalBottomSheet(`/`showCupertinoDialog(` trong `apps/` — dùng `showAfDialog`/`showAfBottomSheet` (`af_ui`) |
| `uuid-import` | FAIL | `package:uuid/` — dùng `uuidV4()` (`af_core`) |
| `token-in-prefs` | FAIL | file vừa có `SharedPreferences` vừa có `refreshToken`/`accessToken` — token chỉ ở secure storage/bộ nhớ |
| `hardcoded-url` | FAIL | `http(s)://` trong `lib/` ngoài `lib/**/config/**`, `sources.dart`, `licenses.dart` |
| `print-call` | FAIL | `print(`/`debugPrint(` ngoài `af_core/lib/src/log/` — dùng `afLog()` |
| `cjk-without-hanzitext` | WARN | chuỗi có chữ Hán trong file không import `HanziText`/`LangText` |

## Cấu trúc

```
mobile/
  pubspec.yaml                # workspace root (name: antfarm_mobile) — KHÔNG có bin/ (gitignore .NET), script ở tool/
  pubspec.lock                # commit
  analysis_options.yaml       # include af_lints
  tool/ci.sh · tool/check_conventions.dart
  packages/
    af_lints/                 # luật analyzer + formatter (page_width 120) dùng chung
    af_core/                  # AppConfig, createApiClient (dio), ApiError, json_read, KeyValueStore, uuidV4, X-AF-Client, afLog
    af_ui/                    # theme sáng/tối, ThemeModeController, LangText/HanziText, AfShellScaffold, StickyActionBar,
                              # showAfDialog/showAfConfirm/showAfBottomSheet, showAfToast, ErrorView, AsyncValueView, EmptyState, SectionCard
    af_auth/                  # phiên đăng nhập mobile (M2): TokenStore (secure storage), AuthSession (refresh single-flight,
                              # ghi kho trước khi dùng), AuthController + authRedirect (go_router), LoginPage/RegisterPage,
                              # AuthGate (splash / không kết nối / nội dung), describeAuthError, validators, deviceTimeZone
  apps/chinese/               # af_chinese — bundle xyz.antfarms.chinese, tên "AntFarm Trung"
    config/*.json · web_dev_config.yaml
    lib/main.dart · app.dart · config/ (links.dart — URL duy nhất) · api/clients.dart (Dio + AuthSession) · router/ (redirect)
    lib/core/session_scope.dart (userScopeProvider) · features/auth/ (auth_providers, sign_out, me_api, error_pages 401/403/404)
    lib/features/profile/ (hồ sơ 4 tab) · features/srs/ (learning-settings, ôn thẻ + outbox bền) · features/dictionary/ (chi tiết từ — M6 tối thiểu)
    lib/features/licenses/ (giấy phép & nguồn)
    lib/features/progress/ (tổng quan trang chủ: domain port web + test, DashboardPage, huy hiệu Ôn tập)
    assets/hanzi-data/ (nét chữ, ARPHICPL.TXT) · assets/licenses/ (hanzi-writer MIT)
    test/                     # widget test shell/thẻ trạng thái/luồng đăng nhập/hồ sơ, parse model (fixtures/)
```

Quy ước: `apps/<ngon-ngu>` = `af_<ngon-ngu>`, bundle `xyz.antfarms.<ngon-ngu>`; mọi chữ Hán qua `HanziText` (locale `zh-CN` + phông CJK dự phòng); pinyin lưu số, hiển thị dấu; không `uuid`; dialog/bottom sheet qua `af_ui`; token chỉ ở `flutter_secure_storage` (refresh) / bộ nhớ (access); "hôm nay" lấy từ server; mobile-first 360–390 px, chữ 1.3× không vỡ.

## Phụ thuộc (ghim chính xác — kiểm pub cache 17/09/2026)

| Package | Version | Giấy phép | Dùng ở |
|---|---|---|---|
| flutter_riverpod / riverpod | 3.4.3 | MIT | af_ui, app (không codegen) |
| go_router | 18.0.1 | BSD-3-Clause | app (`StatefulShellRoute.indexedStack`) |
| dio | 5.11.1 | MIT | af_core |
| shared_preferences | 2.5.5 | BSD-3-Clause | af_core (`SharedPreferencesAsync`), af_ui |
| package_info_plus | 10.2.1 | BSD-3-Clause | af_core (phiên bản cho `X-AF-Client`) |
| flutter_secure_storage | 11.2.0 | BSD-3-Clause | af_auth (`af.auth.session`; iOS Keychain `first_unlock_this_device`, Android RSA-OAEP+AES-GCM mặc định v11, minSdk 24) |
| flutter_timezone | 5.1.0 | Apache-2.0 | af_auth (`deviceTimeZone()` khi đăng ký; `listTimeZones()` cho ô chọn múi giờ ở Hồ sơ) |
| flutter_lints | 6.0.0 | BSD-3-Clause | af_lints |
| fake_async | 1.3.3 | Apache-2.0 | dev af_auth (test hẹn giờ làm mới) |
| mocktail | 1.0.5 | MIT | dev |
| shared_preferences_platform_interface | 2.4.2 | BSD-3-Clause | dev af_core (kho giả cho test) |
| flutter_localizations / intl | SDK / `any` | BSD | app (`vi`, `en`) |
| flutter_tts | 4.2.5 | MIT | af_ui (`AfTts`, `SpeechController`, `VoiceMissingNotice`) — M3 |

Bổ sung ở feature sau (theo hợp đồng §5.3.2): `path_parsing` 1.1.0 (M10.2).
Ghi chú: `uuid` 4.6.0 xuất hiện trong `pubspec.lock` là phụ thuộc **bắc cầu** của `riverpod` — app không import (`uuid-import` FAIL nếu vi phạm).

## Phiên đăng nhập (M2)

- Endpoint riêng `/identity/api/auth/mobile/{register,login,refresh,logout,password}` (header `X-AF-Client` bắt buộc, không cookie). Refresh token + tài khoản rút gọn lưu **chỉ** trong `flutter_secure_storage` (khoá `af.auth.session`); access token chỉ ở bộ nhớ (`AuthSession`).
- Làm mới: single-flight theo thế hệ phiên (`_epoch` tăng khi đăng xuất/đăng nhập — refresh đang bay của phiên cũ bị bỏ, không ghi đè, không xoá phiên mới); lời gọi refresh không gắn Bearer (`skipAuthHeader`); request 401 mang token cũ được gửi lại bằng token hiện tại không xoay thêm; **ghi kho trước rồi mới phát token** (RM-S2); lỗi mạng/5xx thử lại 1 s, 3 s rồi giữ phiên (màn "Không kết nối được máy chủ" có Thử lại/Đăng xuất); chỉ 401/403 mới mất phiên ⇒ `/dang-nhap?reason=expired`. Hẹn giờ 60 s trước hạn + khi app resumed.
- Quyền chỉ từ `GET /chinese/api/me` (fail-closed); thiếu `study.use` ⇒ `/403` có nút Đăng xuất. Người có `content.manage`/`users.manage` thấy dòng "Quản trị … dùng bản web" ở "Thêm" (không có màn quản trị).
- Lần chạy đầu sau khi cài (thiếu cờ `af.install.v1` trong shared_preferences) xoá sạch `af.auth.*` trong secure storage (RM-S4 — Keychain iOS sống sót sau gỡ app).
- **Bản web dev:** `flutter_secure_storage_web` mã hoá bằng WebCrypto và lưu `localStorage`, chỉ chạy trên HTTPS/localhost — đủ để dev ở `http://localhost:3291`, KHÔNG dùng cho người dùng thật (RK-M19).
- Luồng đăng xuất duy nhất: `signOutFlow` (`features/auth/application/sign_out.dart`) — M6 nối số đánh giá chưa gửi (`pendingOutboxCountProvider`) để hỏi xác nhận.

## Nền tiếng Trung — pinyin, chữ Hán, giọng đọc (M3)

- `apps/chinese/lib/core/pinyin/pinyin.dart`: port toàn bộ `frontend/apps/chinese/src/lib/pinyin.ts` (số thanh ⇒ dấu, dấu ⇒ số, dấu câu hai đầu âm tiết, `r5` nhi hoá, `Xī'ān`, gợi ý biến điệu 3-3/不/一). Dart không có `normalize('NFC')` ⇒ tự ghép dấu tổ hợp cho 6 nguyên âm pinyin. Test `test/core/pinyin_test.dart` chép đủ ca của `pinyin.test.ts`.
- Widget dùng chung `lib/core/widgets/`: `PinyinText` (số ⇒ dấu, chú thích biến điệu), `HanziBig` (cỡ đặt tên, luôn qua `HanziText` — RK-M7), `SpeakButton` (vô hiệu + tooltip khi chưa có giọng; huỷ đọc khi rời màn), `MeaningStatusChip` ("Chưa duyệt").
- `af_ui` speech: `AfTts` bọc `flutter_tts` (lọc giọng `zh`, loại Quảng Đông `zh-HK`/`yue`, ưu tiên `zh-CN` rồi Enhanced/Premium), `SpeechController` (`loading|ready|noVoice|unsupported`, nhớ giọng `af.speech.voice.zh`), `VoiceMissingNotice` (hướng dẫn cài giọng theo nền tảng). App ngôn ngữ khác override `speechLangPrefixProvider`/`speechLanguageProvider`.
- **Tốc độ đọc**: người dùng chọn 0,5–1,2 (mặc định 0,8). Từ M4 **nguồn sự thật là `learner_settings.tts_rate`** (`GET/PUT /me/learning-settings`, `learningSettingsProvider`); `af.chinese.ttsRate` trong shared_preferences chỉ là cache để có giá trị ngay khi mở app (`ttsRateProvider`: server → cache → 0,8; kéo thanh trượt ở Hồ sơ → Giao diện ghi máy chủ khi thả tay; chọn trước khi server trả lời thì giá trị chọn thắng và được đẩy lên). Quy đổi sang plugin theo mã flutter_tts 4.2.5: web giữ nguyên; **Android và iOS nhân 0,5** (Android plugin gọi `setSpeechRate(rate × 2)`, iOS gán thẳng `AVSpeechUtterance.rate` với 0,5 = bình thường) — khác BA-mặc định §5.3.7 (Android giữ nguyên); cần kiểm máy thật (VERIFY-DEVICE #14).
- Chọn giọng/tốc độ/nghe thử 你好 ở Thêm → Hồ sơ → tab **Giao diện** (màn tạm `/giong-doc` của M3 đã gỡ); không giọng ⇒ hướng dẫn cài + nút "Dò lại giọng". `SpeakButton` chỉ hiện tooltip "Chưa có giọng" khi thật sự `noVoice`/`unsupported` (đang dò thì chỉ vô hiệu).
- iOS: `setSharedInstance(true)` + audio category `playback` + `mixWithOthers` để phát cả khi gạt im lặng (chưa verify).

## Hồ sơ & cài đặt (M4)

- `/ho-so?tab=thong-tin|mat-khau|hoc-tap|giao-dien` — tab khởi đầu từ query, sau đó state cục bộ, không ghi lại URL (RM-L6). Thiếu `study.use` ⇒ ẩn tab Học tập kèm dải giải thích.
- **Thông tin**: `PUT /identity/api/account` ⇒ `AuthController.refreshSession()` (xoay refresh token ⇒ claim mới ⇒ `GET /account` ⇒ `GET /chinese/api/me`) ⇒ toast; `422 INVALID_TIME_ZONE` dưới ô múi giờ. Ô múi giờ = `TimeZoneField` (af_auth): bottom sheet có ô tìm (`matchesTimeZoneQuery`: không phân biệt hoa thường, `_`≡khoảng trắng, "ho chi" ⇒ `Asia/Ho_Chi_Minh`), danh sách `FlutterTimezone.getAvailableTimezones()` đã quy bí danh (lỗi/quá ngắn ⇒ `kFallbackTimeZones` ~36 múi giờ), ghim "Múi giờ của máy"; máy ≠ hồ sơ ⇒ dải + "Dùng múi giờ này".
- **Mật khẩu**: `POST /auth/mobile/password` kèm refresh token hiện tại (chờ lời làm mới đang bay xong để gửi token mới nhất); `currentSessionKept=false` ⇒ `signOutLocally(passwordChanged)` ⇒ `/dang-nhap?reason=password-changed`.
- **Học tập**: port `LearningSettingsTab` web (thanh trượt từ mới/độ nhớ/tốc độ + nghe thử theo giá trị đang kéo, ô số giới hạn ôn, công tắc tự đọc); `400 VALIDATION` `details` hiện dưới đúng ô. M6 thêm `invalidate(srsSummaryProvider)` sau khi lưu.
- **Giao diện**: chế độ Hệ thống/Sáng/Tối (`af.themeMode`) + giọng đọc (xem mục M3).
- `/giay-phep`: nguồn học liệu như `SourceAttribution` web + Arphic/hanzi-writer + py-fsrs (`features/licenses/data/sources.dart` — file duy nhất ngoài `config/` được chứa URL), mỗi nguồn kèm URL giấy phép (nghĩa vụ CC BY-SA 4.0 §3(a)(1)(C)); nút "Giấy phép phần mềm" = `showLicensePage` với Arphic (`assets/hanzi-data/ARPHICPL.TXT`), MIT hanzi-writer, CC BY-SA 4.0, Unicode License v3, MIT (hsk30, complete-hsk-vocabulary, py-fsrs) — bản sao nguyên văn từ `content/chinese/LICENSES/` ở `assets/licenses/` (xem `NOTICE.md` ở đó), đăng ký qua `registerAntFarmLicenses()` ở `main.dart`. Không có `url_launcher` ⇒ URL hiện dạng chữ.
- **Đổi tài khoản trên cùng máy:** provider theo người dùng (`learningSettingsProvider`) khi dựng lại vẫn giữ `.value` của người trước trong lúc `AsyncLoading` (Riverpod 3) ⇒ mọi chỗ đọc để DÙNG dữ liệu phải qua `unwrapPrevious()` (`ttsRateProvider`, `autoPlayAudioProvider`, tab Học tập); cache tốc độ đọc khoá theo người dùng (`af.chinese.ttsRate.<userId>`); hộp "giá trị chờ" của thanh tốc độ cũng theo `userScopeProvider`. Áp dụng mẫu này cho mọi provider theo người dùng ở M5+.
- `AuthController.refreshSession()` luôn mở lượt làm mới MỚI (`AuthSession.refresh(force: true)`, xếp sau lượt đang bay) và trả `bool`: `false` ⇒ đã lưu nhưng chưa tải lại được hồ sơ/quyền (toast cảnh báo). `describeAuthError(context: AuthErrorContext.session)` cho màn đã đăng nhập: 401 không mã ⇒ "Phiên đăng nhập không còn hợp lệ" (không nói sai mật khẩu).

- Sau khi lưu cài đặt học tập / hồ sơ: `ref.invalidateProgressOverview()` + `ref.invalidateSrsSummary()` (M6 — cả nhánh "đã lưu nhưng chưa tải lại được hồ sơ").

## Tổng quan — trang chủ (M5)

- `/` = `DashboardPage` (`features/progress/`, port `features/progress/` web): tiêu đề "Hôm nay, {thứ} {dd/MM}" theo `localDate` của server (không dùng ngày máy); thứ tự khối §5.3.4: chuỗi ngày → mục tiêu → việc hôm nay → lịch 90 ngày → từ vựng → bài học → luyện viết → thanh điệu; khối vắng trong JSON ⇒ ẩn; số 0 ⇒ nút mời (CTA); lỗi ⇒ `ErrorView` trong trang + Thử lại (503 `CONTENT_UNAVAILABLE` ⇒ "Học liệu chưa sẵn sàng"); kéo để làm mới. Khối "Trạng thái hệ thống" chỉ hiện với `users.manage`, thu gọn cuối trang (`ExpansionTile`, chỉ gọi `/system/info` khi mở).
- `domain/` port thuần từ web kèm test cùng số ca: `dates.dart` (số ngày epoch, tuần bắt đầu Thứ Hai), `heatmap.dart` (`levelOf` 0 · 1–9 · 10–29 · 30–59 · ≥60, `buildHeatmap` 13–14 cột, nhãn tháng), `today_tasks.dart` (thứ tự R-PG9, `kPinyinMinAnswered = 40`, đường dẫn giống web), `labels.dart` (`todayHeading`, `streakMessage`, `longestLabel`, `isDeviceTimeZoneDifferent` quy bí danh `Asia/Saigon`, `toPercent`).
- `ActivityHeatmap`: lưới tự vẽ ô 16 px khe 3 (14 cột = 263 px + cột nhãn 22 ⇒ vừa 360 px); điện thoại không có hover ⇒ **chạm ô ghim nhãn "dd/MM: N lượt" 3 s** dưới lưới; hẹp hơn lưới ⇒ cuộn ngang mở sẵn ở tuần hiện tại.
- **Provider theo người dùng:** `progressOverviewByUserProvider` là `FutureProvider.autoDispose.family` **theo id người dùng** (`progressOverviewProvider` chọn instance theo `userScopeProvider`): đổi tài khoản ⇒ instance mới (không mang `.value` người trước — cùng mục đích với `unwrapPrevious()`), còn làm mới cùng người thì giữ số cũ trong lúc tải nên huy hiệu không nháy về 0. Làm mới bằng `ref.invalidateProgressOverview()` (extension cho `WidgetRef`/`Ref`): app resumed (`AuthLifecycleObserver.onResumed`), chọn lại tab Trang chủ, kéo-để-làm-mới, sau khi lưu hồ sơ (múi giờ đổi ⇒ "hôm nay" đổi) và lưu cài đặt học tập (hạn mức thẻ mới đổi). M6+ gọi thêm sau khi gửi đánh giá/nộp quiz/viết chữ (danh sách invalidate như web).
- **Huy hiệu "Ôn tập"** = `dueNow + newAvailableToday` — từ M6 nguồn DUY NHẤT là `srsSummaryProvider` (`features/srs/application/providers.dart`, không còn đọc khối `srs` của tổng quan); thiếu `study.use`/lỗi ⇒ ẩn. `ActivityHeatmap` cuộn ngang bằng `ScrollController` + `jumpTo(maxScrollExtent)` chỉ khi thật sự cuộn được (không `reverse`).
- Việc hôm nay/nút trong thẻ dẫn tới `/on-tap`, `/bai-hoc/<slug>`, `/luyen-viet?tab=…`, `/pinyin?tab=luyen` — màn chưa có trên app ⇒ `ComingSoonPage` (route `/bai-hoc/:slug` thêm ở M5, M9 thay).
- Múi giờ máy ≠ `overview.timeZone` ⇒ dòng "Ngày học tính theo múi giờ hồ sơ (…) — đổi ở Hồ sơ" (chạm ⇒ `/ho-so`). `deviceTimeZoneProvider` (af_auth) trả **`null` khi không đọc được múi giờ máy** (`tryDeviceTimeZone`) — không so với mặc định nên không nhắc oan; trang đăng ký vẫn rơi về `Asia/Ho_Chi_Minh`.
- Fixture `test/fixtures/progress_overview_{full,minimal,new_user}.json` (minimal = chỉ 5 trường luôn có; new_user = bản ghi thật của tài khoản mới); widget test `buildTestApp` mặc định trả fixture người mới cho `/progress/overview` (`overviewBody`/`overview` để đổi).

## Ôn thẻ SRS FSRS-6 (M6)

- `features/srs/`: `domain/` port thuần từ web kèm test cùng số ca — `format_interval.dart` (ISO-8601 + TimeSpan .NET ⇒ "5,5 phút"/"1,5 tháng"), `session_deck.dart` (`mergeIncoming`, `shouldLoadMore` ngưỡng 5, `countRatings`, `formatSessionDuration`, `clampDurationMs`), `ratings.dart`, `review_outbox.dart` (`OutboxItem` JSON, `enqueue`, `isRetryableError` mạng/5xx/408/429, `retryDelayMs` 1/2/5/10 s rồi 30 s, `flushOutbox` tuần tự).
- **Outbox bền theo người dùng** (`application/outbox_controller.dart`, `reviewOutboxProvider`): ghi `shared_preferences` khoá `af.srs.outbox.<userId>` (qua `KeyValueStore`, không token) sau MỖI thay đổi; single-flight, hẹn giờ bậc thang; gửi khi chấm, khi dựng (mở app/đăng nhập đúng user — kho của người khác không gửi), app resumed (`app.dart`), kéo-làm-mới `/on-tap`, mở phiên. `clientReviewId = uuidV4()` sinh một lần lúc chấm, giữ nguyên mọi lần gửi lại (server idempotent ⇒ DB không nhân đôi). Gửi xong ⇒ `SrsSummaryNotifier.apply(summary)` + invalidate tổng quan; 4xx thật (409/422/400/404) ⇒ bỏ + toast thông điệp server (`OutboxNotices` trong `MaterialApp.builder`). `unsentCount` (banner "Đang chờ gửi N", hộp xác nhận rời phiên) chỉ > 0 khi có phần tử `attempts > 0`; `pendingCount` cho hộp xác nhận đăng xuất (RM-S7).
- **Giữ dữ liệu học (quyết định orchestrator M6, RK-M1 — lệch hợp đồng §5.3.6 "onSignedOut: app xoá outbox"):** kho outbox CHỈ bị xoá khi người dùng **chủ động đăng xuất** và đã xác nhận (`signOutFlow` ⇒ `clearForUser`, lệnh xoá nối vào chuỗi ghi); hết phiên/`signOutLocally` (đổi mật khẩu)/mất phiên ⇒ **giữ** kho theo `userId`, cùng người đăng nhập lại sẽ gửi tiếp; lượt đang gửi nhận **401** ⇒ dừng và giữ (không bỏ, không tăng `attempts`); phiên CHƯA mất (vẫn cùng người) ⇒ hẹn lại sau 30 s, mất phiên thật ⇒ Notifier dựng lại, kho giữ. `onSignedOut` không làm gì; `srsSummaryByUserProvider.build` tự chặn khi `userScopeProvider ≠ userId` nên không gọi `/srs/summary` không token.
- **Đổi tài khoản giữa lúc gửi (review C1):** `flushOutbox(shouldStop:)` hỏi TRƯỚC mỗi lần gửi (`ref.mounted`, thế hệ, `userScopeProvider` ≠ user của kho) ⇒ phần còn lại của A không đi bằng phiên B, kho A nguyên vẹn. **Chấm trước khi đọc xong kho (review C2):** `submit` chỉ giữ bộ nhớ (không ghi kho/gửi) tới khi `_restore` gộp "đã lưu trước, mới sau", ghi kho rồi flush. Kho tối đa **500** phần tử (`trimOutbox` ở submit/gộp sau gửi/`_restore` — vượt ⇒ bỏ lượt CŨ nhất, `lastDrop` mang lượt bị bỏ ⇒ toast); lượt nối đuôi trong lúc gửi so theo `clientReviewId` (không theo độ dài). `pendingOutboxCountProvider` (autoDispose) chờ `whenRestored` rồi mới đếm — bấm Đăng xuất ngay lúc app vừa mở vẫn hỏi xác nhận. Riverpod 3.4 TÁI DÙNG instance Notifier khi rebuild ⇒ completer/token single-flight được đặt lại trong `build`, `ref.mounted` không đủ để nhận biết đổi người.
- `srsSummaryByUserProvider` — `AsyncNotifierProvider.family` theo id người dùng (Riverpod 3 truyền tham số qua constructor), KHÔNG autoDispose (outbox `apply` lúc shell không mount), invalidate cả family khi đăng xuất; `srsSummaryProvider` chọn theo `userScopeProvider`; `ref.invalidateSrsSummary()` sau lưu cài đặt/hồ sơ, thêm/tạm dừng thẻ, resumed, chọn lại tab Ôn tập.
- `/on-tap` (`ReviewHomePage`): 3 ô số (`IntrinsicHeight`), "Bắt đầu ôn (N)" 56 px, hết thẻ ⇒ "Hôm nay xong rồi!" + "Lượt ôn kế tiếp HH:mm dd/MM" theo **giờ máy** (`toLocal()`, kèm "(giờ trên máy — hồ sơ dùng múi giờ …)" khi máy ≠ hồ sơ — BA-mặc định RK-M23), banner hết lượt ôn, "Từ vững m/500", liên kết `/ho-so?tab=hoc-tap`, `PendingReviewsBanner`.
- `/on-tap/phien` (`ReviewSessionPage`, root navigator + `AuthGate`, ẩn bottom nav): `SessionHeader`, `Flashcard` (chạm thẻ cũng lật; chữ ≥ 4 ký tự hạ cỡ), `RatingBar` 4 nút ≥ 56 px hai dòng, khoá 300 ms + `HapticFeedback.selectionClick`, `durationMs` đo bằng `Stopwatch`, `StickyActionBar`; tự đọc khi thẻ hiện nếu `autoPlayAudio` (khi chấm đọc thẻ kế ngay trong thao tác chạm — iOS), huỷ đọc khi rời màn; "Xem chi tiết" ⇒ `showWordDetailSheet` (`closeOnBarrier: true`, chỉ đọc) dùng `WordDetailView` + `AddToSrsButton` (`features/dictionary/`, M8 tái dùng; ô "Chữ trong từ" chưa dẫn tới `/tu-dien/chu/:hanzi`). Tải thêm khi còn ≤ 5 thẻ với `skipNew` khi có lượt chấm chưa chắc tới server; lô không thêm gì mà outbox còn ⇒ **không** gọi lại `/srs/queue` ngay (chờ outbox đổi/lượt chấm kế — tương đương effect theo deps của web). Hết thẻ ⇒ `SessionSummary` (vẫn hiện banner chờ gửi nếu còn). Rời khi `unsentCount > 0` ⇒ `PopScope` + hỏi.
- `af_ui` `StickyActionBar`: `Align(heightFactor: 1)` — `Center` trần trong `bottomNavigationBar` phình ra toàn màn và đè lên thân trang (phát hiện ở M6).
- Test: domain (format_interval 21 ca, session_deck 10, review_outbox 16: thêm `shouldStop`, 401 giữ, `trimOutbox`), `OutboxController` với `InMemoryKeyValueStore` + API giả (mất mạng 3 lần ⇒ cùng id/thứ tự; controller mới đọc lại kho; user A không gửi khi B đăng nhập; đổi A→B giữa lúc gửi; chấm trước khi đọc xong kho; 401 giữ; kho đầy; 422 ⇒ drop; clearForUser), widget phiên (lật/chấm/tổng kết/huy hiệu, offline ⇒ banner ⇒ online gửi lại không trùng, tắt app còn kho ⇒ tự gửi, đăng xuất còn outbox ⇒ hỏi/xoá kho, sheet chi tiết, tối + 360×740 + 1,3×). Fixture `test/fixtures/srs_queue.json`; `buildTestApp(srs:)` stub `/srs/*` (mặc định `/srs/summary` suy từ thân tổng quan để huy hiệu khớp).
- Chạy thật web-dev 17/09/2026 (Chrome headless, tài khoản mới): ôn đủ 10 thẻ mới, `srs_review_logs` 15 = 15 `client_review_id` phân biệt = 15 `study_events`, `state_before='new'` đúng 10; offline (CDP) chấm 3 ⇒ banner ⇒ online ⇒ gửi cùng id; tải lại trang khi còn outbox ⇒ mở lại tự gửi; huy hiệu/`/on-tap` cập nhật. Android/iOS: CHƯA VERIFY (VERIFY-DEVICE #12).

## Chưa verify

Build Android/iOS chưa chạy được trên máy phát triển hiện tại (chưa có Android SDK; Xcode chưa `xcode-select`, chưa CocoaPods). Checklist khi có máy: `VERIFY-DEVICE.md`. Cấu hình native đã đặt sẵn theo hợp đồng: `INTERNET` ở manifest chính, `allowBackup=false`, `<queries>` TTS, network security config chỉ ở debug (cleartext `10.0.2.2`/`localhost`/`127.0.0.1`), iOS `NSAllowsLocalNetworking`, `CFBundleLocalizations` vi/en.
