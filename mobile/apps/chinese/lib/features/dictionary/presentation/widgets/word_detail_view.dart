import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/meaning_status_chip.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../../../router/routes.dart';
import '../../../srs/presentation/widgets/add_to_srs_button.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import '../../domain/character_reading.dart';
import '../../domain/pos.dart';
import '../../domain/source_labels.dart';

/// Thân chi tiết từ (port `WordDetailBody.tsx`, hợp đồng M8 — M6 tạo để tái dùng ở sheet "Xem chi tiết" của phiên
/// ôn): chữ 56 + nghe, pinyin 20, Hán Việt (+ chip "Hán Việt suy ra"), phồn thể/dạng khác/ví dụ dùng, chip HSK + từ
/// loại, nghĩa Việt đánh số + nguồn + `MeaningStatusChip`, nghĩa Anh (`ExpansionTile` đóng), lưới "Chữ trong từ",
/// [AddToSrsButton], dòng nguồn (chạm ⇒ `/giay-phep`). Ô chữ chưa dẫn tới `/tu-dien/chu/:hanzi` — route đó thuộc M8.
class WordDetailView extends StatelessWidget {
  const WordDetailView({super.key, required this.word});

  final WordDetail word;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    final pos = posLabels(word.pos);
    final sourceNames = [
      for (final s in word.sources)
        if (sourceLabel(s) case final info?) '${info.label} (${info.license})',
    ];
    final viSource = meaningViSourceLabel(word.meaningViSource);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // ── Khối đầu: chữ + loa, pinyin, Hán Việt, phồn thể/dạng khác/ví dụ ──
        Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Flexible(child: HanziBig(word.simplified, size: HanziSize.xl)),
            const SizedBox(width: 8),
            SpeakButton(text: word.simplified, iconSize: 32),
          ],
        ),
        PinyinText(word.pinyin, style: theme.textTheme.titleLarge?.copyWith(fontSize: 20, fontWeight: FontWeight.w500)),
        if (word.hanViet != null && word.hanViet!.isNotEmpty) ...[
          const SizedBox(height: 4),
          Wrap(
            spacing: 8,
            runSpacing: 4,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text('HÁN VIỆT: ${word.hanViet!.toUpperCase()}', style: muted?.copyWith(letterSpacing: 0.5)),
              if (word.hanVietStatus == 'derived')
                const Chip(
                  label: Text('Hán Việt suy ra'),
                  visualDensity: VisualDensity.compact,
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
            ],
          ),
        ],
        if (word.showTraditional) ...[
          const SizedBox(height: 6),
          _LabeledHanzi(label: 'Phồn thể: ', text: word.traditional!, traditional: true),
        ],
        if (word.variants.isNotEmpty) ...[
          const SizedBox(height: 4),
          _LabeledHanzi(label: 'Dạng khác: ', text: word.variants.join('、')),
        ],
        if (word.usageNote != null && word.usageNote!.isNotEmpty) ...[
          const SizedBox(height: 4),
          _LabeledHanzi(label: 'Ví dụ dùng: ', text: word.usageNote!),
        ],

        // ── Chip cấp HSK + từ loại ──
        if (word.hsk3Level != null || pos.isNotEmpty) ...[
          const SizedBox(height: 12),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              if (word.hsk3Level != null)
                Chip(
                  label: Text('HSK 3.0 cấp ${word.hsk3Level}'),
                  visualDensity: VisualDensity.compact,
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
              for (final p in pos)
                Chip(
                  label: Text(p),
                  visualDensity: VisualDensity.compact,
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
            ],
          ),
        ],

        // ── Nghĩa tiếng Việt ──
        const SizedBox(height: 16),
        Wrap(
          spacing: 8,
          runSpacing: 4,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            Text('Nghĩa tiếng Việt', style: theme.textTheme.titleMedium),
            MeaningStatusChip(status: word.meaningViStatus),
            if (viSource != null) Text(viSource, style: theme.textTheme.bodySmall?.copyWith(color: muted?.color)),
          ],
        ),
        const SizedBox(height: 4),
        if (word.meaningsVi.isEmpty)
          Text('Chưa có nghĩa tiếng Việt — xem nghĩa tiếng Anh bên dưới.', style: muted)
        else
          for (var i = 0; i < word.meaningsVi.length; i++)
            Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Text('${i + 1}. ${word.meaningsVi[i]}', style: theme.textTheme.bodyLarge),
            ),

        // ── Nghĩa tiếng Anh (mặc định đóng) ──
        const SizedBox(height: 8),
        Card(
          clipBehavior: Clip.antiAlias,
          child: ExpansionTile(
            title: Text('Nghĩa tiếng Anh (CC-CEDICT)', style: theme.textTheme.titleSmall),
            shape: const Border(),
            childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
            expandedCrossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (word.meaningsEn.isEmpty)
                Text('Không có.', style: muted)
              else
                for (var i = 0; i < word.meaningsEn.length; i++)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: LangText('${i + 1}. ${word.meaningsEn[i]}', locale: const Locale('en')),
                  ),
            ],
          ),
        ),

        // ── Chữ trong từ ──
        if (word.characters.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text('Chữ trong từ', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (var i = 0; i < word.characters.length; i++)
                _CharacterTile(
                  character: word.characters[i],
                  reading: readingAt(word.pinyin, i, word.characters[i].pinyinReadings),
                ),
            ],
          ),
        ],

        // ── SRS ──
        const SizedBox(height: 16),
        AddToSrsButton(word: word),

        // ── Nguồn ──
        if (sourceNames.isNotEmpty) ...[
          const SizedBox(height: 16),
          InkWell(
            onTap: () => context.push(AppRoutes.licenses),
            borderRadius: BorderRadius.circular(8),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Text(
                'Nguồn: ${sourceNames.join(' · ')} — xem Giấy phép & nguồn',
                style: theme.textTheme.bodySmall?.copyWith(color: muted?.color),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

/// "Nhãn: 汉字" — nhãn Việt + chữ Hán (phồn thể đặt locale `zh-TW` qua `LangText`).
class _LabeledHanzi extends StatelessWidget {
  const _LabeledHanzi({required this.label, required this.text, this.traditional = false});

  final String label;
  final String text;
  final bool traditional;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    return Wrap(
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        Text(label, style: muted),
        if (traditional)
          LangText(
            text,
            locale: const Locale('zh', 'TW'),
            fontFamilyFallback: kCjkFontFallback,
            style: const TextStyle(fontSize: 18),
          )
        else
          HanziText(text, style: const TextStyle(fontSize: 18)),
      ],
    );
  }
}

/// Ô 72×88: chữ 32, Hán Việt in hoa, cách đọc dạng dấu. M8 nối chạm ⇒ `/tu-dien/chu/:hanzi`.
class _CharacterTile extends StatelessWidget {
  const _CharacterTile({required this.character, required this.reading});

  final WordCharacter character;
  final String? reading;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hanViet = character.hanViet.isEmpty ? null : character.hanViet.first;
    return Card(
      child: SizedBox(
        width: 72,
        height: 88,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              HanziBig(character.hanzi, size: HanziSize.lg, style: const TextStyle(fontSize: 32)),
              if (hanViet != null)
                Text(
                  hanViet.toUpperCase(),
                  style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              if (reading != null)
                Text(
                  numberedToMarked(reading!),
                  style: theme.textTheme.labelSmall,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Sheet "Xem chi tiết" trong phiên ôn (chỉ đọc ⇒ `closeOnBarrier: true` để quay lại thẻ nhanh). Tải
/// `/dictionary/words/{id}`; lỗi ⇒ `ErrorView` + Thử lại.
Future<void> showWordDetailSheet(BuildContext context, String wordId) {
  return showAfBottomSheet<void>(
    context: context,
    // Chỉ đọc (xem nghĩa/chữ), không có dữ liệu nhập ⇒ chạm ngoài/kéo xuống đóng nhanh để quay lại thẻ.
    closeOnBarrier: true,
    builder: (ctx) => _WordDetailSheet(wordId: wordId),
  );
}

class _WordDetailSheet extends ConsumerWidget {
  const _WordDetailSheet({required this.wordId});

  final String wordId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final value = ref.watch(wordDetailProvider(wordId));
    final maxHeight = MediaQuery.sizeOf(context).height * 0.85;
    return ConstrainedBox(
      constraints: BoxConstraints(maxHeight: maxHeight),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 4, 0),
            child: Row(
              children: [
                Expanded(child: Text('Chi tiết từ', style: Theme.of(context).textTheme.titleMedium)),
                IconButton(
                  tooltip: 'Đóng',
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.close),
                  constraints: const BoxConstraints(minWidth: 48, minHeight: 48),
                ),
              ],
            ),
          ),
          Flexible(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
              child: AsyncValueView<WordDetail>(
                value: value,
                onRetry: () => ref.invalidate(wordDetailProvider(wordId)),
                data: (word) => WordDetailView(word: word),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
