import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../../progress/application/providers.dart';
import '../data/srs_api.dart';
import '../domain/review_outbox.dart';
import 'providers.dart';

/// Hàm gửi một lượt chấm lên server — test override bằng bản giả (đếm lần gọi, ném lỗi mạng).
final outboxSenderProvider = Provider<SendReview>(
  (ref) =>
      (item) => postReview(
        ref.read(chineseDioProvider),
        cardId: item.cardId,
        clientReviewId: item.clientReviewId,
        rating: item.rating,
        durationMs: item.durationMs,
      ),
);

/// Một lượt bị BỎ vì 4xx thật (409 trùng mã, 422 thẻ tạm dừng/đủ từ mới, 400/404) hoặc vì kho đầy — app hiện toast
/// (`OutboxNotices` ở `app.dart`). [seq] tăng dần để `ref.listen` phân biệt hai lần bỏ liên tiếp cùng nội dung.
@immutable
class OutboxDrop {
  const OutboxDrop({required this.seq, required this.item, required this.error});

  final int seq;
  final OutboxItem item;
  final ApiError error;

  @override
  bool operator ==(Object other) => other is OutboxDrop && other.seq == seq;

  @override
  int get hashCode => seq;
}

/// Trạng thái outbox của người dùng hiện tại (port `ReviewOutboxApi` web).
@immutable
class ReviewOutboxState {
  const ReviewOutboxState({this.userId, this.items = const [], this.restored = false, this.lastDrop});

  final String? userId;

  /// Hàng đợi theo thứ tự chấm.
  final List<OutboxItem> items;

  /// Đã đọc xong kho bền của [userId]. Trước đó [items] chỉ là lượt vừa chấm trong phiên này — CHƯA ghi kho, CHƯA gửi
  /// (ghi kho lúc chưa đọc xong sẽ đè mất phần đã lưu — review M6 C2).
  final bool restored;
  final OutboxDrop? lastDrop;

  /// Số đánh giá đang trong hàng đợi (kể cả lượt vừa chấm đang gửi lần đầu) — dùng cho xác nhận đăng xuất (RM-S7).
  int get pendingCount => items.length;

  /// Số đánh giá đang KẸT: `0` khi mọi phần tử còn đang gửi lần đầu (banner không nháy mỗi lần chấm); khi có phần tử
  /// `attempts > 0` (gửi hỏng, hàng đợi dừng tại đó) ⇒ bằng cả hàng đợi, vì các lượt sau cũng chờ theo (offline chấm
  /// 3 thẻ ⇒ "Đang chờ gửi 3"). Dùng cho banner và hộp xác nhận khi đóng phiên.
  int get unsentCount => items.any((it) => it.attempts > 0) ? items.length : 0;

  /// `cardId` của các lượt đang chờ — bộ bài bỏ thẻ này khi tải thêm (server chưa biết lượt vừa chấm).
  Set<String> get pendingCardIds => {for (final it in items) it.cardId};

  ReviewOutboxState copyWith({List<OutboxItem>? items, bool? restored, OutboxDrop? lastDrop}) => ReviewOutboxState(
    userId: userId,
    items: items ?? this.items,
    restored: restored ?? this.restored,
    lastDrop: lastDrop ?? this.lastDrop,
  );

  @override
  bool operator ==(Object other) =>
      other is ReviewOutboxState &&
      other.userId == userId &&
      listEquals(other.items, items) &&
      other.restored == restored &&
      other.lastDrop == lastDrop;

  @override
  int get hashCode => Object.hash(userId, Object.hashAll(items), restored, lastDrop);
}

