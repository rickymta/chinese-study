import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/speech/chinese_speech.dart';
import '../../../../../core/widgets/hanzi_big.dart';
import '../../../../../core/widgets/pinyin_text.dart';
import '../../../../../core/widgets/speak_button.dart';
import '../../../data/models.dart';
import '../../../domain/drill_types.dart';
import '../../../domain/generate_drill.dart';
import 'tone_buttons.dart';

/// Câu đúng tự sang câu sau khoảng này.
const kAutoNextDelay = Duration(milliseconds: 1200);

/// Key nút "Nghe"/"Nghe lại" và nút "Tiếp" (test).
const kDrillPlayKey = ValueKey('drill-play');
const kDrillNextKey = ValueKey('drill-next');

enum _Phase { answering, feedback }

/// Bộ chạy bài luyện (R5-9, port `DrillRunner.tsx`): phát chữ, chọn thanh (1 hoặc 2 phần), chấm ngay, nghe lại thanh
/// đúng/thanh đã chọn; câu đúng tự sang sau 1,2 s, câu sai phải bấm "Tiếp" (thanh dính đáy). TTS: tự phát khi vào câu
/// mới CHỈ khi câu trước được chuyển bằng thao tác người dùng (iOS/web chỉ cho phát trong thao tác chạm) — tự sang sau
/// 1,2 s thì người học bấm "Nghe". Rời màn ⇒ huỷ đọc + huỷ hẹn giờ.
class DrillRunner extends ConsumerStatefulWidget {
  const DrillRunner({super.key, required this.session, required this.chart, required this.onFinish});

  final DrillSession session;
  final PinyinChart chart;

  /// Gọi khi trả lời xong câu cuối (thời điểm kết thúc UTC).
  final void Function(List<AnsweredItem> answers, DateTime finishedAt) onFinish;

  @override
  ConsumerState<DrillRunner> createState() => _DrillRunnerState();
}

class _DrillRunnerState extends ConsumerState<DrillRunner> {
  int _index = 0;
  _Phase _phase = _Phase.answering;
  final List<int> _selections = [];
  final List<AnsweredItem> _answers = [];
  int _replay = 0;
  int? _firstPlayEnd;
  bool _playedOnce = false;
  Timer? _autoNext;

  /// Đồng hồ đơn điệu cho `responseMs` (không lệch khi giờ máy đổi).
  final Stopwatch _clock = Stopwatch()..start();
  SpeechController? _speech;

  List<DrillItem> get _items => widget.session.items;
  DrillItem get _item => _items[_index];
  AnsweredItem? get _lastAnswer => _phase == _Phase.feedback && _answers.isNotEmpty ? _answers.last : null;

