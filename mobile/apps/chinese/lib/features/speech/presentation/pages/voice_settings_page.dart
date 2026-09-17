import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/speech/chinese_speech.dart';
import '../../../../core/widgets/hanzi_big.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';

/// Câu nghe thử (你好 — "xin chào").
const kVoiceSampleHanzi = '你好';
const kVoiceSamplePinyin = 'ni3 hao3';

/// Màn thử nhanh "Giọng đọc" trong "Thêm" (M3, tạm — M4 chuyển vào Hồ sơ → tab Giao diện): danh sách giọng `zh`,
/// chọn giọng, thanh tốc độ 0,5–1,2, nghe thử "你好". Rời màn ⇒ huỷ đọc.
class VoiceSettingsPage extends ConsumerStatefulWidget {
  const VoiceSettingsPage({super.key});

  @override
  ConsumerState<VoiceSettingsPage> createState() => _VoiceSettingsPageState();
}

class _VoiceSettingsPageState extends ConsumerState<VoiceSettingsPage> {
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
    final speech = ref.watch(speechControllerProvider);
    final rate = ref.watch(ttsRateProvider);
    final notifier = ref.read(speechControllerProvider.notifier);
    _controller = notifier;

    return Scaffold(
      appBar: AppBar(title: const Text('Giọng đọc')),
      body: AfPageBody(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Khối nghe thử: chữ Hán lớn + pinyin + nút nghe (tốc độ đang kéo, chưa cần lưu).
            SectionCard(
              title: 'Nghe thử',
              child: Column(
                children: [
                  const HanziBig(kVoiceSampleHanzi, size: HanziSize.xl, textAlign: TextAlign.center),
                  const PinyinText(kVoiceSamplePinyin, hanzi: kVoiceSampleHanzi, showSandhi: true),
                  const SizedBox(height: 12),
                  SpeakButton(
                    text: kVoiceSampleHanzi,
                    variant: SpeakButtonVariant.button,
                    label: 'Nghe thử',
                    rate: rate,
                  ),
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
              subtitle: 'Mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh',
              child: Row(
                children: [
                  Expanded(
                    child: Slider(
                      value: rate,
                      min: kTtsRateMin,
                      max: kTtsRateMax,
                      divisions: ((kTtsRateMax - kTtsRateMin) / 0.05).round(),
                      label: _fmtRate(rate),
                      onChanged: (v) => ref.read(ttsRateProvider.notifier).setRate(v),
                    ),
                  ),
                  SizedBox(
                    width: 44,
                    child: Text(_fmtRate(rate), textAlign: TextAlign.end, style: theme.textTheme.titleMedium),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            SectionCard(
              title: 'Giọng tiếng Trung',
              subtitle: speech.status == SpeechStatus.ready && speech.voices.isEmpty
                  ? 'Engine không liệt kê giọng — dùng giọng mặc định của máy'
                  : 'Giọng Quảng Đông (zh-HK) đã được loại — chỉ giữ phổ thông',
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
                              title: Text(v.name),
                              subtitle: Text(v.locale),
                              contentPadding: EdgeInsets.zero,
                              // Nghe ngay giọng vừa chọn — thao tác người dùng, hợp lệ với iOS.
                              secondary: SpeakButton(
                                text: kVoiceSampleHanzi,
                                rate: rate,
                                tooltip: 'Nghe thử ${v.name}',
                              ),
                            ),
                        ],
                      ),
                    ),
            ),
            const SizedBox(height: 12),
            Text(
              'Giọng và tốc độ lưu trên máy này. Từ bản sau, tốc độ đồng bộ với cài đặt học tập trên web.',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }

  static String _fmtRate(double v) => v.toStringAsFixed(2).replaceAll('.', ',');
}
