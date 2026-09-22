import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../api/clients.dart';
import '../../../../../core/speech/chinese_speech.dart';
import '../../../application/providers.dart';
import '../../../data/lessons_api.dart';
import '../../../data/models.dart';
import '../../../domain/lesson_errors.dart';
import '../../../domain/quiz_score.dart';
import '../../../domain/shuffle.dart';
import 'quiz_history.dart';
import 'quiz_question_view.dart';
import 'quiz_result_view.dart';

/// Key các nút của quiz (test).
const kQuizStartKey = ValueKey('quiz-start');
const kQuizPrevKey = ValueKey('quiz-prev');
const kQuizNextKey = ValueKey('quiz-next');
const kQuizSubmitKey = ValueKey('quiz-submit');
const kQuizRetrySubmitKey = ValueKey('quiz-retry-submit');
const kQuizQuitKey = ValueKey('quiz-quit');

/// Nội dung hộp xác nhận thoát lượt làm (cùng chuỗi với web).
const kQuizQuitTitle = 'Thoát lượt làm?';
const kQuizQuitMessage = 'Đáp án đã chọn sẽ không được lưu. Bạn có thể làm lại từ đầu bất cứ lúc nào.';

enum _Phase { intro, running, result }

/// Một lượt làm: id idempotent + mốc bắt đầu + ẢNH CHỤP câu hỏi và thứ tự lựa chọn (cố định suốt lượt).
class _Attempt {
  _Attempt({required this.clientAttemptId, required this.startedAt, required this.questions, required this.order});

  final String clientAttemptId;
  final DateTime startedAt;
  final List<QuizQuestion> questions;
  final OptionOrder order;
}

/// Máy trạng thái quiz (§5.3.1, port `QuizRunner.tsx`): intro → câu i/n → nộp → kết quả. Không chấm tại chỗ.
/// `clientAttemptId = uuidV4()` sinh LÚC BẮT ĐẦU lượt (R-LS9, RM-L2) — lỗi mạng giữ đáp án (đã khoá), "Thử lại" gửi
/// cùng id ⇒ server không tạo bản ghi thứ hai. TTS câu nghe: chỉ gọi `speak` trong handler Bắt đầu/Tiếp/Trước (iOS).
/// Trạng thái sống qua đổi tab (`AutomaticKeepAliveClientMixin`). Phím tắt của web bỏ (không bàn phím).
class QuizRunner extends ConsumerStatefulWidget {
  const QuizRunner({
    super.key,
    required this.lesson,
    required this.slug,
    required this.onLessonChanged,
    required this.onDirtyChanged,
  });

  final LessonDetail lesson;
  final String slug;

  /// Server báo `422 QUIZ_CHANGED`/`QUIZ_EMPTY` ⇒ trang tải lại bài; runner về màn mở đầu.
  final VoidCallback onLessonChanged;

  /// Báo cho trang biết đang làm dở (đã trả lời ≥ 1 câu, chưa nộp) để chặn pop + hỏi (RM-L7).
  final ValueChanged<bool> onDirtyChanged;

  @override
  ConsumerState<QuizRunner> createState() => QuizRunnerState();
}

class QuizRunnerState extends ConsumerState<QuizRunner> with AutomaticKeepAliveClientMixin {
  _Phase _phase = _Phase.intro;
  _Attempt? _attempt;
  final Map<String, String> _answers = {};
  int _index = 0;
  QuizResult? _result;

  /// Đã bấm "Nộp bài" ít nhất một lần ⇒ khoá đổi đáp án (gửi lại phải đúng bộ đáp án cũ — R-LS9).
  bool _locked = false;
  bool _submitting = false;
  ApiError? _submitError;
  bool _dirty = false;
  bool _confirming = false;

  @override
  bool get wantKeepAlive => true;

  /// Id lượt hiện tại (test/kiểm tra).
  String? get clientAttemptId => _attempt?.clientAttemptId;

