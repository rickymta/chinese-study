import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../progress/domain/labels.dart';
import '../../../progress/presentation/widgets/dashboard_card.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import 'pinyin_load_error.dart';

/// Thẻ thống kê thanh (R5-13, port `ToneStatsCard.tsx`): 4 thanh `LinearProgressIndicator` + `a/b câu`; nhầm lẫn tối
/// đa 3 dòng; `g0Reached` ⇒ banner thành công; chưa có phiên ⇒ lời mời làm bài đầu; 503 ⇒ báo tại chỗ.
class ToneStatsCard extends ConsumerWidget {
  const ToneStatsCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final provider = ref.watch(toneStatsProvider);
    final stats = ref.watch(provider);
    final theme = Theme.of(context);
    return SectionCard(
      title: 'Bạn nghe thanh nào tốt?',
      child: AsyncValueView<ToneStats>(
        value: stats,
        loading: const PinyinSkeleton(rows: 4, height: 28),
        error: (e) =>
            PinyinLoadError(error: e, onRetry: () => ref.invalidate(provider), title: 'Không tải được thống kê'),
        data: (s) => s.sessionsCount == 0
            ? Text(
                'Làm bài đầu tiên để xem bạn yếu thanh nào.',
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              )
            : _StatsBody(stats: s),
      ),
    );
  }
}

class _StatsBody extends StatelessWidget {
  const _StatsBody({required this.stats});

  final ToneStats stats;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant);
    final warning = warningColorOf(context);
    final success = successColorOf(context);
    final overall = toPercent(stats.accuracy);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (stats.g0Reached) ...[
          Material(
            color: scheme.primaryContainer,
            borderRadius: BorderRadius.circular(10),
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Icon(Icons.check_circle_outline, color: scheme.onPrimaryContainer),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Bạn đã nghe thanh khá vững — có thể bắt đầu học từ vựng.',
                      style: TextStyle(color: scheme.onPrimaryContainer),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
        ],
        for (final t in kDrillTones) ...[
          Builder(
            builder: (context) {
              final acc = stats.byTone[t] ?? const ToneAccuracy();
              final p = toPercent(acc.accuracy);
              final weak = stats.recommendedFocus.contains(t);
              final barColor = p == null
                  ? scheme.outlineVariant
                  : p >= 85
                  ? success
                  : p >= 80
                  ? scheme.primary
                  : warning;
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            weak ? 'Thanh $t · cần luyện' : 'Thanh $t',
                            style: theme.textTheme.bodyMedium?.copyWith(
                              fontWeight: weak ? FontWeight.w700 : FontWeight.w500,
                              color: weak ? warning : null,
                            ),
                          ),
                        ),
                        Text(p == null ? 'chưa có dữ liệu' : '$p% · ${acc.correct}/${acc.total} câu', style: muted),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Semantics(
                      label: 'Độ chính xác thanh $t',
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(4),
                        child: LinearProgressIndicator(value: (p ?? 0) / 100, minHeight: 8, color: barColor),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ],
        if (stats.confusions.isNotEmpty) ...[
          Text('Hay nhầm', style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 4),
          for (final c in stats.confusions.take(3))
            Text('Bạn hay nghe thanh ${c.expected} thành thanh ${c.answered} (${c.count} lần)', style: muted),
          const SizedBox(height: 8),
        ],
        Text(
          'Tính trên ${stats.windowSize} câu gần nhất mỗi thanh · ${stats.sessionsCount} phiên · '
          '${stats.totalAnswered} câu tổng cộng${overall == null ? '' : ' · chính xác chung $overall%'}',
          style: muted,
        ),
      ],
    );
  }
}
