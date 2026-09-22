import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../application/providers.dart';
import '../../../data/models.dart';

/// `HH:mm dd/MM/yyyy` theo GIỜ MÁY (`toLocal()`) — Dart không có CSDL múi giờ (BA-mặc định M6, RK-M23); web dùng múi
/// giờ hồ sơ.
String formatAttemptTime(DateTime? utc) {
  if (utc == null) return '';
  final d = utc.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${two(d.hour)}:${two(d.minute)} ${two(d.day)}/${two(d.month)}/${d.year}';
}

/// 5 lần làm gần nhất của bài (`GET /lessons/{id}/quiz-attempts?limit=5`, port `QuizHistory.tsx`), hiện ở màn mở đầu
/// quiz. Lỗi tải ⇒ ẩn (không quan trọng bằng việc làm quiz); rỗng ⇒ "Chưa làm lần nào".
class QuizHistory extends ConsumerWidget {
  const QuizHistory({super.key, required this.lessonId});

  final String lessonId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final value = ref.watch(quizAttemptsProvider(lessonId));
    if (value.hasError && !value.hasValue) return const SizedBox.shrink();
    final items = value.value?.items;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          'LẦN LÀM GẦN ĐÂY',
          style: theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant, letterSpacing: 0.5),
        ),
        const SizedBox(height: 4),
        if (items == null)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 8),
            child: SizedBox(height: 18, child: LinearProgressIndicator(minHeight: 2)),
          )
        else if (items.isEmpty)
          Text('Chưa làm lần nào.', style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant))
        else
          for (final a in items) _AttemptRow(attempt: a),
      ],
    );
  }
}

class _AttemptRow extends StatelessWidget {
  const _AttemptRow({required this.attempt});

  final QuizAttemptSummary attempt;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 6),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: scheme.outlineVariant)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              formatAttemptTime(attempt.submittedAt),
              style: theme.textTheme.bodyMedium?.copyWith(fontFeatures: const [FontFeature.tabularFigures()]),
            ),
          ),
          Text(
            '${attempt.correct}/${attempt.total} · ${attempt.scorePercent}%',
            style: theme.textTheme.bodyMedium?.copyWith(
              fontWeight: FontWeight.w600,
              fontFeatures: const [FontFeature.tabularFigures()],
            ),
          ),
          const SizedBox(width: 8),
          Chip(
            label: Text(attempt.passed ? 'Đạt' : 'Chưa đạt'),
            visualDensity: VisualDensity.compact,
            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
            labelStyle: TextStyle(color: attempt.passed ? scheme.onPrimary : scheme.onSurfaceVariant, fontSize: 12),
            backgroundColor: attempt.passed ? scheme.primary : Colors.transparent,
            side: attempt.passed ? BorderSide.none : BorderSide(color: scheme.outlineVariant),
            padding: const EdgeInsets.symmetric(horizontal: 4),
          ),
        ],
      ),
    );
  }
}
