import 'package:flutter/material.dart';

import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/meaning_status_chip.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../data/models.dart';

/// Key mặt sau (test: lật ⇒ hiện).
const kFlashcardBackKey = ValueKey('flashcard-back');

/// Thẻ ôn (port `Flashcard.tsx`): mặt trước = chữ Hán 72–96 + loa + chip trạng thái; lật ⇒ mặt sau hiện DƯỚI mặt
/// trước (pinyin dấu 24, Hán Việt in hoa, ≤ 3 nghĩa Việt, chip "Chưa duyệt", "Xem chi tiết"). Không có nút lật ở
/// đây — nút nằm ở thanh đáy để ngón cái không phải di chuyển; chạm vào thẻ cũng lật [BA-mặc định].
class Flashcard extends StatelessWidget {
  const Flashcard({
    super.key,
    required this.card,
    required this.flipped,
    required this.onFlip,
    required this.onOpenDetail,
  });

  final SrsQueueCard card;
  final bool flipped;
  final VoidCallback onFlip;

  /// Mở chi tiết từ (bottom sheet) — không rời phiên.
  final ValueChanged<String> onOpenDetail;

  static const _stateChip = <SrsCardState, String>{
    SrsCardState.fresh: 'Mới',
    SrsCardState.learning: 'Đang học',
    SrsCardState.relearning: 'Học lại',
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final word = card.word;
    final chip = _stateChip[card.state];
    final meanings = word.meaningsVi.take(3).toList();
    // Chữ dài (4+ chữ) hạ cỡ để không tràn 360 px ở chữ 1,3×.
    final size = word.simplified.length >= 4 ? HanziSize.xl : HanziSize.xxl;

    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: flipped ? null : onFlip,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (chip != null)
            Chip(
              label: Text(chip),
              visualDensity: VisualDensity.compact,
              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
              side: BorderSide(color: card.state == SrsCardState.relearning ? scheme.tertiary : scheme.primary),
              labelStyle: TextStyle(color: card.state == SrsCardState.relearning ? scheme.tertiary : scheme.primary),
            ),
          const SizedBox(height: 8),
          // ── Mặt trước ──
          Wrap(
            alignment: WrapAlignment.center,
            crossAxisAlignment: WrapCrossAlignment.center,
            spacing: 8,
            children: [
              HanziBig(word.simplified, size: size, textAlign: TextAlign.center),
              SpeakButton(text: word.simplified, iconSize: 32),
            ],
          ),
          // ── Mặt sau ──
          if (flipped) ...[
            const SizedBox(height: 12),
            SizedBox(width: 200, child: Divider(color: scheme.outlineVariant)),
            const SizedBox(height: 8),
            KeyedSubtree(
              key: kFlashcardBackKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  PinyinText(
                    word.pinyin,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w500),
                  ),
                  if (word.hanViet != null && word.hanViet!.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(
                      word.hanViet!.toUpperCase(),
                      textAlign: TextAlign.center,
                      style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant, letterSpacing: 0.5),
                    ),
                  ],
                  const SizedBox(height: 8),
                  if (meanings.isEmpty)
                    Text(
                      'Chưa có nghĩa tiếng Việt — bấm "Xem chi tiết" để xem nghĩa tiếng Anh.',
                      textAlign: TextAlign.center,
                      style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                    )
                  else
                    for (var i = 0; i < meanings.length; i++)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Text(
                          '${i + 1}. ${meanings[i]}',
                          textAlign: TextAlign.center,
                          style: const TextStyle(fontSize: 18),
                        ),
                      ),
                  MeaningStatusChip(status: word.meaningViStatus),
                  TextButton.icon(
                    onPressed: () => onOpenDetail(word.id),
                    icon: const Icon(Icons.open_in_new, size: 18),
                    label: const Text('Xem chi tiết'),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
