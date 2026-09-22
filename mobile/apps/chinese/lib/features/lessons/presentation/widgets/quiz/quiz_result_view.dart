import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../../core/widgets/hanzi_big.dart';
import '../../../../../core/widgets/speak_button.dart';
import '../../../../../router/routes.dart';
import '../../../data/models.dart';
import '../../../domain/quiz_score.dart';
import '../inline_zh.dart';
import 'question_parts.dart';

/// Key nút "Làm lại" / "Ôn tập ngay" / điểm (test).
const kQuizRetakeKey = ValueKey('quiz-retake');
const kQuizReviewNowKey = ValueKey('quiz-review-now');
const kQuizScoreKey = ValueKey('quiz-score');

/// Kết quả lượt quiz (§5.3.1, port `QuizResultView.tsx`): điểm lớn, Đạt/Chưa đạt kèm ngưỡng, khối chúc mừng khi
/// `firstCompletion` ("Đã thêm N từ vào ôn tập" chỉ khi N > 0 — phát lại cùng `clientAttemptId` trả 0) + "Ôn tập ngay",
/// từng câu với đáp án đã chọn / đáp án đúng / lời giải (chữ Hán nội dòng), nút "Làm lại". Nút "Luyện viết chữ của
/// bài" hiện sau M10.
class QuizResultView extends StatelessWidget {
  const QuizResultView({super.key, required this.result, required this.questions, required this.onRetry});

  final QuizResult result;

  /// Ảnh chụp câu hỏi của lượt (không đọc lại từ provider — admin có thể vừa sửa quiz).
  final List<QuizQuestion> questions;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final byId = {for (final q in questions) q.id: q};
    final needed = minCorrectToPass(result.total, result.passThresholdPercent);
    final passed = result.passed;
    final fg = passed ? scheme.onPrimary : scheme.onSurface;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Card(
          color: passed ? scheme.primary : null,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                Text(
                  '${result.scorePercent}%',
                  key: kQuizScoreKey,
                  style: theme.textTheme.displayMedium?.copyWith(
                    fontWeight: FontWeight.w800,
                    height: 1.1,
                    color: fg,
                    fontFeatures: const [FontFeature.tabularFigures()],
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Đúng ${result.correct}/${result.total} câu',
                  style: theme.textTheme.bodyLarge?.copyWith(color: fg),
                ),
                const SizedBox(height: 12),
                Chip(
                  label: Text(passed ? 'Đạt' : 'Chưa đạt'),
                  labelStyle: TextStyle(color: passed ? scheme.primary : scheme.onSurface, fontWeight: FontWeight.w700),
                  backgroundColor: passed ? scheme.onPrimary : scheme.surfaceContainerHighest,
                  side: BorderSide.none,
                ),
                const SizedBox(height: 8),
                Text(
                  'Ngưỡng hoàn thành ${result.passThresholdPercent}% (đúng ít nhất $needed/${result.total} câu).'
                  '${passed ? '' : ' Đọc lại hội thoại và từ vựng rồi thử lại nhé.'}',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium?.copyWith(color: fg.withValues(alpha: 0.9)),
                ),
              ],
            ),
          ),
        ),
        if (result.firstCompletion) ...[
          const SizedBox(height: 12),
          Card(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
              side: BorderSide(color: scheme.primary, width: 2),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.emoji_events_outlined, size: 36, color: scheme.primary),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Hoàn thành bài!',
                              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                            ),
                            Text(
                              result.srsCardsAdded > 0
                                  ? 'Đã thêm ${result.srsCardsAdded} từ của bài vào ôn tập — thẻ mới sẽ được ưu tiên '
                                        'trong hạn mức mỗi ngày.'
                                  : 'Từ của bài đã có trong ôn tập.',
                              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    key: kQuizReviewNowKey,
                    onPressed: () => context.go(AppRoutes.review),
                    icon: const Icon(Icons.style_outlined),
                    label: const Text('Ôn tập ngay'),
                  ),
                ],
              ),
            ),
          ),
        ],
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Từng câu', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                for (var i = 0; i < result.results.length; i++) ...[
                  if (i > 0) const Divider(height: 8),
                  _ResultRow(index: i, question: byId[result.results[i].questionId], r: result.results[i]),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        if (passed)
          OutlinedButton.icon(
            key: kQuizRetakeKey,
            onPressed: onRetry,
            icon: const Icon(Icons.replay),
            label: const Text('Làm lại'),
            style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(52)),
          )
        else
          FilledButton.icon(
            key: kQuizRetakeKey,
            onPressed: onRetry,
            icon: const Icon(Icons.replay),
            label: const Text('Làm lại'),
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
          ),
      ],
    );
  }
}

class _ResultRow extends StatelessWidget {
  const _ResultRow({required this.index, required this.question, required this.r});

  final int index;
  final QuizQuestion? question;
  final QuizQuestionResult r;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant);
    final q = question;
    final chosen = q?.optionById(r.optionId);
    final correct = q?.optionById(r.correctOptionId);
    final audio = q?.audioText;
    final explanation = r.explanation?.trim();
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(r.correct ? Icons.check_circle : Icons.cancel, color: r.correct ? scheme.primary : scheme.error),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  'Câu ${index + 1}${q?.type == 'listen_choice' ? ' · nghe' : ''}',
                  style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
                ),
                if (q != null)
                  QuestionPrompt(question: q, compact: true)
                else
                  Text('(Câu hỏi đã thay đổi)', style: muted),
                if (q != null && q.type == 'listen_choice' && audio != null && audio.isNotEmpty)
                  Row(
                    children: [
                      HanziBig(audio, size: HanziSize.sm, style: const TextStyle(fontSize: 20)),
                      SpeakButton(text: audio),
                    ],
                  ),
                const SizedBox(height: 4),
                Wrap(
                  spacing: 6,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Text('Bạn chọn:', style: muted),
                    if (chosen != null) OptionText(option: chosen, small: true) else const Text('—'),
                  ],
                ),
                if (!r.correct)
                  Wrap(
                    spacing: 6,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(
                        'Đáp án đúng:',
                        style: theme.textTheme.bodyMedium?.copyWith(color: scheme.primary, fontWeight: FontWeight.w600),
                      ),
                      if (correct != null) OptionText(option: correct, small: true) else const Text('—'),
                    ],
                  ),
                if (explanation != null && explanation.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: InlineZhText(explanation, style: muted),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
