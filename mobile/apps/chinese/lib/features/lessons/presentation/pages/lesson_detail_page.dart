import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../api/clients.dart';
import '../../../../router/routes.dart';
import '../../../pinyin/application/providers.dart';
import '../../application/providers.dart';
import '../../data/lessons_api.dart';
import '../../data/models.dart';
import '../widgets/glossary_list.dart';
import '../widgets/lesson_card.dart';
import '../widgets/lesson_content.dart';
import '../widgets/lesson_error_view.dart';
import '../widgets/lesson_word_list.dart';
import '../widgets/quiz/quiz_runner.dart';

/// Các tab của `/bai-hoc/:slug?tab=` (giá trị URL giống web).
enum LessonTab {
  content('noi-dung', 'Nội dung'),
  vocabulary('tu-vung', 'Từ vựng'),
  quiz('quiz', 'Quiz');

  const LessonTab(this.slug, this.label);

  final String slug;
  final String label;

  static LessonTab fromSlug(String? slug) {
    for (final t in values) {
      if (t.slug == slug) return t;
    }
    return LessonTab.content;
  }
}

/// Dưới ngưỡng này (số câu luyện thanh đã trả lời ở M7) ⇒ gợi ý học Pinyin trước — không chặn.
const kToneDrillHintThreshold = 40;

/// Key công tắc Pinyin / Nghĩa tiếng Việt, banner gợi ý pinyin (test).
const kShowPinyinSwitchKey = ValueKey('lesson-show-pinyin');
const kShowViSwitchKey = ValueKey('lesson-show-vi');
const kToneHintKey = ValueKey('lesson-tone-hint');

/// `/bai-hoc/:slug?tab=noi-dung|tu-vung|quiz` (hợp đồng M9, cần `study.use`, port `LessonDetailPage.tsx`). Tab khởi
/// đầu từ query lúc mở, sau đó state cục bộ, KHÔNG ghi lại URL (RM-L6). Ba tab GIỮ MOUNTED (keep-alive): đổi tab giữa
/// chừng quiz không mất đáp án/`clientAttemptId`; tab Nội dung giữ "Nghe cả đoạn". Gọi `POST /start` MỘT LẦN khi mở
/// bài chưa có tiến độ (R-LS12). 404 (bài không published) ⇒ `/404` qua interceptor. Rời trang khi quiz đã trả lời ≥ 1
/// câu ⇒ hỏi (RM-L7).
///
/// Mobile-first: phần đầu bài (tóm tắt, mục tiêu, chip, banner gợi ý pinyin, công tắc) nằm ở ĐẦU tab Nội dung thay vì
/// trên thanh tab như web — màn 360×740 không đủ chỗ cho khối đầu cố định + tab + nội dung.
class LessonDetailPage extends ConsumerStatefulWidget {
  const LessonDetailPage({super.key, required this.slug});

  final String slug;

  @override
  ConsumerState<LessonDetailPage> createState() => _LessonDetailPageState();
}

class _LessonDetailPageState extends ConsumerState<LessonDetailPage> with SingleTickerProviderStateMixin {
  TabController? _tabs;

