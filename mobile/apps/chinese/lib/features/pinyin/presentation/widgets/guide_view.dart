import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import 'pinyin_load_error.dart';
import 'tone_contour.dart';

/// Key nút "Sang bảng âm tiết" (test).
const kGoToChartKey = ValueKey('guide-go-to-chart');

/// Tab Hướng dẫn (port `GuideView.tsx`): chủ đề dạng `ExpansionTile` (mở sẵn chủ đề đầu), block theo `guide.json`
/// (`paragraph`/`tip`/`tone_contour`/`examples`/`compare`). Cuối tab: nút "Sang bảng âm tiết".
class GuideView extends ConsumerWidget {
  const GuideView({super.key, required this.onGoToChart});

  final VoidCallback onGoToChart;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final guide = ref.watch(pinyinGuideProvider);
    return AfPageBody(
      child: AsyncValueView<PinyinGuide>(
        value: guide,
        loading: const PinyinSkeleton(),
        error: (e) => PinyinLoadError(error: e, onRetry: () => ref.invalidate(pinyinGuideProvider)),
        data: (g) => Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            for (var i = 0; i < g.topics.length; i++) _TopicTile(index: i, topic: g.topics[i]),
            const SizedBox(height: 12),
            Align(
              alignment: Alignment.centerRight,
              child: FilledButton.icon(
                key: kGoToChartKey,
                onPressed: onGoToChart,
                icon: const Icon(Icons.arrow_forward),
                label: const Text('Sang bảng âm tiết'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TopicTile extends StatelessWidget {
  const _TopicTile({required this.index, required this.topic});

  final int index;
  final GuideTopic topic;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // Chủ đề biến điệu: hiện gợi ý dưới pinyin để người học thấy đúng cái đang được giảng.
    final showSandhi = topic.id == 'bien-dieu';
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        key: PageStorageKey('guide-${topic.id}'),
        initiallyExpanded: index == 0,
        shape: const Border(),
        collapsedShape: const Border(),
        title: Text('${index + 1}. ${topic.title}', style: theme.textTheme.titleSmall),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        expandedCrossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (var i = 0; i < topic.blocks.length; i++) ...[
            if (i > 0) const SizedBox(height: 16),
            _Block(block: topic.blocks[i], showSandhi: showSandhi),
          ],
        ],
      ),
    );
  }
}

class _Block extends StatelessWidget {
  const _Block({required this.block, required this.showSandhi});

  final GuideBlock block;
  final bool showSandhi;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return switch (block) {
      GuideParagraph(:final text) => Text(text, style: theme.textTheme.bodyMedium?.copyWith(height: 1.6)),
      GuideTip(:final text) => _TipBox(text: text),
      GuideToneContour(:final tones) => ToneContour(tones: tones),
      GuideExamples(:final items) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (var i = 0; i < items.length; i++) ...[
            if (i > 0) const Divider(height: 1),
            ExampleRow(example: items[i], showSandhi: showSandhi),
          ],
        ],
      ),
      GuideCompare(:final title, :final pairs) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(title, style: theme.textTheme.titleSmall),
          const SizedBox(height: 8),
          for (final pair in pairs) _ComparePair(pair: pair),
        ],
      ),
    };
  }
}

/// Một dòng ví dụ: chữ Hán lớn · pinyin dấu (kèm gợi ý biến điệu) · nghĩa · nút nghe.
class ExampleRow extends StatelessWidget {
  const ExampleRow({super.key, required this.example, this.showSandhi = false});

  final GuideExample example;
  final bool showSandhi;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          ConstrainedBox(
            constraints: const BoxConstraints(minWidth: 48),
            child: HanziBig(example.hanzi, size: HanziSize.lg),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                PinyinText(
                  example.pinyin,
                  hanzi: example.hanzi,
                  showSandhi: showSandhi,
                  style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
                ),
                Text(
                  example.meaningVi,
                  style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                ),
              ],
            ),
          ),
          SpeakButton(text: example.hanzi),
        ],
      ),
    );
  }
}

class _ComparePair extends StatelessWidget {
  const _ComparePair({required this.pair});

  final GuideComparePair pair;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: Border.all(color: theme.colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: _CompareSide(example: pair.left)),
              const SizedBox(width: 8),
              Expanded(child: _CompareSide(example: pair.right)),
            ],
          ),
          if (pair.noteVi != null && pair.noteVi!.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(pair.noteVi!, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
          ],
        ],
      ),
    );
  }
}

/// Một bên của cặp so sánh — ở 360 px mỗi bên ~150 px: chữ + (pinyin/nghĩa) + loa; nghĩa cắt một dòng như web.
class _CompareSide extends StatelessWidget {
  const _CompareSide({required this.example});

  final GuideExample example;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        HanziBig(example.hanzi, size: HanziSize.lg),
        const SizedBox(width: 6),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              PinyinText(example.pinyin, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
              Text(
                example.meaningVi,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ],
          ),
        ),
        SpeakButton(text: example.hanzi, iconSize: 20),
      ],
    );
  }
}

/// Khối mẹo (thay `Alert severity="info"`).
class _TipBox extends StatelessWidget {
  const _TipBox({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.secondaryContainer,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.lightbulb_outline, color: scheme.onSecondaryContainer, size: 20),
            const SizedBox(width: 8),
            Expanded(
              child: Text(text, style: TextStyle(color: scheme.onSecondaryContainer, height: 1.5)),
            ),
          ],
        ),
      ),
    );
  }
}
