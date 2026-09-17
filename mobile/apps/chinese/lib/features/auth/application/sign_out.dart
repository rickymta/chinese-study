import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_providers.dart';

/// Luồng đăng xuất DUY NHẤT của app (RM-S7): còn đánh giá ôn thẻ chưa gửi ⇒ hỏi xác nhận; đồng ý ⇒
/// `AuthController.logout()` (gọi `/mobile/logout` tối đa 5 s, xoá kho, hook app) ⇒ redirect đưa về `/dang-nhap`.
///
/// Dùng ở "Thêm", `/401`, `/403` và màn "Không kết nối được máy chủ" — không gọi `logout()` rải rác.
Future<void> signOutFlow(BuildContext context, WidgetRef ref) async {
  final pending = ref.read(pendingOutboxCountProvider);
  if (pending > 0) {
    final ok = await showAfConfirm(
      context: context,
      title: 'Đăng xuất?',
      message: 'Còn $pending đánh giá chưa gửi — đăng xuất sẽ bỏ chúng.',
      confirmLabel: 'Đăng xuất',
      destructive: true,
    );
    if (!ok) return;
  }
  await ref.read(authControllerProvider.notifier).logout();
}
