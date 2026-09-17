import 'package:flutter/material.dart';

import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../data/models.dart';

/// "Từ bổ sung (không vào ôn tập)" (port `GlossaryList.tsx`): tên riêng, địa danh... chỉ để hiểu bài — không phải từ
/// HSK, không thành thẻ.
class GlossaryList extends StatelessWidget {
  const GlossaryList({super.key, required this.entries});

  final List<GlossaryEntry> entries;

  @override
  Widget build(BuildContext context) {
    if (entries.isEmpty) return const SizedBox.shrink();
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 4, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('Từ bổ sung', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
            Text(
              'Chỉ để hiểu bài — không vào ôn tập.',
              style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 4),
            for (final g in entries)
              Row(
                children: [
                  HanziBig(g.hanzi, size: HanziSize.md),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        PinyinText(g.pinyin, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                        Text(g.vi, style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant)),
                      ],
                    ),
                  ),
                  SpeakButton(text: g.hanzi),
                ],
              ),
          ],
        ),
      ),
    );
  }
}
