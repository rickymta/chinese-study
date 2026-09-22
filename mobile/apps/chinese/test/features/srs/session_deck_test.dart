import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_chinese/features/srs/domain/session_deck.dart';
import 'package:flutter_test/flutter_test.dart';

// Chép đủ ca của `sessionDeck.test.ts` web.
SrsQueueCard card(String cardId, [SrsCardState state = SrsCardState.review]) => SrsQueueCard(
  cardId: cardId,
  state: state,
  queue: state == SrsCardState.fresh
      ? 'new'
      : state == SrsCardState.review
      ? 'review'
      : 'learning',
  dueAt: DateTime.utc(2026, 9, 18),
  word: SrsQueueWord(id: 'w-$cardId', simplified: '爱', pinyin: 'ai4', meaningViStatus: 'machine'),
  intervals: const {SrsRating.again: 'PT1M', SrsRating.hard: 'PT5M30S', SrsRating.good: 'PT10M', SrsRating.easy: 'P8D'},
);

List<String> ids(List<SrsQueueCard> cards) => cards.map((c) => c.cardId).toList();

void main() {
  group('mergeIncoming', () {
    test('bỏ thẻ đã có trong bộ (chưa chấm)', () {
      final deck = [card('a'), card('b')];
      expect(ids(mergeIncoming(deck, {}, {}, [card('b'), card('c')])), ['c']);
    });

    test('thẻ đã chấm chỉ quay lại khi server trả learning/relearning', () {
      final out = mergeIncoming(
        [],
        {'a', 'b'},
        {},
        [card('a', SrsCardState.learning), card('b', SrsCardState.review), card('c', SrsCardState.relearning)],
      );
      expect(ids(out), ['a', 'c']);
    });

    test('bỏ thẻ đang chờ gửi trong outbox', () {
      final out = mergeIncoming([], {'a'}, {'a'}, [card('a', SrsCardState.learning), card('b')]);
      expect(ids(out), ['b']);
    });

    test('bản quay lại của thẻ đã chấm đang nằm trong phần chưa chấm ⇒ không nối thêm lần nữa (review F7)', () {
      final deck = [card('a', SrsCardState.fresh), card('b'), card('a', SrsCardState.learning)];
      final unrated = deck.sublist(1);
      expect(mergeIncoming(unrated, {'a'}, {}, [card('a', SrsCardState.learning)]), isEmpty);
    });

    test('skipNew ⇒ bỏ thẻ new (server đếm từ mới chưa kịp khi còn lượt chấm đang bay) — tích hợp F7', () {
      final incoming = [card('n1', SrsCardState.fresh), card('r1'), card('l1', SrsCardState.learning)];
      expect(ids(mergeIncoming([], {'p'}, {'p'}, incoming, skipNew: true)), ['r1', 'l1']);
      // Không bật cờ (tải lần đầu / không có lượt đang bay) ⇒ không lọc.
      expect(ids(mergeIncoming([], {}, {'p'}, incoming)), ['n1', 'r1', 'l1']);
    });

    test('không thêm trùng trong cùng một lô tải', () {
      expect(mergeIncoming([], {}, {}, [card('x'), card('x')]), hasLength(1));
    });
  });

  group('shouldLoadMore', () {
    test('còn ≤ 5 thẻ, không đang tải, chưa hết ⇒ tải', () {
      expect(shouldLoadMore(5, loading: false, exhausted: false), isTrue);
      expect(shouldLoadMore(0, loading: false, exhausted: false), isTrue);
      expect(shouldLoadMore(6, loading: false, exhausted: false), isFalse);
      expect(shouldLoadMore(2, loading: true, exhausted: false), isFalse);
      expect(shouldLoadMore(2, loading: false, exhausted: true), isFalse);
    });
  });

  group('countRatings / formatSessionDuration / clampDurationMs', () {
    test('đếm theo 4 mức', () {
      expect(countRatings([SrsRating.good, SrsRating.again, SrsRating.good, SrsRating.easy]), {
        SrsRating.again: 1,
        SrsRating.hard: 0,
        SrsRating.good: 2,
        SrsRating.easy: 1,
      });
    });

    test('định dạng thời gian phiên', () {
      expect(formatSessionDuration(45000), '45 giây');
      expect(formatSessionDuration(200000), '3 phút 20 giây');
      expect(formatSessionDuration(120000), '2 phút');
      expect(formatSessionDuration(3720000), '1 giờ 2 phút');
      expect(formatSessionDuration(-5), '0 giây');
    });

    test('kẹp durationMs 0..600000', () {
      expect(clampDurationMs(-10), 0);
      expect(clampDurationMs(4200.6), 4201);
      expect(clampDurationMs(999999), 600000);
      expect(clampDurationMs(double.nan), 0);
    });
  });
}
