import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';
import 'dashboard_card.dart';

/// Bài học (port `LessonProgressCard.tsx`): `completed/published` + bài tiếp theo (R-LS4); chưa có bài published ⇒
/// thông báo; hết bài ⇒ chúc mừng.
class LessonProgressCard extends StatelessWidget {
  const LessonProgressCard({super.key, required this.lessons});

  final ProgressLessons lessons;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l = lessons;
    final percent = l.published > 0 ? (l.completed / l.published * 100).round() : 0;
    final firstTime = l.completed == 0 && l.inProgress == 0;
    final next = l.next;

    return DashboardCard(
      title: 'Bài học',
      icon: Icons.auto_stories_outlined,
      children: [
        if (l.published == 0)
          const MutedText('Chưa có bài học nào được xuất bản.')
        else ...[
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.baseline,
                textBaseline: TextBaseline.alphabetic,
                children: [
                  Expanded(
                    child: BigNumber(value: '${l.completed}', suffix: '/ ${l.published} bài hoàn thành'),
                  ),
                  Text('$percent%', style: theme.textTheme.bodyMedium?.copyWith(fontFeatures: kTabularNumbers)),
                ],
              ),
              const SizedBox(height: 6),
              RoundedProgressBar(value: percent / 100, height: 8, semanticsLabel: 'Tiến độ bài học'),
              if (l.inProgress > 0) ...[
                const SizedBox(height: 4),
                MutedText('Đang học dở ${l.inProgress} bài', small: true),
              ],
            ],
          ),
          if (next != null)
            Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  firstTime ? 'BẮT ĐẦU TỪ' : 'BÀI TIẾP THEO',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                    letterSpacing: 0.5,
                  ),
                ),
                Text(
                  next.title,
                  style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 8),
                if (firstTime)
                  FilledButton(
                    onPressed: () => context.go(AppRoutes.lesson(next.slug)),
                    child: const Text('Bắt đầu bài học đầu tiên'),
                  )
                else
                  OutlinedButton(
                    onPressed: () => context.go(AppRoutes.lesson(next.slug)),
                    child: const Text('Học tiếp'),
                  ),
              ],
            )
          else
            MutedText(
              'Bạn đã hoàn thành mọi bài hiện có — tiếp tục ôn tập để giữ từ vựng.',
              color: successColorOf(context),
              bold: true,
            ),
        ],
      ],
    );
  }
}
