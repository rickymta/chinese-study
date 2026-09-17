import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../data/me_api.dart';

/// Kho phiên bền (secure storage). Test ghi đè bằng `InMemoryTokenStore`.
final tokenStoreProvider = Provider<TokenStore>((ref) => SecureTokenStore());

/// Bộ lời gọi identity mobile (Dio có `X-AF-Client`).
final Provider<IdentityClient> identityClientProvider = Provider<IdentityClient>(
  (ref) => IdentityClient(ref.watch(identityDioProvider)),
);

/// Phiên xác thực dùng chung cho MỌI Dio client (`api/clients.dart` đọc lười qua `ref.read` để không tạo vòng
/// phụ thuộc: Dio identity cần `session.refresh`, session cần Dio identity để gọi `/auth/mobile/refresh`).
final Provider<AuthSession> authSessionProvider = Provider<AuthSession>((ref) {
  final session = AuthSession(
    refreshCall: (token) => ref.read(identityClientProvider).refresh(token),
    store: ref.watch(tokenStoreProvider),
  );
  ref.onDispose(session.dispose);
  return session;
});

/// `deviceName` gửi khi đăng nhập/đăng ký (RM-A6) — [BA-mặc định] `"<platform> app"`, không thêm plugin đọc model máy.
String deviceNameForPlatform() {
  if (kIsWeb) return 'Web dev';
  return switch (defaultTargetPlatform) {
    TargetPlatform.android => 'Android app',
    TargetPlatform.iOS => 'iOS app',
    _ => 'Desktop app',
  };
}

/// Số đánh giá ôn thẻ CHƯA GỬI của người dùng hiện tại — M6 thay bằng outbox thật (RM-S7). M2: luôn 0.
final pendingOutboxCountProvider = Provider<int>((ref) => 0);

/// Phụ thuộc cho `AuthController` của app tiếng Trung — `main.dart`/test: `authDepsProvider.overrideWith(buildChineseAuthDeps)`.
AuthDeps buildChineseAuthDeps(Ref ref) {
  return AuthDeps(
    identity: ref.watch(identityClientProvider),
    session: ref.watch(authSessionProvider),
    loadMe: () => loadMe(ref.read(chineseDioProvider)),
    installGuard: InstallGuard(prefs: ref.watch(keyValueStoreProvider), tokenStore: ref.watch(tokenStoreProvider)),
    deviceName: deviceNameForPlatform(),
    // Sau đăng xuất/mất phiên: provider theo người dùng tự huỷ qua `userScopeProvider`; M6 thêm xoá outbox của userId.
    onSignedOut: (userId) async {},
  );
}
