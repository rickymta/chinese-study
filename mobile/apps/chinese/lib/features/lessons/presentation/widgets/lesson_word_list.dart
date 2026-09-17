import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/meaning_status_chip.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../../../router/routes.dart';
import '../../data/models.dart';

/// Key dòng từ theo id (test).
Key lessonWordKey(String id) => ValueKey('lesson-word-$id');

/// Danh sách từ của bài (tab Từ vựng, port `LessonWordList.tsx`): chữ Hán 30 · pinyin dấu + Hán Việt in hoa · ≤ 2 nghĩa
/// một dòng (chip "Chưa duyệt" khi `machine`) · chip "Đang ôn" khi đã có thẻ SRS · nút nghe. Dòng ≥ 64 px. Chạm ⇒
/// `push` `/tu-dien/:id` (M8): go_router 18 đặt trang từ điển LÊN TRÊN trang bài trong cùng nhánh "Bài học" (đã kiểm
/// bằng widget test) ⇒ quay lại là về bài, đúng tab và trạng thái quiz đang làm.
class LessonWordList extends StatelessWidget {
  const LessonWordList({super.key, required this.words});

  final List<LessonWord> words;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    if (words.isEmpty) {
      return Text('Bài này chưa có từ vựng.', style: TextStyle(color: scheme.onSurfaceVariant));
    }
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var i = 0; i < words.length; i++) ...[if (i > 0) const Divider(height: 1), _WordRow(word: words[i])],
        ],
      ),
    );
  }
}

class _WordRow extends StatelessWidget {
  const _WordRow({required this.word});

  final LessonWord word;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = scheme.onSurfaceVariant;
    final meanings = word.meaningsVi.take(2).join('; ');
    final hanViet = word.hanViet?.trim();
    return InkWell(
      key: lessonWordKey(word.id),
      onTap: () => context.push(AppRoutes.word(word.id)),
      child: Container(
        constraints: const BoxConstraints(minHeight: 64),
        padding: const EdgeInsets.fromLTRB(12, 6, 4, 6),
        child: Row(
          children: [
            ConstrainedBox(
              constraints: const BoxConstraints(minWidth: 44),
              child: HanziBig(
                word.simplified,
                size: HanziSize.md,
                style: const TextStyle(fontSize: 30),
                textAlign: TextAlign.center,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Wrap(
                    spacing: 8,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(
                        numberedToMarked(word.pinyin),
                        style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
                      ),
                      if (hanViet != null && hanViet.isNotEmpty)
                        Text(
                          hanViet.toUpperCase(),
                          style: theme.textTheme.labelSmall?.copyWith(color: muted, letterSpacing: 0.5),
                        ),
                    ],
                  ),
                  Text(
                    meanings.isEmpty ? '—' : meanings,
                    style: theme.textTheme.bodyMedium?.copyWith(color: muted),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  // Chip xuống dòng riêng (Wrap) — ở chữ 1,3× hai chip rộng hơn phần còn lại của dòng 360 px.
                  if (word.meaningViStatus == 'machine' || word.inSrs)
                    Wrap(
                      spacing: 6,
                      runSpacing: 2,
                      children: [
                        if (word.meaningViStatus == 'machine')
                          MeaningStatusChip(status: word.meaningViStatus, triggerMode: TooltipTriggerMode.longPress),
                        if (word.inSrs)
                          Chip(
                            label: const Text('Đang ôn'),
                            visualDensity: VisualDensity.compact,
                            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            labelStyle: TextStyle(color: scheme.primary, fontSize: 12),
                            side: BorderSide(color: scheme.primary),
                            backgroundColor: Colors.transparent,
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                          ),
                      ],
                    ),
                ],
              ),
            ),
            SpeakButton(text: word.simplified),
          ],
        ),
      ),
    );
  }
}
