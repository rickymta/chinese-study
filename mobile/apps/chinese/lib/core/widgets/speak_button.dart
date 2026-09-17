import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../speech/chinese_speech.dart';

/// Tooltip khi chưa có giọng (cùng chuỗi với web).
const kNoVoiceTooltip = 'Chưa có giọng tiếng Trung';

/// Dạng nút nghe.
enum SpeakButtonVariant { icon, button }

/// Nút nghe dùng chung (port `SpeakButton.tsx`): gọi `speakZh` trong `onPressed`; chưa có giọng/không hỗ trợ ⇒ vô
/// hiệu + tooltip [kNoVoiceTooltip]. Rời màn khi nút này đang phát ⇒ huỷ đọc (hợp đồng §5.3.7 "huỷ khi rời màn").
class SpeakButton extends ConsumerStatefulWidget {
  const SpeakButton({
    super.key,
    required this.text,
    this.variant = SpeakButtonVariant.icon,
    this.label = 'Nghe',
    this.rate,
    this.disabled = false,
    this.onDone,
    this.iconSize,
    this.tooltip,
  });

  /// Chữ Hán cần đọc.
  final String text;
  final SpeakButtonVariant variant;

  /// Nhãn dạng `button` (chuỗi tiếng Việt; muốn nhãn chữ Hán thì đặt [tooltip] và dùng `HanziText` bên ngoài).
  final String label;

  /// Tốc độ ghi đè (nút "Nghe thử" đọc theo giá trị đang kéo). Bỏ trống ⇒ tốc độ đã lưu.
  final double? rate;
  final bool disabled;

  /// Gọi sau khi phát xong (hoặc bị ngắt).
  final VoidCallback? onDone;
  final double? iconSize;

  /// Tooltip khi phát được (mặc định "Nghe" + chữ cần đọc).
  final String? tooltip;

  @override
  ConsumerState<SpeakButton> createState() => _SpeakButtonState();
}

class _SpeakButtonState extends ConsumerState<SpeakButton> {
  bool _speakingHere = false;

  /// Giữ notifier từ lúc build — `ref.read` trong `dispose` không hợp lệ (widget đã rời cây).
  SpeechController? _controller;

  @override
  void dispose() {
    // Chỉ huỷ khi chính nút này khởi phát lượt đọc — không cắt lượt của nút khác còn trên màn. Hoãn một tick:
    // Riverpod cấm đổi state provider ngay trong dispose (cây đang finalize).
    final controller = _controller;
    if (_speakingHere && controller != null) scheduleMicrotask(controller.cancel);
    super.dispose();
  }

  Future<void> _handlePressed() async {
    _speakingHere = true;
    try {
      await speakZh(ref, widget.text, rate: widget.rate);
    } finally {
      _speakingHere = false;
      widget.onDone?.call();
    }
  }

  @override
  Widget build(BuildContext context) {
    final speech = ref.watch(speechControllerProvider);
    // Watch để tốc độ đã lưu được nạp từ kho trước khi người dùng bấm (Notifier chỉ khởi tạo khi có người đọc).
    ref.watch(ttsRateProvider);
    _controller = ref.read(speechControllerProvider.notifier);
    final canSpeak = speech.canSpeak && !widget.disabled;
    final onPressed = canSpeak ? _handlePressed : null;
    final icon = Icon(
      speech.speaking && _speakingHere ? Icons.volume_up : Icons.volume_up_outlined,
      size: widget.iconSize,
    );
    final semantics = '${widget.label} ${widget.text}';
    final Widget control = switch (widget.variant) {
      SpeakButtonVariant.icon => IconButton(
        onPressed: onPressed,
        icon: icon,
        color: Theme.of(context).colorScheme.primary,
        // Vùng chạm ≥ 48 kể cả khi icon nhỏ.
        constraints: const BoxConstraints(minWidth: 48, minHeight: 48),
        tooltip: canSpeak ? (widget.tooltip ?? semantics) : null,
      ),
      SpeakButtonVariant.button => OutlinedButton.icon(onPressed: onPressed, icon: icon, label: Text(widget.label)),
    };
    if (!canSpeak && !widget.disabled) {
      // Tooltip bọc ngoài để vẫn hiện trên nút bị vô hiệu (bấm/giữ).
      return Tooltip(message: kNoVoiceTooltip, triggerMode: TooltipTriggerMode.tap, child: control);
    }
    return Semantics(label: semantics, button: true, child: control);
  }
}
