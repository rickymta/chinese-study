import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';
import 'dashboard_card.dart';

/// Từ vựng (R-PG8, port `VocabularyCard.tsx`): `introduced/totalInPath`; thanh xếp chồng tự vẽ: vững (xanh) + đang
/// học (xanh nhạt) + chưa học (nền). Chưa có thẻ nào ⇒ CTA sang ôn tập.
class VocabularyCard extends StatelessWidget {
  const VocabularyCard({super.key, required this.vocabulary});

  final ProgressVocabulary vocabulary;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final v = vocabulary;
    final denom = [v.totalInPath, v.introduced, 1].reduce((a, b) => a > b ? a : b);
    final percent = v.totalInPath > 0 ? (v.introduced / v.totalInPath * 100).round() : 0;
    final notYet = (v.totalInPath - v.introduced).clamp(0, v.totalInPath);
    final success = successColorOf(context);
    final learningColor = theme.colorScheme.primary.withValues(alpha: 0.6);
    final restColor = theme.colorScheme.onSurface.withValues(alpha: 0.08);
    final rest = (denom - v.mature - v.learning).clamp(0, denom);

    return DashboardCard(
      title: 'Từ vựng',
      icon: Icons.translate_outlined,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.baseline,
          textBaseline: TextBaseline.alphabetic,
          children: [
            Expanded(
              child: BigNumber(value: '${v.introduced}', suffix: '/ ${v.totalInPath} từ trong lộ trình'),
            ),
            Text('$percent%', style: theme.textTheme.bodyMedium?.copyWith(fontFeatures: kTabularNumbers)),
          ],
        ),
        // Thanh xếp chồng: các đoạn theo tỉ lệ `flex`; đoạn 0 bỏ qua (Expanded không nhận flex 0).
        Semantics(
          label: '${v.mature} từ vững, ${v.learning} từ đang học, $notYet từ chưa học',
          child: ClipRRect(
            borderRadius: BorderRadius.circular(5),
            child: SizedBox(
              height: 10,
              child: Row(
                children: [
                  if (v.mature > 0)
                    Expanded(
                      flex: v.mature,
                      child: ColoredBox(color: success),
                    ),
                  if (v.learning > 0)
                    Expanded(
                      flex: v.learning,
                      child: ColoredBox(color: learningColor),
                    ),
                  if (rest > 0)
                    Expanded(
                      flex: rest,
                      child: ColoredBox(color: restColor),
                    ),
                ],
              ),
            ),
          ),
        ),
        Wrap(
          spacing: 16,
          runSpacing: 4,
          children: [
            _Legend(color: success, label: 'Vững', value: v.mature),
            _Legend(color: learningColor, label: 'Đang học', value: v.learning),
            _Legend(color: restColor, label: 'Chưa học', value: notYet),
          ],
        ),
        if (v.introduced == 0)
          OutlinedButton(onPressed: () => context.go(AppRoutes.review), child: const Text('Học từ đầu tiên')),
      ],
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend({required this.color, required this.label, required this.value});

  final Color color;
  final String label;
  final int value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text.rich(
          TextSpan(
            text: '$label: ',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            children: [
              TextSpan(
                text: '$value',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
