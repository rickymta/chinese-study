import 'package:dio/dio.dart';

/// Khoá trong `Options.extra` — tương đương hai cờ mở rộng của axios ở web (`@af/api`).
const kSkipErrorRedirect = 'af.skipErrorRedirect';
const kSkipAuthRefresh = 'af.skipAuthRefresh';

/// Tạo [Options] cho một request với cờ tuỳ chọn.
///
/// - [skipErrorRedirect] `true` ⇒ TẮT điều hướng tự động tới `/403`/`/404` khi request GET này nhận lỗi tương ứng.
///   Dùng cho lời gọi nền không muốn làm mất trang hiện tại (kiểm tra trạng thái service, `/me`, badge...).
/// - [skipAuthRefresh] `true` ⇒ KHÔNG làm mới token rồi gửi lại khi nhận 401. Tự bật cho lần gửi lại và cho
///   `/auth/(mobile/)?(login|register|refresh|logout)`.
Options afOptions({bool skipErrorRedirect = false, bool skipAuthRefresh = false, Options? base}) {
  final extra = <String, Object?>{...?base?.extra};
  if (skipErrorRedirect) extra[kSkipErrorRedirect] = true;
  if (skipAuthRefresh) extra[kSkipAuthRefresh] = true;
  return (base ?? Options()).copyWith(extra: extra);
}

extension AfRequestOptionsX on RequestOptions {
  bool get skipErrorRedirect => extra[kSkipErrorRedirect] == true;
  bool get skipAuthRefresh => extra[kSkipAuthRefresh] == true;
}
