import 'package:af_chinese/features/lessons/application/providers.dart';
import 'package:af_chinese/features/lessons/data/models.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

// Fixture lấy ĐÚNG từ chinese-backend chạy thật (18/09/2026, tài khoản mới): danh sách 5 bài, bài `chao-hoi` đủ 4 loại
// khối, kết quả quiz sai/đạt lần đầu/phát lại, lịch sử, tiến độ sau `start`.
void main() {
  test('LessonListResponse.fromJson: 5 bài, bài tiếp theo chao-hoi, bài chưa bắt đầu không có progress', () {
    final l = LessonListResponse.fromJson(loadFixture('lessons_list.json'));
    expect(l.items, hasLength(5));
    expect(l.nextLessonSlug, 'chao-hoi');
    final first = l.sorted.first;
    expect(first.slug, 'chao-hoi');
    expect(first.title, 'Chào hỏi');
    expect(first.orderIndex, 1);
    expect(first.wordCount, 14);
    expect(first.questionCount, 7);
    expect(first.estimatedMinutes, 15);
    expect(first.isUnreviewed, isTrue);
    expect(first.progress, isNull);
    expect(l.sorted.map((x) => x.orderIndex).toList(), [1, 2, 3, 4, 5]);
  });

  test('LessonListResponse: dòng thiếu id/slug bị bỏ; sắp theo orderIndex rồi tiêu đề', () {
    final l = LessonListResponse.fromJson({
      'items': [
        {'id': 'b', 'slug': 'b', 'title': 'B', 'orderIndex': 2},
        {'id': 'a', 'slug': 'a', 'title': 'A', 'orderIndex': 2},
        {'slug': 'khong-id', 'title': 'x'},
        {'id': 'c', 'slug': 'c', 'title': 'C', 'orderIndex': 1},
      ],
    });
    expect(l.items, hasLength(3));
    expect(l.sorted.map((x) => x.slug).toList(), ['c', 'a', 'b']);
    expect(l.nextLessonSlug, isNull);
  });

  test('LessonDetail.fromJson (chao-hoi): 8 khối đủ 4 loại, 14 từ, 7 câu KHÔNG có đáp án, chưa có progress', () {
    final d = LessonDetail.fromJson(loadFixture('lesson_chao_hoi.json'));
    expect(d.slug, 'chao-hoi');
    expect(d.objectives, hasLength(3));
    expect(d.glossary, isEmpty);
    expect(d.progress, isNull);
    expect(d.blocks.map((b) => b.type).toList(), [
      'text',
      'dialogue',
      'dialogue',
      'grammar',
      'grammar',
      'grammar',
      'tip',
      'tip',
    ]);
    final text = d.blocks.first as TextBlock;
    expect(text.paragraphs, hasLength(3));
    expect(text.paragraphs.first, contains('[[你好|ni3 hao3]]'));
    final dialogue = d.blocks[1] as DialogueBlock;
    expect(dialogue.title, 'Gặp nhau buổi sáng');
    expect(dialogue.lines, hasLength(4));
    expect(dialogue.lines.first.speaker, 'Lan');
    expect(dialogue.lines.first.hanzi, '你好！');
    expect(dialogue.lines.first.pinyin, 'Ni3 hao3!');
    expect(dialogue.lines.first.vi, 'Chào bạn!');
    final grammar = d.blocks[3] as GrammarBlock;
    expect(grammar.title, 'Câu hỏi có/không với 吗');
    expect(grammar.pattern, 'Câu trần thuật + [[吗|ma5]]？');
    expect(grammar.examples, hasLength(2));
    expect(grammar.examples.first.note, isNotEmpty);
    final tip = d.blocks[6] as TipBlock;
    expect(tip.variant, 'pronunciation');
    expect(tip.text, contains('[[你好|ni3 hao3]]'));

    expect(d.words, hasLength(14));
    expect(d.words.first.simplified, '你');
    expect(d.words.first.pinyin, 'ni3');
    expect(d.words.first.hanViet, 'nễ');
    expect(d.words.first.meaningsVi, ['bạn']);
    expect(d.words.first.meaningViStatus, 'machine');
    expect(d.words.first.inSrs, isFalse);

    expect(d.quiz, hasLength(7));
    final q1 = d.quiz.first;
    expect(q1.type, 'listen_choice');
    expect(q1.isListen, isTrue);
    expect(q1.audioText, '你好');
    expect(q1.options.map((o) => o.id).toList(), ['a', 'b', 'c', 'd']);
    expect(q1.optionById('b')!.text, 'Chào bạn');
    final q4 = d.quiz[3];
    expect(q4.promptLang, 'zh');
    expect(q4.promptPinyin, 'Ni3 hao3 ma5?');
    expect(d.quiz.where((q) => q.options.any((o) => o.lang == 'zh')), hasLength(1));
    // R-LS10: fixture thô không chứa đáp án.
    final raw = loadFixture('lesson_chao_hoi.json');
    for (final q in raw['quiz']! as List) {
      expect((q as Map).containsKey('correctOptionId'), isFalse);
      expect(q.containsKey('explanation'), isFalse);
    }
  });

  test('LessonBlock.fromJson: kiểu lạ ⇒ UnknownBlock (không ném); dòng hội thoại thiếu hanzi bị bỏ', () {
    final d = LessonDetail.fromJson({
      'id': 'x',
      'slug': 'x',
      'title': 'x',
      'blocks': [
        {'id': '1', 'type': 'video', 'payload': <String, Object?>{}},
        {
          'id': '2',
          'type': 'dialogue',
          'payload': {
            'lines': [
              {'speaker': 'A', 'pinyin': 'x'},
              {'hanzi': '好', 'pinyin': 'hao3', 'vi': 'tốt'},
            ],
          },
        },
        {'id': '3', 'type': 'text'},
      ],
    });
    expect(d.blocks, hasLength(3));
    expect(d.blocks.first, isA<UnknownBlock>());
    expect((d.blocks.first as UnknownBlock).type, 'video');
    expect((d.blocks[1] as DialogueBlock).lines, hasLength(1));
    expect((d.blocks[2] as TextBlock).paragraphs, isEmpty);
  });

  test('QuizResult.fromJson: lần sai (28%, chưa đạt), đạt lần đầu (firstCompletion, 14 thẻ), phát lại (0 thẻ)', () {
    final failed = QuizResult.fromJson(loadFixture('quiz_result_failed.json'));
    expect(failed.total, 7);
    expect(failed.correct, 2);
    expect(failed.scorePercent, 28);
    expect(failed.passed, isFalse);
    expect(failed.firstCompletion, isFalse);
    expect(failed.srsCardsAdded, 0);
    expect(failed.results, hasLength(7));
    expect(failed.results.first.correct, isFalse);
    expect(failed.results.first.correctOptionId, 'b');
    expect(failed.results.first.explanation, '[[你好|ni3 hao3]] là lời chào phổ biến nhất.');
    expect(failed.progress!.status, 'in_progress');
    expect(failed.progress!.bestScorePercent, 28);
    expect(failed.progress!.attemptsCount, 1);

    final first = QuizResult.fromJson(loadFixture('quiz_result_first.json'));
    expect(first.scorePercent, 100);
    expect(first.passed, isTrue);
    expect(first.passThresholdPercent, 80);
    expect(first.firstCompletion, isTrue);
    expect(first.srsCardsAdded, 14);
    expect(first.progress!.isCompleted, isTrue);
    expect(first.progress!.completedAt, isNotNull);
    expect(first.submittedAt, DateTime.utc(2026, 9, 17, 19, 46, 4, 944, 300));

    final replay = QuizResult.fromJson(loadFixture('quiz_result_replay.json'));
    expect(replay.attemptId, first.attemptId);
    expect(replay.firstCompletion, isTrue);
    expect(replay.srsCardsAdded, 0);
  });

  test('QuizAttemptsResponse.fromJson: 2 lần, mới nhất trước, có durationMs', () {
    final a = QuizAttemptsResponse.fromJson(loadFixture('quiz_attempts.json'));
    expect(a.items, hasLength(2));
    expect(a.items.first.passed, isTrue);
    expect(a.items.first.scorePercent, 100);
    expect(a.items.first.durationMs, 944);
    expect(a.items.last.passed, isFalse);
    expect(a.items.last.correct, 2);
  });

  test('LessonProgress.fromJson sau POST /start: in_progress, 0 lần, chưa nộp', () {
    final p = LessonProgress.fromJson(loadFixture('lesson_start.json'));
    expect(p.status, 'in_progress');
    expect(p.attemptsCount, 0);
    expect(p.bestScorePercent, isNull);
    expect(p.lastAttemptAt, isNull);
    expect(p.startedAt, isNotNull);
  });

  test('SubmitQuizRequest.toJson: startedAt ISO có Z, answers theo thứ tự', () {
    final body = SubmitQuizRequest(
      clientAttemptId: 'id-1',
      startedAt: DateTime(2026, 9, 18, 9, 30).toUtc(),
      answers: const [QuizAnswer(questionId: 'q1', optionId: 'a')],
    ).toJson();
    expect(body['clientAttemptId'], 'id-1');
    expect((body['startedAt']! as String).endsWith('Z'), isTrue);
    expect(body['answers'], [
      {'questionId': 'q1', 'optionId': 'a'},
    ]);
  });

  test(
    'DisplayPrefsController: mặc định bật; đọc kho; đổi ⇒ ghi kho; chọn trước khi đọc xong kho ⇒ giữ lựa chọn',
    () async {
      final store = InMemoryKeyValueStore({kShowPinyinKey: false});
      final container = ProviderContainer(overrides: [keyValueStoreProvider.overrideWithValue(store)]);
      addTearDown(container.dispose);
      expect(container.read(displayPrefsProvider), const DisplayPrefs());
      await Future<void>.delayed(Duration.zero);
      expect(container.read(displayPrefsProvider), const DisplayPrefs(showPinyin: false));
      await container.read(displayPrefsProvider.notifier).setShowVi(false);
      expect(store.snapshot[kShowViKey], false);
      expect(container.read(displayPrefsProvider), const DisplayPrefs(showPinyin: false, showVi: false));

      final store2 = InMemoryKeyValueStore({kShowPinyinKey: false, kShowViKey: false});
      final c2 = ProviderContainer(overrides: [keyValueStoreProvider.overrideWithValue(store2)]);
      addTearDown(c2.dispose);
      c2.read(displayPrefsProvider); // bắt đầu đọc kho
      await c2.read(displayPrefsProvider.notifier).setShowPinyin(true);
      await Future<void>.delayed(Duration.zero);
      expect(c2.read(displayPrefsProvider).showPinyin, isTrue);
    },
  );
}
