import 'package:flutter/material.dart';

import '../../data/models.dart';
import '../../domain/labels.dart';
import 'dashboard_card.dart';

/// Key của số chuỗi hiện tại (test đọc số, tránh trùng với số ở thẻ khác).
const kStreakCurrentKey = ValueKey('streak-current');

/// Chuỗi ngày học (R-PG3/R-PG4, port `StreakCard.tsx`): số ngày lớn + "Dài nhất: N"; chưa học hôm nay ⇒ nhắc
/// "Học 1 hoạt động để giữ chuỗi". Không đóng băng, không bù ngày (D8) — chuỗi hiện tại vẫn còn cho tới hết ngày
/// hôm nay theo múi giờ hồ sơ.
class StreakCard extends StatelessWidget {
  const StreakCard({super.key, required this.streak, required this.today});

  final ProgressStreak streak;
  final ProgressToday today;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final active = streak.current > 0;
    final numberColor = !active
        ? theme.disabledColor
        : streak.studiedToday
        ? successColorOf(context)
        : warningColorOf(context);
    final warning = warningColorOf(context);

    return DashboardCard(
      title: 'Chuỗi ngày học',
      icon: Icons.local_fire_department_outlined,
      aside: streak.studiedToday
          ? Chip(
              avatar: Icon(Icons.check_circle_outline, size: 18, color: successColorOf(context)),
              label: const Text('Hôm nay đã học'),
              side: BorderSide(color: successColorOf(context)),
              visualDensity: VisualDensity.compact,
            )
          : const Chip(label: Text('Hôm nay chưa học'), visualDensity: VisualDensity.compact),
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.baseline,
          textBaseline: TextBaseline.alphabetic,
          children: [
            Text(
              '${streak.current}',
              key: kStreakCurrentKey,
              semanticsLabel: 'Chuỗi hiện tại ${streak.current} ngày',
              style: TextStyle(
                fontSize: 48,
                height: 1,
                fontWeight: FontWeight.w800,
                fontFeatures: kTabularNumbers,
                color: numberColor,
              ),
            ),
            const SizedBox(width: 8),
            const Expanded(child: MutedText('ngày liên tiếp')),
          ],
        ),
        MutedText(streakMessage(streak), color: streak.studiedToday ? null : warning, bold: !streak.studiedToday),
        MutedText('${longestLabel(streak)}${today.activityCount > 0 ? ' · hôm nay ${today.activityCount} lượt' : ''}'),
      ],
    );
  }
}