  @override
  void initState() {
    super.initState();
    // Câu đầu: "Bắt đầu" là thao tác người dùng ⇒ phát ngay sau khung hình đầu.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _play();
    });
  }

  @override
  void dispose() {
    _autoNext?.cancel();
    final speech = _speech;
    if (speech != null) scheduleMicrotask(speech.cancel);
    super.dispose();
  }

  /// Phát chữ của câu hiện tại; lần phát xong ĐẦU TIÊN là mốc tính `responseMs`.
  void _play() {
    if (!ref.read(speechControllerProvider).canSpeak) return;
    if (_playedOnce) _replay++;
    final index = _index;
    unawaited(
      speakZh(ref, _item.text).whenComplete(() {
        if (!mounted || index != _index || _playedOnce) return;
        setState(() {
          _playedOnce = true;
          _firstPlayEnd = _clock.elapsedMilliseconds;
        });
      }),
    );
  }

  void _goNext({required bool byUser}) {
    _autoNext?.cancel();
    _autoNext = null;
    if (_index + 1 >= _items.length) {
      widget.onFinish(List.unmodifiable(_answers), DateTime.now().toUtc());
      return;
    }
    setState(() {
      _index += 1;
      _phase = _Phase.answering;
      _selections.clear();
      _replay = 0;
      _firstPlayEnd = null;
      _playedOnce = false;
    });
    // Chuyển bằng thao tác chạm ⇒ phát câu mới ngay trong thao tác đó.
    if (byUser) _play();
  }

  void _select(int tone) {
    if (_phase != _Phase.answering) return;
    final parts = _item.parts;
    unawaited(HapticFeedback.selectionClick());
    _selections.add(tone);
    if (_selections.length < parts.length) {
      setState(() {});
      return;
    }
    var correct = true;
    for (var i = 0; i < parts.length; i++) {
      if (parts[i].tone != _selections[i]) correct = false;
    }
    final responseMs = computeResponseMs(_firstPlayEnd, _clock.elapsedMilliseconds);
    _answers.add(
      AnsweredItem(
        item: _item,
        answered: List.unmodifiable(_selections),
        correct: correct,
        responseMs: responseMs,
        replayCount: _replay > kReplayCountMax ? kReplayCountMax : _replay,
      ),
    );
    setState(() => _phase = _Phase.feedback);
    if (correct) _autoNext = Timer(kAutoNextDelay, () => _goNext(byUser: false));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final speech = ref.watch(speechControllerProvider);
    _speech = ref.read(speechControllerProvider.notifier);
    final parts = _item.parts;
    final last = _lastAnswer;
    final feedback = _phase == _Phase.feedback;
    final progress = (_index + (feedback ? 1 : 0)) / _items.length;
    final isLast = _index + 1 >= _items.length;

    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            child: AfPageBody(
              scrollable: false,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          'Câu ${_index + 1}/${_items.length}',
                          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                        ),
                      ),
                      Text(
                        widget.session.mode.label,
                        style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Semantics(
                    label: 'Tiến độ bài luyện',
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(value: progress, minHeight: 8),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          FilledButton.icon(
                            key: kDrillPlayKey,
                            onPressed: speech.canSpeak && !speech.speaking ? _play : null,
                            icon: const Icon(Icons.volume_up),
                            label: Text(_playedOnce ? 'Nghe lại' : 'Nghe'),
                            style: FilledButton.styleFrom(
                              minimumSize: const Size.fromHeight(64),
                              textStyle: const TextStyle(fontSize: 18),
                            ),
                          ),
                          if (!speech.canSpeak)
                            Padding(
                              padding: const EdgeInsets.only(top: 8),
                              child: Text(
                                'Chưa có giọng tiếng Trung — không phát được câu hỏi.',
                                textAlign: TextAlign.center,
                                style: theme.textTheme.bodySmall?.copyWith(color: scheme.error),
                              ),
                            ),
                          const SizedBox(height: 12),
                          if (!feedback)
                            Text(
                              parts.length == 2
                                  ? 'Chọn thanh cho chữ thứ ${_selections.length + 1} / 2'
                                  : 'Bạn nghe được thanh nào?',
                              textAlign: TextAlign.center,
                              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                            )
                          else if (last != null)
                            _Feedback(answer: last, chart: widget.chart),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        // Nút thanh ở nửa dưới màn hình (tầm ngón cái), không cuộn.
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
              child: parts.length == 1
                  ? ToneButtons(
                      value: _selections.isEmpty ? null : _selections[0],
                      onSelect: _select,
                      disabled: feedback,
                      correctTone: feedback ? parts[0].tone : null,
                    )
                  : Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        ToneButtons(
                          row: 0,
                          label: 'Chữ thứ nhất',
                          value: _selections.isEmpty ? null : _selections[0],
                          onSelect: _select,
                          disabled: feedback || _selections.isNotEmpty,
                          correctTone: feedback ? parts[0].tone : null,
                        ),
                        const SizedBox(height: 8),
                        ToneButtons(
                          row: 1,
                          label: 'Chữ thứ hai',
                          value: _selections.length < 2 ? null : _selections[1],
                          onSelect: _select,
                          disabled: feedback || _selections.length != 1,
                          correctTone: feedback ? parts[1].tone : null,
                        ),
                      ],
                    ),
            ),
          ),
        ),
        if (feedback)
          // Dính đáy: câu sai có phần giải thích dài — nút "Tiếp" phải luôn trong tầm tay (bài học F5 web).
          StickyActionBar(
            children: [
              if (last?.correct ?? false)
                OutlinedButton.icon(
                  key: kDrillNextKey,
                  onPressed: () => _goNext(byUser: true),
                  icon: const Icon(Icons.arrow_forward),
                  label: Text(isLast ? 'Xem kết quả' : 'Tiếp (tự chuyển)'),
                  style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                )
              else
                FilledButton.icon(
                  key: kDrillNextKey,
                  onPressed: () => _goNext(byUser: true),
                  icon: const Icon(Icons.arrow_forward),
                  label: Text(isLast ? 'Xem kết quả' : 'Tiếp'),
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                ),
            ],
          ),
      ],
    );
  }
}

