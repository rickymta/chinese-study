import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/features/dictionary/presentation/pages/word_detail_page.dart';
import 'package:af_chinese/features/lessons/presentation/pages/lesson_detail_page.dart';
import 'package:af_chinese/features/lessons/presentation/pages/lesson_list_page.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/blocks/dialogue_block_view.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/inline_zh.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/lesson_card.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/lesson_word_list.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/quiz/quiz_question_view.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/quiz/quiz_result_view.dart';
import 'package:af_chinese/features/lessons/presentation/widgets/quiz/quiz_runner.dart';
import 'package:af_chinese/features/progress/presentation/pages/dashboard_page.dart';
import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';
import '../../helpers/test_app.dart';

/// Máy chủ bài học giả theo đúng hành vi chinese-backend đã kiểm thật (18/09/2026): danh sách + bài `chao-hoi` từ
/// fixture; `POST /start` ⇒ `in_progress`; nộp quiz chấm theo đáp án lấy từ `quiz_result_failed.json` (fixture thật),
/// idempotent theo `clientAttemptId` (lần đầu 201, phát lại 200 với `srsCardsAdded = 0`), đạt lần đầu ⇒ `firstCompletion`
/// + 14 thẻ + `inSrs = true` cho từ của bài; [offline] ⇒ lỗi kết nối; [fail500Once]; [rejectChanged] ⇒ 422
/// `QUIZ_CHANGED`; [unavailable] ⇒ 503.
class FakeLessonServer {
  FakeLessonServer() {
    final failed = loadFixture('quiz_result_failed.json');
    for (final r in failed['results']! as List) {
      final m = r as Map;
      answerKey[m['questionId'] as String] = m['correctOptionId'] as String;
      explanations[m['questionId'] as String] = m['explanation'] as String;
    }
  }

  final Map<String, String> answerKey = {};
  final Map<String, String> explanations = {};
  bool offline = false;
  bool fail500Once = false;
  bool rejectChanged = false;
  bool unavailable = false;
  int startCalls = 0;
  int listCalls = 0;
  int detailCalls = 0;
  int attemptsCalls = 0;
  int summaryCalls = 0;
  int toneStatsCalls = 0;
  final List<Map<String, Object?>> submitBodies = [];

  /// `clientAttemptId` ⇒ kết quả đã chấm (phát lại trả lại y nguyên, `srsCardsAdded = 0`).
  final Map<String, Map<String, Object?>> attempts = {};
  final List<Map<String, Object?>> history = [];
  Map<String, Object?>? progress;
  bool completed = false;

  String get lessonId => loadFixture('lesson_chao_hoi.json')['id']! as String;

