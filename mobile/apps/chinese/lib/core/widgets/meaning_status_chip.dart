import 'package:flutter/material.dart';

/// Tooltip của chip nghĩa dịch máy (cùng chuỗi với `MeaningStatusChip.tsx` web).
const kMachineMeaningTooltip = 'Nghĩa dịch máy, có thể chưa chính xác — đối chiếu nghĩa tiếng Anh';

/// Chip "Chưa duyệt" cho nghĩa Việt `machine` (D5: mọi nghĩa đều dịch máy/CVDICT tới khi duyệt ở F10).
/// [status] khác `machine` ⇒ không vẽ gì. Chạm/giữ hiện tooltip giải thích.
class MeaningStatusChip extends StatelessWidget {
  const MeaningStatusChip({
    super.key,
    required this.status,
    this.compact = true,
    this.triggerMode = TooltipTriggerMode.tap,
  });

  /// `machine` | `reviewed` | null.
  final String? status;

  /// Chip nhỏ (mặc định) cho dòng danh sách; `false` ⇒ cỡ thường.
  final bool compact;

  /// Cách mở tooltip: trang chi tiết dùng chạm (mặc định); dòng danh sách dùng `longPress` để chạm vào chip vẫn mở từ
  /// (review M8).
  final TooltipTriggerMode triggerMode;

  @override
  Widget build(BuildContext context) {
    if (status != 'machine') return const SizedBox.shrink();
    final scheme = Theme.of(context).colorScheme;
    return Tooltip(
      message: kMachineMeaningTooltip,
      triggerMode: triggerMode,
      child: Chip(
        label: const Text('Chưa duyệt'),
        visualDensity: compact ? VisualDensity.compact : VisualDensity.standard,
        materialTapTargetSize: compact ? MaterialTapTargetSize.shrinkWrap : MaterialTapTargetSize.padded,
        labelStyle: TextStyle(color: scheme.tertiary, fontSize: compact ? 12 : null),
        side: BorderSide(color: scheme.tertiary),
        backgroundColor: Colors.transparent,
        padding: compact ? const EdgeInsets.symmetric(horizontal: 4) : null,
      ),
    );
  }
}
