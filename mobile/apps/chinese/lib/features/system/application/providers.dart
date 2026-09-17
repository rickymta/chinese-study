import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../data/models.dart';
import '../data/system_api.dart';

/// Trạng thái từng service (tương đương `useSystemInfo` web). `autoDispose` ⇒ rời trang chủ là ngừng giữ cache;
/// `ref.invalidate` khi người dùng bấm chip/kéo làm mới.
final systemInfoProvider = FutureProvider.autoDispose.family<SystemInfo, SystemService>((ref, service) {
  final dio = switch (service) {
    SystemService.chinese => ref.watch(chineseDioProvider),
    SystemService.identity => ref.watch(identityDioProvider),
  };
  return getSystemInfo(dio);
});
