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
    af_auth/                  # (M2) phiên đăng nhập mobile — chưa tạo ở M0
  apps/chinese/               # af_chinese — bundle xyz.antfarms.chinese, tên "AntFarm Trung"
    config/*.json · web_dev_config.yaml
    lib/main.dart · app.dart · config/ · api/clients.dart · router/ · shell/ · core/ · features/<module>/{data,application,presentation}
    test/                     # widget test shell, thẻ trạng thái, parse model (fixtures/)
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
| flutter_lints | 6.0.0 | BSD-3-Clause | af_lints |
| mocktail | 1.0.5 | MIT | dev |
| shared_preferences_platform_interface | 2.4.2 | BSD-3-Clause | dev af_core (kho giả cho test) |
| flutter_localizations / intl | SDK / `any` | BSD | app (`vi`, `en`) |

Bổ sung ở feature sau (theo hợp đồng §5.3.2): `flutter_secure_storage` 11.2.0 (M2), `flutter_timezone` 5.1.0 (M2), `flutter_tts` 4.2.5 (M3), `path_parsing` 1.1.0 (M10.2).
Ghi chú: `uuid` 4.6.0 xuất hiện trong `pubspec.lock` là phụ thuộc **bắc cầu** của `riverpod` — app không import (`uuid-import` FAIL nếu vi phạm).

## Chưa verify

Build Android/iOS chưa chạy được trên máy phát triển hiện tại (chưa có Android SDK; Xcode chưa `xcode-select`, chưa CocoaPods). Checklist khi có máy: `VERIFY-DEVICE.md`. Cấu hình native đã đặt sẵn theo hợp đồng: `INTERNET` ở manifest chính, `allowBackup=false`, `<queries>` TTS, network security config chỉ ở debug (cleartext `10.0.2.2`/`localhost`/`127.0.0.1`), iOS `NSAllowsLocalNetworking`, `CFBundleLocalizations` vi/en.
