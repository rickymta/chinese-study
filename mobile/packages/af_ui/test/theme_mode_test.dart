import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ThemeModeController', () {
    test('mặc định system, nạp giá trị đã lưu ở nền', () async {
      final store = InMemoryKeyValueStore({kThemeModeKey: 'dark'});
      final container = ProviderContainer.test(overrides: [keyValueStoreProvider.overrideWithValue(store)]);
      expect(container.read(themeModeProvider), ThemeMode.system);
      await Future<void>.delayed(Duration.zero);
      expect(container.read(themeModeProvider), ThemeMode.dark);
    });

    test('setMode đổi state và lưu bền', () async {
      final store = InMemoryKeyValueStore();
      final container = ProviderContainer.test(overrides: [keyValueStoreProvider.overrideWithValue(store)]);
      await container.read(themeModeProvider.notifier).setMode(ThemeMode.light);
      expect(container.read(themeModeProvider), ThemeMode.light);
      expect(store.snapshot[kThemeModeKey], 'light');
    });

    test('toggle từ system theo độ sáng nền tảng', () async {
      final container = ProviderContainer.test(
        overrides: [keyValueStoreProvider.overrideWithValue(InMemoryKeyValueStore())],
      );
      final n = container.read(themeModeProvider.notifier);
      await n.toggle(Brightness.light);
      expect(container.read(themeModeProvider), ThemeMode.dark);
      await n.toggle(Brightness.light);
      expect(container.read(themeModeProvider), ThemeMode.light);
    });

    test('người dùng chọn trước khi nạp kho xong ⇒ giá trị nạp không đè lựa chọn', () async {
      final slow = _SlowStore(saved: 'dark');
      final container = ProviderContainer.test(overrides: [keyValueStoreProvider.overrideWithValue(slow)]);
      final n = container.read(themeModeProvider.notifier);
      await n.setMode(ThemeMode.light); // bấm trước khi kho trả về
      slow.release();
      await Future<void>.delayed(Duration.zero);
      expect(container.read(themeModeProvider), ThemeMode.light);
      expect(slow.snapshot[kThemeModeKey], 'light');
    });

    test('giá trị lưu lạ ⇒ giữ system, không ném', () async {
      final container = ProviderContainer.test(
        overrides: [
          keyValueStoreProvider.overrideWithValue(InMemoryKeyValueStore({kThemeModeKey: 'xyz'})),
        ],
      );
      container.read(themeModeProvider);
      await Future<void>.delayed(Duration.zero);
      expect(container.read(themeModeProvider), ThemeMode.system);
    });
  });

  testWidgets('đổi chế độ tối làm Theme.of(context).brightness đổi', (tester) async {
    late BuildContext captured;
    await tester.pumpWidget(
      ProviderScope(
        overrides: [keyValueStoreProvider.overrideWithValue(InMemoryKeyValueStore())],
        child: Consumer(
          builder: (context, ref, _) => MaterialApp(
            theme: buildAfTheme(brightness: Brightness.light),
            darkTheme: buildAfTheme(brightness: Brightness.dark),
            themeMode: ref.watch(themeModeProvider),
            home: Builder(
              builder: (ctx) {
                captured = ctx;
                return const SizedBox();
              },
            ),
          ),
        ),
      ),
    );
    expect(Theme.of(captured).brightness, Brightness.light);
    final container = ProviderScope.containerOf(captured);
    await container.read(themeModeProvider.notifier).setMode(ThemeMode.dark);
    await tester.pumpAndSettle();
    expect(Theme.of(captured).brightness, Brightness.dark);
  });
}

/// Kho trả `getString` chậm (chờ [release]) để mô phỏng người dùng bấm trước khi nạp xong.
class _SlowStore extends InMemoryKeyValueStore {
  _SlowStore({required String saved}) : super({kThemeModeKey: saved});

  final _gate = Completer<void>();

  void release() => _gate.complete();

  @override
  Future<String?> getString(String key) async {
    await _gate.future;
    return super.getString(key);
  }
}
