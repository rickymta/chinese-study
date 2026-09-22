import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import '../widgets/dictionary_error_view.dart';
import '../widgets/source_attribution.dart';
import '../widgets/word_list_tile.dart';
import 'dictionary_search_page.dart';

/// `/tu-dien/chu/:hanzi` (hợp đồng M8): chữ 96 + nghe, cách đọc (dạng dấu), Hán Việt (âm đầu đậm, in hoa), số nét, bộ thủ
/// "爪 (bộ số 87)", phồn thể, "Từ có chữ này" (≤ 20, chạm ⇒ `/tu-dien/:id`). Nút "Luyện viết" chỉ hiện sau M10.
/// [hanzi] đã được go_router giải mã URL. 404/400 ⇒ trang lỗi chung / khối lỗi tại chỗ.
class CharacterDetailPage extends ConsumerWidget {
  const CharacterDetailPage({super.key, required this.hanzi});

  final String hanzi;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final value = ref.watch(characterDetailProvider(hanzi));
    return Scaffold(
      appBar: AppBar(leading: const DictionaryBackButton(), title: const Text('Chi tiết chữ')),
      body: AfPageBody(
        // Danh sách từ dùng dòng viền sát mép ⇒ bỏ đệm ngang, từng khối tự đệm.
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: AsyncValueView<CharacterDetail>(
          value: value,
          onRetry: () => ref.invalidate(characterDetailProvider(hanzi)),
          error: (e) => DictionaryErrorView(error: e, onRetry: () => ref.invalidate(characterDetailProvider(hanzi))),
          data: (ch) => _CharacterBody(ch: ch),
        ),
      ),
    );
  }
}

class _CharacterBody extends StatelessWidget {
  const _CharacterBody({required this.ch});

  final CharacterDetail ch;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    final traditional = ch.traditionalShown;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Flexible(
                    child: HanziBig(ch.hanzi, size: HanziSize.xxl, style: const TextStyle(fontSize: 96)),
                  ),
                  const SizedBox(width: 12),
                  SpeakButton(text: ch.hanzi, iconSize: 36),
                ],
              ),
              const SizedBox(height: 12),
              _InfoRow(
                label: 'Cách đọc',
                child: ch.pinyinReadings.isEmpty
                    ? Text('—', style: muted)
                    : Text(ch.pinyinReadings.map(numberedToMarked).join(' · '), style: theme.textTheme.bodyLarge),
              ),
              _InfoRow(
                label: 'Hán Việt',
                child: ch.hanViet.isEmpty
                    ? Text('—', style: muted)
                    : Text.rich(
                        TextSpan(
                          style: theme.textTheme.bodyLarge,
                          children: [
                            for (var i = 0; i < ch.hanViet.length; i++) ...[
                              if (i > 0) const TextSpan(text: ' · '),
                              TextSpan(
                                text: ch.hanViet[i].toUpperCase(),
                                style: TextStyle(fontWeight: i == 0 ? FontWeight.w700 : FontWeight.w400),
                              ),
                            ],
                          ],
                        ),
                      ),
              ),
              if (ch.strokeCount != null)
                _InfoRow(
                  label: 'Số nét',
                  child: Text('${ch.strokeCount}', style: theme.textTheme.bodyLarge),
                ),
              if (ch.radical != null && ch.radical!.isNotEmpty)
                _InfoRow(
                  label: 'Bộ thủ',
                  child: Wrap(
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      HanziText(ch.radical!, style: const TextStyle(fontSize: 20)),
                      if (ch.radicalNumber != null)
                        Text(' (bộ số ${ch.radicalNumber})', style: theme.textTheme.bodyLarge),
                    ],
                  ),
                ),
              if (traditional.isNotEmpty)
                _InfoRow(
                  label: 'Phồn thể',
                  child: LangText(
                    traditional.join('、'),
                    locale: const Locale('zh', 'TW'),
                    fontFamilyFallback: kCjkFontFallback,
                    style: const TextStyle(fontSize: 20),
                  ),
                ),
              const SizedBox(height: 16),
              Text('Từ có chữ này', style: theme.textTheme.titleMedium),
              if (ch.words.isEmpty) ...[
                const SizedBox(height: 4),
                Text('Chưa có từ nào trong kho chứa chữ này.', style: muted),
              ],
            ],
          ),
        ),
        if (ch.words.isNotEmpty) ...[
          const SizedBox(height: 4),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 4),
            child: Column(children: [for (final w in ch.words) WordListTile(word: w)]),
          ),
        ],
        const Padding(padding: EdgeInsets.fromLTRB(16, 16, 16, 0), child: SourceAttribution()),
      ],
    );
  }
}

/// Một dòng "Nhãn: giá trị" trong bảng thông tin chữ (nhãn rộng cố định 88 như web).
class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 88,
            child: Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Text(
                label,
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ),
          ),
          Expanded(child: child),
        ],
      ),
    );
  }
}