  List<QuizQuestion> get _questions => _attempt?.questions ?? const [];
  QuizQuestion? get _current => _index < _questions.length ? _questions[_index] : null;

  void _syncDirty() {
    final dirty = _phase == _Phase.running && _answers.isNotEmpty;
    if (dirty != _dirty) {
      _dirty = dirty;
      widget.onDirtyChanged(dirty);
    }
  }

  /// Tự phát câu nghe khi đi tới câu đó — gọi từ handler nút (không từ effect/build).
  void _autoplayFor(QuizQuestion? q) {
    if (q == null || !q.isListen) return;
    if (!ref.read(autoPlayAudioProvider) || !ref.read(speechControllerProvider).canSpeak) return;
    unawaited(speakZh(ref, q.audioText!));
  }

  void _start() {
    final snapshot = [for (final q in widget.lesson.quiz) q];
    final attempt = _Attempt(
      clientAttemptId: uuidV4(),
      startedAt: DateTime.now().toUtc(),
      questions: snapshot,
      order: shuffleOptions(snapshot),
    );
    setState(() {
      _attempt = attempt;
      _answers.clear();
      _index = 0;
      _result = null;
      _locked = false;
      _submitError = null;
      _phase = _Phase.running;
    });
    _syncDirty();
    _autoplayFor(snapshot.isEmpty ? null : snapshot.first);
  }

  void _goTo(int i) {
    if (i < 0 || i >= _questions.length) return;
    setState(() => _index = i);
    _autoplayFor(_questions[i]);
  }

  void _select(String optionId) {
    final q = _current;
    if (q == null || _locked) return;
    setState(() => _answers[q.id] = optionId);
    _syncDirty();
  }

  Future<void> _submit() async {
    final attempt = _attempt;
    if (attempt == null || _submitting) return;
    setState(() {
      _locked = true;
      _submitting = true;
      _submitError = null;
    });
    final body = SubmitQuizRequest(
      clientAttemptId: attempt.clientAttemptId,
      startedAt: attempt.startedAt,
      answers: buildAnswers(attempt.questions, _answers),
    );
    try {
      final res = await submitQuiz(ref.read(chineseDioProvider), widget.lesson.id, body);
      if (!mounted) return;
      setState(() {
        _result = res;
        _phase = _Phase.result;
        _submitting = false;
      });
      _syncDirty();
      ref.afterQuizSubmitted(widget.slug, widget.lesson.id, res);
    } on Object catch (e) {
      final err = ApiError.from(e);
      if (!mounted) return;
      if (isQuizReloadError(err)) {
        // Admin vừa sửa quiz / bài không còn câu hỏi ⇒ tải lại bài, về màn mở đầu (không thể gửi lại đáp án cũ).
        showAfToast(
          context,
          err.code == kQuizEmptyCode ? 'Bài này chưa có câu hỏi.' : 'Bài vừa được cập nhật — tải lại quiz.',
          kind: err.code == kQuizEmptyCode ? AfToastKind.error : AfToastKind.warning,
        );
        setState(() {
          _attempt = null;
          _answers.clear();
          _locked = false;
          _submitting = false;
          _phase = _Phase.intro;
        });
        _syncDirty();
        widget.onLessonChanged();
        return;
      }
      // Lỗi khác (mạng, 5xx, 409...): giữ đáp án (đã khoá), hiện dải + "Thử lại" cùng clientAttemptId.
      setState(() {
        _submitting = false;
        _submitError = err;
      });
    }
  }

  /// Thoát lượt làm (nút trong thanh đáy hoặc rời trang): hỏi xác nhận rồi về màn mở đầu, không lưu gì.
  Future<bool> confirmQuit() async {
    if (_confirming) return false;
    _confirming = true;
    try {
      final ok = await showAfConfirm(
        context: context,
        title: kQuizQuitTitle,
        message: kQuizQuitMessage,
        confirmLabel: 'Thoát',
        cancelLabel: 'Ở lại',
        destructive: true,
      );
      if (!ok || !mounted) return false;
      abandon();
      return true;
    } finally {
      _confirming = false;
    }
  }

