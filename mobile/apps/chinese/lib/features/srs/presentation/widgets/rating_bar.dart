import 'package:flutter/material.dart';

import '../../data/models.dart';
import '../../domain/format_interval.dart';
import '../../domain/ratings.dart';

/// Màu nền nút theo [RatingColor] (quy palette MUI của web sang `ColorScheme` + màu cố định cho warning/info).
Color ratingBackground(ColorScheme scheme, RatingColor color) => switch (color) {
  RatingColor.error => scheme.error,
  RatingColor.warning => scheme.brightness == Brightness.dark ? const Color(0xFFFFB74D) : const Color(0xFFED6C02),
  RatingColor.success => scheme.primary,
  RatingColor.info => scheme.brightness == Brightness.dark ? const Color(0xFF4FC3F7) : const Color(0xFF0288D1),
};

Color ratingForeground(ColorScheme scheme, RatingColor color) => switch (color) {
  RatingColor.error => scheme.onError,
  RatingColor.success => scheme.onPrimary,
  RatingColor.warning ||
  RatingColor.info => scheme.brightness == Brightness.dark ? const Color(0xFF1A1A1A) : Colors.white,
};

/// 4 nút chấm Quên/Khó/Được/Dễ — 4 cột bằng nhau, mỗi nút cao ≥ 56, hai dòng: nhãn đậm + khoảng dự kiến
/// (`formatInterval`). Ở 360 px mỗi nút rộng ~80 ⇒ vẫn đủ cho "5,5 phút". [disabled] khoá 300 ms sau khi chấm.
class RatingBar extends StatelessWidget {
  const RatingBar({super.key, required this.intervals, required this.onRate, this.disabled = false});

  final Map<SrsRating, String?> intervals;
  final ValueChanged<SrsRating> onRate;
  final bool disabled;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      container: true,
      label: 'Chấm thẻ',
      child: Row(
        children: [
          for (var i = 0; i < SrsRating.values.length; i++) ...[
            if (i > 0) const SizedBox(width: 8),
            Expanded(
              child: _RatingButton(
                rating: SrsRating.values[i],
                interval: formatInterval(intervals[SrsRating.values[i]]),
                background: ratingBackground(scheme, kRatingMeta[SrsRating.values[i]]!.color),
                foreground: ratingForeground(scheme, kRatingMeta[SrsRating.values[i]]!.color),
                onPressed: disabled ? null : () => onRate(SrsRating.values[i]),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _RatingButton extends StatelessWidget {
  const _RatingButton({
    required this.rating,
    required this.interval,
    required this.background,
    required this.foreground,
    required this.onPressed,
  });

  final SrsRating rating;
  final String interval;
  final Color background;
  final Color foreground;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    final meta = kRatingMeta[rating]!;
    return Semantics(
      button: true,
      label: '${meta.label} — $interval',
      child: FilledButton(
        key: ValueKey('rate-${rating.apiValue}'),
        onPressed: onPressed,
        style: FilledButton.styleFrom(
          backgroundColor: background,
          foregroundColor: foreground,
          minimumSize: const Size(0, 56),
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 6),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(meta.label, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16), maxLines: 1),
            FittedBox(
              fit: BoxFit.scaleDown,
              child: Text(
                interval,
                style: TextStyle(fontSize: 12, color: foreground.withValues(alpha: 0.92)),
                maxLines: 1,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
