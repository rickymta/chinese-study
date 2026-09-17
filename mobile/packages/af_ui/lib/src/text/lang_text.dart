import 'package:flutter/material.dart';

/// Phông dự phòng CJK (không đóng gói phông — DB-M19): iOS/macOS có PingFang SC, Android có Noto Sans CJK SC,
/// Windows có Microsoft YaHei. Đặt cùng `locale zh-CN` để tránh Han unification ra glyph kiểu Nhật/phồn thể (RK-M7).
const kCjkFontFallback = <String>[
  'PingFang SC',
  'Noto Sans CJK SC',
  'Noto Sans SC',
  'Source Han Sans SC',
  'Microsoft YaHei',
];

/// Locale chữ Hán giản thể.
const kZhCn = Locale('zh', 'CN');

/// Văn bản có locale riêng (tương đương `LangText` của `@af/ui` web): đặt `locale` vào [TextStyle] để engine chọn
/// đúng glyph/phông theo ngôn ngữ.
class LangText extends StatelessWidget {
  const LangText(
    this.text, {
    super.key,
    required this.locale,
    this.style,
    this.fontFamilyFallback,
    this.textAlign,
    this.maxLines,
    this.overflow,
    this.softWrap,
    this.textScaler,
    this.semanticsLabel,
  });

  final String text;
  final Locale locale;
  final TextStyle? style;
  final List<String>? fontFamilyFallback;
  final TextAlign? textAlign;
  final int? maxLines;
  final TextOverflow? overflow;
  final bool? softWrap;
  final TextScaler? textScaler;
  final String? semanticsLabel;

  @override
  Widget build(BuildContext context) {
    final base = style ?? DefaultTextStyle.of(context).style;
    final merged = base.copyWith(locale: locale, fontFamilyFallback: fontFamilyFallback ?? base.fontFamilyFallback);
    return Text(
      text,
      locale: locale,
      style: merged,
      textAlign: textAlign,
      maxLines: maxLines,
      overflow: overflow,
      softWrap: softWrap,
      textScaler: textScaler,
      semanticsLabel: semanticsLabel,
    );
  }
}

/// Chữ Hán giản thể: `LangText` với locale `zh-CN` + phông dự phòng CJK. MỌI chữ Hán trong app phải đi qua đây
/// (luật `cjk-without-hanzitext` của `tool/check_conventions.dart`).
class HanziText extends StatelessWidget {
  const HanziText(
    this.text, {
    super.key,
    this.style,
    this.textAlign,
    this.maxLines,
    this.overflow,
    this.softWrap,
    this.semanticsLabel,
  });

  final String text;
  final TextStyle? style;
  final TextAlign? textAlign;
  final int? maxLines;
  final TextOverflow? overflow;
  final bool? softWrap;
  final String? semanticsLabel;

  @override
  Widget build(BuildContext context) {
    return LangText(
      text,
      locale: kZhCn,
      style: style,
      fontFamilyFallback: kCjkFontFallback,
      textAlign: textAlign,
      maxLines: maxLines,
      overflow: overflow,
      softWrap: softWrap,
      semanticsLabel: semanticsLabel,
    );
  }
}

/// `TextStyle` chữ Hán để dùng trong `TextSpan`/`RichText` (khi không tiện dùng widget [HanziText]).
TextStyle hanziStyle([TextStyle? base]) =>
    (base ?? const TextStyle()).copyWith(locale: kZhCn, fontFamilyFallback: kCjkFontFallback);
