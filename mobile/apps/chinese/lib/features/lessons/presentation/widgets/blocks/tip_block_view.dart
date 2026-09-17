import 'package:flutter/material.dart';

import '../../../data/models.dart';
import '../inline_zh.dart';

/// Tiêu đề + biểu tượng theo `variant` (không có ⇒ "Mẹo") — cùng chuỗi với web.
({String title, IconData icon}) tipMeta(String? variant) => switch (variant) {
  'pronunciation' => (title: 'Mẹo phát âm', icon: Icons.record_voice_over_outlined),
  'culture' => (title: 'Văn hoá', icon: Icons.public_outlined),
  'memory' => (title: 'Mẹo ghi nhớ', icon: Icons.psychology_outlined),
  'grammar' => (title: 'Lưu ý ngữ pháp', icon: Icons.menu_book_outlined),
  _ => (title: 'Mẹo', icon: Icons.lightbulb_outline),
};

/// Khối mẹo (port `TipBlock.tsx`): dải màu nhẹ với tiêu đề/biểu tượng theo `variant`, nội dung có chữ Hán nội dòng.
class TipBlockView extends StatelessWidget {
  const TipBlockView({super.key, required this.block});

  final TipBlock block;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final meta = tipMeta(block.variant);
    return Container(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
      decoration: BoxDecoration(color: scheme.secondaryContainer, borderRadius: BorderRadius.circular(10)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(meta.icon, size: 22, color: scheme.onSecondaryContainer),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  meta.title,
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                    color: scheme.onSecondaryContainer,
                  ),
                ),
                const SizedBox(height: 2),
                InlineZhText(
                  block.text,
                  style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSecondaryContainer),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
