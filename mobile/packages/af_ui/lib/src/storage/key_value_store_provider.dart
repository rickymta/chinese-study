import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Kho khoá-giá trị dùng chung cho mọi provider trong app (chế độ giao diện, giọng đọc, outbox...).
///
/// Mặc định bọc `shared_preferences`; test/widget test ghi đè bằng `InMemoryKeyValueStore`:
/// `ProviderScope(overrides: [keyValueStoreProvider.overrideWithValue(InMemoryKeyValueStore())])`.
final keyValueStoreProvider = Provider<KeyValueStore>((ref) => SharedPrefsKeyValueStore());
