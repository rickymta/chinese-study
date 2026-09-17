import 'package:flutter/material.dart';

/// Loại thông báo ngắn.
enum AfToastKind { info, success, warning, error }

/// Thông báo ngắn ở đáy màn (SnackBar) — dùng cho kết quả lời gọi ghi (POST/PUT) báo tại chỗ.
///
/// Cần `ScaffoldMessenger` phía trên (MaterialApp có sẵn). Gọi khi không có ⇒ bỏ qua, không ném.
void showAfToast(
  BuildContext context,
  String message, {
  AfToastKind kind = AfToastKind.info,
  Duration duration = const Duration(seconds: 3),
  String? actionLabel,
  VoidCallback? onAction,
}) {
  final messenger = ScaffoldMessenger.maybeOf(context);
  if (messenger == null) return;
  final scheme = Theme.of(context).colorScheme;
  final (Color bg, Color fg) = switch (kind) {
    AfToastKind.info => (scheme.inverseSurface, scheme.onInverseSurface),
    AfToastKind.success => (scheme.primary, scheme.onPrimary),
    AfToastKind.warning => (scheme.tertiaryContainer, scheme.onTertiaryContainer),
    AfToastKind.error => (scheme.error, scheme.onError),
  };
  messenger
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(message, style: TextStyle(color: fg)),
        backgroundColor: bg,
        duration: duration,
        action: actionLabel == null
            ? null
            : SnackBarAction(label: actionLabel, textColor: fg, onPressed: onAction ?? () {}),
      ),
    );
}
