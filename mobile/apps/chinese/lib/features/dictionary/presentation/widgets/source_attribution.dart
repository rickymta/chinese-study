import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../domain/source_labels.dart';

/// Thứ tự ghi công cố định theo hợp đồng §5.3.1 web — không phụ thuộc `sources[]` của từng từ.
const kAttributionOrder = ['hsk30-official', 'cc-cedict', 'cvdict', 'unihan', 'han-viet-curated'];

/// Dòng chữ nhỏ cuối các trang từ điển (port `SourceAttribution.tsx`): ghi công nguồn + giấy phép cho CẢ bộ dữ liệu
/// (nghĩa vụ CC BY-SA 4.0: ghi công, ghi đã chỉnh sửa, cùng giấy phép). Chạm ⇒ `/giay-phep` (có URL đầy đủ).
class SourceAttribution extends StatelessWidget {
  const SourceAttribution({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final parts = [
      for (final key in kAttributionOrder)
        if (sourceLabel(key) case final s?) '${s.label} (${s.license})',
    ];
    return InkWell(
      onTap: () => context.push(AppRoutes.licenses),
      borderRadius: BorderRadius.circular(8),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Text(
          'Nguồn: ${parts.join(' · ')}. Dữ liệu đã được chỉnh sửa — xem Giấy phép & nguồn.',
          style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant, height: 1.5),
        ),
      ),
    );
  }
}
