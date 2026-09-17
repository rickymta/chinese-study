import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/pinyin/pinyin.dart';
import '../../../../../core/widgets/hanzi_big.dart';
import '../../../../../core/widgets/pinyin_text.dart';
import '../../../application/providers.dart';
import '../../../data/models.dart';

/// Nội dung một lựa chọn theo `lang` (port `OptionText`): `zh` ⇒ chữ Hán (`HanziText`), `pinyin` ⇒ dạng dấu đậm, `vi` ⇒
/// chữ thường. Dùng ở nút chọn (lúc làm) và ở bảng kết quả. [color] ép màu chữ (nút đã chọn ⇒ `onPrimary`).
class OptionText extends StatelessWidget {
  const OptionText({super.key, required this.option, this.small = false, this.color});

  final QuizOption option;
  final bool small;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final base = (small ? theme.textTheme.bodyMedium : theme.textTheme.bodyLarge)?.copyWith(color: color);
    return switch (option.lang) {
      'zh' => HanziBig(
        option.text,
        size: HanziSize.md,
        style: TextStyle(fontSize: small ? 20 : 26, color: color),
      ),
      'pinyin' => Text(numberedToMarked(option.text), style: base?.copyWith(fontWeight: FontWeight.w600)),
      _ => Text(option.text, style: base),
    };
  }
}

/// Đề bài (port `QuestionPrompt`): `promptLang = 'zh'` ⇒ chữ Hán lớn + pinyin dấu (theo công tắc Pinyin); `vi` ⇒ chữ
/// thường. Câu nghe (`listen_choice`) không hiện `audioText` ở đây (người học phải nghe).
class QuestionPrompt extends ConsumerWidget {
  const QuestionPrompt({super.key, required this.question, this.compact = false});

  final QuizQuestion question;
  final bool compact;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    if (question.promptLang == 'zh') {
      final showPinyin = ref.watch(displayPrefsProvider.select((p) => p.showPinyin));
      final pinyin = question.promptPinyin;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          HanziBig(
            question.prompt,
            size: HanziSize.lg,
            style: TextStyle(fontSize: compact ? 24 : 30, height: 1.4),
          ),
          if (showPinyin && pinyin != null && pinyin.isNotEmpty)
            PinyinText(
              pinyin,
              hanzi: question.prompt,
              style: (compact ? theme.textTheme.bodyMedium : theme.textTheme.bodyLarge)?.copyWith(
                color: scheme.primary,
                fontWeight: FontWeight.w500,
              ),
            ),
        ],
      );
    }
    return Text(
      question.prompt,
      style: (compact ? theme.textTheme.bodyLarge : theme.textTheme.titleMedium)?.copyWith(
        fontWeight: compact ? FontWeight.w500 : FontWeight.w600,
        height: 1.4,
      ),
    );
  }
}
