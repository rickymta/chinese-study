import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('SecureTokenStore (flutter_secure_storage giả trong bộ nhớ)', () {
    test('write → read vòng tròn dưới khoá af.auth.session; clear xoá', () async {
      FlutterSecureStorage.setMockInitialValues({});
      final store = SecureTokenStore();
      expect(await store.read(), isNull);

      await store.write(storedSession(token: 'rt-x'));
      final raw = await SecureTokenStore.defaultStorage.read(key: kSessionStorageKey);
      expect(raw, isNotNull);
      expect((jsonDecode(raw!) as Map)['refreshToken'], 'rt-x');
      expect(raw.contains('accessToken'), isFalse); // RM-S1: access token không bao giờ vào kho

      final back = await store.read();
      expect(back?.refreshToken, 'rt-x');
      expect(back?.account, testAccount);

      await store.clear();
      expect(await store.read(), isNull);
    });

    test('nội dung rác / sai phiên bản ⇒ null và kho tự xoá (RM-S4)', () async {
      FlutterSecureStorage.setMockInitialValues({kSessionStorageKey: 'không phải json'});
      final store = SecureTokenStore();
      expect(await store.read(), isNull);
      expect(await SecureTokenStore.defaultStorage.read(key: kSessionStorageKey), isNull);

      FlutterSecureStorage.setMockInitialValues({
        kSessionStorageKey: jsonEncode({'v': 99, 'refreshToken': 'x'}),
      });
      expect(await SecureTokenStore().read(), isNull);
      expect(await SecureTokenStore.defaultStorage.read(key: kSessionStorageKey), isNull);
    });

    test('clearAll chỉ xoá khoá af.auth.*', () async {
      FlutterSecureStorage.setMockInitialValues({kSessionStorageKey: '{}', 'af.auth.khac': 'x', 'khac.app': 'giữ'});
      await SecureTokenStore().clearAll();
      final all = await SecureTokenStore.defaultStorage.readAll();
      expect(all, {'khac.app': 'giữ'});
    });

    test('cấu hình iOS first_unlock_this_device (không đồng bộ iCloud)', () {
      final opts = SecureTokenStore.defaultStorage.iOptions;
      expect(opts.accessibility, KeychainAccessibility.first_unlock_this_device);
      expect(opts.synchronizable, isFalse);
    });
  });

  group('InstallGuard (RM-S4)', () {
    test('lần chạy đầu (thiếu cờ) ⇒ xoá sạch kho token rồi đặt cờ; lần sau không xoá', () async {
      final prefs = InMemoryKeyValueStore();
      final tokens = InMemoryTokenStore(storedSession());
      final guard = InstallGuard(prefs: prefs, tokenStore: tokens);

      expect(await guard.ensure(), isTrue);
      expect(tokens.session, isNull);
      expect(tokens.log, ['clearAll']);
      expect(await prefs.getBool(kInstallFlagKey), isTrue);

      tokens.session = storedSession();
      expect(await guard.ensure(), isFalse);
      expect(tokens.session, isNotNull);
    });
  });
  test('InstallGuard: shared_preferences hỏng (đọc null, ghi thất bại) ⇒ KHÔNG xoá phiên, không đặt cờ', () async {
    final tokens = InMemoryTokenStore(storedSession());
    final guard = InstallGuard(prefs: _BrokenPrefs(), tokenStore: tokens);
    expect(await guard.ensure(), isFalse);
    expect(tokens.session, isNotNull);
    expect(tokens.log, isEmpty);
  });
}

/// Kho prefs giả lập hỏng: mọi đọc trả null, mọi ghi trả false (như `SharedPrefsKeyValueStore` khi nền tảng lỗi).
class _BrokenPrefs implements KeyValueStore {
  @override
  Future<bool> containsKey(String key) async => false;
  @override
  Future<bool?> getBool(String key) async => null;
  @override
  Future<int?> getInt(String key) async => null;
  @override
  Future<String?> getString(String key) async => null;
  @override
  Future<bool> remove(String key) async => false;
  @override
  Future<bool> setBool(String key, bool value) async => false;
  @override
  Future<bool> setInt(String key, int value) async => false;
  @override
  Future<bool> setString(String key, String value) async => false;
}