/// Outbox ôn thẻ BỀN theo người dùng (RM-L1): giữ hàng đợi trong bộ nhớ + ghi `KeyValueStore` khoá
/// `af.srs.outbox.<userId>` sau MỖI thay đổi (sau khi đã đọc kho); gửi tuần tự, single-flight; hẹn gửi lại bậc thang;
/// kích hoạt gửi khi: chấm (`submit`), đọc xong kho lúc dựng (mở app / đăng nhập đúng user), app resumed (`flushNow`
/// từ `app.dart`). Gửi thành công ⇒ `SrsSummaryNotifier.apply(summary)` + làm mới tổng quan; bị bỏ (4xx) ⇒ [OutboxDrop].
///
/// Theo `userScopeProvider`: đổi người ⇒ Riverpod 3 dựng lại Notifier; vòng gửi của instance cũ kiểm `shouldStop`
/// TRƯỚC mỗi lần gửi (`ref.mounted`, thế hệ, `userScopeProvider` ≠ user của kho) nên phần còn lại KHÔNG đi bằng phiên
/// người khác (review M6 C1). Kho chỉ bị xoá khi người dùng CHỦ ĐỘNG đăng xuất (`signOutFlow` ⇒ [clearForUser]);
/// hết phiên/đổi mật khẩu/mất phiên ⇒ GIỮ kho, cùng người đăng nhập lại sẽ gửi tiếp (RK-M1 — lệch hợp đồng có chủ
/// đích). KHÔNG chứa token (luật `token-in-prefs`).
///
/// Riverpod 3.4 TÁI DÙNG instance Notifier khi provider rebuild (đổi người) ⇒ mọi trường phải được đặt lại trong
/// `build` (completer đọc kho, token vòng gửi); `ref.mounted` không đủ để nhận biết đổi người — dùng thêm `_alive`.
class ReviewOutboxController extends Notifier<ReviewOutboxState> {
  Timer? _timer;

  /// Token của vòng gửi đang chạy (single-flight QUA CẢ các đời build): vòng mới không chạy chồng lên lượt gửi đang
  /// bay của vòng cũ (R3-1 — hai vòng song song cho cùng người ⇒ gửi lặp); vòng cũ buông token ⇒ gọi lại nếu có yêu cầu.
  Object? _flushToken;
  bool _flushRequested = false;
  int _dropSeq = 0;

  /// Thế hệ hàng đợi: tăng khi `clearForUser` để vòng gửi đang bay không ghi lại phần tử đã xoá.
  int _gen = 0;

  /// Đời `build`: tăng mỗi lần provider dựng lại (đổi người, kể cả A→null→A) — vòng gửi/đọc kho của đời cũ phải
  /// dừng dù cùng người và cùng `_gen` (review R3-1: hai vòng gửi song song cho A ⇒ gửi lặp).
  int _epoch = 0;
  Future<void> _writeChain = Future.value();
  Completer<void> _restored = Completer<void>();

  /// Hoàn tất khi đã đọc xong kho bền (hoặc không có người dùng) — `signOutFlow` chờ trước khi đếm lượt chờ gửi.
  Future<void> get whenRestored => _restored.future;

  /// Số lượt chờ gửi SAU khi đã đọc xong kho (bấm Đăng xuất ngay lúc app vừa mở vẫn đếm đúng).
  Future<int> pendingCountWhenRestored() async {
    await whenRestored;
    return ref.mounted ? state.pendingCount : 0;
  }

  @override
  ReviewOutboxState build() {
    final userId = ref.watch(userScopeProvider);
    // Rebuild trên cùng instance: đời mới, giải phóng người chờ đọc kho cũ, mở completer mới, huỷ single-flight cũ.
    _epoch++;
    if (!_restored.isCompleted) _restored.complete();
    _restored = Completer<void>();
    ref.onDispose(() {
      _cancelTimer();
      if (!_restored.isCompleted) _restored.complete();
    });
    if (userId != null) {
      unawaited(_restore(userId));
    } else {
      _restored.complete();
    }
    return ReviewOutboxState(userId: userId, restored: userId == null);
  }

  KeyValueStore get _store => ref.read(keyValueStoreProvider);

  void _cancelTimer() {
    _timer?.cancel();
    _timer = null;
  }

  /// Còn đúng người/đúng thế hệ/đúng đời `build` và controller còn sống — điều kiện để tiếp tục gửi/ghi.
  bool _alive(int gen, String userId, [int? epoch]) {
    if (!ref.mounted || gen != _gen || (epoch != null && epoch != _epoch)) return false;
    try {
      return ref.read(userScopeProvider) == userId && state.userId == userId;
    } on Object {
      return false;
    }
  }

  Future<void> _restore(String userId) async {
    final gen = _gen;
    final epoch = _epoch;
    final raw = await _store.getString(outboxStorageKey(userId));
    if (!_alive(gen, userId, epoch)) return;
    // Gộp: phần ĐÃ LƯU trước, lượt chấm nộp trong lúc đang đọc kho nối SAU (giữ thứ tự thời gian, không mất s1/s2);
    // rồi cắt theo giới hạn kho.
    var mergedAll = decodeOutbox(raw);
    for (final it in state.items) {
      mergedAll = enqueue(mergedAll, it);
    }
    final merged = _trim(mergedAll); // (ghi lastDrop nếu cắt) — gán items sau
    state = state.copyWith(items: merged, restored: true);
    if (!_restored.isCompleted) _restored.complete();
    if (merged.isNotEmpty) {
      await _persist();
      if (!_alive(gen, userId, epoch)) return;
      unawaited(flush());
    }
  }

