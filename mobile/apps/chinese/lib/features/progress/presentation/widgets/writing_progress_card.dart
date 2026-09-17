import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';
import 'dashboard_card.dart';

/// Luyện viết (port `WritingProgressCard.tsx`): đã luyện / đã thuộc / cần luyện trên tổng bộ HSK 1; CTA khi chưa viết
/// chữ nào; lối tắt tới chữ yếu và chữ bài vừa học chưa luyện (R-PG9 mục 4).
class WritingProgressCard extends StatelessWidget {
  const WritingProgressCard({super.key, required this.writing, this.lastCompleted});

  final ProgressWriting writing;

  /// Bài hoàn thành gần nhất — để gợi ý "chữ bài vừa học chưa luyện".
  final ProgressLastCompletedLesson? lastCompleted;

  @override
  Widget build(BuildContext context) {
    final w = writing;
    final last = lastCompleted;
    final unpracticed = last != null && last.unpracticedChars > 0 ? last : null;

    return DashboardCard(
      title: 'Luyện viết',
      icon: Icons.draw_outlined,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Stat(label: 'đã luyện / ${w.totalChars} chữ', value: w.practicedChars),
            const SizedBox(width: 16),
            _Stat(label: 'đã thuộc', value: w.masteredChars, color: successColorOf(context)),
            const SizedBox(width: 16),
            _Stat(label: 'cần luyện', value: w.weakChars, color: w.weakChars > 0 ? warningColorOf(context) : null),
          ],
        ),
        if (w.practicedChars == 0)
          FilledButton(onPressed: () => context.go(AppRoutes.writing), child: const Text('Viết chữ đầu tiên'))
        else if (w.weakChars > 0 || unpracticed != null)
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (w.weakChars > 0)
                OutlinedButton(
                  onPressed: () => context.go('${AppRoutes.writing}?tab=can-luyen'),
                  child: Text('Luyện ${w.weakChars} chữ yếu'),
                ),
              if (unpracticed != null)
                OutlinedButton(
                  onPressed: () =>
                      context.go('${AppRoutes.writing}?tab=bai-hoc&bai=${Uri.encodeComponent(unpracticed.slug)}'),
                  child: Text('${unpracticed.unpracticedChars} chữ bài "${unpracticed.title}"'),
                ),
            ],
          ),
      ],
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value, this.color});

  final String label;
  final int value;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '$value',
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w700,
              fontFeatures: kTabularNumbers,
              color: color,
            ),
          ),
          Text(label, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
        ],
      ),
    );
  }
}
