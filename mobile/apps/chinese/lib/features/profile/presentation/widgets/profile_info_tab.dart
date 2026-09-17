import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../auth/application/auth_providers.dart';
import '../../../progress/application/providers.dart';
import '../pages/profile_page.dart';

/// Tab "Thông tin" (port `ProfileForm.tsx`): email chỉ đọc, tên hiển thị, múi giờ (`TimeZoneField`). Lưu ⇒
/// `PUT /api/account` ⇒ `refreshSession()` (R4-4: token mới mang tên/múi giờ mới ⇒ `/chinese/api/me` đồng bộ ngay)
/// ⇒ toast "Đã lưu hồ sơ". `422 INVALID_TIME_ZONE` ⇒ lỗi dưới ô múi giờ; lỗi khác ⇒ dải lỗi tại chỗ.
class ProfileInfoTab extends ConsumerStatefulWidget {
  const ProfileInfoTab({super.key});

  @override
  ConsumerState<ProfileInfoTab> createState() => _ProfileInfoTabState();
}

class _ProfileInfoTabState extends ConsumerState<ProfileInfoTab> {
  final _formKey = GlobalKey<FormState>();
  final _displayName = TextEditingController();
  String _timeZone = '';
  Account? _baseline;
  String? _formError;
  String? _timeZoneError;
  String? _displayNameError;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _syncBaseline(ref.read(currentAccountProvider));
  }

  @override
  void dispose() {
    _displayName.dispose();
    super.dispose();
  }

  /// Tài khoản đổi (sau refreshSession hoặc tab khác cập nhật) ⇒ đồng bộ lại giá trị mặc định (form "không đổi").
  /// Gọi ngoài build (initState / ref.listen) vì đổi `controller.text` trong build làm TextField setState sai lúc.
  void _syncBaseline(Account? account) {
    if (account == null || account == _baseline) return;
    _baseline = account;
    _displayName.text = account.displayName;
    _timeZone = normalizeTimeZone(account.timeZone);
  }

  bool get _dirty {
    final b = _baseline;
    if (b == null) return false;
    return _displayName.text.trim() != b.displayName || _timeZone != normalizeTimeZone(b.timeZone);
  }

  void _reset() {
    final b = _baseline;
    if (b == null) return;
    setState(() {
      _displayName.text = b.displayName;
      _timeZone = normalizeTimeZone(b.timeZone);
      _formError = null;
      _timeZoneError = null;
      _displayNameError = null;
    });
  }

  Future<void> _submit() async {
    if (_submitting) return;
    setState(() {
      _formError = null;
      _timeZoneError = null;
      _displayNameError = null;
    });
    final tzError = validateTimeZone(_timeZone);
    if (!(_formKey.currentState?.validate() ?? false) || tzError != null) {
      setState(() => _timeZoneError = tzError);
      return;
    }
    setState(() => _submitting = true);
    try {
      await ref
          .read(identityClientProvider)
          .updateAccount(displayName: _displayName.text.trim(), timeZone: normalizeTimeZone(_timeZone));
      final bool synced;
      try {
        synced = await ref.read(authControllerProvider.notifier).refreshSession();
      } on Object catch (refreshErr) {
        // Lưu đã thành công. 401/403 khi làm mới = MẤT PHIÊN (router đưa về đăng nhập) ⇒ không báo thêm;
        // `AuthSessionChanged` = phiên đã bị đăng xuất/đổi người trong lúc làm mới (đang chuyển về đăng nhập) ⇒ cũng
        // im lặng (review M4). Lỗi khác (mạng/5xx): token cũ vẫn dùng tới hạn — báo nhẹ, không coi là lỗi lưu.
        if (refreshErr is AuthSessionChanged || !mounted) return;
        final status = ApiError.from(refreshErr).status;
        if (status == 401 || status == 403) return;
        showAfToast(
          context,
          'Đã lưu hồ sơ, nhưng chưa làm mới được phiên — tên/múi giờ mới sẽ hiện sau khi mở lại ứng dụng.',
          kind: AfToastKind.warning,
        );
        return;
      }
      if (!mounted) return;
      if (!synced) {
        // Token đã xoay nhưng GET /account hoặc /me lỗi mạng ⇒ AuthGate đang hiện "Không kết nối được" — toast phải
        // nói đúng: đã lưu, chưa tải lại được hồ sơ.
        showAfToast(
          context,
          'Đã lưu hồ sơ, nhưng chưa tải lại được hồ sơ học tập — bấm Thử lại hoặc mở lại ứng dụng.',
          kind: AfToastKind.warning,
        );
        return;
      }
      // Múi giờ mới chỉ tới service tiếng Trung qua token mới ⇒ "hôm nay"/chuỗi ngày/thẻ đến hạn đổi: làm mới tổng
      // quan + huy hiệu (M5); M6 thêm `ref.invalidate(srsSummaryProvider)`.
      ref.invalidateProgressOverview();
      showAfToast(context, 'Đã lưu hồ sơ', kind: AfToastKind.success);
    } on Object catch (err) {
      final view = describeAuthError(err, context: AuthErrorContext.session);
      if (!mounted) return;
      setState(() {
        _timeZoneError = view['timeZone'];
        _displayNameError = view['displayName'];
        // Lỗi đã gắn vào ô thì không lặp lại ở dải lỗi (INVALID_TIME_ZONE chỉ hiện dưới ô múi giờ).
        if (view.error.code != 'INVALID_TIME_ZONE') _formError = view.message;
      });
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    ref.listen(currentAccountProvider, (_, next) => setState(() => _syncBaseline(next)));
    final account = ref.watch(currentAccountProvider);
    if (account == null) return const SizedBox.shrink();
    final deviceTz = ref.watch(deviceTimeZoneProvider).value;
    final deviceDiffers = deviceTz != null && deviceTz != _timeZone;
    final dirty = _dirty;

    return ProfileTabBody(
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_formError != null) ...[AuthBanner(message: _formError!, isError: true), const SizedBox(height: 16)],
            TextFormField(
              initialValue: account.email,
              enabled: false,
              decoration: const InputDecoration(
                labelText: 'Email',
                helperText: 'Không đổi được email trong phiên bản này.',
              ),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _displayName,
              enabled: !_submitting,
              textInputAction: TextInputAction.done,
              autofillHints: const [AutofillHints.nickname],
              textCapitalization: TextCapitalization.words,
              validator: validateDisplayName,
              autovalidateMode: AutovalidateMode.onUserInteraction,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                labelText: 'Tên hiển thị',
                helperText: 'Từ 1 đến 100 ký tự.',
                errorText: _displayNameError,
              ),
            ),
            const SizedBox(height: 16),
            TimeZoneField(
              value: _timeZone,
              enabled: !_submitting,
              deviceTimeZone: deviceTz,
              errorText: _timeZoneError,
              helperText:
                  'Múi giờ quyết định lúc nào sang ngày học mới (chuỗi ngày học, thẻ đến hạn). Đổi múi giờ không làm '
                  'thay đổi lịch sử đã ghi.',
              onChanged: (v) => setState(() {
                _timeZone = v;
                _timeZoneError = null;
              }),
            ),
            if (deviceDiffers) ...[
              const SizedBox(height: 12),
              _DeviceTimeZoneBanner(
                deviceTimeZone: deviceTz,
                onUse: _submitting
                    ? null
                    : () => setState(() {
                        _timeZone = deviceTz;
                        _timeZoneError = null;
                      }),
              ),
            ],
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(onPressed: dirty && !_submitting ? _reset : null, child: const Text('Hoàn tác')),
                const SizedBox(width: 8),
                FilledButton(
                  onPressed: dirty && !_submitting ? _submit : null,
                  child: _submitting
                      ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Lưu'),
                ),
              ],
            ),
            if (!dirty) ...[
              const SizedBox(height: 4),
              Text(
                'Chưa có thay đổi.',
                textAlign: TextAlign.end,
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Dải "Thiết bị của bạn đang ở múi giờ X" + nút "Dùng múi giờ này" (port Alert của web).
class _DeviceTimeZoneBanner extends StatelessWidget {
  const _DeviceTimeZoneBanner({required this.deviceTimeZone, required this.onUse});

  final String deviceTimeZone;
  final VoidCallback? onUse;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
      decoration: BoxDecoration(color: scheme.secondaryContainer, borderRadius: BorderRadius.circular(10)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.info_outline, size: 20, color: scheme.onSecondaryContainer),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Thiết bị của bạn đang ở múi giờ $deviceTimeZone.',
                  style: TextStyle(color: scheme.onSecondaryContainer),
                ),
              ),
            ],
          ),
          Align(
            alignment: Alignment.centerRight,
            child: TextButton(onPressed: onUse, child: const Text('Dùng múi giờ này')),
          ),
        ],
      ),
    );
  }
}
