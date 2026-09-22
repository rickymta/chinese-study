// Hàng đợi gửi lại đánh giá thẻ (RM-L1, port `lib/reviewOutbox.ts` web): chấm thẻ là LẠC QUAN — chuyển thẻ kế ngay,
// gửi ở nền; mất mạng/5xx thì giữ lại (KeyValueStore, khoá theo người dùng) và thử lại 1 s, 2 s, 5 s, 10 s rồi mỗi 30 s.
// Phần này THUẦN (không Riverpod/Flutter) để test được; `application/outbox_controller.dart` lo timer/kho/sự kiện.
import 'dart:convert';

import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

import '../data/models.dart';

/// Một lượt chấm chờ gửi. [clientReviewId] sinh MỘT lần lúc chấm (`uuidV4()`), GIỮ NGUYÊN mọi lần gửi lại (server
/// idempotent — R7-8). [attempts] = số lần gửi thất bại (để tính khoảng chờ).
@immutable
class OutboxItem {
  const OutboxItem({
    required this.clientReviewId,
    required this.cardId,
    required this.rating,
    required this.durationMs,
    this.attempts = 0,
  });

  /// Phần tử sai dạng (thiếu id, mức chấm lạ) ⇒ null — bị lọc khi đọc kho.
  static OutboxItem? fromJson(JsonMap? json) {
    final id = readString(json, 'clientReviewId');
    final cardId = readString(json, 'cardId');
    final rating = SrsRating.fromApi(readString(json, 'rating'));
    final durationMs = readInt(json, 'durationMs');
    final attempts = readInt(json, 'attempts');
    if (id == null || id.isEmpty || cardId == null || cardId.isEmpty || rating == null) return null;
    if (durationMs == null || attempts == null) return null;
    return OutboxItem(clientReviewId: id, cardId: cardId, rating: rating, durationMs: durationMs, attempts: attempts);
  }

  final String clientReviewId;
  final String cardId;
  final SrsRating rating;
  final int durationMs;
  final int attempts;

  JsonMap toJson() => {
    'clientReviewId': clientReviewId,
    'cardId': cardId,
    'rating': rating.apiValue,
    'durationMs': durationMs,
    'attempts': attempts,
  };

  OutboxItem copyWith({int? attempts}) => OutboxItem(
    clientReviewId: clientReviewId,
    cardId: cardId,
    rating: rating,
    durationMs: durationMs,
    attempts: attempts ?? this.attempts,
  );

  @override
  bool operator ==(Object other) =>
      other is OutboxItem &&
      other.clientReviewId == clientReviewId &&
      other.cardId == cardId &&
      other.rating == rating &&
      other.durationMs == durationMs &&
      other.attempts == attempts;

  @override
  int get hashCode => Object.hash(clientReviewId, cardId, rating, durationMs, attempts);

  @override
  String toString() => 'OutboxItem($clientReviewId card=$cardId ${rating.apiValue} attempts=$attempts)';
}

/// Tiền tố khoá `shared_preferences` (hợp đồng §5.1.2): `af.srs.outbox.<userId>`.
const kOutboxKeyPrefix = 'af.srs.outbox';

String outboxStorageKey(String userId) => '$kOutboxKeyPrefix.$userId';

/// Khoảng chờ trước lần gửi lại thứ n (n = số lần đã thất bại). Hết bậc thang ⇒ mỗi 30 s.
const List<int> kRetryDelaysMs = [1000, 2000, 5000, 10000];
const kRetrySteadyMs = 30000;

int retryDelayMs(int attempts) {
  final n = attempts < 1 ? 1 : attempts;
  return n - 1 < kRetryDelaysMs.length ? kRetryDelaysMs[n - 1] : kRetrySteadyMs;
}

/// Giải mã JSON mảng đã lưu; dữ liệu hỏng/khác dạng ⇒ mảng rỗng (không ném); phần tử sai dạng bị lọc.
List<OutboxItem> decodeOutbox(String? raw) {
  if (raw == null || raw.isEmpty) return const [];
  try {
    final parsed = jsonDecode(raw);
    if (parsed is! List) return const [];
    return [for (final e in parsed) ?OutboxItem.fromJson(asJsonMap(e))];
  } on Object {
    return const [];
  }
}

String encodeOutbox(List<OutboxItem> items) => jsonEncode([for (final i in items) i.toJson()]);

/// Thêm vào cuối hàng đợi (thuần, trả danh sách mới). Trùng `clientReviewId` ⇒ giữ nguyên, không thêm lần hai.
List<OutboxItem> enqueue(List<OutboxItem> items, OutboxItem item) {
  if (items.any((it) => it.clientReviewId == item.clientReviewId)) return [...items];
  return [...items, item];
}

