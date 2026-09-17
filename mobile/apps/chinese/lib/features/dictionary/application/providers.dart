import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../data/dictionary_api.dart';
import '../data/models.dart';

/// Chi tiết từ theo id cho SHEET "Xem chi tiết" trong phiên ôn (`skipErrorRedirect` — 404 báo trong sheet). autoDispose:
/// sheet đóng ⇒ huỷ. Theo người dùng (khối `srs` là của người đang gọi).
final wordDetailProvider = FutureProvider.autoDispose.family<WordDetail, String>((ref, id) {
  ref.watch(userScopeProvider);
  return getWord(ref.watch(chineseDioProvider), id);
});

/// Chi tiết từ cho TRANG `/tu-dien/:id` — cùng API nhưng 404 ⇒ `/404` qua interceptor (quy tắc trang lỗi thống nhất).
/// Tách khỏi [wordDetailProvider] vì sheet trong phiên ôn không được điều hướng; hai provider đều autoDispose nên chỉ
/// bên đang mở mới giữ cache. Sau khi thêm/tạm dừng thẻ, `AddToSrsButton` làm mới cả hai.
final wordPageProvider = FutureProvider.autoDispose.family<WordDetail, String>((ref, id) {
  ref.watch(userScopeProvider);
  return getWord(ref.watch(chineseDioProvider), id, skipErrorRedirect: false);
});

/// Chi tiết chữ `/tu-dien/chu/:hanzi` — học liệu chung (không theo người dùng), autoDispose (giữ khi trang còn trong
/// ngăn xếp; rời trang ⇒ huỷ). 404 ⇒ `/404` qua interceptor.
final characterDetailProvider = FutureProvider.autoDispose.family<CharacterDetail, String>(
  (ref, hanzi) => getCharacter(ref.watch(chineseDioProvider), hanzi),
);

/// Cấp HSK duy nhất có trong kho (chip "HSK 1") — giữ chỗ cho các cấp sau (§5.3.1 web).
const kHskFilterLevel = 1;

/// Trạng thái trang tìm từ: từ khoá + lọc đang áp, các trang đã tải nối nhau (cuộn vô hạn), lỗi trang đầu / trang kế.
@immutable
class DictionarySearchState {
  const DictionarySearchState({
    this.q = '',
    this.hsk,
    this.items = const [],
    this.page = 0,
    this.totalCount = 0,
    this.loaded = false,
    this.endReached = false,
    this.loading = false,
    this.loadingMore = false,
    this.error,
    this.loadMoreError,
  });

  /// Từ khoá đã áp (đã `trim`).
  final String q;
  final int? hsk;
  final List<WordSummary> items;

  /// Trang cao nhất đã tải (0 = chưa có).
  final int page;
  final int totalCount;

  /// Đã có ít nhất một phản hồi thành công cho `q`/`hsk` hiện tại (phân biệt "chưa tìm" với "rỗng").
  final bool loaded;

  /// Server đã trả trang cuối (ít hơn `pageSize`/rỗng) hoặc trang kế không thêm được dòng mới ⇒ dừng tải, KHÔNG dựa
  /// vào `items.length < totalCount` (dòng hỏng bị lược, học liệu đổi giữa lúc phân trang ⇒ không bao giờ đuổi kịp ⇒
  /// gọi API liên tục — review M8).
  final bool endReached;

  /// Đang tải trang đầu của `q`/`hsk` hiện tại (danh sách cũ vẫn hiện mờ — như `keepPreviousData` web).
  final bool loading;
  final bool loadingMore;

  /// Lỗi trang đầu (không có dữ liệu để hiện).
  final ApiError? error;

  /// Lỗi khi tải trang kế — danh sách vẫn hiện, kèm nút thử lại ở cuối.
  final ApiError? loadMoreError;

  bool get isBrowsing => q.isEmpty;
  bool get hasMore => loaded && !endReached && items.length < totalCount;

  DictionarySearchState copyWith({
    String? q,
    int? hsk,
    bool clearHsk = false,
    List<WordSummary>? items,
    int? page,
    int? totalCount,
    bool? loaded,
    bool? endReached,
    bool? loading,
    bool? loadingMore,
    ApiError? error,
    bool clearError = false,
    ApiError? loadMoreError,
    bool clearLoadMoreError = false,
  }) => DictionarySearchState(
    q: q ?? this.q,
    hsk: clearHsk ? null : (hsk ?? this.hsk),
    items: items ?? this.items,
    page: page ?? this.page,
    totalCount: totalCount ?? this.totalCount,
    loaded: loaded ?? this.loaded,
    endReached: endReached ?? this.endReached,
    loading: loading ?? this.loading,
    loadingMore: loadingMore ?? this.loadingMore,
    error: clearError ? null : (error ?? this.error),
    loadMoreError: clearLoadMoreError ? null : (loadMoreError ?? this.loadMoreError),
  );
}

