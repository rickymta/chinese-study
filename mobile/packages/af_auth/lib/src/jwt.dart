import 'dart:convert';

import 'package:af_core/af_core.dart';

import 'models.dart';

/// Giải mã payload JWT (base64url) — KHÔNG kiểm chữ ký (chỉ để hiển thị; quyền lấy từ service ngôn ngữ).
/// Token hỏng ⇒ null. Port `decodeJwtPayload` của `@af/auth`.
JsonMap? decodeJwtPayload(String token) {
  try {
    final parts = token.split('.');
    if (parts.length < 2 || parts[1].isEmpty) return null;
    final json = utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
    return asJsonMap(jsonDecode(json));
  } on Object {
    return null;
  }
}

/// Dựng [Account] tối thiểu từ claim (`sub`, `email`, `name`, `zoneinfo`) sau khi làm mới — không cần gọi
/// `GET /api/account`. Thiếu `sub` ⇒ null. Port `accountFromToken` của `@af/auth`.
Account? accountFromToken(String token) {
  final claims = decodeJwtPayload(token);
  final sub = readString(claims, 'sub');
  if (claims == null || sub == null || sub.isEmpty) return null;
  final tz = readString(claims, 'zoneinfo');
  return Account(
    id: sub,
    email: readStringOr(claims, 'email'),
    displayName: readStringOr(claims, 'name'),
    timeZone: (tz == null || tz.isEmpty) ? kDefaultTimeZone : tz,
  );
}
