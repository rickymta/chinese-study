import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/speech/chinese_speech.dart';
import '../../../../../core/widgets/hanzi_big.dart';
import '../../../../../core/widgets/pinyin_text.dart';
import '../../../data/models.dart';
import 'question_parts.dart';

/// Key nút "Nghe" của câu nghe, nút "Hiện chữ", và từng lựa chọn theo id (test).
const kQuizPlayKey = ValueKey('quiz-play');
const kQuizRevealKey = ValueKey('quiz-reveal');
Key quizOptionKey(String optionId) => ValueKey('quiz-option-$optionId');

/// Một câu hỏi (port `QuizQuestionView.tsx`): đề bài (+ nút loa lớn 64 px với `listen_choice`), lựa chọn dạng nút to
/// xếp dọc (≥ 56 px cho ngón tay). KHÔNG chấm tại chỗ — đáp án chỉ hiện ở kết quả cuối bài. Không có giọng ⇒ dải
/// cảnh báo + "Hiện chữ". Cha đặt `key` theo id câu để trạng thái "đã hiện chữ" đặt lại theo câu.
class QuizQuestionView extends ConsumerStatefulWidget {
  const QuizQuestionView({
    super.key,
    required this.question,
    required this.options,
    required this.selectedOptionId,
    required this.onSelect,
    this.locked = false,
  });

  final QuizQuestion question;

  /// Lựa chọn theo thứ tự HIỂN THỊ đã xáo (cố định trong lượt).
  final List<QuizOption> options;
  final String? selectedOptionId;
  final ValueChanged<String> onSelect;

  /// `true` sau lần bấm "Nộp bài" đầu tiên: khoá đổi đáp án để "Thử lại" gửi ĐÚNG bộ đáp án cũ cùng `clientAttemptId`
  /// (server phát lại kết quả đã lưu — đổi đáp án lúc này sẽ không được chấm). Giữ màu lựa chọn đã chọn để đối chiếu.
  final bool locked;

  @override
  ConsumerState<QuizQuestionView> createState() => _QuizQuestionViewState();
}

class _QuizQuestionViewState extends ConsumerState<QuizQuestionView> {
  bool _revealed = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final speech = ref.watch(speechControllerProvider);
    final q = widget.question;
    final audio = q.audioText;
    final isListen = q.isListen && audio != null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: [
                QuestionPrompt(question: q),
                if (isListen) ...[
                  const SizedBox(height: 12),
                  Center(
                    child: FilledButton.icon(
                      key: kQuizPlayKey,
                      onPressed: speech.canSpeak && !speech.speaking ? () => unawaited(speakZh(ref, audio)) : null,
                      icon: const Icon(Icons.volume_up),
                      label: const Text('Nghe'),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size(200, 64),
                        textStyle: const TextStyle(fontSize: 18),
                      ),
                    ),
                  ),
                  if (!speech.canSpeak && speech.status != SpeechStatus.loading) ...[
                    const SizedBox(height: 10),
                    Container(
                      padding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
                      decoration: BoxDecoration(
                        color: scheme.tertiaryContainer,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Text(
                              'Chưa có giọng tiếng Trung — không phát được câu hỏi. Bạn có thể xem chữ để trả lời.',
                              style: theme.textTheme.bodySmall?.copyWith(color: scheme.onTertiaryContainer),
                            ),
                          ),
                          if (!_revealed)
                            TextButton(
                              key: kQuizRevealKey,
                              onPressed: () => setState(() => _revealed = true),
                              style: TextButton.styleFrom(foregroundColor: scheme.onTertiaryContainer),
                              child: const Text('Hiện chữ'),
                            ),
                        ],
                      ),
                    ),
                  ],
                  if (_revealed) ...[
                    const SizedBox(height: 8),
                    HanziBig(audio, size: HanziSize.lg, textAlign: TextAlign.center),
                    if (q.audioPinyin != null && q.audioPinyin!.isNotEmpty)
                      PinyinText(
                        q.audioPinyin!,
                        hanzi: audio,
                        textAlign: TextAlign.center,
                        style: theme.textTheme.bodyLarge?.copyWith(color: scheme.primary),
                      ),
                  ],
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Semantics(
          label: 'Lựa chọn',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              for (var i = 0; i < widget.options.length; i++) ...[
                if (i > 0) const SizedBox(height: 8),
                _OptionButton(
                  index: i,
                  option: widget.options[i],
                  selected: widget.options[i].id == widget.selectedOptionId,
                  locked: widget.locked,
                  onTap: () => widget.onSelect(widget.options[i].id),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// Nút lựa chọn to (≥ 56 px): số thứ tự trong vòng tròn + nội dung; đã chọn ⇒ tô màu chính. Khi khoá: bỏ qua bấm nhưng
/// KHÔNG vô hiệu (giữ màu để người học đối chiếu).
class _OptionButton extends StatelessWidget {
  const _OptionButton({
    required this.index,
    required this.option,
    required this.selected,
    required this.locked,
    required this.onTap,
  });

  final int index;
  final QuizOption option;
  final bool selected;
  final bool locked;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final fg = selected ? scheme.onPrimary : scheme.onSurface;
    return Semantics(
      inMutuallyExclusiveGroup: true,
      checked: selected,
      button: true,
      child: Material(
        key: quizOptionKey(option.id),
        color: selected ? scheme.primary : scheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(10),
          side: BorderSide(color: selected ? scheme.primary : scheme.outlineVariant),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: locked ? null : onTap,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 56),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              child: Row(
                children: [
                  Container(
                    width: 24,
                    height: 24,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: fg),
                    ),
                    child: Text(
                      '${index + 1}',
                      style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w700),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OptionText(option: option, color: fg),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
