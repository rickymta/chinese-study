import 'dart:convert';

import 'package:af_core/af_core.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'models.dart';

/// Tiền tố mọi khoá của af_auth trong secure storage — `InstallGuard` xoá theo tiền tố này (RM-S4).
const kAuthStoragePrefix = 'af.auth.';

/// Khoá lưu phiên (hợp đồng mobile §5.1.2).
const kSessionStorageKey = '${kAuthStoragePrefix}session';

/// Kho refresh token — nơi DUY NHẤT refresh token được lưu bền (RM-S1). Access token không bao giờ vào đây.
abstract class TokenStore {
  /// Phiên đã lưu; không có / hỏng ⇒ null. Đọc hỏng (giải mã lỗi, JSON rác) ⇒ tự xoá kho, KHÔNG ném (RM-S4).
  Future<StoredSession?> read();

  /// Ghi đè phiên. Ném khi nền tảng ghi lỗi — người gọi quyết định (AuthSession ghi log và tiếp tục trong bộ nhớ).
  Future<void> write(StoredSession session);

  /// Xoá phiên. Không ném.
  Future<void> clear();

  /// Xoá MỌI khoá `af.auth.*` (cài lại app trên iOS còn Keychain cũ — RM-S4). Không ném.
  Future<void> clearAll();
}

/// Bọc `flutter_secure_storage` 11.x: iOS Keychain `first_unlock_this_device` (không đồng bộ iCloud, không migrate
/// sang máy khác); Android mặc định v11 (RSA-OAEP + AES-GCM, không còn backend Jetpack Security cũ);
/// web dev: WebCrypto + localStorage, chỉ chạy trên HTTPS/localhost — chấp nhận vì bản web chỉ để dev (RK-M19).
class SecureTokenStore implements TokenStore {
  SecureTokenStore([FlutterSecureStorage? storage]) : _storage = storage ?? defaultStorage;

  /// Cấu hình dùng chung mọi app AntFarm.
  static const FlutterSecureStorage defaultStorage = FlutterSecureStorage(
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock_this_device),
    aOptions: AndroidOptions(),
  );

  final FlutterSecureStorage _storage;

  @override
  Future<StoredSession?> read() async {
    String? raw;
    try {
      raw = await _storage.read(key: kSessionStorageKey);
    } on Object catch (e) {
      // Giải mã hỏng (khoá Keystore đổi sau khôi phục backup — RK-M3) ⇒ xoá kho, coi như chưa đăng nhập.
      afLog('SecureTokenStore.read lỗi (${e.runtimeType}) — xoá kho');
      await clear();
      return null;
    }
    if (raw == null || raw.isEmpty) return null;
    StoredSession? session;
    try {
      session = StoredSession.fromJson(asJsonMap(jsonDecode(raw)));
    } on Object {
      session = null;
    }
    if (session == null) {
      afLog('SecureTokenStore.read: nội dung không hợp lệ — xoá kho');
      await clear();
    }
    return session;
  }

  @override
  Future<void> write(StoredSession session) =>
      _storage.write(key: kSessionStorageKey, value: jsonEncode(session.toJson()));

  @override
  Future<void> clear() async {
    try {
      await _storage.delete(key: kSessionStorageKey);
    } on Object catch (e) {
      afLog('SecureTokenStore.clear lỗi (${e.runtimeType})');
    }
  }

  @override
  Future<void> clearAll() async {
    try {
      final all = await _storage.readAll();
      for (final key in all.keys.where((k) => k.startsWith(kAuthStoragePrefix)).toList()) {
        await _storage.delete(key: key);
      }
    } on Object catch (e) {
      // Không liệt kê được (dữ liệu cũ giải mã hỏng) ⇒ xoá toàn bộ kho của app — chỉ af_auth dùng secure storage.
      afLog('SecureTokenStore.clearAll: readAll lỗi (${e.runtimeType}) — deleteAll');
      try {
        await _storage.deleteAll();
      } on Object catch (e2) {
        afLog('SecureTokenStore.clearAll: deleteAll lỗi (${e2.runtimeType})');
      }
    }
  }
}

/// Kho trong bộ nhớ cho test — ghi lại thứ tự thao tác vào [log] để kiểm "ghi trước khi dùng" (RM-S2).
class InMemoryTokenStore implements TokenStore {
  InMemoryTokenStore([this.session]);

  StoredSession? session;

  /// `read` · `write:<token>` · `clear` · `clearAll` theo thứ tự xảy ra.
  final List<String> log = [];

  /// Ném ở lần `write` kế tiếp (giả lập nền tảng ghi lỗi).
  Object? failNextWrite;

  /// Ném ở lần `read` kế tiếp (giả lập giải mã hỏng) — kho tự xoá như bản thật.
  Object? failNextRead;

  /// Nếu khác null, `write` chờ Future này xong mới ghi (giả lập nền tảng ghi chậm).
  Future<void>? writeGate;

  @override
  Future<StoredSession?> read() async {
    log.add('read');
    final err = failNextRead;
    if (err != null) {
      failNextRead = null;
      session = null;
      log.add('clear');
      return null;
    }
    return session;
  }

  @override
  Future<void> write(StoredSession s) async {
    final gate = writeGate;
    if (gate != null) await gate;
    final err = failNextWrite;
    if (err != null) {
      failNextWrite = null;
      throw err;
    }
    session = s;
    log.add('write:${s.refreshToken}');
  }

  @override
  Future<void> clear() async {
    session = null;
    log.add('clear');
  }

  @override
  Future<void> clearAll() async {
    session = null;
    log.add('clearAll');
  }
}
