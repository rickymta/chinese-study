import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/speech/chinese_speech.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import 'learning_settings_tab.dart' show viNumber;

/// Tab "Giao diện" (hợp đồng M4): chế độ Hệ thống/Sáng/Tối (chuyển từ "Thêm") + giọng đọc (chuyển từ màn tạm
/// `/giong-doc` của M3): tốc độ (nguồn sự thật `learning-settings`, ghi máy chủ khi thả thanh trượt), danh sách
/// giọng `zh`, nghe thử 你好. Rời màn ⇒ huỷ đọc.
class AppearanceTab extends ConsumerStatefulWidget {
  const AppearanceTab({super.key});

  @override
  ConsumerState<AppearanceTab> createState() => _AppearanceTabState();
}

class _AppearanceTabState extends ConsumerState<AppearanceTab> {
  /// Giữ notifier từ lúc build — `ref.read` trong `dispose` không hợp lệ (widget đã rời cây).
  SpeechController? _controller;

  @override
  void dispose() {
    // Rời màn ⇒ huỷ lượt đang đọc (hợp đồng §5.3.7). Hoãn một tick: Riverpod cấm đổi state provider trong dispose.
    final controller = _controller;
    if (controller != null) scheduleMicrotask(controller.cancel);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final mode = ref.watch(themeModeProvider);
    final speech = ref.watch(speechControllerProvider);
    final rate = ref.watch(ttsRateProvider);
    final notifier = ref.read(speechControllerProvider.notifier);
    _controller = notifier;

    return AfPageBody(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SectionCard(
            title: 'Chế độ giao diện',
            subtitle: 'Lưu trên máy này',
            child: SegmentedButton<ThemeMode>(
              segments: [
                for (final m in ThemeMode.values)
                  ButtonSegment(
                    value: m,
                    label: Text(themeModeLabel(m)),
                    icon: Icon(switch (m) {
                      ThemeMode.system => Icons.brightness_auto_outlined,
                      ThemeMode.light => Icons.light_mode_outlined,
                      ThemeMode.dark => Icons.dark_mode_outlined,
                    }),
                  ),
              ],
              selected: {mode},
              showSelectedIcon: false,
              onSelectionChanged: (s) => ref.read(themeModeProvider.notifier).setMode(s.first),
            ),
          ),
          const SizedBox(height: 12),
          // Khối nghe thử: chữ Hán lớn + pinyin + nút nghe (tốc độ đang kéo).
          SectionCard(
            title: 'Nghe thử',
            child: Column(
              children: [
                const HanziBig(kSampleHanzi, size: HanziSize.xl, textAlign: TextAlign.center),
                const PinyinText(kSamplePinyin, hanzi: kSampleHanzi, showSandhi: true),
                const SizedBox(height: 12),
                SpeakButton(text: kSampleHanzi, variant: SpeakButtonVariant.button, label: 'Nghe thử', rate: rate),
                if (speech.status == SpeechStatus.loading) ...[
                  const SizedBox(height: 12),
                  const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
                      SizedBox(width: 8),
                      Flexible(child: Text('Đang dò giọng đọc…')),
                    ],
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 12),
          VoiceMissingNotice(status: speech.status, onRetry: notifier.refreshVoices),
          if (speech.status == SpeechStatus.noVoice || speech.status == SpeechStatus.unsupported)
            const SizedBox(height: 12),
          SectionCard(
            title: 'Tốc độ đọc',
            subtitle: 'Mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh. Đồng bộ với cài đặt học tập (cả bản web).',
            child: Row(
              children: [
                Expanded(
                  child: Slider(
                    value: rate.clamp(kTtsRateMin, kTtsRateMax),
                    min: kTtsRateMin,
                    max: kTtsRateMax,
                    divisions: ((kTtsRateMax - kTtsRateMin) / 0.05).round(),
                    label: viNumber(rate, 2),
                    onChanged: (v) => ref.read(ttsRateProvider.notifier).setRate(v),
                    // Thả tay mới ghi máy chủ (một lời gọi PUT cho cả lượt kéo).
                    onChangeEnd: (v) => ref.read(ttsRateProvider.notifier).persist(v),
                  ),
                ),
                SizedBox(
                  width: 44,
                  child: Text(viNumber(rate, 2), textAlign: TextAlign.end, style: theme.textTheme.titleMedium),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          SectionCard(
            title: 'Giọng tiếng Trung',
            subtitle: speech.status == SpeechStatus.ready && speech.voices.isEmpty
                ? 'Engine không liệt kê giọng — dùng giọng mặc định của máy'
                : 'Giọng Quảng Đông (zh-HK) đã được loại — chỉ giữ phổ thông. Giọng lưu trên máy này.',
            trailing: IconButton(
              tooltip: 'Dò lại giọng',
              onPressed: speech.status == SpeechStatus.loading ? null : notifier.refreshVoices,
              icon: const Icon(Icons.refresh),
            ),
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
            child: speech.voices.isEmpty
                ? Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text(switch (speech.status) {
                      SpeechStatus.loading => 'Đang dò…',
                      SpeechStatus.ready => 'Không có giọng để chọn.',
                      SpeechStatus.noVoice => 'Chưa có giọng tiếng Trung trên thiết bị này.',
                      SpeechStatus.unsupported => 'Thiết bị không hỗ trợ đọc văn bản.',
                    }, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
                  )
                : RadioGroup<String>(
                    groupValue: speech.voice?.name,
                    onChanged: (name) {
                      if (name == null) return;
                      final v = speech.voices.firstWhere((x) => x.name == name);
                      notifier.setVoice(v);
                    },
                    child: Column(
                      children: [
                        for (final v in speech.voices)
                          RadioListTile<String>(
                            value: v.name,
                            // Tên giọng có thể là chữ Hán (婷婷, 小艺) ⇒ locale zh-CN + phông CJK (review M3).
                            title: HanziText(v.name),
                            subtitle: Text(v.locale),
                            contentPadding: EdgeInsets.zero,
                            // Nghe ngay giọng vừa chọn — thao tác người dùng, hợp lệ với iOS.
                            secondary: SpeakButton(text: kSampleHanzi, rate: rate, tooltip: 'Nghe thử ${v.name}'),
                          ),
                      ],
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}
