import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../auth/application/auth_providers.dart';
import '../pages/profile_page.dart';

/// Mật khẩu mới phải khác mật khẩu hiện tại (validator phía app — server cũng kiểm `PASSWORD_UNCHANGED`).
String? validateNewPassword(String? value, String current) {
  final base = validatePassword(value);
  if (base != null) return base;
  if (value == current) return 'Mật khẩu mới phải khác mật khẩu hiện tại';
  return null;
}

/// Tab "Mật khẩu" (port `ChangePasswordForm.tsx`, RM-A8): `POST /auth/mobile/password` kèm refresh token hiện tại để
/// server giữ họ này. `currentSessionKept=true` ⇒ xoá form + toast (thiết bị khác bị đăng xuất); `false` ⇒ đăng xuất
/// cục bộ ⇒ router đưa về `/dang-nhap?reason=password-changed`. `422 WRONG_PASSWORD` ⇒ lỗi ô hiện tại;
/// `PASSWORD_UNCHANGED` ⇒ lỗi ô mới (qua `describeAuthError`).
class ChangePasswordTab extends ConsumerStatefulWidget {
  const ChangePasswordTab({super.key});

  @override
  ConsumerState<ChangePasswordTab> createState() => _ChangePasswordTabState();
}

class _ChangePasswordTabState extends ConsumerState<ChangePasswordTab> {
  final _formKey = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  String? _formError;
  String? _currentError;
  String? _nextError;
  bool _submitting = false;

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    setState(() {
      _formError = null;
      _currentError = null;
      _nextError = null;
    });
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _submitting = true);
    try {
      final session = ref.read(authSessionProvider);
      // Chờ lời gọi làm mới đang bay (nếu có) để gửi refresh token MỚI NHẤT — token cũ đã xoay thì server không nhận
      // ra họ hiện tại ⇒ thu hồi cả phiên này oan.
      await session.waitForInflight();
      final result = await ref
          .read(identityClientProvider)
          .changePassword(currentPassword: _current.text, newPassword: _next.text, refreshToken: session.refreshToken);
      if (!mounted) return;
      if (result.currentSessionKept) {
        // Xoá controller TRƯỚC rồi mới reset(): clear() sau reset() kích hoạt autovalidate (đã tương tác) ⇒ ba ô đỏ
        // "Vui lòng nhập…" ngay sau khi đổi thành công (phát hiện khi chạy thật M4).
        _current.clear();
        _next.clear();
        _confirm.clear();
        _formKey.currentState?.reset();
        showAfToast(
          context,
          result.otherSessionsRevoked > 0 ? 'Đã đổi mật khẩu. Các thiết bị khác đã bị đăng xuất.' : 'Đã đổi mật khẩu.',
          kind: AfToastKind.success,
        );
        return;
      }
      // Mọi phiên (kể cả phiên này) đã bị thu hồi ⇒ đăng xuất cục bộ; redirect của router thêm reason=password-changed.
      await ref.read(authControllerProvider.notifier).signOutLocally(AuthLostReason.passwordChanged);
    } on Object catch (err) {
      final view = describeAuthError(err, context: AuthErrorContext.session);
      if (!mounted) return;
      setState(() {
        _currentError = view['currentPassword'];
        _nextError = view['newPassword'];
        // Lỗi đã gắn vào ô (WRONG_PASSWORD / PASSWORD_UNCHANGED) thì không lặp lại ở dải lỗi.
        if (view.error.code != 'WRONG_PASSWORD' && view.error.code != 'PASSWORD_UNCHANGED') {
          _formError = view.message;
        }
      });
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ProfileTabBody(
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_formError != null) ...[AuthBanner(message: _formError!, isError: true), const SizedBox(height: 16)],
              PasswordField(
                label: 'Mật khẩu hiện tại',
                controller: _current,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.password],
                textInputAction: TextInputAction.next,
                validator: (v) => (v ?? '').isEmpty ? 'Vui lòng nhập mật khẩu hiện tại' : null,
                errorText: _currentError,
              ),
              const SizedBox(height: 16),
              PasswordField(
                label: 'Mật khẩu mới (8–128 ký tự)',
                controller: _next,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.newPassword],
                textInputAction: TextInputAction.next,
                validator: (v) => validateNewPassword(v, _current.text),
                errorText: _nextError,
              ),
              const SizedBox(height: 16),
              PasswordField(
                label: 'Nhập lại mật khẩu mới',
                controller: _confirm,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.newPassword],
                validator: (v) => (v ?? '').isEmpty
                    ? 'Vui lòng nhập lại mật khẩu mới'
                    : (v != _next.text ? 'Mật khẩu nhập lại không khớp' : null),
                onFieldSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: 12),
              Text(
                'Sau khi đổi, các thiết bị khác đang đăng nhập (kể cả bản web) sẽ bị đăng xuất; thiết bị này vẫn dùng '
                'tiếp.',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: 20),
              Align(
                alignment: Alignment.centerRight,
                child: FilledButton(
                  onPressed: _submitting ? null : _submit,
                  child: _submitting
                      ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Đổi mật khẩu'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
