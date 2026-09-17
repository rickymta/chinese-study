import 'package:flutter/material.dart';

import 'af_tts.dart';

/// Tên ngôn ngữ dùng trong lời hướng dẫn (mặc định "tiếng Trung").
const kVoiceMissingDefaultLanguageName = 'tiếng Trung';

/// Lời hướng dẫn cài giọng theo nền tảng (hợp đồng §5.3.7). Hàm thuần để test và để màn khác dùng lại.
String voiceInstallInstructions(TtsPlatform platform, {String languageName = kVoiceMissingDefaultLanguageName}) =>
    switch (platform) {
      TtsPlatform.android =>
        'Cài đặt → Hệ thống → Ngôn ngữ → Đầu ra chuyển văn bản sang lời nói → Dịch vụ của Google → '
            'Cài dữ liệu giọng nói → ${_capitalize(languageName)}.',
      TtsPlatform.ios => 'Cài đặt → Trợ năng → Nội dung được đọc → Giọng nói → ${_capitalize(languageName)}.',
      TtsPlatform.web =>
        'Dùng Chrome/Edge có giọng $languageName (macOS: Cài đặt hệ thống → Trợ năng → Nội dung '
            'được đọc → Giọng nói hệ thống).',
      TtsPlatform.other =>
        'Cài giọng $languageName trong phần Trợ năng / Chuyển văn bản sang lời nói của hệ điều hành.',
    };

String _capitalize(String s) => s.isEmpty ? s : '${s[0].toUpperCase()}${s.substring(1)}';

/// Dải hướng dẫn khi thiếu giọng (`noVoice`) hoặc thiết bị không hỗ trợ TTS (`unsupported`). `loading`/`ready`
/// ⇒ không vẽ gì. Tương đương thông báo ở web khi `useSpeech` trả `no-voice`/`unsupported`.
class VoiceMissingNotice extends StatelessWidget {
  const VoiceMissingNotice({
    super.key,
    required this.status,
    this.platform,
    this.languageName = kVoiceMissingDefaultLanguageName,
    this.onRetry,
  });

  final SpeechStatus status;

  /// Mặc định suy từ nền tảng đang chạy.
  final TtsPlatform? platform;
  final String languageName;

  /// Nút "Dò lại giọng" sau khi người dùng cài xong (không bắt buộc).
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    if (status == SpeechStatus.ready || status == SpeechStatus.loading) return const SizedBox.shrink();
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final unsupported = status == SpeechStatus.unsupported;
    final title = unsupported ? 'Thiết bị không hỗ trợ đọc văn bản' : 'Chưa có giọng $languageName';
    final body = unsupported
        ? 'Không tìm thấy bộ đọc văn bản trên thiết bị/trình duyệt này. Nút nghe sẽ bị tắt.'
        : 'Cài giọng $languageName rồi mở lại ứng dụng: ${voiceInstallInstructions(platform ?? TtsPlatform.current(), languageName: languageName)}';
    return Material(
      color: scheme.tertiaryContainer,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.volume_off_outlined, color: scheme.onTertiaryContainer),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(title, style: theme.textTheme.titleSmall?.copyWith(color: scheme.onTertiaryContainer)),
                      const SizedBox(height: 4),
                      Text(body, style: theme.textTheme.bodySmall?.copyWith(color: scheme.onTertiaryContainer)),
                    ],
                  ),
                ),
              ],
            ),
            if (onRetry != null && !unsupported)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: onRetry,
                  icon: const Icon(Icons.refresh),
                  label: const Text('Dò lại giọng'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
