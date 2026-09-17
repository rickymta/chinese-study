# VERIFY-DEVICE — checklist chạy app trên Android/iOS thật (CHƯA VERIFY)

Máy phát triển lúc dựng M0 (17/09/2026) **chưa có Android SDK, Xcode chưa `xcode-select`, chưa CocoaPods** ⇒ agent chỉ kiểm được bằng `flutter analyze`, `flutter test`, `flutter build web` và chạy thử bản web. Mọi mục dưới đây do người dùng (hoặc agent khi có máy) chạy và tick; việc cài SDK cần quyền quản trị — xem hợp đồng mobile §10.5.

Tương tự `deploy/VERIFY-DOCKER.md`: mục nào chưa tick nghĩa là **chưa được xác nhận trên thiết bị**.

## Chuẩn bị (người dùng tự làm)

```bash
# iOS
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -runFirstLaunch && sudo xcodebuild -license accept
brew install cocoapods
xcodebuild -downloadPlatform iOS
# Android
brew install --cask android-studio      # SDK Manager: Platform + Build-Tools + Command-line Tools + Emulator
flutter config --android-sdk ~/Library/Android/sdk
flutter doctor --android-licenses
flutter doctor                          # mục Android + Xcode phải xanh
```

## Checklist

| # | Việc | Lệnh / cách kiểm | Kết quả mong đợi | Trạng thái |
|---|---|---|---|---|
| 1 | `flutter doctor` | `flutter doctor` | Android toolchain + Xcode xanh | ☐ |
| 2 | Build debug Android | `cd mobile/apps/chinese && flutter build apk --debug --dart-define-from-file=config/dev-android.json` | Thành công; `applicationId` = `xyz.antfarms.chinese` | ☐ |
| 3 | Build iOS simulator | `flutter build ios --simulator --dart-define-from-file=config/dev-ios.json` | CocoaPods cài plugin OK; bundle `xyz.antfarms.chinese`; tên launcher "AntFarm Trung" | ☐ |
| 4 | Emulator gọi gateway | chạy 3 backend; `flutter run -d emulator-5554 --dart-define-from-file=config/dev-android.json` | Thẻ "Trạng thái hệ thống" hai chip "đang chạy" qua `http://10.0.2.2:5280` (cleartext chỉ ở debug) | ☐ |
| 5 | Release KHÔNG cleartext | `flutter build apk --release --dart-define-from-file=config/dev-android.json` rồi cài | Chip báo lỗi (đúng thiết kế — release chỉ HTTPS) | ☐ |
| 6 | Quyền INTERNET ở manifest chính | `aapt dump permissions app-release.apk` hoặc `grep INTERNET android/app/src/main/AndroidManifest.xml` | Có `android.permission.INTERNET` | ☐ |
| 7 | iOS simulator gọi gateway | `flutter run -d "iPhone 17" --dart-define-from-file=config/dev-ios.json` | Hai chip "đang chạy" qua `http://localhost:5280` (`NSAllowsLocalNetworking`) | ☐ |
| 8 | Android thật qua `adb reverse` | `adb reverse tcp:5280 tcp:5280 && flutter run -d <id> --dart-define-from-file=config/dev-ios.json` | Như #4 | ☐ |
| 9 | Chế độ tối | Thêm → Giao diện → Tối; tắt app mở lại | Vẫn tối (lưu `af.themeMode`) | ☐ |
| 10 | Chữ Hán đúng glyph giản thể (RK-M7) | Máy đặt ngôn ngữ tiếng Việt, xem chữ 直 骨 角 (từ M3) | Giống web (không ra glyph Nhật/phồn thể) | ☐ |
| 11 | Bố cục 360×740, chữ hệ thống lớn, xoay ngang | Cài đặt hiển thị → cỡ chữ lớn nhất | Không overflow, bottom nav còn 5 nhãn | ☐ |
| 12 | Tắt app khi còn outbox ôn thẻ (M6) | Ôn 3 thẻ khi tắt mạng → tắt app → bật mạng → mở lại | Đánh giá tự gửi, DB không trùng | ☐ |
| 13 | Gỡ app iOS rồi cài lại (RM-S4, M2) | Xoá app → cài lại | Phải đăng nhập lại (Keychain cũ bị xoá) | ☐ |
| 14 | TTS (M3) | Thêm → Giọng đọc: danh sách giọng `zh` (không có `zh-HK`), Nghe thử 你好 ở tốc độ 0,8 rồi 1,2; Android có giọng Google tiếng Trung; iOS gạt im lặng vẫn nghe; máy chưa có giọng ⇒ hướng dẫn cài + "Dò lại giọng" sau khi cài | Nghe được, 0,8 hơi chậm hơn tự nhiên (không nhanh gấp đôi). Hệ số quy đổi hiện tại: Android và iOS ×0,5, web ×1 (`AfTts.platformRate`, đọc từ mã plugin) — nếu Android đọc quá chậm thì sửa hệ số Android về ×1 và ghi lại đây | ☐ |
| 15 | Viết chữ bằng ngón tay (M10) | Vẽ 爱 đúng thứ tự | Không cuộn trang khi vẽ; không báo sai oan | ☐ |
| 16 | Release với `config/prod.json` (khi F12 lên server) | `flutter build apk --release --dart-define-from-file=config/prod.json` | Đăng nhập `https://id.antfarms.xyz/api/auth/mobile/login` OK; `identity.refresh_tokens.client_app` = `chinese-mobile/… (android|ios)` | ☐ |
| 17 | Secure storage iOS (M2) | Đăng nhập → tắt hẳn app → mở lại | Vẫn đăng nhập (Keychain `first_unlock_this_device`). Nếu KHÔNG giữ phiên: README flutter_secure_storage 11 yêu cầu thêm `keychain-access-groups` (mảng rỗng) vào `ios/Runner/*.entitlements` + `CODE_SIGN_ENTITLEMENTS` — chưa thêm vì chưa build được để kiểm | ☐ |
| 18 | Secure storage Android (M2) | Như #17 trên emulator API ≥ 24 | Vẫn đăng nhập; `adb logcat` không có `InvalidKeyException` (allowBackup=false) | ☐ |
| 19 | Múi giờ đăng ký (M2) | Máy đặt múi giờ khác (vd Asia/Tokyo) → đăng ký | Dòng "Múi giờ: Asia/Tokyo (theo máy)"; `identity.users.time_zone` đúng | ☐ |
| 20 | Mất phiên khi app ở nền (M2) | Đăng nhập → revoke họ token trong DB → đưa app ra nền > 15 phút → mở lại | Về `/dang-nhap` có banner "Phiên đăng nhập đã hết hạn", không crash | ☐ |

Ghi kết quả (ngày, thiết bị, phiên bản OS, lệch nếu có) vào bảng này khi verify.
