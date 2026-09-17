import 'package:flutter/material.dart';

import '../../../data/models.dart';
import '../inline_zh.dart';

/// Khối văn bản: mỗi đoạn một dòng, chữ Hán nội dòng dạng ruby (chạm để nghe).
class TextBlockView extends StatelessWidget {
  const TextBlockView({super.key, required this.block});

  final TextBlock block;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        for (var i = 0; i < block.paragraphs.length; i++) ...[
          if (i > 0) const SizedBox(height: 8),
          InlineZhText(block.paragraphs[i], style: Theme.of(context).textTheme.bodyLarge),
        ],
      ],
    );
  }
}
