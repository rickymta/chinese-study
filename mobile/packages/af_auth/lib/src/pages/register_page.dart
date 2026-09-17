import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../auth_controller.dart';
import '../auth_errors.dart';
import '../auth_state.dart';
import '../models.dart';
import '../time_zones.dart';
import '../validators.dart';
import '../widgets/auth_gate.dart';
import '../widgets/auth_scaffold.dart';
import '../widgets/password_field.dart';

/// Múi giờ máy — `FutureProvider` để trang đăng ký hiện dòng "Múi giờ: … (theo máy)"; plugin lỗi ⇒ `Asia/Ho_Chi_Minh`.
/// Test override bằng `deviceTimeZoneProvider.overrideWith((_) async => 'Asia/Ho_Chi_Minh')`.
final deviceTimeZoneProvider = FutureProvider<String>((ref) => deviceTimeZone());

/// Trang đăng ký DÙNG CHUNG (hợp đồng mobile §5.3.6): tên hiển thị, email, mật khẩu, nhập lại; `timeZone` =
/// múi giờ máy (quy bí danh); 409 `EMAIL_TAKEN` gắn vào ô email; 403 `REGISTRATION_CLOSED` hiện banner.
class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key, required this.brand, this.loginPath = '/dang-nhap', this.logo});

  final String brand;
  final String loginPath;
  final Widget? logo;

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _displayName = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  String? _formError;
  final Map<String, String?> _fieldErrors = {};
  bool _submitting = false;

  @override
  void dispose() {
    _displayName.dispose();
    _email.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    setState(() {
      _formError = null;
      _fieldErrors.clear();
    });
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _submitting = true);
    final timeZone = await ref.read(deviceTimeZoneProvider.future).then((tz) => tz, onError: (_) => kDefaultTimeZone);
    try {
      await ref
          .read(authControllerProvider.notifier)
          .register(
            email: _email.text.trim(),
            password: _password.text,
            displayName: _displayName.text.trim(),
            timeZone: timeZone,
          );
    } on Object catch (err) {
      final view = describeAuthError(err);
      if (!mounted) return;
      setState(() {
        _fieldErrors
          ..['email'] = view['email']
          ..['password'] = view['password']
          ..['displayName'] = view['displayName'];
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
    final theme = Theme.of(context);
    final tz = ref.watch(deviceTimeZoneProvider).asData?.value ?? kDefaultTimeZone;

    return AuthScaffold(
      brand: widget.brand,
      title: 'Tạo tài khoản',
      logo: widget.logo,
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_formError != null) ...[AuthBanner(message: _formError!, isError: true), const SizedBox(height: 16)],
              TextFormField(
                controller: _displayName,
                autofocus: true,
                enabled: !_submitting,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.name],
                textCapitalization: TextCapitalization.words,
                validator: validateDisplayName,
                autovalidateMode: AutovalidateMode.onUserInteraction,
                decoration: InputDecoration(labelText: 'Tên hiển thị', errorText: _fieldErrors['displayName']),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _email,
                enabled: !_submitting,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                autocorrect: false,
                textCapitalization: TextCapitalization.none,
                validator: validateEmail,
                autovalidateMode: AutovalidateMode.onUserInteraction,
                decoration: InputDecoration(labelText: 'Email', errorText: _fieldErrors['email']),
              ),
              const SizedBox(height: 16),
              PasswordField(
                label: 'Mật khẩu (8–128 ký tự)',
                controller: _password,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.newPassword],
                textInputAction: TextInputAction.next,
                validator: validatePassword,
                errorText: _fieldErrors['password'],
              ),
              const SizedBox(height: 16),
              PasswordField(
                label: 'Nhập lại mật khẩu',
                controller: _confirm,
                enabled: !_submitting,
                autofillHints: const [AutofillHints.newPassword],
                validator: (v) => validateConfirmPassword(v, _password.text),
                onFieldSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: 12),
              Text(
                'Múi giờ: $tz (theo máy) — dùng để tính "hôm nay" cho thẻ ôn tập và chuỗi ngày học; đổi lại được ở Hồ sơ.',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Đăng ký'),
              ),
              const SizedBox(height: 16),
              Wrap(
                alignment: WrapAlignment.center,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text('Đã có tài khoản?', style: theme.textTheme.bodyMedium),
                  TextButton(
                    onPressed: () => context.go(uri.replace(path: widget.loginPath).toString()),
                    child: const Text('Đăng nhập'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
