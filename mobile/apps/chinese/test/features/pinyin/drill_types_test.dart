import 'package:af_chinese/features/pinyin/data/models.dart';
import 'package:af_chinese/features/pinyin/domain/drill_types.dart';
import 'package:af_chinese/features/pinyin/domain/generate_drill.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('computeResponseMs — kẹp trong 0..600000 (validator backend)', () {
    test('chưa phát ⇒ null', () => expect(computeResponseMs(null, 1000), isNull));
    test('bình thường ⇒ làm tròn ms', () => expect(computeResponseMs(1000, 2800.4), 1800));
    test('đúng cận trên ⇒ giữ', () => expect(computeResponseMs(0, kResponseMsMax), kResponseMsMax));
    test(
      'quá 10 phút (để yên rồi quay lại) ⇒ null, không gửi giá trị bị 400',
      () => expect(computeResponseMs(0, kResponseMsMax + 1), isNull),
    );
    test('đồng hồ lùi (âm) ⇒ null', () => expect(computeResponseMs(5000, 4000), isNull));
  });

  DrillPart part(int tone) => DrillPart(syllable: 'ma', hanzi: '妈', tone: tone);

  test('summarizeByTone đếm theo phần', () {
    final out = summarizeByTone([
      AnsweredItem(
        item: DrillItem([part(1), part(2)]),
        answered: const [1, 3],
        correct: false,
        responseMs: null,
        replayCount: 0,
      ),
      AnsweredItem(item: DrillItem([part(2)]), answered: const [2], correct: true, responseMs: 100, replayCount: 1),
    ]);
    expect(out[1], const ToneCount(total: 1, correct: 1));
    expect(out[2], const ToneCount(total: 2, correct: 1));
    expect(out[3], const ToneCount());
    expect(out[4], const ToneCount());
  });

  test('buildSubmitRequest: đúng shape §6.1, thời điểm ISO có Z, mode snake_case', () {
    final session = DrillSession(
      clientSessionId: '11111111-1111-4111-8111-111111111111',
      mode: DrillMode.tonePair,
      items: [
        DrillItem([part(1), const DrillPart(syllable: 'ba', hanzi: '爸', tone: 4)]),
      ],
      startedAt: DateTime.utc(2026, 9, 18, 1, 2, 3),
    );
    final outcome = DrillOutcome(
      session: session,
      answers: [
        AnsweredItem(item: session.items[0], answered: const [1, 3], correct: false, responseMs: 1234, replayCount: 2),
      ],
      finishedAt: DateTime.utc(2026, 9, 18, 1, 5),
    );
    final json = buildSubmitRequest(outcome).toJson();
    expect(json['clientSessionId'], session.clientSessionId);
    expect(json['mode'], 'tone_pair');
    expect(json['startedAt'], '2026-09-18T01:02:03.000Z');
    expect(json['finishedAt'], '2026-09-18T01:05:00.000Z');
    final items = json['items'] as List;
    expect(items, hasLength(1));
    final item = items[0] as Map;
    expect(item['responseMs'], 1234);
    expect(item['replayCount'], 2);
    expect(item['parts'], [
      {'syllable': 'ma', 'hanzi': '妈', 'expectedTone': 1, 'answeredTone': 1},
      {'syllable': 'ba', 'hanzi': '爸', 'expectedTone': 4, 'answeredTone': 3},
    ]);
    // Giờ máy (không UTC) vẫn ra chuỗi Z.
    final local = SubmitToneDrillRequest(
      clientSessionId: 'x',
      mode: DrillMode.listenTone,
      startedAt: DateTime(2026, 9, 18, 8),
      finishedAt: DateTime(2026, 9, 18, 8, 1),
      items: const [],
    ).toJson();
    expect((local['startedAt'] as String).endsWith('Z'), isTrue);
  });

  test('DrillOutcome: total/correct/wrong', () {
    final items = [
      DrillItem([part(1)]),
      DrillItem([part(2)]),
    ];
    final outcome = DrillOutcome(
      session: DrillSession(
        clientSessionId: 'x',
        mode: DrillMode.listenTone,
        items: items,
        startedAt: DateTime.utc(2026),
      ),
      answers: [
        AnsweredItem(item: items[0], answered: const [1], correct: true, responseMs: 10, replayCount: 0),
        AnsweredItem(item: items[1], answered: const [4], correct: false, responseMs: 10, replayCount: 0),
      ],
      finishedAt: DateTime.utc(2026),
    );
    expect(outcome.total, 2);
    expect(outcome.correct, 1);
    expect(outcome.wrong.single.item, items[1]);
  });
}
