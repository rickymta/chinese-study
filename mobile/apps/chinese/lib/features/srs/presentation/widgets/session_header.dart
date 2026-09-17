import 'package:flutter/material.dart';

/// Đầu màn ôn (port `SessionHeader.tsx`): nút đóng (X) + thanh tiến độ + "đã ôn/tổng".
class SessionHeader extends StatelessWidget {
  const SessionHeader({super.key, required this.done, required this.total, required this.onClose});

  /// Số thẻ đã chấm trong phiên.
  final int done;

  /// Tổng thẻ trong bộ bài hiện tại (tăng khi tải thêm).
  final int total;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final fraction = total > 0 ? (done / total).clamp(0.0, 1.0) : 0.0;
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(4, 4, 16, 4),
        child: Row(
          children: [
            IconButton(
              key: const ValueKey('session-close'),
              tooltip: 'Kết thúc phiên',
              onPressed: onClose,
              icon: const Icon(Icons.close),
              constraints: const BoxConstraints(minWidth: 48, minHeight: 48),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Semantics(
                label: 'Tiến độ phiên',
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(value: fraction, minHeight: 8),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Text(
              '$done/$total',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