  Future<(int, String)> handle(RequestOptions req) async {
    final path = req.uri.path;
    if (path.endsWith('/srs/summary')) {
      summaryCalls++;
      return (
        200,
        jsonEncode({
          'localDate': '2026-09-18',
          'timeZone': 'Asia/Ho_Chi_Minh',
          'dueToday': 0,
          'dueNow': 0,
          'reviewedToday': 0,
          'reviewsDoneToday': 0,
          'reviewLimitRemaining': 200,
          'dailyReviewLimit': 200,
          'newIntroducedToday': 0,
          'newAvailableToday': completed ? 10 : 0,
          'dailyNewCards': 10,
          'totalCards': completed ? 14 : 0,
          'matureCards': 0,
        }),
      );
    }
    if (path.endsWith('/pinyin/tone-stats')) {
      toneStatsCalls++;
      return (200, jsonEncode(loadFixture('tone_stats_new_user.json')));
    }
    if (path.contains('/dictionary/words/')) {
      final word = loadFixture('dictionary_word.json');
      word['id'] = req.uri.pathSegments.last;
      return (200, jsonEncode(word));
    }
    if (unavailable && path.contains('/lessons')) {
      return (503, jsonEncode({'error': 'Học liệu chưa sẵn sàng.', 'code': 'CONTENT_UNAVAILABLE'}));
    }
    if (path.endsWith('/lessons') && req.method == 'GET') {
      listCalls++;
      final list = loadFixture('lessons_list.json');
      final items = (list['items']! as List).cast<Map<String, Object?>>();
      for (final it in items) {
        if (it['slug'] == 'chao-hoi' && progress != null) it['progress'] = progress;
      }
      if (completed) list['nextLessonSlug'] = 'ban-than';
      return (200, jsonEncode(list));
    }
    if (path.endsWith('/lessons/chao-hoi')) {
      detailCalls++;
      final d = loadFixture('lesson_chao_hoi.json');
      if (progress != null) d['progress'] = progress;
      if (completed) {
        for (final w in d['words']! as List) {
          (w as Map)['inSrs'] = true;
        }
      }
      return (200, jsonEncode(d));
    }
    if (path.contains('/lessons/') && path.endsWith('/start')) {
      startCalls++;
      progress ??= {'status': 'in_progress', 'attemptsCount': 0, 'startedAt': '2026-09-18T02:00:00Z'};
      return (200, jsonEncode(progress));
    }
    if (path.endsWith('/quiz-attempts') && req.method == 'GET') {
      attemptsCalls++;
      return (200, jsonEncode({'items': history.reversed.toList()}));
    }
    if (path.endsWith('/quiz-attempts') && req.method == 'POST') {
      if (offline) throw DioException.connectionError(requestOptions: req, reason: 'offline');
      if (fail500Once) {
        fail500Once = false;
        return (500, jsonEncode({'error': 'Lỗi máy chủ.'}));
      }
      if (rejectChanged) {
        return (422, jsonEncode({'error': 'Bài vừa được cập nhật.', 'code': 'QUIZ_CHANGED'}));
      }
      final body = asJsonMap(req.data is String ? jsonDecode(req.data as String) : req.data) ?? const {};
      submitBodies.add(body);
      final id = body['clientAttemptId'] as String;
      final existing = attempts[id];
      if (existing != null) return (200, jsonEncode({...existing, 'srsCardsAdded': 0}));
      final answers = (body['answers']! as List).cast<Map<String, Object?>>();
      final results = <Map<String, Object?>>[];
      var correct = 0;
      for (final a in answers) {
        final qid = a['questionId'] as String;
        final ok = answerKey[qid] == a['optionId'];
        if (ok) correct++;
        results.add({
          'questionId': qid,
          'optionId': a['optionId'],
          'correct': ok,
          'correctOptionId': answerKey[qid],
          'explanation': explanations[qid],
        });
      }
      final total = answerKey.length;
      final passed = correct * 100 >= 80 * total;
      final first = passed && !completed;
      if (passed) completed = true;
      final best = progress?['bestScorePercent'] as int? ?? 0;
      final score = (correct * 100) ~/ total;
      progress = {
        'status': completed ? 'completed' : 'in_progress',
        'bestScorePercent': score > best ? score : best,
        'attemptsCount': (progress?['attemptsCount'] as int? ?? 0) + 1,
        'startedAt': '2026-09-18T02:00:00Z',
        'lastAttemptAt': '2026-09-18T02:05:00Z',
        if (completed) 'completedAt': '2026-09-18T02:05:00Z',
      };
      final result = {
        'attemptId': 'att-${attempts.length + 1}',
        'submittedAt': '2026-09-18T02:05:00Z',
        'total': total,
        'correct': correct,
        'scorePercent': score,
        'passed': passed,
        'passThresholdPercent': 80,
        'firstCompletion': first,
        'srsCardsAdded': first ? 14 : 0,
        'results': results,
        'progress': progress,
      };
      attempts[id] = result;
      history.add({
        'attemptId': result['attemptId'],
        'submittedAt': result['submittedAt'],
        'total': total,
        'correct': correct,
        'scorePercent': score,
        'passed': passed,
        'durationMs': 1000,
      });
      return (201, jsonEncode(result));
    }
    return (404, jsonEncode({'error': 'không có'}));
  }
}

