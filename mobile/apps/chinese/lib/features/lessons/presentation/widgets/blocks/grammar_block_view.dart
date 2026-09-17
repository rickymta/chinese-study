import 'package:flutter/material.dart';

import '../../../data/models.dart';
import '../inline_zh.dart';
import '../zh_line_row.dart';

/// Khối ngữ pháp (port `GrammarBlock.tsx`): tiêu đề, mẫu câu (nền nổi, viền trái), giải thích (chữ Hán nội dòng), ví
/// dụ (dòng như hội thoại + ghi chú, chữ 22).
class GrammarBlockView extends StatelessWidget {
  const GrammarBlockView({super.key, required this.block});

  final GrammarBlock block;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final label = theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant, letterSpacing: 0.5);
    final pattern = block.pattern?.trim();
    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(block.title, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
            if (pattern != null && pattern.isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.fromLTRB(12, 6, 12, 8),
                decoration: BoxDecoration(
                  color: scheme.surfaceContainerHighest.withValues(alpha: 0.6),
                  borderRadius: BorderRadius.circular(8),
                  border: Border(left: BorderSide(color: scheme.primary, width: 3)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('MẪU CÂU', style: label),
                    InlineZhText(pattern, style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 8),
            InlineZhText(block.explanation, style: theme.textTheme.bodyLarge),
            if (block.examples.isNotEmpty) ...[
              const SizedBox(height: 12),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: Text('VÍ DỤ', style: label),
              ),
              for (final ex in block.examples) ZhLineRow(line: ex, hanziSize: 22),
            ],
          ],
        ),
      ),
    );
  }
}
