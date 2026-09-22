import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth_controller.dart';
import '../auth_state.dart';

/// Màn chờ lúc khởi động (AuthLoading) — không điều hướng, giữ nguyên URL đích (deep link) tới khi biết phiên.
class AuthSplash extends StatelessWidget {
  const AuthSplash({super.key, this.label = 'Đang nạp phiên đăng nhập…'});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 16),
            Text(label, style: Theme.of(context).textTheme.bodyMedium),
          ],
        ),
      ),
    );
  }
}

/// Màn "Không kết nối được máy chủ" (AuthUnreachable): có phiên trong kho nhưng identity/service ngôn ngữ không
/// trả lời — KHÔNG đăng xuất (RM-S3). Thử lại ⇒ `bootstrap()`; Đăng xuất ⇒ [onLogout].
class AuthUnreachableView extends StatelessWidget {
  const AuthUnreachableView({super.key, required this.message, required this.onRetry, required this.onLogout});

  final String message;
  final VoidCallback onRetry;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Không kết nối được máy chủ')),
      body: ErrorView(
        kind: ErrorViewKind.network,
        message: '$message\nPhiên đăng nhập của bạn vẫn được giữ.',
        onRetry: onRetry,
        actions: [
          OutlinedButton.icon(onPressed: onLogout, icon: const Icon(Icons.logout), label: const Text('Đăng xuất')),
        ],
      ),
    );
  }
}

/// Cổng phiên cho cây widget cần đăng nhập (shell 5 nhánh, màn toàn màn hình):
/// - [AuthLoading] / [AuthAnonymous] ⇒ [AuthSplash] (redirect của router đang/ sẽ đưa về đăng nhập);
/// - [AuthUnreachable] ⇒ [AuthUnreachableView];
/// - [AuthAuthenticated] ⇒ [child].
///
/// Nhờ đó trang bên trong KHÔNG được dựng (không gọi API) trước khi có phiên.
class AuthGate extends ConsumerWidget {
  const AuthGate({super.key, required this.child, this.onLogout});

  final Widget child;

  /// Đăng xuất từ màn không kết nối; null ⇒ `AuthController.logout()` trực tiếp (app truyền luồng có xác nhận outbox).
  final Future<void> Function(BuildContext context, WidgetRef ref)? onLogout;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);
    return switch (state) {
      AuthAuthenticated() => child,
      AuthUnreachable(:final message) => AuthUnreachableView(
        message: message,
        onRetry: () => ref.read(authControllerProvider.notifier).bootstrap(),
        onLogout: () {
          final custom = onLogout;
          if (custom != null) {
            custom(context, ref);
          } else {
            ref.read(authControllerProvider.notifier).logout();
          }
        },
      ),
      AuthLoading() || AuthAnonymous() => const AuthSplash(),
    };
  }
}
