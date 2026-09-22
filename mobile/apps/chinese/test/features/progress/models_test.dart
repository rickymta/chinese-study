import 'package:af_chinese/features/progress/data/models.dart';
import 'package:af_chinese/features/progress/data/progress_api.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  test('ProgressOverview.fromJson parse fixture đầy đủ (mọi khối, activity 90 ngày)', () {
    final o = ProgressOverview.fromJson(loadFixture('progress_overview_full.json'));
    expect(o.localDate, '2026-09-18');
    expect(o.timeZone, 'Asia/Ho_Chi_Minh');
    expect(o.streak, const ProgressStreak(current: 5, longest: 12, studiedToday: false));
    expect(o.today.activityCount, 12);
    expect(o.srs?.dueToday, 23);
    expect(o.srs?.reviewBadge, 30);
    expect(o.srs?.nextDueAt, DateTime.utc(2026, 9, 18, 9, 30));
    expect(o.dailyGoal?.total, 33);
    expect(o.vocabulary?.mature, 12);
    expect(o.lessons?.next?.slug, 'so-dem');
    expect(o.lessons?.lastCompleted?.unpracticedChars, 9);
    expect(o.lessons?.lastCompleted?.completedAt, DateTime.utc(2026, 9, 17, 10));
    expect(o.writing?.totalChars, 297);
    expect(o.tone?.accuracy, 0.82);
    expect(o.tone?.recommendedFocus, [2, 3]);
    expect(o.activity, hasLength(90));
    expect(o.activity.first.date, '2026-06-21');
    expect(o.activity.last, const ActivityDay(date: '2026-09-18', count: 12));
  });

  test('fixture tối thiểu (chỉ localDate/timeZone/streak/today/activity) ⇒ mọi khối phụ null, không ném', () {
    final o = ProgressOverview.fromJson(loadFixture('progress_overview_minimal.json'));
    expect(o.localDate, '2026-09-17');
    expect(o.srs, isNull);
    expect(o.dailyGoal, isNull);
    expect(o.vocabulary, isNull);
    expect(o.lessons, isNull);
    expect(o.writing, isNull);
    expect(o.tone, isNull);
    expect(o.activity, hasLength(90));
    expect(o.streak.current, 0);
  });

  test('fixture người mới: tone.accuracy null giữ khoá; lessons không có lastCompleted', () {
    final o = ProgressOverview.fromJson(loadFixture('progress_overview_new_user.json'));
    expect(o.tone?.totalAnswered, 0);
    expect(o.tone?.accuracy, isNull);
    expect(o.tone?.recommendedFocus, isEmpty);
    expect(o.lessons?.next?.title, 'Chào hỏi');
    expect(o.lessons?.lastCompleted, isNull);
    expect(o.srs?.nextDueAt, isNull);
  });

  test('JSON rỗng/sai kiểu ⇒ mặc định an toàn (WhenWritingNull, RK-M21)', () {
    final o = ProgressOverview.fromJson(null);
    expect(o.localDate, '');
    expect(o.streak.studiedToday, isFalse);
    expect(o.activity, isEmpty);

    final weird = ProgressOverview.fromJson({
      'localDate': 20260917,
      'streak': 'x',
      'srs': {'dueToday': '3', 'dueNow': 2.0},
      'lessons': {
        'published': 1,
        'next': {'title': 'không slug'},
        'lastCompleted': {'slug': 'a'},
      },
      'tone': {
        'totalAnswered': 1,
        'recommendedFocus': [1, 'x', null, 2.0],
      },
      'activity': [
        {'date': '2026-09-17', 'count': 1},
        {'count': 5},
        'rác',
      ],
    });
    expect(weird.localDate, '20260917');
    expect(weird.streak.current, 0);
    expect(weird.srs?.dueToday, 3);
    expect(weird.srs?.dueNow, 2);
    expect(weird.lessons?.next, isNull); // thiếu slug ⇒ coi như vắng
    expect(weird.lessons?.lastCompleted?.title, 'a'); // thiếu title ⇒ dùng slug
    expect(weird.tone?.recommendedFocus, [1, 2]);
    expect(weird.activity, [const ActivityDay(date: '2026-09-17', count: 1)]);
  });

  test('getProgressOverview: GET /progress/overview với skipErrorRedirect', () async {
    final adapter = FakeAdapter((_) async => (200, overviewFixture('progress_overview_minimal.json')));
    final dio = createApiClient(baseUrl: testConfig.chineseApiUrl, getAccessToken: () => 'tok')
      ..httpClientAdapter = adapter;
    final o = await getProgressOverview(dio);
    expect(o.localDate, '2026-09-17');
    final req = adapter.requests.single;
    expect(req.method, 'GET');
    expect(req.uri.path, endsWith('/progress/overview'));
    expect(req.skipErrorRedirect, isTrue);
    expect(req.headers['Authorization'], 'Bearer tok');
  });
}
