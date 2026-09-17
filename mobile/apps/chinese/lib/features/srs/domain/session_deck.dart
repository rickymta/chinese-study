// Quy tắc bộ bài của một phiên ôn (hợp đồng F6/F7 §5.3.2 "Bộ bài") — port 1-1 `lib/sessionDeck.ts` web, thuần để test.
import '../data/models.dart';

/// Còn ≤ 5 thẻ chưa chấm ⇒ tải thêm.
const kLoadMoreThreshold = 5;

/// Thẻ đang học dở (`learning`/`relearning`) được phép quay lại phiên sau khi đã chấm (nhóm "học trước" của server).
bool isRelearnableState(SrsCardState state) => state == SrsCardState.learning || state == SrsCardState.relearning;

/// Lọc thẻ tải thêm trước khi nối vào bộ bài:
/// - bỏ `cardId` đang có trong phần bộ bài CHƯA chấm ([unrated] = `deck.sublist(index)`, kể cả thẻ hiện tại và bản
///   quay lại của thẻ đã chấm trước đó — không lọc theo [ratedIds], nếu không thẻ learning bị nối thêm lần 3),
/// - bỏ thẻ đang chờ gửi trong outbox ([pendingIds] — server chưa biết lượt vừa chấm ⇒ trạng thái trả về đã cũ),
/// - thẻ đã chấm trong phiên chỉ được quay lại khi server trả `learning`/`relearning` (thẻ "Quên" học lại sau 1 phút),
/// - [skipNew] (bật khi TẢI THÊM mà có lượt chấm chưa tới server trong lúc gọi): bỏ thẻ `new`. Server chưa đếm các
///   lượt đó vào "từ mới hôm nay" nên cấp dư thẻ mới ⇒ lượt chấm thẻ dư bị 422 NEW_CARD_LIMIT_REACHED. Trang tự tải
///   lại khi outbox trống, lúc đó số đếm của server đã đúng.
List<SrsQueueCard> mergeIncoming(
  List<SrsQueueCard> unrated,
  Set<String> ratedIds,
  Set<String> pendingIds,
  List<SrsQueueCard> incoming, {
  bool skipNew = false,
}) {
  final inDeck = {for (final c in unrated) c.cardId};
  final out = <SrsQueueCard>[];
  for (final card in incoming) {
    if (inDeck.contains(card.cardId)) continue;
    if (skipNew && card.state == SrsCardState.fresh) continue;
    if (pendingIds.contains(card.cardId)) continue;
    if (ratedIds.contains(card.cardId) && !isRelearnableState(card.state)) continue;
    inDeck.add(card.cardId);
    out.add(card);
  }
  return out;
}

/// Có nên gọi tải thêm không: còn ít thẻ, không đang tải, server chưa báo hết.
bool shouldLoadMore(int remaining, {required bool loading, required bool exhausted}) =>
    !loading && !exhausted && remaining <= kLoadMoreThreshold;

/// Số lượt theo 4 mức (luôn đủ 4 khoá).
Map<SrsRating, int> countRatings(Iterable<SrsRating> ratings) {
  final counts = {for (final r in SrsRating.values) r: 0};
  for (final r in ratings) {
    counts[r] = counts[r]! + 1;
  }
  return counts;
}

/// "3 phút 20 giây" / "45 giây" / "1 giờ 2 phút" — thời gian phiên ở màn tổng kết.
String formatSessionDuration(int ms) {
  final total = (ms / 1000).round().clamp(0, 1 << 30);
  final h = total ~/ 3600;
  final m = (total % 3600) ~/ 60;
  final s = total % 60;
  if (h > 0) return m > 0 ? '$h giờ $m phút' : '$h giờ';
  if (m > 0) return s > 0 ? '$m phút $s giây' : '$m phút';
  return '$s giây';
}

/// Kẹp `durationMs` theo luật server (0..600000) để 400 không xảy ra vì người học để thẻ mở quá lâu.
const kDurationMsMax = 600000;

int clampDurationMs(num ms) {
  if (ms is double && !ms.isFinite) return 0;
  return ms.round().clamp(0, kDurationMsMax);
}
