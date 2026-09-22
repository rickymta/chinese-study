import 'package:flutter/material.dart';

import '../pinyin/pinyin.dart';

/// Hiển thị pinyin dạng dấu từ chuỗi SỐ (`ni3 hao3` ⇒ `nǐ hǎo`) — port `Pinyin.tsx` web (§5.3.C, R5-6).
/// Gợi ý biến điệu ([showSandhi]) chỉ là chú thích — app vẫn lưu thanh gốc (R-C3).
class PinyinText extends StatelessWidget {
  const PinyinText(
    this.value, {
    super.key,
    this.hanzi,
    this.showSandhi = false,
    this.join = false,
    this.style,
    this.textAlign,
    this.maxLines,
  });

  /// Pinyin dạng SỐ — dạng lưu trữ; widget tự đổi sang dạng dấu.
  final String value;

  /// Chữ Hán tương ứng (nhận diện 不/一 cho gợi ý biến điệu).
  final String? hanzi;

  /// Hiện gợi ý biến điệu (3-3, 不, 一) dưới dạng chú thích nhỏ. Mặc định tắt.
  final bool showSandhi;

  /// Nối liền âm tiết thành một từ (`Xī'ān`).
  final bool join;
  final TextStyle? style;
  final TextAlign? textAlign;
  final int? maxLines;

  @override
  Widget build(BuildContext context) {
    final marked = numberedToMarked(value, join: join);
    final hints = showSandhi ? sandhiHints(value, hanzi) : const <SandhiHint>[];
    final text = Text(
      marked,
      style: style,
      textAlign: textAlign,
      maxLines: maxLines,
      overflow: maxLines == null ? null : TextOverflow.ellipsis,
    );
    if (hints.isEmpty) return text;
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        text,
        Text(
          'Biến điệu: ${hints.map((h) => 'âm tiết ${h.index + 1} ${h.text}').join('; ')}',
          style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
      ],
    );
  }
}
