import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../auth_controller.dart';
import '../auth_errors.dart';
import '../auth_state.dart';
import '../validators.dart';
import '../widgets/auth_gate.dart';
import '../widgets/auth_scaffold.dart';
import '../widgets/password_field.dart';

/// Thông điệp banner theo `?reason=` (port `LoginPage.tsx`).
String? loginReasonMessage(AuthLostReason? reason) => switch (reason) {
  AuthLostReason.expired => 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại để tiếp tục.',
  AuthLostReason.passwordChanged => 'Bạn vừa đổi mật khẩu. Vui lòng đăng nhập lại bằng mật khẩu mới.',
  null => null,
};

/// Trang đăng nhập DÙNG CHUNG mọi app ngôn ngữ (hợp đồng mobile §5.3.6): một cột, `AutofillGroup`, lỗi
/// 401/423/403/429 hiện tại chỗ; `?returnTo=` (chỉ đường dẫn nội bộ) và `?reason=` do redirect của router xử lý —
/// đăng nhập xong `AuthState` đổi ⇒ router tự đưa về `returnTo`/`/`.
class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key, required this.brand, this.registerPath = '/dang-ky', this.logo});

  /// Tên app hiện trên đầu, vd "AntFarm · Tiếng Trung".
  final String brand;

  /// Đường dẫn trang đăng ký; null ⇒ ẩn liên kết (hệ thống đóng đăng ký).
  final String? registerPath;
  final Widget? logo;

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  String? _formError;
  String? _emailError;
  String? _passwordError;
  bool _submitting = false;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    setState(() {
      _formError = null;
      _emailError = null;
      _passwordError = null;
    });
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _submitting = true);
    try {
      await ref.read(authControllerProvider.notifier).login(email: _email.text.trim(), password: _password.text);
      // Thành công ⇒ AuthState = authenticated ⇒ redirect của router đưa về returnTo/`/`.
    } on Object catch (err) {
      final view = describeAuthError(err);
      if (!mounted) return;
      setState(() {
        _emailError = view['email'];
        _passwordError = view['password'];
        _formError = view.message;
      });
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);
    if (state is AuthLoading) return const AuthSplash();

    final uri = GoRouterState.of(context).uri;
    final reason =
        AuthLostReason.fromQuery(uri.queryParameters['reason']) ??
        switch (state) {
          AuthAnonymous(:final reason) => reason,
          _ => null,
        };
    final reasonMessage = loginReasonMessage(reason);
    final theme = Theme.of(context);

    return AuthScaffold(
      brand: widget.brand,
      title: 'Đăng nhập',
      logo: widget.logo,
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (reasonMessage != null && _formError == null) ...[
                AuthBanner(message: reasonMessage),
                const SizedBox(height: 16),
              ],
              if (_formError != null) ...[AuthBanner(message: _formError!, isError: true), const SizedBox(height: 16)],
              TextFormField(
                controller: _email,
                autofocus: true,
                enabled: !_submitting,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                autocorrect: false,
                textCapitalization: TextCapitalization.none,
                validator: validateEmail,
                autovalidateMode: AutovalidateMode.onUserInteraction,
                decoration: InputDecoration(labelText: 'Email', errorText: _emailError),
              ),
              const SizedBox(height: 16),
              PasswordField(
                label: 'Mật khẩu',
                controller: _password,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.password],
                validator: validateLoginPassword,
                errorText: _passwordError,
                onFieldSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Đăng nhập'),
              ),
              if (widget.registerPath != null) ...[
                const SizedBox(height: 16),
                Wrap(
                  alignment: WrapAlignment.center,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Text('Chưa có tài khoản?', style: theme.textTheme.bodyMedium),
                    TextButton(
                      // Giữ nguyên query (returnTo) khi sang trang đăng ký.
                      onPressed: () => context.go(uri.replace(path: widget.registerPath).toString()),
                      child: const Text('Đăng ký'),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
