import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../application/sign_out.dart';

/// Nút "Về trang chủ" dùng chung cho trang lỗi.
class HomeButton extends StatelessWidget {
  const HomeButton({super.key});

  @override
  Widget build(BuildContext context) => OutlinedButton.icon(
    onPressed: () => context.go(AppRoutes.home),
    icon: const Icon(Icons.home_outlined),
    label: const Text('Về trang chủ'),
  );
}

/// Nút "Đăng xuất" dùng chung (luồng RM-S7 qua `signOutFlow`).
class SignOutButton extends ConsumerWidget {
  const SignOutButton({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => OutlinedButton.icon(
    onPressed: () => signOutFlow(context, ref),
    icon: const Icon(Icons.logout),
    label: const Text('Đăng xuất'),
  );
}

/// `/401` — chưa đăng nhập (lời gọi trả 401 mà không làm mới được): nút Đăng nhập; đã đăng nhập thì Đăng xuất.
class UnauthorizedPage extends ConsumerWidget {
  const UnauthorizedPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final signedIn = ref.watch(authControllerProvider).isAuthenticated;
    return Scaffold(
      appBar: AppBar(title: Text(ErrorView.defaultTitle(ErrorViewKind.unauthorized))),
      body: ErrorView(
        kind: ErrorViewKind.unauthorized,
        actions: [
          if (signedIn)
            const SignOutButton()
          else
            FilledButton.icon(
              onPressed: () => context.go(AppRoutes.login),
              icon: const Icon(Icons.login),
              label: const Text('Đăng nhập'),
            ),
        ],
      ),
    );
  }
}

/// `/403` — đã đăng nhập nhưng thiếu quyền (RM-S5: mọi màn học cần `study.use`): lời giải thích + Đăng xuất.
class ForbiddenPage extends ConsumerWidget {
  const ForbiddenPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);
    final account = state.account;
    final hasStudy = state is AuthAuthenticated && state.hasPermission(kStudyUsePermission);
    final who = account == null ? '' : ' (${account.email})';
    final message = hasStudy
        ? 'Tài khoản của bạn$who không có quyền xem nội dung này.'
        : 'Tài khoản của bạn$who chưa được cấp quyền học ("study.use") ở dịch vụ tiếng Trung, nên chưa dùng được các '
              'màn học. Hãy liên hệ quản trị viên để được cấp quyền, hoặc đăng xuất để dùng tài khoản khác.';
    return Scaffold(
      appBar: AppBar(title: Text(ErrorView.defaultTitle(ErrorViewKind.forbidden))),
      body: ErrorView(
        kind: ErrorViewKind.forbidden,
        message: message,
        actions: [if (hasStudy) const HomeButton(), const SignOutButton()],
      ),
    );
  }
}

/// `/404` và `errorBuilder` — giữ nút quay lại khi được `push` (GET 404 — DB-M14).
class NotFoundPage extends StatelessWidget {
  const NotFoundPage({super.key});

  @override
  Widget build(BuildContext context) {
    final canPop = context.canPop();
    return Scaffold(
      appBar: AppBar(title: Text(ErrorView.defaultTitle(ErrorViewKind.notFound))),
      body: ErrorView(
        kind: ErrorViewKind.notFound,
        actions: [
          if (canPop)
            OutlinedButton.icon(
              onPressed: () => context.pop(),
              icon: const Icon(Icons.arrow_back),
              label: const Text('Quay lại'),
            ),
          const HomeButton(),
        ],
      ),
    );
  }
}
