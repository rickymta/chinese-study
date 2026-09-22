import 'package:flutter/foundation.dart';

/// Ghi log chẩn đoán — CHỈ ở bản debug (`kDebugMode`), bản release im lặng.
///
/// Nơi DUY NHẤT được gọi `debugPrint` trong workspace (luật `print-call` của `tool/check_conventions.dart`).
/// KHÔNG bao giờ log token, mật khẩu hay thân request/response (RM-A10, RM-S1).
void afLog(String message) {
  if (kDebugMode) debugPrint('[af] $message');
}
