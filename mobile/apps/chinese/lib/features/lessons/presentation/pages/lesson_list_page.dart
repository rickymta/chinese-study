import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../application/providers.dart';
import '../../data/models.dart';
import '../widgets/lesson_card.dart';
import '../widgets/lesson_error_view.dart';

/// `/bai-hoc` (hợp đồng M9, cần `study.use`, port `LessonListPage.tsx`): thẻ "Bài tiếp theo" nổi bật
/// (`nextLessonSlug`, R-LS4), rồi "Tất cả bài học (n)" theo `orderIndex`; không khoá tuần tự. Rỗng ⇒ "Chưa có bài học
/// nào được xuất bản". Kéo để làm mới. Thiếu `study.use` ⇒ dải giải thích chế độ chỉ xem.
class LessonListPage extends ConsumerWidget {
  const LessonListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canStudy = ref.watch(permissionsProvider).contains(kStudyUsePermission);
    final provider = ref.watch(lessonsProvider);
    final value = canStudy ? ref.watch(provider) : const AsyncValue<LessonListResponse>.loading();

    return Scaffold(
      appBar: AppBar(title: const Text('Bài học')),
      body: RefreshIndicator(
        onRefresh: () async {
          if (!canStudy) return;
          await ref.refresh(provider.future).then<void>((_) {}, onError: (Object _) {});
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          child: AfPageBody(
            scrollable: false,
            child: !canStudy
                ? const AuthBanner(
                    message:
                        'Chế độ chỉ xem: tài khoản của bạn chưa có quyền "Học tập" nên chưa mở bài học được. Cần học thì '
                        'liên hệ quản trị viên để được gán vai trò Học viên.',
                  )
                : AsyncValueView<LessonListResponse>(
                    value: value,
                    loading: const _ListSkeleton(),
                    error: (e) => LessonErrorView(error: e, onRetry: () => ref.invalidateLessons()),
                    data: (data) => _ListBody(data: data),
                  ),
          ),
        ),
      ),
    );
  }
}

class _ListBody extends StatelessWidget {
  const _ListBody({required this.data});

  final LessonListResponse data;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final items = data.sorted;
    if (items.isEmpty) {
      return const EmptyState(
        icon: Icons.menu_book_outlined,
        title: 'Chưa có bài học nào được xuất bản.',
        message: 'Quản trị viên sẽ xuất bản bài học sớm — trong lúc chờ, hãy học pinyin và ôn thẻ.',
      );
    }
    final nextSlug = data.nextLessonSlug;
    LessonSummary? next;
    if (nextSlug != null) {
      for (final l in items) {
        if (l.slug == nextSlug) next = l;
      }
    }
    final completedAll = next == null && items.every((l) => l.progress?.isCompleted ?? false);
    final heading = theme.textTheme.labelMedium?.copyWith(color: scheme.onSurfaceVariant, letterSpacing: 0.5);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (next != null) ...[
          Text('BÀI TIẾP THEO', style: heading),
          const SizedBox(height: 6),
          LessonCard(key: kNextLessonCardKey, lesson: next, highlighted: true),
          const SizedBox(height: 20),
        ] else if (completedAll) ...[
          const AuthBanner(message: 'Bạn đã hoàn thành tất cả bài học hiện có. Hãy tiếp tục ôn tập để giữ từ vựng.'),
          const SizedBox(height: 20),
        ],
        Text('TẤT CẢ BÀI HỌC (${items.length})', style: heading),
        const SizedBox(height: 6),
        for (var i = 0; i < items.length; i++) ...[
          if (i > 0) const SizedBox(height: 10),
          LessonCard(key: lessonCardKey(items[i].slug), lesson: items[i], highlighted: items[i].slug == next?.slug),
        ],
      ],
    );
  }
}

class _ListSkeleton extends StatelessWidget {
  const _ListSkeleton();

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.surfaceContainerHighest;
    Widget box(double h) => Container(
      height: h,
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
    );
    return Column(
      children: [
        box(160),
        const SizedBox(height: 16),
        for (var i = 0; i < 3; i++) ...[box(140), const SizedBox(height: 10)],
      ],
    );
  }
}