/// Bộ điều khiển tìm từ (`/tu-dien`) — tương đương `useWordSearch` + URL của web nhưng giữ state TRONG NHÁNH (hợp đồng
/// M8): trang chi tiết đẩy lên trên, quay lại vẫn thấy đúng kết quả + vị trí cuộn (widget giữ `ScrollController`,
/// provider giữ danh sách). KHÔNG autoDispose để không mất kết quả khi trang tìm bị che (Riverpod 3 tạm dừng
/// subscription của widget không hiển thị); `build` theo `userScopeProvider` ⇒ đăng xuất/đổi người tự đặt lại.
///
/// Mọi phản hồi mang "thế hệ" lúc gửi: đổi từ khoá trong lúc chờ ⇒ phản hồi cũ bị bỏ (không ghép số cũ với `q` mới).
class DictionarySearchNotifier extends Notifier<DictionarySearchState> {
  int _generation = 0;

  @override
  DictionarySearchState build() {
    ref.watch(userScopeProvider);
    _generation++;
    return const DictionarySearchState();
  }

  /// Áp từ khoá/lọc mới rồi tải trang 1. Không đổi gì so với state đã tải ⇒ bỏ qua (mở lại trang không gọi lại API).
  Future<void> search({String? q, int? hsk, bool clearHsk = false}) async {
    final nextQ = (q ?? state.q).trim();
    final nextHsk = clearHsk ? null : (hsk ?? state.hsk);
    if (nextQ == state.q && nextHsk == state.hsk && (state.loaded || state.loading)) return;
    state = state.copyWith(
      q: nextQ,
      hsk: nextHsk,
      clearHsk: nextHsk == null,
      loading: true,
      loaded: false,
      endReached: false,
      clearError: true,
      clearLoadMoreError: true,
      loadingMore: false,
    );
    await _fetchFirstPage();
  }

  /// Chip "HSK 1": bật/tắt lọc.
  Future<void> toggleHsk() => state.hsk == kHskFilterLevel ? search(clearHsk: true) : search(hsk: kHskFilterLevel);

  /// Tải lại trang 1 của từ khoá hiện tại (kéo-để-làm-mới, nút Thử lại).
  Future<void> reload() async {
    state = state.copyWith(
      loading: true,
      endReached: false,
      clearError: true,
      clearLoadMoreError: true,
      loadingMore: false,
    );
    await _fetchFirstPage();
  }

  /// Tải trang kế (cuộn vô hạn) — bỏ qua khi đang tải hoặc đã hết.
  Future<void> loadMore() async {
    if (!state.hasMore || state.loading || state.loadingMore) return;
    final gen = _generation;
    final nextPage = state.page + 1;
    state = state.copyWith(loadingMore: true, clearLoadMoreError: true);
    try {
      final res = await searchWords(ref.read(chineseDioProvider), q: state.q, hsk: state.hsk, page: nextPage);
      if (!ref.mounted || gen != _generation) return;
      final merged = mergeUniqueById(state.items, res.items);
      state = state.copyWith(
        items: merged,
        page: nextPage,
        totalCount: res.totalCount,
        // Trang cuối / rỗng / không thêm được dòng mới (học liệu đổi, trùng id) ⇒ hết.
        endReached: res.isLastPage || merged.length == state.items.length,
        loadingMore: false,
      );
    } on Object catch (e) {
      if (!ref.mounted || gen != _generation) return;
      state = state.copyWith(loadingMore: false, loadMoreError: ApiError.from(e));
    }
  }

  Future<void> _fetchFirstPage() async {
    final gen = ++_generation;
    final q = state.q;
    final hsk = state.hsk;
    try {
      final res = await searchWords(ref.read(chineseDioProvider), q: q, hsk: hsk, page: 1);
      if (!ref.mounted || gen != _generation) return;
      state = state.copyWith(
        items: mergeUniqueById(const [], res.items),
        page: 1,
        totalCount: res.totalCount,
        loaded: true,
        endReached: res.isLastPage,
        loading: false,
        clearError: true,
      );
    } on Object catch (e) {
      if (!ref.mounted || gen != _generation) return;
      state = state.copyWith(
        items: const [],
        page: 0,
        totalCount: 0,
        loaded: false,
        loading: false,
        error: ApiError.from(e),
      );
    }
  }
}

/// Nối trang kế vào danh sách, bỏ dòng trùng `id` (server đổi thứ tự/học liệu giữa lúc phân trang ⇒ tránh hai
/// `ValueKey` giống nhau trong `ListView`).
List<WordSummary> mergeUniqueById(List<WordSummary> current, List<WordSummary> incoming) {
  final seen = {for (final w in current) w.id};
  final out = [...current];
  for (final w in incoming) {
    if (seen.add(w.id)) out.add(w);
  }
  return out;
}

final dictionarySearchProvider = NotifierProvider<DictionarySearchNotifier, DictionarySearchState>(
  DictionarySearchNotifier.new,
);
