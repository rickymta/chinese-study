import 'dart:math';

final Random _random = Random.secure();

/// Sinh UUID v4 (RFC 4122) bằng `Random.secure()` — KHÔNG dùng package `uuid` (hợp đồng mobile RM-L3, DB-M12).
///
/// Dùng cho `clientReviewId`, `clientAttemptId`, `clientSessionId` (idempotent) — sinh MỘT lần lúc bắt đầu lượt,
/// giữ nguyên mọi lần gửi lại.
String uuidV4() {
  final bytes = List<int>.generate(16, (_) => _random.nextInt(256));
  bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
  bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant RFC 4122
  final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-'
      '${hex.substring(16, 20)}-${hex.substring(20)}';
}

final RegExp _uuidV4Pattern = RegExp(r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$');

/// Kiểm chuỗi có đúng dạng UUID v4 chữ thường (đúng dạng backend chấp nhận).
bool isUuidV4(String value) => _uuidV4Pattern.hasMatch(value);
