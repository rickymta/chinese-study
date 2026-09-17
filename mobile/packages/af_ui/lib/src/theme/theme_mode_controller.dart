import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/key_value_store_provider.dart';

/// Khoá lưu chế độ giao diện trong `shared_preferences` (hợp đồng §5.1.2).
const kThemeModeKey = 'af.themeMode';

/// Chế độ giao diện `system | light | dark`, lưu bền, đọc lại lúc khởi động.
///
/// Riverpod 3: `Notifier.build()` đồng bộ ⇒ trả `system` ngay rồi nạp giá trị đã lưu ở nền (không chặn khung hình
/// đầu; đổi từ system sang dark sau vài ms là chấp nhận được).
class ThemeModeController extends Notifier<ThemeMode> {
  /// Người dùng đã tự chọn trong phiên ⇒ kết quả nạp từ kho (về sau) không được đè lên lựa chọn đó.
  bool _userChanged = false;

  @override
  ThemeMode build() {
    unawaited(_restore());
    return ThemeMode.system;
  }

  Future<void> _restore() async {
    final store = ref.read(keyValueStoreProvider);
    final saved = await store.getString(kThemeModeKey);
    if (!ref.mounted || _userChanged) return;
    final mode = parseThemeMode(saved);
    if (mode != null && mode != state) state = mode;
  }

  /// Đặt chế độ và lưu bền. Lưu lỗi ⇒ vẫn đổi giao diện trong phiên (kho hỏng không chặn người dùng).
  Future<void> setMode(ThemeMode mode) async {
    _userChanged = true;
    state = mode;
    await ref.read(keyValueStoreProvider).setString(kThemeModeKey, mode.name);
  }

  /// Chuyển nhanh sáng ⇄ tối (từ `system` ⇒ theo độ sáng hiện tại của nền tảng để đảo ngược).
  Future<void> toggle(Brightness platformBrightness) {
    final current = state == ThemeMode.system
        ? (platformBrightness == Brightness.dark ? ThemeMode.dark : ThemeMode.light)
        : state;
    return setMode(current == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark);
  }
}

final themeModeProvider = NotifierProvider<ThemeModeController, ThemeMode>(ThemeModeController.new);

/// `system|light|dark` ⇒ [ThemeMode]; chuỗi lạ/null ⇒ null.
ThemeMode? parseThemeMode(String? value) => switch (value) {
  'system' => ThemeMode.system,
  'light' => ThemeMode.light,
  'dark' => ThemeMode.dark,
  _ => null,
};

/// Nhãn tiếng Việt cho màn Hồ sơ/Thêm.
String themeModeLabel(ThemeMode mode) => switch (mode) {
  ThemeMode.system => 'Theo hệ thống',
  ThemeMode.light => 'Sáng',
  ThemeMode.dark => 'Tối',
};
