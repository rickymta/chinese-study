import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/meaning_status_chip.dart';
import '../../../../router/routes.dart';
import '../../data/models.dart';

/// Key của dòng kết quả theo id từ (test).
Key wordTileKey(String id) => ValueKey('word-$id');

/// Một dòng kết quả tra từ (port `WordListItem.tsx`, hợp đồng M8): trái chữ Hán 28 · giữa pinyin dấu đậm + Hán Việt in
/// hoa nhỏ, dưới là ≤ 2 nghĩa Việt cắt một dòng · phải chip "Chưa duyệt" (tooltip khi giữ lâu — chạm vẫn mở từ). Cao
/// ≥ 64, cột giữa `Expanded` để không tràn ngang ở 360 px. Chạm ⇒ `push` `/tu-dien/:id` (giữ trang tìm trong ngăn
/// xếp ⇒ quay lại giữ kết quả + vị trí cuộn).
class WordListTile extends StatelessWidget {
  const WordListTile({super.key, required this.word});

  final WordSummary word;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.colorScheme.onSurfaceVariant;
    final meanings = word.meaningsVi.take(2).join('; ');
    final hanViet = word.hanViet?.trim();
    return InkWell(
      key: wordTileKey(word.id),
      onTap: () => context.push(AppRoutes.word(word.id)),
      child: Container(
        constraints: const BoxConstraints(minHeight: 64),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          border: Border(bottom: BorderSide(color: theme.colorScheme.outlineVariant)),
        ),
        child: Row(
          children: [
            ConstrainedBox(
              constraints: const BoxConstraints(minWidth: 40),
              child: HanziBig(
                word.simplified,
                size: HanziSize.md,
                style: const TextStyle(fontSize: 28),
                textAlign: TextAlign.center,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.baseline,
                    textBaseline: TextBaseline.alphabetic,
                    children: [
                      Flexible(
                        child: Text(
                          numberedToMarked(word.pinyin),
                          style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (hanViet != null && hanViet.isNotEmpty) ...[
                        const SizedBox(width: 8),
                        Flexible(
                          child: Text(
                            hanViet.toUpperCase(),
                            style: theme.textTheme.labelSmall?.copyWith(color: muted, letterSpacing: 0.5),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ],
                  ),
                  Text(
                    meanings.isEmpty ? '—' : meanings,
                    style: theme.textTheme.bodyMedium?.copyWith(color: muted),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
            if (word.meaningViStatus == 'machine') ...[
              const SizedBox(width: 8),
              MeaningStatusChip(status: word.meaningViStatus, triggerMode: TooltipTriggerMode.longPress),
            ],
          ],
        ),
      ),
    );
  }
}