/// Đúng/sai + từng phần: chữ + pinyin + nghĩa + "Nghe thanh đúng"/"Nghe thanh bạn chọn" (chữ minh hoạ của thanh đã chọn).
class _Feedback extends StatelessWidget {
  const _Feedback({required this.answer, required this.chart});

  final AnsweredItem answer;
  final PinyinChart chart;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final parts = answer.item.parts;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Center(
          child: Chip(
            avatar: Icon(
              answer.correct ? Icons.check_circle : Icons.cancel,
              color: answer.correct ? scheme.onPrimary : scheme.onError,
              size: 18,
            ),
            label: Text(answer.correct ? 'Đúng!' : 'Chưa đúng'),
            backgroundColor: answer.correct ? scheme.primary : scheme.error,
            labelStyle: TextStyle(
              color: answer.correct ? scheme.onPrimary : scheme.onError,
              fontWeight: FontWeight.w600,
            ),
            side: BorderSide.none,
          ),
        ),
        const SizedBox(height: 12),
        Wrap(
          alignment: WrapAlignment.center,
          spacing: 24,
          runSpacing: 12,
          children: [
            for (var i = 0; i < parts.length; i++)
              _PartFeedback(
                part: parts[i],
                chosen: answer.answered[i],
                chosenExample: answer.answered[i] == parts[i].tone
                    ? null
                    : chart.syllableByKey(parts[i].syllable)?.tones[answer.answered[i]],
              ),
          ],
        ),
      ],
    );
  }
}

class _PartFeedback extends StatelessWidget {
  const _PartFeedback({required this.part, required this.chosen, required this.chosenExample});

  final DrillPart part;
  final int chosen;
  final ToneExample? chosenExample;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final ok = chosen == part.tone;
    final ex = chosenExample;
    return ConstrainedBox(
      constraints: const BoxConstraints(minWidth: 140, maxWidth: 260),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          HanziBig(part.hanzi, size: HanziSize.xl, textAlign: TextAlign.center),
          PinyinText(
            '${part.syllable}${part.tone}',
            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
            textAlign: TextAlign.center,
          ),
          Text(
            part.meaningVi,
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
          ),
          Text(
            ok ? 'Thanh ${part.tone} — đúng' : 'Đúng là thanh ${part.tone}, bạn chọn thanh $chosen',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodySmall?.copyWith(color: ok ? scheme.primary : scheme.error),
          ),
          const SizedBox(height: 6),
          Wrap(
            alignment: WrapAlignment.center,
            spacing: 8,
            runSpacing: 6,
            children: [
              SpeakButton(text: part.hanzi, variant: SpeakButtonVariant.button, label: 'Nghe thanh ${part.tone}'),
              if (ex != null)
                SpeakButton(
                  text: ex.hanzi,
                  variant: SpeakButtonVariant.button,
                  label: 'Nghe thanh $chosen',
                  tooltip: 'Nghe thanh $chosen (${ex.hanzi})',
                ),
            ],
          ),
        ],
      ),
    );
  }
}
