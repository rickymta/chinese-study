import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../../progress/domain/labels.dart';
import '../../application/outbox_controller.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import '../widgets/pending_reviews_banner.dart';

/// Tổng số từ trong lộ trình HSK 3.0 cấp 1 (D1) — mẫu số của "Từ vững".
const kPathTotal = 500;

/// Key nút bắt đầu (test).
const kStartReviewKey = ValueKey('start-review');

/// `HH:mm dd/MM` theo GIỜ MÁY (`toLocal()`) — Dart không có CSDL múi giờ sẵn (BA-mặc định M6, RK-M23); web dùng múi
/// giờ hồ sơ. Kèm "(giờ trên máy)" khi múi giờ máy ≠ hồ sơ.
String formatNextDue(DateTime utc) {
  final d = utc.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${two(d.hour)}:${two(d.minute)} ${two(d.day)}/${two(d.month)}';
}

/// `/on-tap` (hợp đồng M6, port `ReviewHomePage.tsx`): 3 thẻ số (Đến hạn hôm nay, Từ mới còn học được x/y, Đã ôn
/// hôm nay); nút chính cao 56 "Bắt đầu ôn (N)"; N = 0 ⇒ "Hôm nay xong rồi!" + lượt ôn kế tiếp; "Từ vững: m/500";
/// liên kết "Cài đặt học tập" ⇒ `/ho-so?tab=hoc-tap`; hết lượt ôn ⇒ banner; `PendingReviewsBanner` khi outbox kẹt.
/// Nguồn số: `srsSummaryProvider` (cùng nguồn với huy hiệu). Kéo để làm mới. Thiếu `study.use` ⇒ dải giải thích.
class ReviewHomePage extends ConsumerWidget {
  const ReviewHomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canStudy = ref.watch(permissionsProvider).contains(kStudyUsePermission);
    final summaryProvider = ref.watch(srsSummaryProvider);
    final summary = canStudy ? ref.watch(summaryProvider) : const AsyncValue<SrsSummary>.loading();
    final unsent = ref.watch(reviewOutboxProvider.select((s) => s.unsentCount));