  /// Id bài đã gọi `start` — chặn gọi đôi; đổi slug (bài khác) thì gọi lại cho bài mới. API vốn idempotent.
  String? _startedFor;
  bool _quizDirty = false;
  final GlobalKey<QuizRunnerState> _quizKey = GlobalKey<QuizRunnerState>();
  bool _leaving = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Đọc query MỘT lần lúc mở (`GoRouterState.of` phụ thuộc InheritedWidget nên không gọi được trong initState).
    if (_tabs != null) return;
    final query = GoRouterState.of(context).uri.queryParameters;
    _tabs = TabController(
      length: LessonTab.values.length,
      vsync: this,
      initialIndex: LessonTab.fromSlug(query['tab']).index,
    );
  }

  @override
  void dispose() {
    _tabs?.dispose();
    super.dispose();
  }

  /// "Bắt đầu bài" ngầm (R-LS12) — không toast; lỗi chỉ log (bài vẫn xem được, lần mở sau gọi lại).
  Future<void> _start(LessonDetail lesson) async {
    try {
      final progress = await startLesson(ref.read(chineseDioProvider), lesson.id);
      if (!mounted) return;
      ref.afterLessonStarted(widget.slug, progress);
    } on Object catch (e) {
      afLog('lessons: không ghi được "bắt đầu bài" ${lesson.slug} (${ApiError.from(e).message})');
    }
  }

  void _maybeStart(LessonDetail lesson) {
    if (lesson.progress != null || _startedFor == lesson.id || lesson.id.isEmpty) return;
    _startedFor = lesson.id;
    // Đang trong build ⇒ hoãn sang sau khung hình.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) unawaited(_start(lesson));
    });
  }

  void _goBack() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRoutes.lessons);
    }
  }

  Future<void> _confirmLeave() async {
    if (_leaving) return;
    _leaving = true;
    try {
      final ok = await _quizKey.currentState?.confirmQuit() ?? true;
      if (!ok || !mounted) return;
      _goBack();
    } finally {
      _leaving = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    final value = ref.watch(lessonDetailProvider(widget.slug));
    final tabs = _tabs!;
    final lesson = value.value;
    if (lesson != null) _maybeStart(lesson);

    return PopScope(
      canPop: !_quizDirty,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) unawaited(_confirmLeave());
      },
      child: Scaffold(
        appBar: AppBar(
          leading: BackButton(onPressed: _quizDirty ? () => unawaited(_confirmLeave()) : _goBack),
          titleSpacing: 0,
          title: lesson == null
              ? const Text('Bài học')
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text('Bài ${lesson.orderIndex}', style: Theme.of(context).textTheme.labelSmall),
                    Text(lesson.title, maxLines: 1, overflow: TextOverflow.ellipsis),
                  ],
                ),
          bottom: lesson == null
              ? null
              : TabBar(
                  controller: tabs,
                  tabs: [
                    const Tab(text: 'Nội dung'),
                    Tab(text: 'Từ vựng (${lesson.words.length})'),
                    Tab(text: 'Quiz (${lesson.quiz.length})'),
                  ],
                ),
        ),
        body: AsyncValueView<LessonDetail>(
          value: value,
          loading: const _DetailSkeleton(),
          error: (e) => LessonErrorView(
            error: e,
            compact: false,
            onRetry: () => ref.invalidate(lessonDetailProvider(widget.slug)),
          ),
          data: (l) => TabBarView(
            controller: tabs,
            children: [
              _ContentTab(lesson: l),
              _VocabularyTab(lesson: l),
              QuizRunner(
                key: _quizKey,
                lesson: l,
                slug: widget.slug,
                onLessonChanged: () => ref.invalidate(lessonDetailProvider(widget.slug)),
                onDirtyChanged: (dirty) {
                  if (mounted && dirty != _quizDirty) setState(() => _quizDirty = dirty);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Tab Nội dung: đầu bài (tóm tắt, chip trạng thái/~phút/chưa duyệt, mục tiêu), `VoiceMissingNotice`, gợi ý học pinyin
/// trước, công tắc Pinyin/Nghĩa tiếng Việt, rồi các khối nội dung + từ bổ sung. Keep-alive để "Nghe cả đoạn" không
/// bị cắt khi đổi tab.
class _ContentTab extends ConsumerStatefulWidget {
  const _ContentTab({required this.lesson});

  final LessonDetail lesson;

  @override
  ConsumerState<_ContentTab> createState() => _ContentTabState();
}

class _ContentTabState extends ConsumerState<_ContentTab> with AutomaticKeepAliveClientMixin {
  @override
  bool get wantKeepAlive => true;

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final lesson = widget.lesson;
    final prefs = ref.watch(displayPrefsProvider);
    final prefsCtl = ref.read(displayPrefsProvider.notifier);
    final speech = ref.watch(speechControllerProvider);
    final showVoiceNotice = speech.status == SpeechStatus.noVoice || speech.status == SpeechStatus.unsupported;
    // Lỗi (503 học liệu, mạng...) ⇒ không hiện gì; không retry để khỏi làm chậm trang bài.
    final toneStats = ref.watch(ref.watch(toneStatsProvider)).value;
    final showToneHint = toneStats != null && toneStats.totalAnswered < kToneDrillHintThreshold;
    final summary = lesson.summary?.trim();

    return AfPageBody(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Wrap(
            spacing: 6,
            runSpacing: 4,
            children: [
              LessonStatusChip(progress: lesson.progress),
              Chip(
                label: Text('~${lesson.estimatedMinutes} phút'),
                labelStyle: TextStyle(color: scheme.onSurfaceVariant, fontSize: 12),
                side: BorderSide(color: scheme.outlineVariant),
                backgroundColor: Colors.transparent,
                visualDensity: VisualDensity.compact,
                materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                padding: const EdgeInsets.symmetric(horizontal: 6),
              ),
              if (lesson.isUnreviewed) const UnreviewedLessonChip(),
            ],
          ),
          if (summary != null && summary.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(summary, style: theme.textTheme.bodyLarge?.copyWith(color: scheme.onSurfaceVariant)),
          ],
          if (lesson.objectives.isNotEmpty) ...[
            const SizedBox(height: 12),
            Text('Sau bài này bạn sẽ', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 4),
            for (final o in lesson.objectives)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.check, size: 18, color: scheme.primary),
                    const SizedBox(width: 8),
                    Expanded(child: Text(o, style: theme.textTheme.bodyMedium)),
                  ],
                ),
              ),
          ],
          if (showVoiceNotice) ...[
            const SizedBox(height: 12),
            VoiceMissingNotice(
              status: speech.status,
              onRetry: () => unawaited(ref.read(speechControllerProvider.notifier).refreshVoices()),
            ),
          ],
          if (showToneHint) ...[const SizedBox(height: 12), const _ToneHint(key: kToneHintKey)],
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: SwitchListTile.adaptive(
                  key: kShowPinyinSwitchKey,
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Pinyin'),
                  value: prefs.showPinyin,
                  onChanged: (v) => unawaited(prefsCtl.setShowPinyin(v)),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: SwitchListTile.adaptive(
                  key: kShowViSwitchKey,
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Nghĩa Việt'),
                  value: prefs.showVi,
                  onChanged: (v) => unawaited(prefsCtl.setShowVi(v)),
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          LessonContent(blocks: lesson.blocks, glossary: lesson.glossary),
        ],
      ),
    );
  }
}

