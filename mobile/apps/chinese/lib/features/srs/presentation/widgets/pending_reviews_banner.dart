import 'package:flutter/material.dart';

/// Dải cảnh báo khi còn đánh giá gửi HỎNG ít nhất một lần (`unsentCount`, mất mạng/5xx) — không nháy khi chấm bình
/// thường (port `PendingReviewsBanner.tsx`). [count] ≤ 0 ⇒ không vẽ.
class PendingReviewsBanner extends StatelessWidget {
  const PendingReviewsBanner({super.key, required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    if (count <= 0) return const SizedBox.shrink();
    final scheme = Theme.of(context).colorScheme;
    return Container(
      key: const ValueKey('pending-reviews-banner'),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(color: scheme.tertiaryContainer, borderRadius: BorderRadius.circular(8)),
      child: Row(
        children: [
          Icon(Icons.cloud_upload_outlined, size: 20, color: scheme.onTertiaryContainer),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              'Đang chờ gửi $count đánh giá — sẽ tự gửi lại khi có mạng.',
              style: TextStyle(color: scheme.onTertiaryContainer),
            ),
          ),
        ],
      ),
    );
  }
}
