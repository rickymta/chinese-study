import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

import '../../../../../core/widgets/hanzi_big.dart';
import '../../../../../core/widgets/pinyin_text.dart';
import '../../../../../core/widgets/speak_button.dart';
import '../../../data/models.dart';
import '../../../domain/drill_types.dart';

/// Key nút "Gửi lại" / "Làm bài mới" / "Xem thống kê" (test).
const kRetrySubmitKey = ValueKey('drill-retry-submit');
const kNewDrillKey = ValueKey('drill-new');
const kViewStatsKey = ValueKey('drill-view-stats');

/// Kết quả phiên (port `DrillResult.tsx`): điểm x/20, theo thanh, câu sai (nghe lại), "Làm bài mới"/"Xem thống kê".
/// Gửi lỗi mạng/5xx ⇒ banner + "Gửi lại" (CÙNG `clientSessionId` — idempotent); 400/422 ⇒ thông điệp server + chỉ
/// "Làm bài mới". Kết quả tạm tính ở client hiển thị trong lúc chờ server; có kết quả server thì ưu tiên số server.
class DrillResult extends StatelessWidget {
  const DrillResult({
    super.key,
    required this.outcome,
    required this.submitting,
    required this.onRetry,
    required this.onNewDrill,
    required this.onViewStats,
    this.serverResult,
    this.submitError,
  });

  final DrillOutcome outcome;
  final SubmitToneDrillResponse? serverResult;
  final bool submitting;
  final ApiError? submitError;
  final VoidCallback onRetry;
  final VoidCallback onNewDrill;
  final VoidCallback onViewStats;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant);
    final total = outcome.total;
    final correct = outcome.correct;
    final byTone = summarizeByTone(outcome.answers);
    final wrong = outcome.wrong;
    final err = submitError;
    final isRejected = err != null && (err.status == 422 || err.status == 400);
    final server = serverResult;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                Text('KẾT QUẢ', style: theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant)),
                const SizedBox(height: 4),
                Text(
                  '${server?.correct ?? correct}/${server?.total ?? total}',
                  key: const ValueKey('drill-score'),
                  style: theme.textTheme.displayMedium?.copyWith(fontWeight: FontWeight.w800, height: 1),
                ),
                const SizedBox(height: 8),
                Text(
                  correct == total
                      ? 'Tuyệt vời — đúng hết!'
                      : correct >= total * 0.8
                      ? 'Khá tốt, xem lại các câu sai bên dưới.'
                      : 'Chưa vững — nghe lại các câu sai rồi làm thêm một bài nữa.',
                  textAlign: TextAlign.center,
                  style: muted,
                ),
                const SizedBox(height: 8),
                if (submitting)
                  const Chip(
                    avatar: SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2)),
                    label: Text('Đang lưu kết quả…'),
                  )
                else if (server != null)
                  Chip(
                    avatar: Icon(Icons.check, size: 18, color: scheme.primary),
                    label: Text('Đã lưu · ngày học ${server.localDate}'),
                    side: BorderSide(color: scheme.primary),
                  ),
                const Divider(height: 32),
                Row(
                  children: [
                    for (var i = 0; i < kDrillTones.length; i++) ...[
                      if (i > 0) const SizedBox(width: 8),
                      Expanded(
                        child: Builder(
                          builder: (context) {
                            final t = kDrillTones[i];
                            final s = server?.byTone[t] ?? byTone[t] ?? const ToneCount();
                            return Container(
                              padding: const EdgeInsets.symmetric(vertical: 8),
                              decoration: BoxDecoration(
                                border: Border.all(color: scheme.outlineVariant),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: Column(
                                children: [
                                  Text('Thanh $t', style: theme.textTheme.labelSmall?.copyWith(color: muted?.color)),
                                  Text(
                                    s.total == 0 ? '—' : '${s.correct}/${s.total}',
                                    style: const TextStyle(fontWeight: FontWeight.w700),
                                  ),
                                ],
                              ),
                            );
                          },
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ),
        if (err != null) ...[
          const SizedBox(height: 12),
          Material(
            color: scheme.errorContainer,
            borderRadius: BorderRadius.circular(10),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    isRejected ? 'Máy chủ từ chối kết quả: ${err.message}' : 'Chưa lưu được kết quả: ${err.message}',
                    style: TextStyle(color: scheme.onErrorContainer),
                  ),
                  if (!isRejected)
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton.icon(
                        key: kRetrySubmitKey,
                        onPressed: submitting ? null : onRetry,
                        icon: const Icon(Icons.refresh),
                        label: const Text('Gửi lại'),
                        style: TextButton.styleFrom(foregroundColor: scheme.onErrorContainer),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
        if (wrong.isNotEmpty) ...[
          const SizedBox(height: 12),
          SectionCard(
            title: 'Câu sai (${wrong.length})',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var i = 0; i < wrong.length; i++) ...[
                  if (i > 0) const Divider(height: 16),
                  _WrongRow(answer: wrong[i]),
                ],
              ],
            ),
          ),
        ],
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: FilledButton.icon(
                key: kNewDrillKey,
                onPressed: onNewDrill,
                icon: const Icon(Icons.replay),
                label: const Text('Làm bài mới'),
                style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: OutlinedButton.icon(
                key: kViewStatsKey,
                onPressed: onViewStats,
                icon: const Icon(Icons.bar_chart),
                label: const Text('Xem thống kê'),
                style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _WrongRow extends StatelessWidget {
  const _WrongRow({required this.answer});

  final AnsweredItem answer;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final parts = answer.item.parts;
    final text = answer.item.text;
    return Row(
      children: [
        ConstrainedBox(
          constraints: const BoxConstraints(minWidth: 56),
          child: HanziBig(text, size: HanziSize.lg),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              PinyinText(
                parts.map((p) => '${p.syllable}${p.tone}').join(' '),
                style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
              ),
              Text(
                'Đúng: thanh ${parts.map((p) => p.tone).join('–')} · bạn chọn: thanh ${answer.answered.join('–')}',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ],
          ),
        ),
        SpeakButton(text: text),
      ],
    );
  }
}
