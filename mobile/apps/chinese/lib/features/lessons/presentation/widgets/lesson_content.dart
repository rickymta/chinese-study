import 'package:flutter/material.dart';

import '../../data/models.dart';
import 'blocks/dialogue_block_view.dart';
import 'blocks/grammar_block_view.dart';
import 'blocks/text_block_view.dart';
import 'blocks/tip_block_view.dart';
import 'glossary_list.dart';

/// Danh sách khối nội dung bài theo thứ tự (port `LessonContent.tsx`). Kiểu khối lạ (backend thêm sau) ⇒ dòng báo nhẹ,
/// không vỡ trang. [glossary] hiện cuối nội dung khi có.
class LessonContent extends StatelessWidget {
  const LessonContent({super.key, required this.blocks, this.glossary = const []});

  final List<LessonBlock> blocks;
  final List<GlossaryEntry> glossary;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        if (blocks.isEmpty) Text('Bài này chưa có nội dung.', style: muted),
        for (var i = 0; i < blocks.length; i++) ...[
          if (i > 0) const SizedBox(height: 12),
          switch (blocks[i]) {
            final TextBlock b => TextBlockView(key: ValueKey('block-${b.id}'), block: b),
            final DialogueBlock b => DialogueBlockView(key: ValueKey('block-${b.id}'), block: b),
            final GrammarBlock b => GrammarBlockView(key: ValueKey('block-${b.id}'), block: b),
            final TipBlock b => TipBlockView(key: ValueKey('block-${b.id}'), block: b),
            final UnknownBlock b => Text('(Khối nội dung chưa hỗ trợ: ${b.type})', style: muted),
          },
        ],
        if (glossary.isNotEmpty) ...[const SizedBox(height: 12), GlossaryList(entries: glossary)],
      ],
    );
  }
}
