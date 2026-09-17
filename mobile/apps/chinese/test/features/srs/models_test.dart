import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_chinese/features/srs/data/srs_api.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  test('LearningSettings.fromJson parse fixture (GET có isDefault)', () {
    final s = LearningSettings.fromJson(loadFixture('learning_settings.json'));
    expect(s, LearningSettings.defaults);
    expect(s.isDefault, isTrue);
  });

  test('thiếu khoá / sai kiểu ⇒ mặc định, không ném; toJson đủ 5 trường, không isDefault, làm tròn 2 chữ số', () {
    final s = LearningSettings.fromJson({'dailyNewCards': '15', 'ttsRate': 'abc'});
    expect(s.dailyNewCards, 15);
    expect(s.ttsRate, 0.8);
    expect(s.isDefault, isFalse);
    expect(LearningSettings.fromJson(null), LearningSettings.defaults.copyWith(isDefault: false));

    final json = s.copyWith(desiredRetention: 0.856, ttsRate: 1.005).toJson();
    expect(json.keys, ['dailyNewCards', 'dailyReviewLimit', 'desiredRetention', 'ttsRate', 'autoPlayAudio']);
    expect(json['desiredRetention'], 0.86);
    expect(json['ttsRate'], 1.0);
  });

  test('copyWith/== phân biệt từng trường', () {
    const a = LearningSettings.defaults;
    expect(a.copyWith(autoPlayAudio: false), isNot(a));
    expect(a.copyWith(), a);
    expect(a.copyWith(isDefault: false), isNot(a));
  });

  test('getLearningSettings GET skipErrorRedirect; putLearningSettings gửi body 5 trường', () async {
    final adapter = chineseWithSettings();
    final dio = createApiClient(baseUrl: testConfig.chineseApiUrl, getAccessToken: () => 'tok')
      ..httpClientAdapter = adapter;
    final got = await getLearningSettings(dio);
    expect(got.isDefault, isTrue);
    expect(adapter.requests.single.skipErrorRedirect, isTrue);

    final saved = await putLearningSettings(dio, got.copyWith(dailyNewCards: 20, ttsRate: 1.1));
    expect(saved.dailyNewCards, 20);
    expect(saved.ttsRate, 1.1);
    expect(saved.isDefault, isFalse);
    final put = adapter.requests.last;
    expect(put.method, 'PUT');
    final body = asJsonMap(put.data)!;
    expect(body.containsKey('isDefault'), isFalse);
    expect(body['dailyReviewLimit'], 200);
  });

  test('SrsQueueResponse.fromJson: enum snake_case, intervals theo mức, thẻ thiếu id/chữ bị bỏ, nextDueAt UTC', () {
    final q = SrsQueueResponse.fromJson(loadFixture('srs_queue.json'));
    expect(q.cards.map((c) => c.cardId), ['c1', 'c2', 'c3']);
    expect(q.cards[0].state, SrsCardState.fresh);
    expect(q.cards[2].state, SrsCardState.relearning);
    expect(q.cards[0].intervals[SrsRating.hard], 'PT5M30S');
    expect(q.cards[0].word.meaningsVi, ['yêu', 'thích']);
    expect(q.cards[2].word.hanViet, isNull);
    expect(q.summary.toStart, 3);
    expect(q.summary.nextDueAt, DateTime.utc(2026, 9, 18, 1, 30));
    expect(q.summary.reviewLimitReached, isFalse);
    expect(SrsCardState.fromApi('lạ'), SrsCardState.review);
    expect(SrsRating.fromApi('lạ'), isNull);
  });

  test('ReviewResponse/AddCardsResponse parse dễ tính', () {
    final r = ReviewResponse.fromJson({
      'reviewId': 'r1',
      'duplicate': true,
      'card': {'cardId': 'c1', 'state': 'learning', 'isSuspended': false, 'reps': 1},
      'summary': {'dueNow': 1, 'newAvailableToday': 0},
    });
    expect(r.duplicate, isTrue);
    expect(r.card.state, SrsCardState.learning);
    expect(r.summary.toStart, 1);
    final a = AddCardsResponse.fromJson({
      'added': 1,
      'skipped': 0,
      'cards': [
        {'wordId': 'w1', 'cardId': 'c9', 'created': true},
      ],
    });
    expect(a.createdFor('w1'), isTrue);
    expect(AddCardsResponse.fromJson({'added': 0, 'skipped': 1}).createdFor('w1'), isFalse);
  });
}
