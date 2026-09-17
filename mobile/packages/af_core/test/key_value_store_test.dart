import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences_platform_interface/in_memory_shared_preferences_async.dart';
import 'package:shared_preferences_platform_interface/shared_preferences_async_platform_interface.dart';

void main() {
  group('InMemoryKeyValueStore', () {
    test('đọc/ghi/xoá đúng kiểu', () async {
      final s = InMemoryKeyValueStore({'x': 'y'});
      expect(await s.getString('x'), 'y');
      expect(await s.getBool('x'), isNull); // sai kiểu ⇒ null, không ném
      await s.setBool('b', true);
      await s.setInt('i', 3);
      expect(await s.getBool('b'), isTrue);
      expect(await s.getInt('i'), 3);
      expect(await s.containsKey('i'), isTrue);
      await s.remove('i');
      expect(await s.containsKey('i'), isFalse);
      expect(s.snapshot, {'x': 'y', 'b': true});
    });
  });

  group('SharedPrefsKeyValueStore', () {
    setUp(() {
      TestWidgetsFlutterBinding.ensureInitialized();
      SharedPreferencesAsyncPlatform.instance = InMemorySharedPreferencesAsync.empty();
    });

    test('ghi rồi đọc lại qua SharedPreferencesAsync', () async {
      final s = SharedPrefsKeyValueStore();
      expect(await s.setString('af.themeMode', 'dark'), isTrue);
      expect(await s.getString('af.themeMode'), 'dark');
      expect(await s.getString('thieu'), isNull);
      expect(await s.remove('af.themeMode'), isTrue);
      expect(await s.containsKey('af.themeMode'), isFalse);
    });
  });
}
