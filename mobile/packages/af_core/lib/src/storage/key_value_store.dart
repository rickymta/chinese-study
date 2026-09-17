import 'package:shared_preferences/shared_preferences.dart';

import '../log/af_log.dart';

/// Kho khoá-giá trị KHÔNG nhạy cảm (chế độ giao diện, giọng đọc, outbox ôn thẻ, cờ cài đặt...).
///
/// CẤM lưu refresh/access token ở đây (RM-S1 — token chỉ ở secure storage / bộ nhớ). Mọi đọc/ghi phải nuốt lỗi
/// và trả mặc định (hợp đồng §5.1.2) — kho hỏng không được làm app trắng.
abstract class KeyValueStore {
  Future<String?> getString(String key);
  Future<bool> setString(String key, String value);
  Future<bool?> getBool(String key);
  Future<bool> setBool(String key, bool value);
  Future<int?> getInt(String key);
  Future<bool> setInt(String key, int value);
  Future<bool> remove(String key);
  Future<bool> containsKey(String key);
}

/// Bọc `SharedPreferencesAsync` (API mới của shared_preferences 2.5.x — không cache, mọi lời gọi bất đồng bộ).
class SharedPrefsKeyValueStore implements KeyValueStore {
  SharedPrefsKeyValueStore([SharedPreferencesAsync? prefs]) : _prefs = prefs ?? SharedPreferencesAsync();

  final SharedPreferencesAsync _prefs;

  Future<T?> _guard<T>(String op, String key, Future<T?> Function() run) async {
    try {
      return await run();
    } on Object catch (e) {
      afLog('KeyValueStore.$op("$key") lỗi: $e');
      return null;
    }
  }

  Future<bool> _guardWrite(String op, String key, Future<void> Function() run) async {
    try {
      await run();
      return true;
    } on Object catch (e) {
      afLog('KeyValueStore.$op("$key") lỗi: $e');
      return false;
    }
  }

  @override
  Future<String?> getString(String key) => _guard('getString', key, () => _prefs.getString(key));

  @override
  Future<bool> setString(String key, String value) => _guardWrite('setString', key, () => _prefs.setString(key, value));

  @override
  Future<bool?> getBool(String key) => _guard('getBool', key, () => _prefs.getBool(key));

  @override
  Future<bool> setBool(String key, bool value) => _guardWrite('setBool', key, () => _prefs.setBool(key, value));

  @override
  Future<int?> getInt(String key) => _guard('getInt', key, () => _prefs.getInt(key));

  @override
  Future<bool> setInt(String key, int value) => _guardWrite('setInt', key, () => _prefs.setInt(key, value));

  @override
  Future<bool> remove(String key) => _guardWrite('remove', key, () => _prefs.remove(key));

  @override
  Future<bool> containsKey(String key) async =>
      await _guard('containsKey', key, () => _prefs.containsKey(key)) ?? false;
}

/// Kho trong bộ nhớ cho test/widget test (không đụng plugin).
class InMemoryKeyValueStore implements KeyValueStore {
  InMemoryKeyValueStore([Map<String, Object>? initial]) : _data = {...?initial};

  final Map<String, Object> _data;

  /// Bản sao dữ liệu hiện có (để assert trong test).
  Map<String, Object> get snapshot => Map.unmodifiable(_data);

  @override
  Future<String?> getString(String key) async => _data[key] is String ? _data[key]! as String : null;

  @override
  Future<bool> setString(String key, String value) async {
    _data[key] = value;
    return true;
  }

  @override
  Future<bool?> getBool(String key) async => _data[key] is bool ? _data[key]! as bool : null;

  @override
  Future<bool> setBool(String key, bool value) async {
    _data[key] = value;
    return true;
  }

  @override
  Future<int?> getInt(String key) async => _data[key] is int ? _data[key]! as int : null;

  @override
  Future<bool> setInt(String key, int value) async {
    _data[key] = value;
    return true;
  }

  @override
  Future<bool> remove(String key) async {
    _data.remove(key);
    return true;
  }

  @override
  Future<bool> containsKey(String key) async => _data.containsKey(key);
}
