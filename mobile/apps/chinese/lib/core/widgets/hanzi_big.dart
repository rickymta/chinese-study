import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

/// Cỡ chữ Hán đặt tên (port `HanziSize` web §5.3.D) — chữ Hán cần lớn hơn chữ Latin cùng cấp để thấy rõ nét.
enum HanziSize {
  sm(18),
  md(24),
  lg(36),
  xl(56),
  xxl(80);

  const HanziSize(this.fontSize);

  final double fontSize;
}

/// Chữ Hán cỡ lớn theo ngữ cảnh: bọc [HanziText] (locale `zh-CN` + phông CJK dự phòng, RK-M7) với cỡ đặt tên.
/// Mọi chỗ hiển thị chữ Hán nổi bật (mặt thẻ, đầu trang từ, bảng viết) dùng widget này.
class HanziBig extends StatelessWidget {
  const HanziBig(this.text, {super.key, this.size = HanziSize.md, this.style, this.textAlign, this.maxLines});

  final String text;
  final HanziSize size;

  /// Ghi đè màu/độ đậm; cỡ và chiều cao dòng lấy từ [size] trừ khi style đặt `fontSize`.
  final TextStyle? style;
  final TextAlign? textAlign;
  final int? maxLines;

  @override
  Widget build(BuildContext context) {
    final base = TextStyle(fontSize: size.fontSize, height: 1.25, fontWeight: FontWeight.w400);
    return HanziText(
      text,
      style: style == null ? base : base.merge(style),
      textAlign: textAlign,
      maxLines: maxLines,
      overflow: maxLines == null ? null : TextOverflow.ellipsis,
    );
  }
}
