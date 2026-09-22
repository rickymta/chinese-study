import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../../../core/speech/chinese_speech.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../data/models.dart';

/// Ký hiệu thanh cạnh số (giống web `TONE_LABEL`/`TONE_GLYPH`).
const kToneGlyph = <int, String>{1: 'ˉ', 2: 'ˊ', 3: 'ˇ', 4: 'ˋ'};

/// Khoảng nghỉ giữa hai thanh khi "Nghe lần lượt".
const kSequenceGap = Duration(milliseconds: 600);

/// Key nút "Nghe lần lượt"/"Dừng" (test).
const kPlayAllKey = ValueKey('syllable-play-all');

/// Mở sheet chi tiết một âm tiết (port `SyllableDrawer.tsx`): thanh mẫu/vận mẫu + 4 dòng thanh (chữ minh hoạ, nghĩa,
/// nghe) + "Nghe lần lượt" (cách 600 ms, dừng khi đóng/rời).
Future<void> showSyllableSheet(BuildContext context, {required PinyinChart chart, required PinyinSyllable syllable}) {
  return showAfBottomSheet<void>(
    context: context,
    // Chỉ đọc (nghe/xem), không có dữ liệu nhập ⇒ chạm ngoài/kéo xuống đóng nhanh khi lướt bảng.
    closeOnBarrier: true,
    builder: (ctx) => SyllableSheet(chart: chart, syllable: syllable),
  );
}

class SyllableSheet extends ConsumerStatefulWidget {
  const SyllableSheet({super.key, required this.chart, required this.syllable});

  final PinyinChart chart;
  final PinyinSyllable syllable;

  @override
  ConsumerState<SyllableSheet> createState() => _SyllableSheetState();
}

class _SyllableSheetState extends ConsumerState<SyllableSheet> {
  bool _playingAll = false;
  int _runId = 0;
  SpeechController? _speech;

  @override
  void dispose() {
    // Đóng sheet/rời trang giữa chừng ⇒ vòng lặp "Nghe lần lượt" thấy id lệch và thoát; huỷ lượt đang đọc (hoãn một
    // tick vì Riverpod cấm đổi state provider trong dispose).
    _runId++;
    final speech = _speech;
    if (speech != null) scheduleMicrotask(speech.cancel);
    super.dispose();
  }

  void _stopAll() {
    _runId++;
    setState(() => _playingAll = false);
    unawaited(_speech?.cancel());
  }

  /// Phát các thanh có chữ cách nhau 600 ms. Bắt đầu TRONG onPressed (iOS/web chỉ cho phát trong thao tác chạm);
  /// các lượt sau nối tiếp future — đã "mở khoá" audio sau lượt đầu.
  Future<void> _playAll() async {
    final id = ++_runId;
    setState(() => _playingAll = true);
    try {
      for (final t in kDrillTones) {
        final ex = widget.syllable.tones[t];
        if (ex == null) continue;
        if (_runId != id) return;
        await speakZh(ref, ex.hanzi);
        if (_runId != id) return;
        await Future<void>.delayed(kSequenceGap);
      }
    } finally {
      if (mounted && _runId == id) setState(() => _playingAll = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final speech = ref.watch(speechControllerProvider);
    _speech = ref.read(speechControllerProvider.notifier);
    final s = widget.syllable;
    final initial = widget.chart.initials.where((i) => i.code == s.initial).firstOrNull;
    final fin = widget.chart.finals.where((f) => f.code == s.final_).firstOrNull;
    final hasAny = s.hasAnyTone;
    final muted = theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant);
    final maxHeight = MediaQuery.sizeOf(context).height * 0.85;

    final initialLine = StringBuffer('Thanh mẫu: ');
    initialLine.write(initial != null && initial.code.isNotEmpty ? initial.display : 'không có (Ø)');
    if (initial != null && initial.ipa.isNotEmpty) initialLine.write(' · IPA /${initial.ipa}/');
    if (initial?.aspirated ?? false) initialLine.write(' · bật hơi');
    final finalLine = StringBuffer('Vận mẫu: ${fin?.display ?? s.final_}');
    if (fin?.standaloneSpelling != null && fin!.standaloneSpelling != fin.code) {
      finalLine.write(' · đứng một mình viết "${fin.standaloneSpelling}"');
    }

    return ConstrainedBox(
      constraints: BoxConstraints(maxHeight: maxHeight),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 4, 0),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    displaySyllableKey(s.syllable),
                    style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
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
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(initialLine.toString(), style: theme.textTheme.bodyMedium),
                  if (initial != null && initial.noteVi.isNotEmpty) Text(initial.noteVi, style: muted),
                  const SizedBox(height: 8),
                  Text(finalLine.toString(), style: theme.textTheme.bodyMedium),
                  if (fin != null && fin.noteVi.isNotEmpty) Text(fin.noteVi, style: muted),
                  const SizedBox(height: 12),
                  if (!hasAny)
                    Material(
                      color: scheme.secondaryContainer,
                      borderRadius: BorderRadius.circular(10),
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Text(
                          'Chưa có chữ minh hoạ đọc đúng cho âm tiết này.',
                          style: TextStyle(color: scheme.onSecondaryContainer),
                        ),
                      ),
                    ),
                  for (final t in kDrillTones) ...[
                    if (t > 1) const Divider(height: 1),
                    _ToneRow(syllable: s.syllable, tone: t, example: s.tones[t]),
                  ],
                ],
              ),
            ),
          ),
          if (hasAny)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
              child: _playingAll
                  ? OutlinedButton.icon(
                      key: kPlayAllKey,
                      onPressed: _stopAll,
                      icon: const Icon(Icons.stop),
                      label: const Text('Dừng'),
                      style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    )
                  : FilledButton.icon(
                      key: kPlayAllKey,
                      onPressed: speech.canSpeak ? () => unawaited(_playAll()) : null,
                      icon: const Icon(Icons.playlist_play),
                      label: const Text('Nghe lần lượt'),
                      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    ),
            ),
        ],
      ),
    );
  }
}

/// Một dòng thanh: "1 ˉ" · pinyin dấu · chữ · nghĩa · loa (mờ khi không có chữ minh hoạ).
class _ToneRow extends StatelessWidget {
  const _ToneRow({required this.syllable, required this.tone, required this.example});

  final String syllable;
  final int tone;
  final ToneExample? example;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final ex = example;
    return Opacity(
      opacity: ex == null ? 0.55 : 1,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Row(
          children: [
            SizedBox(
              width: 40,
              child: Text(
                '$tone ${kToneGlyph[tone]}',
                semanticsLabel: 'Thanh $tone',
                style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w700),
              ),
            ),
            SizedBox(
              width: 64,
              child: PinyinText(
                '$syllable$tone',
                style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
              ),
            ),
            if (ex != null) ...[
              ConstrainedBox(
                constraints: const BoxConstraints(minWidth: 44),
                child: HanziBig(ex.hanzi, size: HanziSize.lg),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(ex.meaningVi, style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant)),
              ),
              SpeakButton(text: ex.hanzi),
            ] else
              Expanded(
                child: Text(
                  'không có chữ minh hoạ',
                  style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
