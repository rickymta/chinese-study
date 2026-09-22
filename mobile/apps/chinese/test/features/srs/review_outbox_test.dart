import 'dart:convert';

import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_chinese/features/srs/domain/review_outbox.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

// Chép đủ ca của `reviewOutbox.test.ts` web (read/write đổi thành encode/decode vì kho là KeyValueStore).
OutboxItem item(String id, {String? cardId, int attempts = 0}) => OutboxItem(
  clientReviewId: id,
  cardId: cardId ?? 'card-$id',
  rating: SrsRating.good,
  durationMs: 1200,
  attempts: attempts,
);

ReviewResponse okResponse(String cardId) => ReviewResponse.fromJson({
  'reviewId': 'r',
  'duplicate': false,
  'card': {'cardId': cardId, 'state': 'review', 'isSuspended': false},
  'summary': <String, Object?>{},
});

void main() {
  group('retryDelayMs', () {
    test('bậc thang 1 s, 2 s, 5 s, 10 s rồi mỗi 30 s', () {
      expect(retryDelayMs(1), 1000);
      expect(retryDelayMs(2), 2000);
      expect(retryDelayMs(3), 5000);
      expect(retryDelayMs(4), 10000);
      expect(retryDelayMs(5), 30000);
      expect(retryDelayMs(99), 30000);
      expect(retryDelayMs(0), 1000);
    });
  });

  group('enqueue', () {
    test('thêm vào cuối, không sửa danh sách gốc', () {
      final a = [item('a')];
      final b = enqueue(a, item('b'));
      expect(b.map((x) => x.clientReviewId), ['a', 'b']);
      expect(a, hasLength(1));
    });

    test('trùng clientReviewId không thêm lần hai', () {
      expect(enqueue([item('a')], item('a')), hasLength(1));
    });
  });

  group('encode/decode outbox', () {
    test('mã hoá rồi giải mã giữ nguyên thứ tự và trường', () {
      final decoded = decodeOutbox(encodeOutbox([item('a'), item('b', attempts: 2)]));
      expect(decoded, [item('a'), item('b', attempts: 2)]);
    });

    test('dữ liệu hỏng/khác dạng ⇒ rỗng, không ném', () {
      expect(decodeOutbox('{not json'), isEmpty);
      expect(decodeOutbox(null), isEmpty);
      expect(decodeOutbox(''), isEmpty);
      expect(decodeOutbox('{"a":1}'), isEmpty);
    });

    test('lọc phần tử sai dạng', () {
      final raw = jsonEncode([
        item('a').toJson(),
        {'clientReviewId': 'x'},
        {...item('b').toJson(), 'rating': 'meh'},
      ]);
      expect(decodeOutbox(raw).map((x) => x.clientReviewId), ['a']);
    });

    test('khoá kho theo người dùng', () {
      expect(outboxStorageKey('u-1'), 'af.srs.outbox.u-1');
    });
  });

  group('isRetryableError', () {
    test('mạng/5xx/408/429 thử lại; 4xx khác bỏ', () {
      expect(isRetryableError(ApiError.network()), isTrue);
      expect(isRetryableError(ApiError('x', status: 503)), isTrue);
      expect(isRetryableError(ApiError('x', status: 408)), isTrue);
      expect(isRetryableError(ApiError('x', status: 429)), isTrue);
      expect(isRetryableError(ApiError('x', status: 409)), isFalse);
      expect(isRetryableError(ApiError('x', status: 422)), isFalse);
      expect(isRetryableError(ApiError('x', status: 400)), isFalse);
      expect(isRetryableError(Exception('bug')), isFalse);
    });
  });

  group('flushOutbox', () {
    test('gửi tuần tự, thành công hết ⇒ hàng đợi trống', () async {
      final order = <String>[];
      final out = await flushOutbox([item('a'), item('b'), item('c')], (it) async {
        order.add(it.clientReviewId);
        return okResponse(it.cardId);
      });
      expect(order, ['a', 'b', 'c']);
      expect(out.remaining, isEmpty);
      expect(out.sent, hasLength(3));
      expect(out.dropped, isEmpty);
      expect(out.retryAfterMs, isNull);
    });

    test('lỗi mạng ⇒ dừng tại phần tử đó, giữ phần sau, tăng attempts, hẹn gửi lại', () async {
      var calls = 0;
      final out = await flushOutbox([item('a'), item('b'), item('c')], (it) async {
        calls++;
        if (it.clientReviewId == 'b') throw ApiError.network();
        return okResponse(it.cardId);
      });
      expect(calls, 2);
      expect(out.sent.map((s) => s.item.clientReviewId), ['a']);
      expect(out.remaining.map((x) => (x.clientReviewId, x.attempts)), [('b', 1), ('c', 0)]);
      expect(out.retryAfterMs, 1000);
    });

    test('attempts tích luỹ qua nhiều lần ⇒ khoảng chờ tăng dần rồi giữ 30 s', () async {
      var items = [item('a')];
      final delays = <int>[];
      for (var i = 0; i < 6; i++) {
        final out = await flushOutbox(items, (_) async => throw ApiError('x', status: 502));
        delays.add(out.retryAfterMs!);
        items = out.remaining;
      }
      expect(delays, [1000, 2000, 5000, 10000, 30000, 30000]);
      expect(items.first.attempts, 6);
    });

    test('409/422 ⇒ bỏ phần tử, đi tiếp, không hẹn gửi lại', () async {
      final out = await flushOutbox([item('a'), item('b'), item('c')], (it) async {
        if (it.clientReviewId == 'a') {
          throw ApiError('Mã đánh giá đã dùng cho thẻ khác', status: 409, code: 'CLIENT_REVIEW_ID_CONFLICT');
        }
        if (it.clientReviewId == 'b') throw ApiError('Thẻ đang tạm dừng', status: 422, code: 'CARD_SUSPENDED');
        return okResponse(it.cardId);
      });
      expect(out.dropped.map((d) => d.item.clientReviewId), ['a', 'b']);
      expect(out.sent.map((s) => s.item.clientReviewId), ['c']);
      expect(out.remaining, isEmpty);
      expect(out.retryAfterMs, isNull);
    });

    test(
      'shouldStop hỏi TRƯỚC mỗi lần gửi: dừng ⇒ phần còn lại nguyên vẹn, attempts không tăng, không hẹn lại',
      () async {
        var sentCount = 0;
        final out = await flushOutbox(
          [item('a'), item('b'), item('c')],
          (it) async {
            sentCount++;
            return okResponse(it.cardId);
          },
          shouldStop: () => sentCount >= 1, // sau khi a đi thì "đổi người"
        );
        expect(out.sent.map((s) => s.item.clientReviewId), ['a']);
        expect(out.stopped, isTrue);
        expect(out.remaining, [item('b'), item('c')]);
        expect(out.retryAfterMs, isNull);
      },
    );

    test('401 (mất phiên) ⇒ dừng và GIỮ phần tử (không bỏ, không tăng attempts) — RK-M1', () async {
      final out = await flushOutbox([item('a'), item('b')], (it) async {
        if (it.clientReviewId == 'a') throw ApiError('Cần đăng nhập để tiếp tục.', status: 401);
        return okResponse(it.cardId);
      });
      expect(out.stopped, isTrue);
      expect(out.dropped, isEmpty);
      expect(out.remaining, [item('a'), item('b')]);
      expect(out.retryAfterMs, isNull);
    });

    test('trimOutbox giữ phần MỚI nhất, báo số đã bỏ', () {
      final items = [for (var i = 0; i < 7; i++) item('i$i')];
      final r = trimOutbox(items, max: 5);
      expect(r.dropped.map((x) => x.clientReviewId), ['i0', 'i1']);
      expect(r.items.map((x) => x.clientReviewId), ['i2', 'i3', 'i4', 'i5', 'i6']);
      expect(trimOutbox(items, max: 10).dropped, isEmpty);
      expect(kOutboxMaxItems, 500);
    });

    test('clientReviewId không đổi giữa các lần gửi lại', () async {
      final seen = <String>[];
      var fail = true;
      Future<ReviewResponse> send(OutboxItem it) async {
        seen.add(it.clientReviewId);
        if (fail) throw ApiError.network();
        return okResponse(it.cardId);
      }

      final first = await flushOutbox([item('a')], send);
      fail = false;
      final second = await flushOutbox(first.remaining, send);
      expect(seen, ['a', 'a']);
      expect(second.remaining, isEmpty);
    });
  });
}
