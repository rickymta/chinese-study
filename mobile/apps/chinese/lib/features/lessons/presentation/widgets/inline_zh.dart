import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/speech/chinese_speech.dart';
import '../../application/providers.dart';
import '../../domain/inline_zh.dart';

/// Key token chữ Hán nội dòng theo chữ (test).
Key zhTokenKey(String hanzi) => ValueKey('zh-token-$hanzi');

/// Văn bản có token chữ Hán nội dòng (§5.4.3, port `InlineZh.tsx`): chữ Hán dựng dạng **ruby** — pinyin DẠNG DẤU cỡ
/// nhỏ phía TRÊN chữ (tắt bằng công tắc Pinyin ⇒ chỉ chữ), tự dựng bằng `WidgetSpan` + `Column` (BA-mặc định, khớp
/// chốt tích hợp F9). Chạm vào chữ ⇒ đọc (trong handler thao tác người dùng — iOS). Token hỏng giữ nguyên văn.
class InlineZhText extends ConsumerWidget {
  const InlineZhText(this.text, {super.key, this.style, this.textAlign});

  /// Văn bản có thể chứa `[[chữ Hán|pinyin số thanh]]`.
  final String text;
  final TextStyle? style;
  final TextAlign? textAlign;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final showPinyin = ref.watch(displayPrefsProvider.select((p) => p.showPinyin));
    final theme = Theme.of(context);
    final base = (style ?? theme.textTheme.bodyMedium ?? const TextStyle()).copyWith(height: showPinyin ? 1.7 : 1.5);
    final segments = parseInlineZh(text);
    return Text.rich(
      TextSpan(
        style: base,
        children: [
          for (final s in segments)
            switch (s) {
              InlineText() => TextSpan(text: s.text),
              InlineZh() => WidgetSpan(
                alignment: PlaceholderAlignment.bottom,
                child: _ZhToken(hanzi: s.hanzi, pinyin: s.pinyin, showPinyin: showPinyin, base: base),
              ),
            },
        ],
      ),
      textAlign: textAlign,
    );
  }
}

/// Một token: cột [pinyin dấu nhỏ] + [chữ Hán 1,2× cỡ chữ quanh]; chạm ⇒ đọc; không giọng ⇒ chỉ hiển thị.
class _ZhToken extends ConsumerWidget {
  const _ZhToken({required this.hanzi, required this.pinyin, required this.showPinyin, required this.base});

  final String hanzi;
  final String pinyin;
  final bool showPinyin;
  final TextStyle base;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canSpeak = ref.watch(speechControllerProvider.select((s) => s.canSpeak));
    final scheme = Theme.of(context).colorScheme;
    final fontSize = (base.fontSize ?? 14) * 1.2;
    final marked = numberedToMarked(pinyin);
    final column = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        if (showPinyin)
          Text(
            marked,
            style: base.copyWith(
              fontSize: (base.fontSize ?? 14) * 0.7,
              height: 1.1,
              color: scheme.onSurfaceVariant,
              letterSpacing: 0.2,
              fontWeight: FontWeight.w400,
            ),
            textAlign: TextAlign.center,
          ),
        HanziText(hanzi, style: base.copyWith(fontSize: fontSize, height: 1.15)),
      ],
    );
    return Semantics(
      label: canSpeak ? 'Nghe $hanzi' : null,
      button: canSpeak,
      child: InkWell(
        key: zhTokenKey(hanzi),
        onTap: canSpeak ? () => unawaited(speakZh(ref, hanzi)) : null,
        borderRadius: BorderRadius.circular(4),
        child: Padding(padding: const EdgeInsets.symmetric(horizontal: 2), child: column),
      ),
    );
  }
}
