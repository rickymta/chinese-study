import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/speech/chinese_speech.dart';
import '../../../../../core/widgets/speak_button.dart';
import '../../../data/models.dart';
import '../speak_queue.dart';
import '../zh_line_row.dart';

/// Key nút "Nghe cả đoạn"/"Dừng" theo id khối (test).
Key playAllKey(String blockId) => ValueKey('play-all-$blockId');

/// Khối hội thoại (port `DialogueBlock.tsx`): tiêu đề + nút "Nghe cả đoạn" (đọc lần lượt, bấm lại để dừng) + từng dòng
/// (`ZhLineRow`, chữ 24). Dòng đang đọc được tô nền. Dừng khi rời màn (dispose). Tên người nói luân phiên hai màu theo
/// thứ tự xuất hiện.
class DialogueBlockView extends ConsumerStatefulWidget {
  const DialogueBlockView({super.key, required this.block});

  final DialogueBlock block;

  @override
  ConsumerState<DialogueBlockView> createState() => _DialogueBlockViewState();
}

class _DialogueBlockViewState extends ConsumerState<DialogueBlockView> {
  late final SpeakQueueController _queue;

  /// Giữ notifier từ lúc build — `ref.read` trong `dispose` không hợp lệ.
  SpeechController? _speech;

  @override
  void initState() {
    super.initState();
    _queue = SpeakQueueController(
      speak: (text) => speakZh(ref, text),
      // Hoãn một tick: có thể được gọi từ dispose (cây đang finalize — Riverpod cấm đổi state ngay lúc đó).
      cancel: () async {
        final speech = _speech;
        if (speech != null) scheduleMicrotask(speech.cancel);
      },
    );
    _queue.addListener(_onQueueChanged);
  }

  void _onQueueChanged() {
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _queue
      ..removeListener(_onQueueChanged)
      ..dispose();
    super.dispose();
  }

  void _togglePlayAll() {
    if (_queue.playing) {
      _queue.stop();
    } else {
      unawaited(_queue.playAll([for (final l in widget.block.lines) l.hanzi]));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final speech = ref.watch(speechControllerProvider);
    _speech = ref.read(speechControllerProvider.notifier);
    final lines = widget.block.lines;
    final canPlay = speech.canSpeak && lines.isNotEmpty;

    // Màu theo người nói: ánh xạ tên → màu theo thứ tự xuất hiện lần đầu.
    final palette = [scheme.primary, scheme.secondary];
    final colorBySpeaker = <String, Color>{};
    for (final l in lines) {
      final s = l.speaker?.trim();
      if (s != null && s.isNotEmpty && !colorBySpeaker.containsKey(s)) {
        colorBySpeaker[s] = palette[colorBySpeaker.length % palette.length];
      }
    }

    final playing = _queue.playing;
    final Widget playAll = playing
        ? FilledButton.tonalIcon(
            key: playAllKey(widget.block.id),
            onPressed: _togglePlayAll,
            icon: const Icon(Icons.stop),
            label: const Text('Dừng'),
          )
        : OutlinedButton.icon(
            key: playAllKey(widget.block.id),
            onPressed: canPlay ? _togglePlayAll : null,
            icon: const Icon(Icons.play_arrow),
            label: const Text('Nghe cả đoạn'),
          );

    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 12, 8, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      (widget.block.title?.trim().isNotEmpty ?? false) ? widget.block.title!.trim() : 'Hội thoại',
                      style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                    ),
                  ),
                  const SizedBox(width: 8),
                  if (!canPlay && !playing && !speech.canSpeak)
                    Tooltip(message: kNoVoiceTooltip, triggerMode: TooltipTriggerMode.tap, child: playAll)
                  else
                    playAll,
                ],
              ),
            ),
            const SizedBox(height: 4),
            for (var i = 0; i < lines.length; i++)
              ZhLineRow(
                line: lines[i],
                speakerColor: colorBySpeaker[lines[i].speaker?.trim()],
                active: _queue.activeIndex == i,
              ),
          ],
        ),
      ),
    );
  }
}
