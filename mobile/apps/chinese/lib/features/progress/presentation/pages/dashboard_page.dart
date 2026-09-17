import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../../system/presentation/widgets/system_status_card.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import '../../domain/labels.dart';
import '../../domain/today_tasks.dart';
import '../widgets/activity_heatmap.dart';
import '../widgets/daily_goal_card.dart';
import '../widgets/lesson_progress_card.dart';
import '../widgets/streak_card.dart';
import '../widgets/today_tasks_card.dart';
import '../widgets/tone_accuracy_card.dart';
import '../widgets/vocabulary_card.dart';
import '../widgets/writing_progress_card.dart';

/// Quyền thấy khối "Trạng thái hệ thống" cuối trang (như web F11).
const kUsersManagePermission = 'users.manage';

/// `/` (M5, port `DashboardPage.tsx`): trang chủ = bảng tổng quan tiến độ theo MÚI GIỜ HỒ SƠ — tiêu đề "Hôm nay,
/// {thứ} {dd/MM}" lấy từ `localDate` của server, không dùng ngày máy. Thứ tự khối §5.3.4: chuỗi ngày → mục tiêu →
/// việc hôm nay → lịch 90 ngày → từ vựng → bài học → luyện viết → thanh điệu; khối vắng ⇒ ẩn (R-PG7); số 0 ⇒ CTA.
/// Kéo để làm mới; app resumed / chọn lại tab ⇒ `invalidateProgressOverview` (app.dart, AppShell). Người không có
/// `study.use` thấy dải giải thích; khối trạng thái hệ thống chỉ hiện với `users.manage`, thu gọn cuối trang.
class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final permissions = ref.watch(permissionsProvider);
    final canStudy = permissions.contains(kStudyUsePermission);
    final canManageUsers = permissions.contains(kUsersManagePermission);
    final overviewProvider = ref.watch(progressOverviewProvider);
    // Không có quyền học thì không gọi API (server trả 403) — như `enabled` của query web.
    final overview = canStudy ? ref.watch(overviewProvider) : const AsyncValue<ProgressOverview>.loading();
    final data = overview.value;
    final title = data != null
        ? todayHeading(data.localDate)
        : canStudy && overview.isLoading
        ? 'Hôm nay'
        : 'Trang chủ';

    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: RefreshIndicator(
        onRefresh: () async {
          if (!canStudy) return;
          // Chờ lời gọi xong để vòng xoay không tắt sớm; lỗi đã hiện trong trang.
          await ref.refresh(overviewProvider.future).then<void>((_) {}, onError: (Object _) {});
        },
        child: SingleChildScrollView(
          // AlwaysScrollable để kéo-làm-mới hoạt động cả khi nội dung ngắn hơn màn.
          physics: const AlwaysScrollableScrollPhysics(),
          child: AfPageBody(
            scrollable: false,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (data != null) _TimeZoneHint(overview: data),
                if (!canStudy)
                  const AuthBanner(
                    message:
                        'Chế độ chỉ xem: tài khoản của bạn chưa có quyền "Học tập" nên bảng tổng quan tiến độ không '
                        'hiện. Cần học thì liên hệ quản trị viên để được gán vai trò Học viên.',
                  )
                else
                  AsyncValueView<ProgressOverview>(
                    value: overview,
                    loading: const _DashboardSkeleton(),
                    error: (e) => _OverviewError(error: e, onRetry: () => ref.invalidate(overviewProvider)),
                    data: (o) => _DashboardBody(overview: o),
                  ),
                if (canManageUsers) ...[const SizedBox(height: 12), const _SystemStatusSection()],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Múi giờ máy ≠ múi giờ hồ sơ ⇒ dòng nhỏ dẫn tới Hồ sơ (§5.3.4). Múi giờ máy đọc bất đồng bộ; chưa có hoặc KHÔNG đọc
/// được (`deviceTimeZoneProvider` trả null — không dùng mặc định, review M5) ⇒ không nhắc.
class _TimeZoneHint extends ConsumerWidget {
  const _TimeZoneHint({required this.overview});

  final ProgressOverview overview;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final deviceTz = ref.watch(deviceTimeZoneProvider).value;
    if (!isDeviceTimeZoneDifferent(overview.timeZone, deviceTz)) return const SizedBox.shrink();
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: () => context.go(AppRoutes.profile),
        borderRadius: BorderRadius.circular(8),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Text.rich(
            TextSpan(
              text: 'Ngày học tính theo múi giờ hồ sơ (${overview.timeZone}) — ',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              children: [
                TextSpan(
                  text: 'đổi ở Hồ sơ',
                  style: TextStyle(color: theme.colorScheme.primary, decoration: TextDecoration.underline),
                ),
                const TextSpan(text: '.'),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Nội dung bảng tổng quan theo thứ tự §5.3.4; khối `null`/vắng ⇒ ẩn.
class _DashboardBody extends StatelessWidget {
  const _DashboardBody({required this.overview});

  final ProgressOverview overview;

  @override
  Widget build(BuildContext context) {
    final o = overview;
    final srs = o.srs;
    final goal = o.dailyGoal;
    final cards = <Widget>[
      StreakCard(streak: o.streak, today: o.today),
      if (srs != null && goal != null) DailyGoalCard(goal: goal, srs: srs),
      TodayTasksCard(tasks: buildTodayTasks(o)),
      ActivityHeatmap(activity: o.activity, today: o.localDate),
      if (o.vocabulary != null) VocabularyCard(vocabulary: o.vocabulary!),
      if (o.lessons != null) LessonProgressCard(lessons: o.lessons!),
      if (o.writing != null) WritingProgressCard(writing: o.writing!, lastCompleted: o.lessons?.lastCompleted),
      if (o.tone != null) ToneAccuracyCard(tone: o.tone!),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < cards.length; i++) ...[if (i > 0) const SizedBox(height: 12), cards[i]],
      ],
    );
  }
}

/// Khung xám lúc tải (tương đương `Skeleton` web) — 8 khối, khối lịch cao hơn.
class _DashboardSkeleton extends StatelessWidget {
  const _DashboardSkeleton();

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.08);
    return Column(
      children: [
        for (var i = 0; i < 8; i++) ...[
          if (i > 0) const SizedBox(height: 12),
          Container(
            height: i == 3 ? 220 : 150,
            decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
          ),
        ],
      ],
    );
  }
}

/// Lỗi tải tổng quan: 503 (`CONTENT_UNAVAILABLE`) là cảnh báo học liệu chưa sẵn sàng; còn lại theo `ErrorView`.
class _OverviewError extends StatelessWidget {
  const _OverviewError({required this.error, required this.onRetry});

  final ApiError error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final kind = error.isNetwork
        ? ErrorViewKind.network
        : error.isForbidden
        ? ErrorViewKind.forbidden
        : ErrorViewKind.unknown;
    return Card(
      child: ErrorView(
        kind: kind,
        title: error.status == 503 ? 'Học liệu chưa sẵn sàng' : 'Không tải được tổng quan',
        message: error.status == 503 ? '${error.message}\nBáo quản trị viên nếu kéo dài.' : error.message,
        onRetry: onRetry,
        compact: true,
      ),
    );
  }
}

/// Khối "Trạng thái hệ thống" thu gọn cuối trang (F11 §5.3.4, port `SystemStatusSection.tsx`): chỉ dựng nội dung
/// khi mở (không gọi `/system/info` lúc thu gọn — `ExpansionTile` không `maintainState`).
class _SystemStatusSection extends StatelessWidget {
  const _SystemStatusSection();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        key: const ValueKey('system-status-section'),
        title: Text('Trạng thái hệ thống (qua gateway)', style: theme.textTheme.titleSmall),
        subtitle: Text('Chỉ quản trị viên thấy', style: theme.textTheme.bodySmall),
        shape: const Border(),
        childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
        children: const [SystemStatusCard()],
      ),
    );
  }
}
