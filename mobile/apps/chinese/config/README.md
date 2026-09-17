# `config/*.json` — cấu hình máy chủ truyền bằng `--dart-define-from-file`

Không chứa bí mật (commit). Khoá đọc ở `af_core` `AppConfig` (`String.fromEnvironment`).

| File | Dùng khi | Ghi chú |
|---|---|---|
| `dev-web.json` | `flutter run -d chrome --web-port 3291` | URL rỗng ⇒ dùng origin trang (`http://localhost:3291`) — proxy `web_dev_config.yaml` chuyển `/identity`, `/chinese` sang gateway 5280 |
| `dev-android.json` | Android emulator | `10.0.2.2` = localhost của máy Mac nhìn từ emulator |
| `dev-ios.json` | iOS simulator (dùng chung mạng máy Mac) · máy Android thật sau `adb reverse tcp:5280 tcp:5280` | |
| `prod.json` | bản phát hành | native gọi thẳng `id.` / `chinese.antfarms.xyz`, không CORS; `AF_ENV=prod` bắt buộc `https://` |
