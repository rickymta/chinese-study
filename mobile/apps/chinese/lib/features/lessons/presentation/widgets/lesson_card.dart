import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';

/// Nhãn chip bài chưa duyệt (cùng chuỗi với web).
const kUnreviewedLessonLabel = 'Nội dung chưa được duyệt';

/// Tooltip chip chưa duyệt (cùng chuỗi với web).
const kUnreviewedLessonTooltip = 'Bài do hệ thống soạn, chưa có người duyệt — có thể còn sai sót.';

/// Key thẻ bài theo slug trong danh sách "Tất cả bài học" và key thẻ "Bài tiếp theo" (test) — hai thẻ cùng bài nằm
/// chung một `Column` nên phải khác key.
Key lessonCardKey(String slug) => ValueKey('lesson-$slug');
const kNextLessonCardKey = ValueKey('lesson-next');

/// Chip trạng thái học của bài: Chưa học / Đang học / Hoàn thành {best}% (port `LessonStatusChip`).
class LessonStatusChip extends StatelessWidget {
  const LessonStatusChip({super.key, required this.progress});

  final LessonProgress? progress;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final p = progress;
    if (p == null) return const _SmallChip(label: 'Chưa học');
    if (p.isCompleted) {
      final best = p.bestScorePercent;
      return _SmallChip(
        label: best != null ? 'Hoàn thành $best%' : 'Hoàn thành',
        icon: Icons.check_circle_outline,
        color: scheme.primary,
        filled: true,
      );
    }
    return _SmallChip(label: 'Đang học', icon: Icons.play_circle_outline, color: scheme.primary);
  }
}

/// Chip "Nội dung chưa được duyệt" (R-LS2) — chạm để xem lời giải thích.
class UnreviewedLessonChip extends StatelessWidget {
  const UnreviewedLessonChip({super.key});

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Tooltip(
      message: kUnreviewedLessonTooltip,
      triggerMode: TooltipTriggerMode.tap,
      child: _SmallChip(label: kUnreviewedLessonLabel, color: scheme.tertiary),
    );
  }
}

/// Chip nhỏ gọn (viền màu / tô màu) dùng cho trạng thái bài — cỡ thường 12 để vừa 360 px.
class _SmallChip extends StatelessWidget {
  const _SmallChip({required this.label, this.icon, this.color, this.filled = false});

  final String label;
  final IconData? icon;
  final Color? color;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final c = color ?? scheme.onSurfaceVariant;
    final fg = filled ? scheme.onPrimary : c;
    return Chip(
      avatar: icon == null ? null : Icon(icon, size: 16, color: fg),
      label: Text(label),
      labelStyle: TextStyle(color: fg, fontSize: 12, fontWeight: filled ? FontWeight.w600 : FontWeight.w500),
      backgroundColor: filled ? c : Colors.transparent,
      side: BorderSide(color: filled ? Colors.transparent : c),
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      padding: const EdgeInsets.symmetric(horizontal: 6),
    );
  }
}

/// Thẻ bài học (§5.3.1): số thứ tự, tiêu đề, tóm tắt (≤ 3 dòng), số từ · số câu · ~phút, chip trạng thái, chip
/// "Nội dung chưa được duyệt" khi `reviewStatus = machine`. Cả thẻ chạm ⇒ `/bai-hoc/:slug` (`go` trong nhánh Bài học —
/// route phẳng, không chồng danh sách).
class LessonCard extends StatelessWidget {
  const LessonCard({super.key, required this.lesson, this.highlighted = false});

  final LessonSummary lesson;

  /// `true` ⇒ viền nổi bật (bài tiếp theo).
  final bool highlighted;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant);
    final summary = lesson.summary?.trim();
    return Card(
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: highlighted ? scheme.primary : scheme.outlineVariant, width: highlighted ? 2 : 1),
      ),
      child: InkWell(
        onTap: () => context.go(AppRoutes.lesson(lesson.slug)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    lesson.orderIndex.toString().padLeft(2, '0'),
                    style: theme.textTheme.titleMedium?.copyWith(
                      color: scheme.primary,
                      fontWeight: FontWeight.w700,
                      fontFeatures: const [FontFeature.tabularFigures()],
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      lesson.title,
                      style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                    ),
                  ),
                ],
              ),
              if (summary != null && summary.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text(
                  summary,
                  style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
              const SizedBox(height: 8),
              Row(
                children: [
                  Icon(Icons.schedule_outlined, size: 16, color: scheme.onSurfaceVariant),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      '${lesson.wordCount} từ · ${lesson.questionCount} câu hỏi · ~${lesson.estimatedMinutes} phút',
                      style: muted,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 6,
                runSpacing: 4,
                children: [
                  LessonStatusChip(progress: lesson.progress),
                  if (lesson.isUnreviewed) const UnreviewedLessonChip(),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