  /// Bỏ lượt đang làm (đã xác nhận) — về màn mở đầu.
  void abandon() {
    if (!mounted) return;
    setState(() {
      _attempt = null;
      _answers.clear();
      _locked = false;
      _submitting = false;
      _submitError = null;
      _phase = _Phase.intro;
    });
    _syncDirty();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final lesson = widget.lesson;
    final speech = ref.watch(speechControllerProvider);

    // ─── Bài không có câu hỏi ───
    if (lesson.quiz.isEmpty) {
      return AfPageBody(
        child: Text(
          'Bài này chưa có câu hỏi — hãy đọc nội dung và học từ vựng trước.',
          style: theme.textTheme.bodyLarge?.copyWith(color: scheme.onSurfaceVariant),
        ),
      );
    }

    // ─── Kết quả ───
    final result = _result;
    final attempt = _attempt;
    if (_phase == _Phase.result && result != null && attempt != null) {
      return AfPageBody(
        child: QuizResultView(result: result, questions: attempt.questions, onRetry: _start),
      );
    }

    // ─── Màn mở đầu ───
    final current = _current;
    if (_phase == _Phase.intro || attempt == null || current == null) {
      final progress = lesson.progress;
      final best = progress?.bestScorePercent;
      final listenCount = lesson.quiz.where((q) => q.type == 'listen_choice').length;
      return AfPageBody(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text('Kiểm tra bài', style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700)),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        _InfoChip(label: '${lesson.quiz.length} câu', filled: true),
                        if (listenCount > 0) _InfoChip(label: '$listenCount câu nghe'),
                        _InfoChip(
                          label:
                              'Đạt từ $kPassThresholdPercent% (≥ ${minCorrectToPass(lesson.quiz.length)}/'
                              '${lesson.quiz.length} câu)',
                        ),
                        if (best != null)
                          _InfoChip(
                            label: 'Điểm cao nhất $best%',
                            filled: true,
                            success: progress?.isCompleted ?? false,
                          ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text(
                      'Đáp án chỉ hiện sau khi nộp bài. Đạt $kPassThresholdPercent% trở lên là hoàn thành bài — từ của '
                      'bài sẽ được thêm vào ôn tập.',
                      style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                    ),
                    if (listenCount > 0 && !speech.canSpeak && speech.status != SpeechStatus.loading) ...[
                      const SizedBox(height: 12),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: scheme.tertiaryContainer,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          'Máy chưa có giọng tiếng Trung — ở câu nghe bạn có thể bấm "Hiện chữ" để trả lời.',
                          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onTertiaryContainer),
                        ),
                      ),
                    ],
                    const SizedBox(height: 16),
                    FilledButton.icon(
                      key: kQuizStartKey,
                      onPressed: _start,
                      icon: const Icon(Icons.play_arrow),
                      label: Text((progress?.attemptsCount ?? 0) > 0 ? 'Làm lại' : 'Bắt đầu'),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(52),
                        textStyle: const TextStyle(fontSize: 17),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            QuizHistory(lessonId: lesson.id),
          ],
        ),
      );
    }

    // ─── Đang làm ───
    final total = _questions.length;
    final unanswered = countUnanswered(_questions, _answers);
    final answered = total - unanswered;
    final isLast = _index + 1 >= total;
    final err = _submitError;

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
                          'Câu ${_index + 1}/$total',
                          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                        ),
                      ),
                      Text(
                        'Đã trả lời $answered/$total',
                        style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Semantics(
                    label: 'Tiến độ quiz',
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(value: total == 0 ? 0 : answered / total, minHeight: 8),
                    ),
                  ),
                  const SizedBox(height: 12),
                  QuizQuestionView(
                    key: ValueKey('quiz-q-${current.id}'),
                    question: current,
                    options: attempt.order[current.id] ?? current.options,
                    selectedOptionId: _answers[current.id],
                    onSelect: _select,
                    locked: _locked,
                  ),
                ],
              ),
            ),
          ),
        ),
        // Thanh dính đáy: lỗi nộp + "Thử lại", dòng còn N câu, Trước/Tiếp|Nộp, Thoát lượt làm.
        Material(
          color: scheme.surface,
          child: DecoratedBox(
            decoration: BoxDecoration(
              border: Border(top: BorderSide(color: scheme.outlineVariant)),
            ),
            child: SafeArea(
              top: false,
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (err != null) ...[
                          Container(
                            padding: const EdgeInsets.fromLTRB(12, 8, 8, 4),
                            decoration: BoxDecoration(
                              color: scheme.errorContainer,
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                Text(
                                  'Chưa nộp được bài (${err.message}). Đáp án của bạn vẫn được giữ và đã khoá — bấm '
                                  '"Thử lại" để gửi lại đúng bộ đáp án này.',
                                  style: theme.textTheme.bodySmall?.copyWith(color: scheme.onErrorContainer),
                                ),
                                Align(
                                  alignment: Alignment.centerRight,
                                  child: TextButton.icon(
                                    key: kQuizRetrySubmitKey,
                                    onPressed: _submitting ? null : () => unawaited(_submit()),
                                    icon: const Icon(Icons.refresh),
                                    label: const Text('Thử lại'),
                                    style: TextButton.styleFrom(foregroundColor: scheme.onErrorContainer),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 6),
                        ],
                        if (isLast && unanswered > 0)
                          Padding(
                            padding: const EdgeInsets.only(bottom: 6),
                            child: Text(
                              'Còn $unanswered câu chưa trả lời — dùng nút "Trước" để quay lại.',
                              textAlign: TextAlign.center,
                              style: theme.textTheme.bodySmall?.copyWith(color: scheme.tertiary),
                            ),
                          ),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton.icon(
                                key: kQuizPrevKey,
                                onPressed: _index == 0 || _submitting ? null : () => _goTo(_index - 1),
                                icon: const Icon(Icons.arrow_back),
                                label: const Text('Trước'),
                                style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              flex: 2,
                              child: isLast
                                  ? FilledButton.icon(
                                      key: kQuizSubmitKey,
                                      onPressed: unanswered > 0 || _submitting ? null : () => unawaited(_submit()),
                                      icon: _submitting
                                          ? const SizedBox.square(
                                              dimension: 18,
                                              child: CircularProgressIndicator(strokeWidth: 2),
                                            )
                                          : const Icon(Icons.send),
                                      label: Text(_submitting ? 'Đang chấm…' : 'Nộp bài'),
                                      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                                    )
                                  : FilledButton.icon(
                                      key: kQuizNextKey,
                                      onPressed: _submitting ? null : () => _goTo(_index + 1),
                                      icon: const Icon(Icons.arrow_forward),
                                      label: const Text('Tiếp'),
                                      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                                    ),
                            ),
                          ],
                        ),
                        TextButton(
                          key: kQuizQuitKey,
                          onPressed: _submitting ? null : () => unawaited(confirmQuit()),
                          style: TextButton.styleFrom(foregroundColor: scheme.onSurfaceVariant),
                          child: const Text('Thoát lượt làm'),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.label, this.filled = false, this.success = false});

  final String label;
  final bool filled;
  final bool success;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final bg = success
        ? scheme.primary
        : filled
        ? scheme.surfaceContainerHighest
        : Colors.transparent;
    final fg = success ? scheme.onPrimary : scheme.onSurfaceVariant;
    return Chip(
      label: Text(label),
      labelStyle: TextStyle(color: fg, fontSize: 12),
      backgroundColor: bg,
      side: filled || success ? BorderSide.none : BorderSide(color: scheme.outlineVariant),
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      padding: const EdgeInsets.symmetric(horizontal: 6),
    );
  }
}