/// Giới hạn kho outbox (chặn phình vô hạn khi offline rất lâu); vượt ⇒ bỏ lượt CŨ nhất (review M6).
const kOutboxMaxItems = 500;

/// Cắt về tối đa [max] phần tử, giữ phần mới nhất; trả kèm các phần tử CŨ đã bỏ (để báo đúng lượt bị bỏ).
({List<OutboxItem> items, List<OutboxItem> dropped}) trimOutbox(List<OutboxItem> items, {int max = kOutboxMaxItems}) {
  if (items.length <= max) return (items: items, dropped: const []);
  final cut = items.length - max;
  return (items: items.sublist(cut), dropped: items.sublist(0, cut));
}

/// Lỗi đáng thử lại: không có phản hồi (mất mạng, timeout), 5xx, 408, 429. Các 4xx còn lại (400/404/409/422…) là
/// câu trả lời thật ⇒ bỏ phần tử và báo người dùng. Lỗi không phải [ApiError] (ném từ code) ⇒ không lặp vô hạn.
bool isRetryableError(Object err) {
  if (err is ApiError) {
    final s = err.status;
    return s == null || s >= 500 || s == 408 || s == 429;
  }
  return false;
}

typedef SendReview = Future<ReviewResponse> Function(OutboxItem item);

class SentReview {
  const SentReview(this.item, this.response);

  final OutboxItem item;
  final ReviewResponse response;
}

class DroppedReview {
  const DroppedReview(this.item, this.error);

  final OutboxItem item;
  final Object error;
}

class FlushOutcome {
  const FlushOutcome({
    required this.remaining,
    required this.sent,
    required this.dropped,
    required this.retryAfterMs,
    this.stopped = false,
  });

  /// Phần tử còn lại (đã tăng `attempts` ở phần tử gây dừng — trừ khi [stopped]).
  final List<OutboxItem> remaining;
  final List<SentReview> sent;
  final List<DroppedReview> dropped;

  /// `null` ⇒ không cần hẹn gửi lại (hàng đợi trống, chỉ toàn lỗi bị bỏ, hoặc [stopped]).
  final int? retryAfterMs;

  /// Bị DỪNG chủ động (`shouldStop` — đổi người dùng/huỷ controller) hoặc vì mất phiên (401): phần còn lại giữ
  /// NGUYÊN (không tăng `attempts`), không hẹn gửi lại — người này đăng nhập lại sẽ gửi tiếp (RK-M1).
  final bool stopped;
}

/// Mất phiên (401 sau khi interceptor đã thử làm mới) ⇒ dừng và giữ lại, KHÔNG bỏ (quyết định orchestrator M6).
bool isSessionLostError(Object err) => err is ApiError && err.status == 401;

/// Gửi TUẦN TỰ theo thứ tự chấm (lượt sau của cùng một thẻ phải tới sau lượt trước). Lỗi đáng thử lại ⇒ DỪNG tại
/// phần tử đó (giữ nó và mọi phần tử sau), tăng `attempts`, trả khoảng chờ. Lỗi 4xx khác ⇒ bỏ phần tử, đi tiếp.
/// [shouldStop] được hỏi TRƯỚC mỗi lần gửi (đổi tài khoản giữa chừng ⇒ không gửi phần còn lại bằng phiên người khác);
/// dừng ⇒ [FlushOutcome.stopped], phần còn lại nguyên vẹn. 401 ⇒ cũng dừng và giữ (`isSessionLost`).
Future<FlushOutcome> flushOutbox(
  List<OutboxItem> items,
  SendReview send, {
  bool Function(Object err) isRetryable = isRetryableError,
  bool Function(Object err) isSessionLost = isSessionLostError,
  bool Function()? shouldStop,
}) async {
  final sent = <SentReview>[];
  final dropped = <DroppedReview>[];
  for (var i = 0; i < items.length; i++) {
    final item = items[i];
    if (shouldStop != null && shouldStop()) {
      return FlushOutcome(remaining: items.sublist(i), sent: sent, dropped: dropped, retryAfterMs: null, stopped: true);
    }
    try {
      final response = await send(item);
      sent.add(SentReview(item, response));
    } on Object catch (error) {
      if (isSessionLost(error)) {
        return FlushOutcome(
          remaining: items.sublist(i),
          sent: sent,
          dropped: dropped,
          retryAfterMs: null,
          stopped: true,
        );
      }
      if (isRetryable(error)) {
        final failed = item.copyWith(attempts: item.attempts + 1);
        return FlushOutcome(
          remaining: [failed, ...items.sublist(i + 1)],
          sent: sent,
          dropped: dropped,
          retryAfterMs: retryDelayMs(failed.attempts),
        );
      }
      dropped.add(DroppedReview(item, error));
    }
  }
  return FlushOutcome(remaining: const [], sent: sent, dropped: dropped, retryAfterMs: null);
}
