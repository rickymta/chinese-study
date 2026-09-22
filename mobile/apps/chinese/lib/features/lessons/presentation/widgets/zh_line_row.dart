import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../application/providers.dart';
import '../../data/models.dart';

/// Một dòng chữ Hán dùng chung cho hội thoại và ví dụ ngữ pháp (port `ZhLineRow.tsx`): [tên người nói] · chữ Hán lớn ·
/// pinyin dấu (công tắc) · nghĩa Việt (công tắc) · ghi chú · nút nghe dòng. Cột chữ `Expanded` để câu dài xuống dòng ở
/// 360 px; [active] tô nền dòng đang được đọc trong "Nghe cả đoạn".
class ZhLineRow extends ConsumerWidget {
  const ZhLineRow({super.key, required this.line, this.speakerColor, this.hanziSize = 24, this.active = false});

  final ZhLine line;

  /// Màu tên người nói (hội thoại luân phiên hai màu để dễ theo dõi).
  final Color? speakerColor;

  /// Cỡ chữ Hán. Hội thoại 24, ví dụ ngữ pháp 22.
  final double hanziSize;
  final bool active;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final prefs = ref.watch(displayPrefsProvider);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final speaker = line.speaker?.trim();
    final note = line.note?.trim();
    return AnimatedContainer(
      duration: const Duration(milliseconds: 150),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      decoration: BoxDecoration(
        color: active ? scheme.primaryContainer.withValues(alpha: 0.6) : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                if (speaker != null && speaker.isNotEmpty)
                  Text(
                    speaker,
                    style: theme.textTheme.labelSmall?.copyWith(
                      fontWeight: FontWeight.w700,
                      color: speakerColor ?? scheme.onSurfaceVariant,
                      letterSpacing: 0.3,
                    ),
                  ),
                HanziBig(
                  line.hanzi,
                  size: HanziSize.md,
                  style: TextStyle(fontSize: hanziSize, height: 1.35),
                ),
                if (prefs.showPinyin && line.pinyin.isNotEmpty)
                  PinyinText(
                    line.pinyin,
                    hanzi: line.hanzi,
                    style: theme.textTheme.bodyMedium?.copyWith(color: scheme.primary, fontWeight: FontWeight.w500),
                  ),
                if (prefs.showVi && line.vi.isNotEmpty)
                  Text(line.vi, style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant)),
                if (note != null && note.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      note,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: scheme.onSurfaceVariant,
                        fontStyle: FontStyle.italic,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          SpeakButton(text: line.hanzi),
        ],
      ),
    );
  }
}
