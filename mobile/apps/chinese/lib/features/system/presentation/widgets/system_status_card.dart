import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../application/providers.dart';
import '../../data/models.dart';

/// Thẻ "Trạng thái hệ thống" (tương đương F1 web): hai chip Tiếng Trung / Tài khoản gọi `GET /system/info`
/// của từng service qua gateway; lỗi ⇒ chip đỏ + hướng dẫn chạy đủ backend ở máy dev, app không trắng.
class SystemStatusCard extends ConsumerWidget {
  const SystemStatusCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final chinese = ref.watch(systemInfoProvider(SystemService.chinese));
    final identity = ref.watch(systemInfoProvider(SystemService.identity));
    final failed = [
      if (chinese.hasError) SystemService.chinese.serviceName,
      if (identity.hasError) SystemService.identity.serviceName,
    ];
    final theme = Theme.of(context);

    return SectionCard(
      title: 'Trạng thái hệ thống',
      subtitle: 'Qua gateway',
      trailing: IconButton(
        tooltip: 'Kiểm tra lại',
        icon: const Icon(Icons.refresh),
        onPressed: () {
          ref.invalidate(systemInfoProvider(SystemService.chinese));
          ref.invalidate(systemInfoProvider(SystemService.identity));
        },
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _ServiceStatusChip(service: SystemService.chinese, value: chinese),
              _ServiceStatusChip(service: SystemService.identity, value: identity),
            ],
          ),
          if (failed.isNotEmpty) ...[
            const SizedBox(height: 12),
            DecoratedBox(
              decoration: BoxDecoration(
                color: theme.colorScheme.errorContainer.withValues(alpha: 0.5),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Không tới được: ${failed.join(', ')}', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    Text(
                      'Ở máy dev cần chạy đủ 3 tiến trình theo thứ tự identity-service → chinese-backend → gateway '
                      '(cổng 5281, 5282, 5280) rồi app này (web-dev 3291). Bấm vào chip hoặc nút làm mới để thử lại.',
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Chip trạng thái một service: đang kiểm tra → đang chạy (kèm version/môi trường) → lỗi (kèm thông điệp).
class _ServiceStatusChip extends ConsumerWidget {
  const _ServiceStatusChip({required this.service, required this.value});

  final SystemService service;
  final AsyncValue<SystemInfo> value;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final scheme = Theme.of(context).colorScheme;
    final v = value;
    if (v.isLoading && !v.hasValue && !v.hasError) {
      return Chip(
        avatar: const SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2)),
        label: Text('${service.label}: đang kiểm tra…'),
      );
    }
    if (v.hasError) {
      final message = ApiError.from(v.error!).message;
      return Tooltip(
        message: '$message — bấm để thử lại',
        child: ActionChip(
          avatar: Icon(Icons.error_outline, color: scheme.error, size: 18),
          label: Text('${service.label}: lỗi'),
          side: BorderSide(color: scheme.error),
          onPressed: v.isLoading ? null : () => ref.invalidate(systemInfoProvider(service)),
        ),
      );
    }
    final info = v.requireValue;
    return Tooltip(
      message: '${info.service} v${info.version} · ${info.environment}',
      child: Chip(
        avatar: Icon(Icons.check_circle_outline, color: scheme.primary, size: 18),
        label: Text('${service.label}: đang chạy'),
        side: BorderSide(color: scheme.primary),
      ),
    );
  }
}
