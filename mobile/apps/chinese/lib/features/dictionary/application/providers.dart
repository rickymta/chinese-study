import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../data/dictionary_api.dart';
import '../data/models.dart';

/// Chi tiết từ theo id (`GET /dictionary/words/{id}`) — tương đương `useWord` web. autoDispose: sheet đóng ⇒ huỷ.
/// Theo người dùng (khối `srs` là của người đang gọi).
final wordDetailProvider = FutureProvider.autoDispose.family<WordDetail, String>((ref, id) {
  ref.watch(userScopeProvider);
  return getWord(ref.watch(chineseDioProvider), id);
});