  /// Ghi kho theo chuỗi (mỗi lần ghi mang ảnh chụp `items` lúc gọi ⇒ lần ghi cuối luôn là trạng thái mới nhất).
  Future<void> _persist() {
    final userId = state.userId;
    if (userId == null || !state.restored) return Future.value();
    final items = state.items;
    final key = outboxStorageKey(userId);
    final store = _store;
    _writeChain = _writeChain.then((_) async {
      if (items.isEmpty) {
        await store.remove(key);
      } else {
        await store.setString(key, encodeOutbox(items));
      }
    });
    return _writeChain;
  }

  /// Thêm một lượt chấm (id sinh sẵn ở nơi gọi — `uuidV4()`). Đã đọc kho ⇒ ghi kho + gửi ngay ở nền; chưa đọc xong ⇒
  /// chỉ giữ bộ nhớ, `_restore` sẽ gộp, ghi kho rồi gửi (review M6 C2).
  /// Cắt theo giới hạn kho; có bỏ ⇒ log + `lastDrop` mang lượt CŨ nhất bị bỏ (toast).
  List<OutboxItem> _trim(List<OutboxItem> items) {
    final trimmed = trimOutbox(items);
    if (trimmed.dropped.isNotEmpty) {
      afLog('outbox: kho đầy ($kOutboxMaxItems) — bỏ ${trimmed.dropped.length} lượt cũ nhất');
      state = state.copyWith(
        lastDrop: OutboxDrop(
          seq: ++_dropSeq,
          item: trimmed.dropped.first,
          error: ApiError('Hàng đợi chờ gửi đã đầy ($kOutboxMaxItems) — lượt chấm cũ nhất bị bỏ.'),
        ),
      );
    }
    return trimmed.items;
  }

  void submit(OutboxItem item) {
    if (state.userId == null) return;
    // Cắt TRƯỚC rồi mới gán: `_trim` có thể ghi `lastDrop` vào state — gọi lồng trong `copyWith` sẽ lấy state cũ.
    final items = _trim(enqueue(state.items, item));
    state = state.copyWith(items: items);
    if (!state.restored) return;
    unawaited(_persist());
    unawaited(flush());
  }

  /// Gửi ngay những gì đang chờ (app resumed, mở phiên): huỷ hẹn bậc thang, gửi luôn.
  void flushNow() {
    _cancelTimer();
    unawaited(flush());
  }

