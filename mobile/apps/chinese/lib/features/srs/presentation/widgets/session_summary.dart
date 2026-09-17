import 'package:flutter/material.dart';

import '../../data/models.dart';
import '../../domain/ratings.dart';
import '../../domain/session_deck.dart';
import 'rating_bar.dart';

/// Màn kết thúc phiên (port `SessionSummary.tsx`): tổng lượt, thời gian, % nhớ, thanh ngang 4 mức + số từng mức,
/// "Ôn tiếp" khi server còn thẻ, "Về trang ôn tập".
class SessionSummary extends StatelessWidget {
  const SessionSummary({
    super.key,
    required this.counts,
    required this.durationMs,
    required this.canContinue,
    required this.onContinue,
    required this.onHome,
  });

  final Map<SrsRating, int> counts;
  final int durationMs;

  /// Server còn thẻ (`dueNow + newAvailableToday > 0`) ⇒ hiện "Ôn tiếp".
  final bool canContinue;
  final VoidCallback onContinue;
  final VoidCallback onHome;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final total = SrsRating.values.fold<int>(0, (sum, r) => sum + (counts[r] ?? 0));
    final correct = total - (counts[SrsRating.again] ?? 0);
    final subtitle = StringBuffer('$total lượt · ${formatSessionDuration(durationMs)}');
    if (total > 0) subtitle.write(' · nhớ ${(correct / total * 100).round()}%');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          total == 0 ? 'Không có thẻ để ôn' : 'Xong phiên ôn!',
          textAlign: TextAlign.center,
          style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 4),
        Text(
          subtitle.toString(),
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
        ),
        if (total > 0) ...[
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  // Thanh ngang xếp chồng theo 4 mức.
                  Semantics(
                    label: SrsRating.values.map((r) => '${kRatingMeta[r]!.label} ${counts[r] ?? 0}').join(', '),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(6),
                      child: SizedBox(
                        height: 12,
                        child: Row(
                          children: [
                            for (final r in SrsRating.values)
                              if ((counts[r] ?? 0) > 0)
                                Expanded(
                                  flex: counts[r]!,
                                  child: ColoredBox(color: ratingBackground(scheme, kRatingMeta[r]!.color)),
                                ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      for (final r in SrsRating.values)
                        Expanded(
                          child: Column(
                            children: [
                              Text(
                                '${counts[r] ?? 0}',
                                style: theme.textTheme.titleLarge?.copyWith(
                                  fontWeight: FontWeight.w700,
                                  color: ratingBackground(scheme, kRatingMeta[r]!.color),
                                ),
                              ),
                              Text(
                                kRatingMeta[r]!.label,
                                style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
        const SizedBox(height: 16),
        if (canContinue) ...[
          FilledButton(
            onPressed: onContinue,
            style: FilledButton.styleFrom(minimumSize: const Size(0, 56), textStyle: const TextStyle(fontSize: 18)),
            child: const Text('Ôn tiếp'),
          ),
          const SizedBox(height: 8),
          OutlinedButton(
            onPressed: onHome,
            style: OutlinedButton.styleFrom(minimumSize: const Size(0, 56)),
            child: const Text('Về trang ôn tập'),
          ),
        ] else
          FilledButton(
            onPressed: onHome,
            style: FilledButton.styleFrom(minimumSize: const Size(0, 56), textStyle: const TextStyle(fontSize: 18)),
            child: const Text('Về trang ôn tập'),
          ),
      ],
    );
  }
}
