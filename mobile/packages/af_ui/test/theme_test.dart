import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('buildAfTheme', () {
    test('sáng: đúng màu chốt hợp đồng §5.3.7', () {
      final t = buildAfTheme(brightness: Brightness.light);
      expect(t.brightness, Brightness.light);
      expect(t.colorScheme.primary, const Color(0xFF2E7D32));
      expect(t.colorScheme.onPrimary, const Color(0xFFFFFFFF));
      expect(t.colorScheme.secondary, const Color(0xFFC62828));
      expect(t.colorScheme.surface, const Color(0xFFFFFFFF));
      expect(t.scaffoldBackgroundColor, const Color(0xFFFAFAF7));
      expect(t.useMaterial3, isTrue);
    });

    test('tối: primary sáng hơn, accent được làm sáng, nền tối', () {
      final t = buildAfTheme(brightness: Brightness.dark);
      expect(t.brightness, Brightness.dark);
      expect(t.colorScheme.primary, const Color(0xFF66BB6A));
      expect(t.colorScheme.onPrimary, const Color(0xFF0B1410));
      expect(t.colorScheme.surface, const Color(0xFF1A201C));
      expect(t.scaffoldBackgroundColor, const Color(0xFF101412));
      expect(t.colorScheme.secondary, lightenForDark(const Color(0xFFC62828)));
      expect(t.colorScheme.secondary.computeLuminance(), greaterThan(const Color(0xFFC62828).computeLuminance()));
    });

    test('accentDark tuỳ biến được ưu tiên', () {
      final t = buildAfTheme(brightness: Brightness.dark, accentDark: const Color(0xFFEF5350));
      expect(t.colorScheme.secondary, const Color(0xFFEF5350));
    });

    test('thẻ viền mảnh không bóng, bo góc 10, nút cao ≥ 48', () {
      final t = buildAfTheme(brightness: Brightness.light);
      expect(t.cardTheme.elevation, 0);
      final shape = t.cardTheme.shape! as RoundedRectangleBorder;
      expect(shape.borderRadius, BorderRadius.circular(10));
      expect(shape.side.width, greaterThan(0));
      final min = t.filledButtonTheme.style!.minimumSize!.resolve({});
      expect(min!.height, greaterThanOrEqualTo(48));
    });

    test('lightenForDark: #C62828 ⇒ #DA7373 (khớp hàm lighten web: c + (255 − c) × 0,35)', () {
      expect(lightenForDark(const Color(0xFFC62828)), const Color(0xFFDA7373));
    });
  });

  group('parseThemeMode / themeModeLabel', () {
    test('chuỗi hợp lệ và lạ', () {
      expect(parseThemeMode('system'), ThemeMode.system);
      expect(parseThemeMode('light'), ThemeMode.light);
      expect(parseThemeMode('dark'), ThemeMode.dark);
      expect(parseThemeMode('xyz'), isNull);
      expect(parseThemeMode(null), isNull);
      expect(themeModeLabel(ThemeMode.dark), 'Tối');
    });
  });
}
