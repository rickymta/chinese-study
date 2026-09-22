import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../domain/today_tasks.dart';
import 'dashboard_card.dart';

IconData _iconOf(TodayTaskKind kind) => switch (kind) {
  TodayTaskKind.review => Icons.style_outlined,
  TodayTaskKind.newCards => Icons.add_card_outlined,
  TodayTaskKind.lesson => Icons.auto_stories_outlined,
  TodayTaskKind.writing => Icons.draw_outlined,
  TodayTaskKind.tone => Icons.graphic_eq_outlined,
  TodayTaskKind.pinyin => Icons.record_voice_over_outlined,
};

/// "Việc hôm nay" (R-PG9, port `TodayTasks.tsx`): danh sách bấm được, đúng thứ tự; rỗng ⇒ lời khen ngắn. Mỗi dòng cao
/// ≥ 48 cho ngón tay. Bấm ⇒ `context.go(to)` — đường dẫn giống web; màn chưa có trên app ⇒ `ComingSoonPage`.
class TodayTasksCard extends StatelessWidget {
  const TodayTasksCard({super.key, required this.tasks});

  final List<TodayTask> tasks;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return DashboardCard(
      title: 'Việc hôm nay',
      icon: Icons.checklist_outlined,
      children: [
        if (tasks.isEmpty)
          const MutedText('Không còn việc nào — bạn có thể tra từ điển hoặc luyện viết thêm.')
        else
          Column(
            children: [
              for (final t in tasks)
                ListTile(
                  key: ValueKey('today-task-${t.kind.name}'),
                  leading: Icon(_iconOf(t.kind), color: theme.colorScheme.primary),
                  title: Text(t.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: t.subtitle == null ? null : Text(t.subtitle!, maxLines: 1, overflow: TextOverflow.ellipsis),
                  trailing: const Icon(Icons.chevron_right),
                  minTileHeight: 48,
                  contentPadding: EdgeInsets.zero,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  onTap: () => context.go(t.to),
                ),
            ],
          ),
      ],
    );
  }
}
