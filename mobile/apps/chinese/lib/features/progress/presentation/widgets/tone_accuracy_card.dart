import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../data/models.dart';
import '../../domain/labels.dart';
import '../../domain/today_tasks.dart';
import 'dashboard_card.dart';

/// Thanh điệu (K14, port `ToneAccuracyCard.tsx`): độ chính xác chung + chip thanh cần luyện; chưa làm bài nào ⇒ CTA làm
/// bài đầu tiên. Nút dẫn tới tab luyện thanh (`/pinyin?tab=luyen`, M7).
class ToneAccuracyCard extends StatelessWidget {
  const ToneAccuracyCard({super.key, required this.tone});

  final ProgressTone tone;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final percent = toPercent(tone.accuracy);
    final weak = tone.recommendedFocus;
    final notEnough = tone.totalAnswered < kPinyinMinAnswered;
    final warning = warningColorOf(context);
    final percentColor = percent == null
        ? theme.colorScheme.onSurfaceVariant
        : percent >= 85
        ? successColorOf(context)
        : percent >= 80
        ? theme.colorScheme.onSurface
        : warning;

    return DashboardCard(
      title: 'Thanh điệu',
      icon: Icons.graphic_eq_outlined,
      children: [
        if (tone.totalAnswered == 0) ...[
          const MutedText('Chưa có bài luyện nghe thanh nào. Nghe được 4 thanh là nền tảng trước khi học từ.'),
          FilledButton(onPressed: () => context.go(kToneDrillPath), child: const Text('Làm bài luyện thanh đầu tiên')),
        ] else ...[
          BigNumber(
            value: percent == null ? '—' : '$percent%',
            suffix: 'nghe đúng thanh · ${tone.totalAnswered} câu',
            color: percentColor,
          ),
          if (weak.isNotEmpty)
            Wrap(
              spacing: 6,
              runSpacing: 4,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                const MutedText('Cần luyện:'),
                for (final t in weak)
                  Chip(
                    label: Text('Thanh $t'),
                    side: BorderSide(color: warning),
                    labelStyle: TextStyle(color: warning),
                    visualDensity: VisualDensity.compact,
                  ),
              ],
            )
          else
            MutedText(
              notEnough
                  ? 'Làm thêm ${kPinyinMinAnswered - tone.totalAnswered} câu để có nhận xét theo từng thanh.'
                  : 'Bốn thanh đều khá vững.',
            ),
          OutlinedButton(
            onPressed: () => context.go(kToneDrillPath),
            child: Text(weak.isNotEmpty ? 'Luyện thanh ${weak.join(', ')}' : 'Luyện thanh'),
          ),
        ],
      ],
    );
  }
}