/// Banner "Nên học xong phần Pinyin & thanh điệu trước…" (chạm ⇒ `/pinyin`, nhánh "Thêm").
class _ToneHint extends StatelessWidget {
  const _ToneHint({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Material(
      color: scheme.secondaryContainer,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: () => context.go(AppRoutes.pinyin),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.info_outline, size: 20, color: scheme.onSecondaryContainer),
              const SizedBox(width: 8),
              Expanded(
                child: Text.rich(
                  TextSpan(
                    style: TextStyle(color: scheme.onSecondaryContainer),
                    children: [
                      const TextSpan(text: 'Nên học xong phần '),
                      const TextSpan(
                        text: 'Pinyin & thanh điệu',
                        style: TextStyle(fontWeight: FontWeight.w700, decoration: TextDecoration.underline),
                      ),
                      const TextSpan(text: ' trước để nghe đúng thanh trong hội thoại và câu hỏi nghe.'),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Tab Từ vựng: lời dẫn + `LessonWordList` (+ "Từ bổ sung"); nút "Luyện viết chữ của bài" hiện sau M10.
class _VocabularyTab extends StatefulWidget {
  const _VocabularyTab({required this.lesson});

  final LessonDetail lesson;

  @override
  State<_VocabularyTab> createState() => _VocabularyTabState();
}

class _VocabularyTabState extends State<_VocabularyTab> with AutomaticKeepAliveClientMixin {
  @override
  bool get wantKeepAlive => true;

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final theme = Theme.of(context);
    final lesson = widget.lesson;
    return AfPageBody(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Hoàn thành quiz (≥ $kPassThresholdPercent%) thì các từ dưới đây tự vào ôn tập. Bấm một từ để xem chi tiết '
            'trong từ điển.',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: 12),
          LessonWordList(words: lesson.words),
          if (lesson.glossary.isNotEmpty) ...[const SizedBox(height: 12), GlossaryList(entries: lesson.glossary)],
        ],
      ),
    );
  }
}

class _DetailSkeleton extends StatelessWidget {
  const _DetailSkeleton();

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.surfaceContainerHighest;
    Widget box(double h, [double? w]) => Container(
      height: h,
      width: w,
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(8)),
    );
    return AfPageBody(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          box(28, 200),
          const SizedBox(height: 12),
          box(80),
          const SizedBox(height: 12),
          box(48),
          const SizedBox(height: 12),
          box(220),
        ],
      ),
    );
  }
}
