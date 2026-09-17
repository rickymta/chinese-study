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
}