    return Scaffold(
      appBar: AppBar(title: const Text('Ôn tập')),
      body: RefreshIndicator(
        onRefresh: () async {
          if (!canStudy) return;
          ref.read(reviewOutboxProvider.notifier).flushNow();
          await ref.refresh(summaryProvider.future).then<void>((_) {}, onError: (Object _) {});
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          child: AfPageBody(
            scrollable: false,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (unsent > 0) ...[PendingReviewsBanner(count: unsent), const SizedBox(height: 12)],
                if (!canStudy)
                  const AuthBanner(
                    message:
                        'Chế độ chỉ xem: tài khoản của bạn chưa có quyền "Học tập" nên chưa ôn thẻ được. Cần học thì '
                        'liên hệ quản trị viên để được gán vai trò Học viên.',
                  )
                else
                  AsyncValueView<SrsSummary>(
                    value: summary,
                    loading: const _HomeSkeleton(),
                    error: (e) => _SummaryError(error: e, onRetry: () => ref.invalidate(summaryProvider)),
                    data: (s) => _HomeBody(summary: s),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _HomeBody extends ConsumerWidget {
  const _HomeBody({required this.summary});

  final SrsSummary summary;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final s = summary;
    final toStart = s.toStart;
    final nextDue = s.nextDueAt;
    // Chỉ nhắc khi ĐỌC ĐƯỢC múi giờ máy thật (provider trả null khi lỗi — không so với mặc định).
    final deviceTz = ref.watch(deviceTimeZoneProvider).value;
    final tzDiffers = isDeviceTimeZoneDifferent(s.timeZone, deviceTz);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // IntrinsicHeight: ba ô cao bằng nhau trong Column cuộn (stretch trần ⇒ chiều cao vô hạn).
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: _StatCard(label: 'Đến hạn hôm nay', value: '${s.dueToday}'),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _StatCard(
                  label: 'Từ mới còn học được',
                  value: '${s.newAvailableToday}/${s.dailyNewCards}',
                  hint: s.newIntroducedToday > 0 ? 'Đã học ${s.newIntroducedToday} từ mới hôm nay' : null,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _StatCard(label: 'Đã ôn hôm nay', value: '${s.reviewedToday}'),
              ),
            ],
          ),
        ),
        if (s.reviewLimitReached) ...[
          const SizedBox(height: 12),
          AuthBanner(
            message: 'Đã đạt giới hạn ${s.dailyReviewLimit} lượt ôn hôm nay — thẻ còn lại sẽ chờ sang ngày mai.',
          ),
        ],
        const SizedBox(height: 16),
        if (toStart > 0)
          FilledButton.icon(
            key: kStartReviewKey,
            onPressed: () => context.push(AppRoutes.reviewSession),
            icon: const Icon(Icons.play_arrow),
            label: Text('Bắt đầu ôn ($toStart)'),
            style: FilledButton.styleFrom(minimumSize: const Size(0, 56), textStyle: const TextStyle(fontSize: 18)),
          )
        else
          Card(
            color: scheme.primary,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Icon(Icons.check_circle_outline, size: 36, color: scheme.onPrimary),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Hôm nay xong rồi!',
                          style: theme.textTheme.titleMedium?.copyWith(
                            color: scheme.onPrimary,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        Text(
                          nextDue != null
                              ? 'Lượt ôn kế tiếp: ${formatNextDue(nextDue)}'
                              : 'Chưa có thẻ nào chờ — thêm từ ở Tra từ hoặc chờ từ mới ngày mai.',
                          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onPrimary),
                        ),
                        if (nextDue != null && tzDiffers)
                          Text(
                            '(giờ trên máy — hồ sơ dùng múi giờ ${s.timeZone})',
                            style: theme.textTheme.bodySmall?.copyWith(color: scheme.onPrimary.withValues(alpha: 0.85)),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        const SizedBox(height: 12),
        Wrap(
          alignment: WrapAlignment.spaceBetween,
          crossAxisAlignment: WrapCrossAlignment.center,
          runSpacing: 4,
          children: [
            Text.rich(
              TextSpan(
                text: 'Từ vững: ',
                children: [
                  TextSpan(
                    text: '${s.matureCards}',
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  const TextSpan(text: '/$kPathTotal'),
                  if (s.totalCards > 0) TextSpan(text: ' · đang ôn ${s.totalCards} thẻ'),
                ],
              ),
              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
            ),
            TextButton(
              onPressed: () => context.push('${AppRoutes.profile}?tab=hoc-tap'),
              child: const Text('Cài đặt học tập'),
            ),
          ],
        ),
      ],
    );
  }
}

/// Ô số: nhãn nhỏ in hoa + số lớn đậm (+ gợi ý). Ba ô trên một hàng ở 360 px ⇒ số dùng `FittedBox` khi chữ 1,3×.
class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.value, this.hint});

  final String label;
  final String value;
  final String? hint;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(10, 10, 10, 10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label.toUpperCase(),
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
                letterSpacing: 0.5,
              ),
              maxLines: 2,
            ),
            const SizedBox(height: 4),
            FittedBox(
              fit: BoxFit.scaleDown,
              alignment: Alignment.centerLeft,
              child: Text(
                value,
                style: theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
              ),
            ),
            if (hint != null)
              Text(hint!, style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
          ],
        ),
      ),
    );
  }
}

class _HomeSkeleton extends StatelessWidget {
  const _HomeSkeleton();

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.08);
    Widget box(double h) => Container(
      height: h,
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(child: box(88)),
            const SizedBox(width: 8),
            Expanded(child: box(88)),
            const SizedBox(width: 8),
            Expanded(child: box(88)),
          ],
        ),
        const SizedBox(height: 16),
        box(56),
        const SizedBox(height: 12),
        SizedBox(width: 200, child: box(16)),
      ],
    );
  }
}

/// Lỗi tải tóm tắt: 503 (`CONTENT_UNAVAILABLE`) = học liệu chưa sẵn sàng; còn lại theo `ErrorView`.
class _SummaryError extends StatelessWidget {
  const _SummaryError({required this.error, required this.onRetry});

  final ApiError error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ErrorView(
        kind: error.isNetwork
            ? ErrorViewKind.network
            : error.isForbidden
            ? ErrorViewKind.forbidden
            : ErrorViewKind.unknown,
        title: error.status == 503 ? 'Học liệu chưa sẵn sàng' : 'Không tải được số thẻ đến hạn',
        message: error.message,
        onRetry: onRetry,
        compact: true,
      ),
    );
  }
}