void main() {
  void useSmallPhone(WidgetTester tester, {double textScale = 1.0}) {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = textScale;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
  }

  /// Mở app đã đăng nhập rồi vào Bài học: qua thanh nav (mặc định) hoặc deep link [location] (`/bai-hoc/chao-hoi?tab=quiz`).
  Future<FakeLessonServer> openLessons(
    WidgetTester tester, {
    FakeLessonServer? server,
    String? location,
    FakeAfTts? tts,
    KeyValueStore? store,
    List<RequestOptions>? requestLog,
    Set<String> permissions = const {'study.use'},
  }) async {
    final srv = server ?? FakeLessonServer();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: FakeAdapter((req) {
          final p = req.uri.path;
          if (p.contains('/lessons') || p.contains('/pinyin/') || p.contains('/dictionary/')) return srv.handle(req);
          return okSystemInfo('chinese-backend').handler(req);
        }),
        identityAdapter: okSystemInfo('identity-service'),
        srs: srv.handle,
        tts: tts ?? FakeAfTts(voices: zhVoices),
        store: store,
        requestLog: requestLog,
        permissions: permissions,
      ),
    );
    await tester.pumpAndSettle();
    if (location != null) {
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go(location);
    } else {
      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Bài học')));
    }
    await tester.pumpAndSettle();
    return srv;
  }

  Future<void> goToTab(WidgetTester tester, String label) async {
    await tester.tap(find.descendant(of: find.byType(TabBar), matching: find.textContaining(label)));
    await tester.pumpAndSettle();
  }

  Future<void> tapKey(WidgetTester tester, Key key) async {
    await tester.ensureVisible(find.byKey(key));
    await tester.tap(find.byKey(key));
    await tester.pumpAndSettle();
  }

  /// Chọn lựa chọn [optionId] ở câu hiện tại rồi bấm "Tiếp" (trừ câu cuối).
  Future<void> answer(WidgetTester tester, String optionId, {bool next = true}) async {
    await tapKey(tester, quizOptionKey(optionId));
    if (next) await tapKey(tester, kQuizNextKey);
  }

  testWidgets('danh sách: "Bài tiếp theo" nổi bật + 5 thẻ theo thứ tự, chip Chưa học/chưa duyệt; chạm ⇒ trang bài', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openLessons(tester);
    expect(find.byType(LessonListPage), findsOneWidget);
    expect(find.text('BÀI TIẾP THEO'), findsOneWidget);
    expect(find.byKey(kNextLessonCardKey), findsOneWidget);
    expect(find.text('TẤT CẢ BÀI HỌC (5)'), findsOneWidget);
    expect(find.text('Chào hỏi'), findsNWidgets(2)); // thẻ tiếp theo + thẻ trong danh sách
    expect(find.text('14 từ · 7 câu hỏi · ~15 phút'), findsWidgets);
    expect(find.text('Chưa học'), findsWidgets);
    expect(find.text(kUnreviewedLessonLabel), findsWidgets);
    expect(server.listCalls, 1);
    // Thứ tự theo orderIndex: 01 Chào hỏi trước.
    final first = tester.getTopLeft(find.byKey(lessonCardKey('chao-hoi')));
    final second = tester.getTopLeft(find.byKey(lessonCardKey('ban-than')));
    expect(first.dy, lessThan(second.dy));
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(kNextLessonCardKey));
    await tester.pumpAndSettle();
    expect(find.byType(LessonDetailPage), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget); // vẫn trong nhánh Bài học
    expect(find.widgetWithText(AppBar, 'Chào hỏi'), findsOneWidget);
    expect(find.text('Bài 1'), findsOneWidget);

    // Quay lại ⇒ danh sách (route phẳng ⇒ go('/bai-hoc')), tiến độ "Đang học" đã được làm mới.
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.byType(LessonListPage), findsOneWidget);
    expect(server.listCalls, 2);
    expect(find.text('Đang học'), findsNWidgets(2));
  });

  testWidgets(
    'trang bài: gọi start ĐÚNG MỘT LẦN; tab Nội dung đủ 4 loại khối, ruby pinyin dấu, tắt công tắc ⇒ ẩn pinyin/nghĩa; '
    'chạm chữ ⇒ đọc; "Nghe cả đoạn" đọc 4 dòng',
    (tester) async {
      useSmallPhone(tester);
      final tts = FakeAfTts(voices: zhVoices);
      final server = await openLessons(tester, location: '/bai-hoc/chao-hoi', tts: tts);
      expect(find.byType(LessonDetailPage), findsOneWidget);
      expect(server.detailCalls, 1);
      expect(server.startCalls, 1);
      expect(server.progress!['status'], 'in_progress');
      // Đầu bài trong tab Nội dung.
      expect(find.text('Đang học'), findsOneWidget); // progress server trả về ghi thẳng vào cache
      expect(find.text('~15 phút'), findsOneWidget);
      expect(find.text(kUnreviewedLessonLabel), findsOneWidget);
      expect(find.text('Sau bài này bạn sẽ'), findsOneWidget);
      expect(find.textContaining('Chào hỏi và đáp lại lời chào'), findsOneWidget);
      expect(find.byKey(kToneHintKey), findsOneWidget); // tone-stats người mới < 40 câu
      expect(server.toneStatsCalls, 1);
      // Ruby: token [[你好|ni3 hao3]] ⇒ chữ 你好 + pinyin dấu phía trên.
      expect(find.byKey(zhTokenKey('你好')), findsWidgets);
      expect(find.text('nǐ hǎo'), findsWidgets);
      // Chạm chữ ⇒ đọc (token đầu nằm dưới phần đầu bài ⇒ cuộn tới trước).
      await tester.ensureVisible(find.byKey(zhTokenKey('你好')).first);
      await tester.tap(find.byKey(zhTokenKey('你好')).first);
      await tester.pumpAndSettle();
      expect(tts.spoken, ['你好']);

      // Hội thoại: tiêu đề, người nói, pinyin dấu, nghĩa; "Nghe cả đoạn" đọc 4 dòng rồi tự về trạng thái ban đầu.
      await tester.scrollUntilVisible(find.text('Gặp nhau buổi sáng'), 300, scrollable: find.byType(Scrollable).first);
      expect(find.text('Gặp nhau buổi sáng'), findsOneWidget);
      expect(find.text('Lan'), findsWidgets);
      expect(find.text('Nǐ hǎo!'), findsWidgets);
      expect(find.text('Chào bạn!'), findsWidgets);
      final dialogueId = ((loadFixture('lesson_chao_hoi.json')['blocks']! as List)[1] as Map)['id'] as String;
      tts.spoken.clear();
      await tapKey(tester, playAllKey(dialogueId));
      expect(tts.spoken, ['你好！', '你好！', '谢谢你！', '不客气。']);
      expect(find.text('Nghe cả đoạn'), findsWidgets); // đọc xong ⇒ nút trở lại

      // Ngữ pháp + mẹo.
      await tester.scrollUntilVisible(find.text('MẪU CÂU').first, 300, scrollable: find.byType(Scrollable).first);
      expect(find.text('Câu hỏi có/không với 吗'), findsOneWidget);
      expect(find.text('VÍ DỤ'), findsWidgets);
      await tester.scrollUntilVisible(find.text('Mẹo phát âm').first, 300, scrollable: find.byType(Scrollable).first);
      expect(find.text('Mẹo phát âm'), findsNWidgets(2));
      expect(tester.takeException(), isNull);

      // Tắt Pinyin ⇒ ruby + dòng pinyin biến mất; tắt Nghĩa Việt ⇒ nghĩa biến mất.
      await tester.scrollUntilVisible(
        find.byKey(kShowPinyinSwitchKey),
        -300,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.byKey(kShowPinyinSwitchKey));
      await tester.pumpAndSettle();
      expect(find.text('nǐ hǎo'), findsNothing);
      expect(find.text('Nǐ hǎo!'), findsNothing);
      expect(find.byKey(zhTokenKey('你好')), findsWidgets); // chữ vẫn còn
      await tester.tap(find.byKey(kShowViSwitchKey));
      await tester.pumpAndSettle();
      expect(find.text('Chào bạn!'), findsNothing);

      // Đổi tab qua lại không gọi lại start/chi tiết.
      await goToTab(tester, 'Từ vựng');
      await goToTab(tester, 'Nội dung');
      expect(server.startCalls, 1);
      expect(server.detailCalls, 1);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('tab Từ vựng: 14 từ (pinyin dấu, Hán Việt, chip Chưa duyệt, nghe); chạm từ ⇒ /tu-dien/:id (push)', (
    tester,
  ) async {
    useSmallPhone(tester);
    await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=tu-vung');
    expect(find.textContaining('Hoàn thành quiz (≥ 80%)'), findsOneWidget);
    expect(find.byKey(lessonWordKey('01a0ac50-faac-7b64-9822-097bd06f04f0')), findsOneWidget);
    expect(find.text('nǐ'), findsOneWidget);
    expect(find.text('NỄ'), findsOneWidget);
    expect(find.text('bạn'), findsWidgets); // 你 và 您 đều có nghĩa "bạn"
    expect(find.text('Chưa duyệt'), findsWidgets);
    expect(find.text('Đang ôn'), findsNothing);
    expect(
      tester.getSize(find.byKey(lessonWordKey('01a0ac50-faac-7b64-9822-097bd06f04f0'))).height,
      greaterThanOrEqualTo(64),
    );
    // Chưa có M10 ⇒ chưa có nút luyện viết trong trang.
    expect(
      find.descendant(of: find.byType(LessonDetailPage), matching: find.textContaining('Luyện viết')),
      findsNothing,
    );

    await tester.tapAt(
      tester.getTopLeft(find.byKey(lessonWordKey('01a0ac50-faac-7b64-9822-097bd06f04f0'))) + const Offset(24, 24),
    );
    await tester.pumpAndSettle();
    expect(find.byType(WordDetailPage), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    // go_router 18: `push` đặt trang từ điển LÊN TRÊN trang bài trong cùng nhánh "Bài học" (nav vẫn ở "Bài học",
    // có nút quay lại) ⇒ quay lại là về bài, đúng tab Từ vựng đang mở.
    expect(tester.widget<NavigationBar>(find.byType(NavigationBar)).selectedIndex, 2);
    expect(find.text('Chi tiết từ'), findsOneWidget);
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    expect(find.byType(WordDetailPage), findsNothing);
    expect(find.byType(LessonDetailPage), findsOneWidget);
    expect(find.byKey(lessonWordKey('01a0ac50-faac-7b64-9822-097bd06f04f0')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'quiz: deep link ?tab=quiz ⇒ màn mở đầu + lịch sử; Bắt đầu ⇒ câu nghe tự đọc + nút Nghe; nộp khoá khi còn câu; '
    'đúng 7/7 ⇒ Đạt + Hoàn thành bài + 14 từ + Ôn tập ngay; làm mới danh sách/SRS/lịch sử/chi tiết (chip Đang ôn)',
    (tester) async {
      useSmallPhone(tester);
      final tts = FakeAfTts(voices: zhVoices);
      final log = <RequestOptions>[];
      final server = await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=quiz', tts: tts, requestLog: log);
      expect(find.text('Kiểm tra bài'), findsOneWidget);
      expect(find.text('7 câu'), findsOneWidget);
      expect(find.text('4 câu nghe'), findsOneWidget);
      expect(find.text('Đạt từ 80% (≥ 6/7 câu)'), findsOneWidget);
      expect(find.text('LẦN LÀM GẦN ĐÂY'), findsOneWidget);
      expect(find.text('Chưa làm lần nào.'), findsOneWidget);
      expect(server.attemptsCalls, 1);
      expect(find.text('Bắt đầu'), findsOneWidget);
      final summaryBefore = server.summaryCalls;
      final listBefore = server.listCalls;

      await tapKey(tester, kQuizStartKey);
      expect(find.text('Câu 1/7'), findsOneWidget);
      expect(find.text('Đã trả lời 0/7'), findsOneWidget);
      expect(find.byKey(kQuizPlayKey), findsOneWidget);
      expect(tts.spoken, ['你好']); // autoPlayAudio mặc định bật ⇒ đọc trong handler "Bắt đầu"
      await tapKey(tester, kQuizPlayKey);
      expect(tts.spoken, ['你好', '你好']);
      expect(find.text('Nghe và chọn nghĩa đúng'), findsOneWidget);
      expect(find.byKey(quizOptionKey('a')), findsOneWidget);
      expect(tester.getSize(find.byKey(quizOptionKey('a'))).height, greaterThanOrEqualTo(56));
      final prev = tester.widget<OutlinedButton>(find.byKey(kQuizPrevKey));
      expect(prev.onPressed, isNull); // câu đầu không có "Trước"

      // Bỏ qua câu 1, trả lời các câu 2..7 rồi tới câu cuối ⇒ nút nộp khoá + "Còn 1 câu chưa trả lời".
      final key = server.answerKey;
      final questions = (loadFixture('lesson_chao_hoi.json')['quiz']! as List).cast<Map<String, Object?>>();
      await tapKey(tester, kQuizNextKey);
      for (var i = 1; i < 6; i++) {
        expect(find.text('Câu ${i + 1}/7'), findsOneWidget);
        await answer(tester, key[questions[i]['id'] as String]!);
      }
      expect(find.text('Câu 7/7'), findsOneWidget);
      await tapKey(tester, quizOptionKey(key[questions[6]['id'] as String]!));
      expect(find.text('Đã trả lời 6/7'), findsOneWidget);
      expect(find.text('Còn 1 câu chưa trả lời — dùng nút "Trước" để quay lại.'), findsOneWidget);
      expect(tester.widget<FilledButton>(find.byKey(kQuizSubmitKey)).onPressed, isNull);

      // Quay về câu 1 bằng "Trước" (lựa chọn giữ nguyên thứ tự đã xáo), trả lời, đi tới cuối và nộp.
      for (var i = 0; i < 6; i++) {
        await tapKey(tester, kQuizPrevKey);
      }
      expect(find.text('Câu 1/7'), findsOneWidget);
      // Tự đọc mỗi lần ĐI TỚI câu nghe bằng nút (Bắt đầu q1, Tiếp q2 q3 q7, Trước q3 q2 q1) + 1 lần bấm Nghe = 8.
      expect(tts.spoken.length, 8);
      expect(tts.spoken.toSet(), {'你好', '谢谢', '再见', '没关系'});
      await tapKey(tester, quizOptionKey(key[questions[0]['id'] as String]!));
      for (var i = 0; i < 6; i++) {
        await tapKey(tester, kQuizNextKey);
      }
      expect(find.text('Đã trả lời 7/7'), findsOneWidget);
      expect(find.text('Nộp bài'), findsOneWidget);
      await tapKey(tester, kQuizSubmitKey);

      // Kết quả.
      expect(find.byType(QuizResultView), findsOneWidget);
      expect(find.text('100%'), findsOneWidget);
      expect(find.text('Đúng 7/7 câu'), findsOneWidget);
      expect(find.text('Đạt'), findsWidgets);
      expect(find.text('Hoàn thành bài!'), findsOneWidget);
      expect(find.textContaining('Đã thêm 14 từ của bài vào ôn tập'), findsOneWidget);
      expect(find.byKey(kQuizReviewNowKey), findsOneWidget);
      expect(find.text('Từng câu'), findsOneWidget);
      expect(find.text('Câu 1 · nghe'), findsOneWidget);
      expect(find.text('Bạn chọn:'), findsNWidgets(7));
      expect(find.text('Đáp án đúng:'), findsNothing);
      expect(find.byKey(zhTokenKey('你好')), findsWidgets); // lời giải có chữ Hán nội dòng
      expect(server.submitBodies, hasLength(1));
      final body = server.submitBodies.single;
      expect(isUuidV4(body['clientAttemptId'] as String), isTrue);
      expect((body['startedAt']! as String).endsWith('Z'), isTrue);
      expect(body['answers'], hasLength(7));
      // Invalidate như web: danh sách bài, lịch sử, tóm tắt SRS (huy hiệu 10), chi tiết (firstCompletion ⇒ inSrs).
      expect(server.summaryCalls, greaterThan(summaryBefore));
      expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('10')), findsOneWidget);
      expect(server.detailCalls, 2);
      expect(find.byType(QuizResultView), findsOneWidget); // tải lại chi tiết không làm mất kết quả đang xem
      await goToTab(tester, 'Từ vựng');
      expect(find.text('Đang ôn'), findsNWidgets(14));
      expect(tester.takeException(), isNull);

      // Làm lại ⇒ màn mở đầu? Không — "Làm lại" ở kết quả bắt đầu lượt mới ngay; "Thoát lượt làm" về mở đầu với lịch sử.
      await goToTab(tester, 'Quiz');
      await tapKey(tester, kQuizRetakeKey);
      expect(find.text('Câu 1/7'), findsOneWidget);
      await tapKey(tester, kQuizQuitKey);
      expect(find.text(kQuizQuitTitle), findsOneWidget);
      await tester.tap(find.text('Thoát'));
      await tester.pumpAndSettle();
      expect(find.text('Kiểm tra bài'), findsOneWidget);
      expect(find.text('Điểm cao nhất 100%'), findsOneWidget);
      expect(find.text('Làm lại'), findsOneWidget); // đã có lần làm ⇒ nút "Làm lại"
      expect(find.text('7/7 · 100%'), findsOneWidget); // lịch sử làm mới
      expect(server.attemptsCalls, 2);

      // Về Trang chủ ⇒ tổng quan tải lại (Riverpod 3 tạm dừng subscription offstage ⇒ chỉ khi quay về).
      final overviewBefore = log.where((r) => r.uri.path.endsWith('/progress/overview')).length;
      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Trang chủ')));
      await tester.pumpAndSettle();
      expect(find.byType(DashboardPage), findsOneWidget);
      expect(log.where((r) => r.uri.path.endsWith('/progress/overview')).length, overviewBefore + 1);
      // Deep link vào thẳng bài ⇒ danh sách chưa từng được tải (autoDispose, không ai theo dõi) ⇒ invalidate không gọi
      // API thừa; mở danh sách sau đó mới tải (kiểm ở test "rời trang").
      expect(server.listCalls, listBefore);
    },
  );

  testWidgets('quiz sai < 80% ⇒ Chưa đạt + đáp án đúng + lời giải; không firstCompletion; Làm lại ⇒ lượt mới', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=quiz');
    await tapKey(tester, kQuizStartKey);
    for (var i = 0; i < 6; i++) {
      await answer(tester, 'a');
    }
    await answer(tester, 'a', next: false);
    await tapKey(tester, kQuizSubmitKey);
    expect(find.byType(QuizResultView), findsOneWidget);
    expect(find.text('28%'), findsOneWidget);
    expect(find.text('Đúng 2/7 câu'), findsOneWidget);
    expect(find.text('Chưa đạt'), findsOneWidget);
    expect(find.textContaining('Đọc lại hội thoại và từ vựng rồi thử lại nhé.'), findsOneWidget);
    expect(find.text('Hoàn thành bài!'), findsNothing);
    expect(find.text('Đáp án đúng:'), findsNWidgets(5));
    expect(find.text('Chào bạn'), findsWidgets); // đáp án đúng câu 1
    expect(server.completed, isFalse);
    expect(server.progress!['bestScorePercent'], 28);
    expect(tester.takeException(), isNull);

    final firstId = server.submitBodies.single['clientAttemptId'];
    await tapKey(tester, kQuizRetakeKey);
    expect(find.text('Câu 1/7'), findsOneWidget);
    await tapKey(tester, quizOptionKey('a'));
    // Lượt mới ⇒ id mới (chưa nộp nên chưa tới server).
    expect(server.submitBodies, hasLength(1));
    expect(server.submitBodies.single['clientAttemptId'], firstId);
  });

  testWidgets('mất mạng khi nộp ⇒ "Chưa nộp được bài" + đáp án KHOÁ; Thử lại (rớt mạng lần nữa rồi thành công) cùng '
      'clientAttemptId ⇒ server đúng 1 lượt; phát lại (500 rồi Thử lại) cũng cùng id', (tester) async {
    useSmallPhone(tester);
    final server = await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=quiz');
    await tapKey(tester, kQuizStartKey);
    final key = server.answerKey;
    final questions = (loadFixture('lesson_chao_hoi.json')['quiz']! as List).cast<Map<String, Object?>>();
    for (var i = 0; i < 6; i++) {
      await answer(tester, key[questions[i]['id'] as String]!);
    }
    await answer(tester, key[questions[6]['id'] as String]!, next: false);
    server.offline = true;
    await tapKey(tester, kQuizSubmitKey);
    expect(find.textContaining('Chưa nộp được bài (Không kết nối được máy chủ'), findsOneWidget);
    expect(find.byKey(kQuizRetrySubmitKey), findsOneWidget);
    expect(server.submitBodies, isEmpty);
    // Khoá đáp án: bấm lựa chọn khác không đổi màu/không đổi đáp án.
    final selectedId = key[questions[6]['id'] as String]!;
    final other = questions[6]['options']! as List;
    final otherId = (other.firstWhere((o) => (o as Map)['id'] != selectedId) as Map)['id'] as String;
    await tapKey(tester, quizOptionKey(otherId));
    expect(tester.widget<Material>(find.byKey(quizOptionKey(selectedId))).color, isNot(Colors.transparent));
    final selectedColor = tester.widget<Material>(find.byKey(quizOptionKey(selectedId))).color;
    final otherColor = tester.widget<Material>(find.byKey(quizOptionKey(otherId))).color;
    expect(selectedColor, isNot(otherColor));

    // Thử lại lần 1 vẫn rớt mạng ⇒ vẫn banner; lần 2 tới nơi ⇒ kết quả, cùng id.
    await tapKey(tester, kQuizRetrySubmitKey);
    expect(find.byKey(kQuizRetrySubmitKey), findsOneWidget);
    server.offline = false;
    await tapKey(tester, kQuizRetrySubmitKey);
    expect(find.byType(QuizResultView), findsOneWidget);
    expect(find.text('100%'), findsOneWidget);
    expect(server.submitBodies, hasLength(1));
    expect(server.attempts.length, 1);
    final id = server.submitBodies.single['clientAttemptId'] as String;
    expect(isUuidV4(id), isTrue);
    // Đáp án gửi lên là bộ đã khoá (câu 7 = đáp án đúng, không phải lựa chọn bấm sau khi khoá).
    final sent = (server.submitBodies.single['answers']! as List).cast<Map<String, Object?>>();
    expect(sent.last['optionId'], selectedId);
    expect(tester.takeException(), isNull);

    // Lượt 2: server 500 một lần ⇒ Thử lại cùng id ⇒ 201; server thấy đúng 2 lượt tổng cộng.
    await tapKey(tester, kQuizRetakeKey);
    for (var i = 0; i < 6; i++) {
      await answer(tester, 'a');
    }
    await answer(tester, 'a', next: false);
    server.fail500Once = true;
    await tapKey(tester, kQuizSubmitKey);
    expect(find.textContaining('Chưa nộp được bài (Lỗi máy chủ.)'), findsOneWidget);
    await tapKey(tester, kQuizRetrySubmitKey);
    expect(find.byType(QuizResultView), findsOneWidget);
    expect(find.text('28%'), findsOneWidget);
    expect(server.submitBodies, hasLength(2));
    expect(server.attempts.length, 2);
    expect(server.submitBodies.last['clientAttemptId'], isNot(id));
  });

  testWidgets(
    '422 QUIZ_CHANGED khi nộp ⇒ toast "Bài vừa được cập nhật — tải lại quiz", về màn mở đầu, chi tiết tải lại',
    (tester) async {
      useSmallPhone(tester);
      final server = await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=quiz');
      await tapKey(tester, kQuizStartKey);
      for (var i = 0; i < 6; i++) {
        await answer(tester, 'a');
      }
      await answer(tester, 'a', next: false);
      server.rejectChanged = true;
      await tapKey(tester, kQuizSubmitKey);
      expect(find.text('Bài vừa được cập nhật — tải lại quiz.'), findsOneWidget);
      expect(find.text('Kiểm tra bài'), findsOneWidget);
      expect(find.byKey(kQuizRetrySubmitKey), findsNothing);
      expect(server.detailCalls, 2);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('rời trang khi đã trả lời ≥ 1 câu ⇒ hỏi "Thoát lượt làm?"; Ở lại giữ đáp án; Thoát ⇒ về danh sách', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openLessons(tester);
    await tester.tap(find.byKey(kNextLessonCardKey));
    await tester.pumpAndSettle();
    await goToTab(tester, 'Quiz');
    await tapKey(tester, kQuizStartKey);
    // Chưa trả lời ⇒ quay lại không hỏi.
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.byType(LessonListPage), findsOneWidget);

    await tester.tap(find.byKey(kNextLessonCardKey));
    await tester.pumpAndSettle();
    await goToTab(tester, 'Quiz');
    await tapKey(tester, kQuizStartKey);
    await tapKey(tester, quizOptionKey('b'));
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.text(kQuizQuitTitle), findsOneWidget);
    await tester.tap(find.text('Ở lại'));
    await tester.pumpAndSettle();
    expect(find.text('Câu 1/7'), findsOneWidget);
    expect(find.text('Đã trả lời 1/7'), findsOneWidget);
    // Đổi tab rồi quay lại: đáp án/lượt vẫn còn (keep-alive).
    await goToTab(tester, 'Nội dung');
    await goToTab(tester, 'Quiz');
    expect(find.text('Đã trả lời 1/7'), findsOneWidget);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Thoát'));
    await tester.pumpAndSettle();
    expect(find.byType(LessonListPage), findsOneWidget);
    expect(server.submitBodies, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('không có giọng: câu nghe hiện cảnh báo + "Hiện chữ" ⇒ chữ Hán; màn mở đầu nhắc', (tester) async {
    useSmallPhone(tester);
    await openLessons(tester, location: '/bai-hoc/chao-hoi?tab=quiz', tts: FakeAfTts());
    expect(find.textContaining('Máy chưa có giọng tiếng Trung'), findsOneWidget);
    await tapKey(tester, kQuizStartKey);
    expect(tester.widget<FilledButton>(find.byKey(kQuizPlayKey)).onPressed, isNull);
    expect(find.textContaining('Chưa có giọng tiếng Trung — không phát được câu hỏi.'), findsOneWidget);
    expect(find.text('你好'), findsNothing);
    await tapKey(tester, kQuizRevealKey);
    expect(find.text('你好'), findsOneWidget);
    expect(find.byKey(kQuizRevealKey), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('503 CONTENT_UNAVAILABLE ⇒ "Học liệu chưa sẵn sàng", không nút Thử lại', (tester) async {
    useSmallPhone(tester);
    final server = FakeLessonServer()..unavailable = true;
    await openLessons(tester, server: server);
    expect(find.text('Học liệu chưa sẵn sàng'), findsOneWidget);
    expect(find.text('Thử lại'), findsNothing);
    expect(server.listCalls, 0);
  });

  testWidgets('chế độ tối + 360×740 + chữ 1,3×: danh sách, nội dung, từ vựng, quiz không tràn', (tester) async {
    useSmallPhone(tester, textScale: 1.3);
    final store = InMemoryKeyValueStore({kInstallFlagKey: true, 'af.themeMode': 'dark'});
    await openLessons(tester, store: store);
    expect(Theme.of(tester.element(find.byType(LessonListPage))).brightness, Brightness.dark);
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(kNextLessonCardKey));
    await tester.pumpAndSettle();
    expect(find.byType(LessonDetailPage), findsOneWidget);
    await tester.drag(find.byType(Scrollable).first, const Offset(0, -2000));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await goToTab(tester, 'Từ vựng');
    await tester.drag(find.byType(Scrollable).first, const Offset(0, -1500));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await goToTab(tester, 'Quiz');
    await tapKey(tester, kQuizStartKey);
    expect(find.text('Câu 1/7'), findsOneWidget);
    for (var i = 0; i < 6; i++) {
      await answer(tester, 'b');
    }
    await answer(tester, 'b', next: false);
    await tapKey(tester, kQuizSubmitKey);
    expect(find.byType(QuizResultView), findsOneWidget);
    await tester.drag(find.byType(Scrollable).first, const Offset(0, -2000));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });
}
