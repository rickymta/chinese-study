import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

import '../../../data/models.dart';

/// Key nút "Bắt đầu (20 câu)" (test).
const kStartDrillKey = ValueKey('start-drill');

/// Màn thiết lập bài luyện (port `DrillSetup.tsx`): chọn chế độ (`SegmentedButton` Một âm tiết / Cặp thanh — khởi đầu
/// từ `?che-do=mot|cap`), dòng "tập trung thanh …", nút "Bắt đầu (20 câu)". Không có giọng ⇒ khoá nút kèm lời giải
/// thích (không nghe được thì không luyện được).
class DrillSetup extends StatelessWidget {
  const DrillSetup({
    super.key,
    required this.mode,
    required this.onModeChange,
    required this.focus,
    required this.onStart,
    required this.speechStatus,
    this.disabledReason,
  });

  final DrillMode mode;
  final ValueChanged<DrillMode> onModeChange;

  /// `recommendedFocus` từ thống kê (rỗng ⇒ ngẫu nhiên đều).
  final List<int> focus;
  final VoidCallback onStart;
  final SpeechStatus speechStatus;

  /// Chưa tải xong bảng / học liệu lỗi ⇒ không bắt đầu được (kèm lý do).
  final String? disabledReason;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final muted = theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant);
    final canSpeak = speechStatus == SpeechStatus.ready;
    final noVoice = speechStatus == SpeechStatus.noVoice || speechStatus == SpeechStatus.unsupported;

    return SectionCard(
      title: 'Luyện nghe – chọn thanh',
      subtitle: 'Nghe một chữ (hoặc hai chữ liền nhau) rồi chọn thanh. Mỗi bài 20 câu, khoảng 3–5 phút.',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SegmentedButton<DrillMode>(
            segments: [
              for (final m in DrillMode.values)
                ButtonSegment(
                  value: m,
                  label: Text(m.label),
                  icon: Icon(m == DrillMode.listenTone ? Icons.looks_one_outlined : Icons.looks_two_outlined),
                ),
            ],
            selected: {mode},
            showSelectedIcon: false,
            style: const ButtonStyle(minimumSize: WidgetStatePropertyAll(Size(0, 48))),
            onSelectionChanged: (s) => onModeChange(s.first),
          ),
          const SizedBox(height: 12),
          Text(
            mode == DrillMode.listenTone
                ? 'Mỗi câu phát một chữ, bạn chọn thanh 1–4.'
                : 'Mỗi câu phát hai chữ liền nhau, bạn chọn thanh cho từng chữ. Không có cặp 3-3.',
            style: muted,
          ),
          if (focus.isNotEmpty) ...[
            const SizedBox(height: 12),
            _Notice(
              icon: Icons.info_outline,
              text:
                  'Bài này sẽ tập trung vào thanh ${focus.join(', ')} (một nửa số câu) — vì đây là thanh bạn hay nghe '
                  'nhầm.',
              background: scheme.secondaryContainer,
              foreground: scheme.onSecondaryContainer,
            ),
          ],
          if (noVoice) ...[
            const SizedBox(height: 12),
            _Notice(
              icon: Icons.volume_off_outlined,
              text:
                  'Máy chưa có giọng tiếng Trung nên không phát được câu hỏi — nút "Bắt đầu" tạm khoá. Cài giọng theo '
                  'hướng dẫn ở đầu trang rồi mở lại ứng dụng.',
              background: scheme.tertiaryContainer,
              foreground: scheme.onTertiaryContainer,
            ),
          ],
          if (disabledReason != null) ...[
            const SizedBox(height: 12),
            _Notice(
              icon: Icons.warning_amber_outlined,
              text: disabledReason!,
              background: scheme.tertiaryContainer,
              foreground: scheme.onTertiaryContainer,
            ),
          ],
          const SizedBox(height: 16),
          FilledButton.icon(
            key: kStartDrillKey,
            onPressed: canSpeak && disabledReason == null ? onStart : null,
            icon: const Icon(Icons.play_arrow),
            label: const Text('Bắt đầu (20 câu)'),
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
              textStyle: const TextStyle(fontSize: 16),
            ),
          ),
        ],
      ),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({required this.icon, required this.text, required this.background, required this.foreground});

  final IconData icon;
  final String text;
  final Color background;
  final Color foreground;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: background,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: foreground, size: 20),
            const SizedBox(width: 8),
            Expanded(
              child: Text(text, style: TextStyle(color: foreground)),
            ),
          ],
        ),
      ),
    );
  }
}