  /// Gửi tuần tự tới khi trống hoặc gặp lỗi đáng thử lại (khi đó hẹn giờ). Single-flight; lặp vì trong lúc await
  /// có thể có lượt mới được nối đuôi. Dừng (không ghi gì) khi đổi người/huỷ giữa chừng.
  Future<void> flush() async {
    final userId = state.userId;
    if (userId == null || !state.restored) return;
    if (_flushToken != null) {
      // Vòng khác đang giữ token (có thể là vòng đời cũ đang chờ một lượt gửi) ⇒ xếp yêu cầu, chạy khi nó buông.
      _flushRequested = true;
      return;
    }
    final token = Object();
    _flushToken = token;
    _flushRequested = false;
    final epoch = _epoch;
    // Thoát vì vòng "chết" (đời/người đổi) ⇒ vòng của đời mới đang chờ token phải được gọi lại; thoát thường (hết hàng,
    // hẹn giờ) thì vòng này đã tự gom lượt nối đuôi, không gọi lại (giữ đúng bậc thang chờ).
    var deadExit = false;
    _cancelTimer();
    try {
      for (;;) {
        final gen = _gen;
        // Vòng này chỉ được tiếp tục khi vẫn là vòng đang giữ token, cùng đời build, cùng người, cùng thế hệ.
        bool live() => identical(_flushToken, token) && _alive(gen, userId, epoch);
        if (!live()) {
          deadExit = true;
          return;
        }
        final snapshot = state.items;
        if (snapshot.isEmpty) break;
        final outcome = await flushOutbox(snapshot, ref.read(outboxSenderProvider), shouldStop: () => !live());
        if (!live()) {
          deadExit = true;
          _reconcileStaleSent(outcome.sent, gen, userId);
          return;
        }
        // Lượt nối đuôi trong lúc gửi = phần tử hiện có mà ảnh chụp không có (so theo id — không so độ dài, vì
        // `trimOutbox` có thể đã cắt phần đầu). Gộp xong cắt lại theo giới hạn kho.
        final snapshotIds = {for (final it in snapshot) it.clientReviewId};
        final appended = state.items.where((it) => !snapshotIds.contains(it.clientReviewId)).toList();
        final merged = _trim([...outcome.remaining, ...appended]);
        state = state.copyWith(items: merged);
        unawaited(_persist());
        for (final s in outcome.sent) {
          _onSent(s);
        }
        for (final d in outcome.dropped) {
          _onDropped(d);
        }
        if (outcome.stopped) {
          // Tới đây `_alive` vẫn đúng ⇒ dừng vì 401 mà phiên CHƯA mất (interceptor không làm mới được lúc này):
          // giữ nguyên, hẹn lại sau 30 s. Mất phiên thật ⇒ user về null ⇒ Notifier dựng lại, timer bị huỷ, kho giữ.
          afLog('outbox: 401 khi gửi — giữ ${outcome.remaining.length} lượt, thử lại sau ${kRetrySteadyMs ~/ 1000} s');
          _timer = Timer(const Duration(milliseconds: kRetrySteadyMs), () {
            _timer = null;
            unawaited(flush());
          });
          break;
        }
        final retryAfterMs = outcome.retryAfterMs;
        if (retryAfterMs != null) {
          _timer = Timer(Duration(milliseconds: retryAfterMs), () {
            _timer = null;
            unawaited(flush());
          });
          break;
        }
      }
    } finally {
      if (identical(_flushToken, token)) {
        _flushToken = null;
        final rerun = _flushRequested && deadExit;
        _flushRequested = false;
        if (rerun) unawaited(flush());
      }
    }
  }

  /// Vòng gửi đã "chết" (đời build mới — vd A→null→A) nhưng lượt đang bay vẫn hoàn tất với CÙNG người/thế hệ ⇒ gỡ
  /// các lượt đã gửi khỏi hàng đợi hiện tại (đời mới đọc lại kho nên vẫn còn chúng) để không gửi lại lần hai.
  void _reconcileStaleSent(List<SentReview> sent, int gen, String userId) {
    if (sent.isEmpty || !ref.mounted || gen != _gen || state.userId != userId) return;
    final ids = {for (final s in sent) s.item.clientReviewId};
    state = state.copyWith(items: state.items.where((it) => !ids.contains(it.clientReviewId)).toList());
    unawaited(_persist());
    for (final s in sent) {
      _onSent(s);
    }
  }

  void _onSent(SentReview s) {
    final userId = state.userId;
    if (userId == null) return;
    // Server đã nhận (kể cả `duplicate`): số đến hạn/từ mới đổi ⇒ ghi thẳng summary; chuỗi ngày/mục tiêu ở trang chủ
    // đổi ⇒ chỉ invalidate tổng quan (trang chủ không mount trong phiên ôn nên không gọi API thừa).
    ref.read(srsSummaryByUserProvider(userId).notifier).apply(s.response.summary);
    ref.invalidateProgressOverview();
  }

  void _onDropped(DroppedReview d) {
    final err = ApiError.from(d.error);
    afLog('outbox: bỏ lượt ${d.item.clientReviewId} (${err.status} ${err.code ?? ''}) — ${err.message}');
    state = state.copyWith(
      lastDrop: OutboxDrop(seq: ++_dropSeq, item: d.item, error: err),
    );
  }

  /// Xoá outbox của [userId] — CHỈ khi người dùng chủ động đăng xuất và đã xác nhận bỏ (RM-S7, `signOutFlow`).
  /// Lệnh xoá nối vào chuỗi ghi để không bị một lần ghi đang chờ đè lại.
  Future<void> clearForUser(String userId) async {
    _gen++;
    _cancelTimer();
    if (ref.mounted && state.userId == userId) state = state.copyWith(items: const []);
    final store = _store;
    _writeChain = _writeChain.then((_) => store.remove(outboxStorageKey(userId)));
    await _writeChain;
  }
}

final reviewOutboxProvider = NotifierProvider<ReviewOutboxController, ReviewOutboxState>(ReviewOutboxController.new);
