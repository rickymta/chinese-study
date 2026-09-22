import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';
import 'dashboard_card.dart';

/// Mục tiêu ngày (R-PG5, D8, port `DailyGoalCard.tsx`): hiển thị riêng, không ảnh hưởng chuỗi. Đạt khi hết thẻ đến
/// hạn và đã học đủ thẻ mới trong hạn mức; thanh tiến độ `done/total`, mẫu số 0 ⇒ coi như đạt. "Ôn ngay" ⇒ `/on-tap`.
class DailyGoalCard extends StatelessWidget {
  const DailyGoalCard({super.key, required this.goal, required this.srs});

  final ProgressDailyGoal goal;
  final ProgressSrs srs;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final percent = goal.total == 0 ? 100 : (goal.done / goal.total * 100).round();
    final remaining = srs.dueToday + srs.newAvailableToday;
    final success = successColorOf(context);

    return DashboardCard(
      title: 'Mục tiêu hôm nay',
      icon: Icons.flag_outlined,
      children: [
        Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Expanded(
                  child: BigNumber(value: '${goal.done}/${goal.total}', suffix: 'lượt'),
                ),
                Text('$percent%', style: theme.textTheme.bodyMedium?.copyWith(fontFeatures: kTabularNumbers)),
              ],
            ),
            const SizedBox(height: 6),
            RoundedProgressBar(
              value: percent / 100,
              color: goal.achieved ? success : theme.colorScheme.primary,
              semanticsLabel: 'Tiến độ mục tiêu ngày',
            ),
          ],
        ),
        if (goal.achieved)
          MutedText('Đã đạt mục tiêu hôm nay — hết thẻ đến hạn, đã học đủ thẻ mới.', color: success, bold: true)
        else
          Text.rich(
            TextSpan(
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              children: [
                const TextSpan(text: 'Còn '),
                TextSpan(
                  text: '${srs.dueToday}',
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const TextSpan(text: ' thẻ đến hạn · '),
                TextSpan(
                  text: '${srs.newAvailableToday}',
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const TextSpan(text: ' thẻ mới'),
                if (srs.reviewedToday > 0) TextSpan(text: ' · đã ôn ${srs.reviewedToday} lượt'),
              ],
            ),
          ),
        if (remaining > 0)
          FilledButton.icon(
            onPressed: () => context.go(AppRoutes.review),
            icon: const Icon(Icons.play_arrow),
            label: Text('Ôn ngay ($remaining)'),
          ),
      ],
    );
  }
}
