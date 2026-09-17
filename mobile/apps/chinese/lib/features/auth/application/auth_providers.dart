import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../srs/application/outbox_controller.dart';
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

/// Số đánh giá ôn thẻ CHƯA GỬI (còn trong outbox, kể cả đang gửi lần đầu) của người dùng hiện tại — hộp xác nhận
/// đăng xuất (RM-S7). CHỜ đọc xong kho bền (`whenRestored`) rồi mới đếm: bấm Đăng xuất ngay khi app vừa mở vẫn
/// không bỏ qua hộp xác nhận (review M6). Nguồn: `reviewOutboxProvider`.
///
/// `autoDispose` + KHÔNG watch state: mỗi lần `ref.read(.future)` tính mới; watch state sẽ làm provider bị vô hiệu
/// đúng lúc `restored` đổi và future đang chờ không bao giờ hoàn tất khi không có listener (signOutFlow treo).
final pendingOutboxCountProvider = FutureProvider.autoDispose<int>(
  // Không dùng `ref` sau khoảng chờ (provider autoDispose có thể đã bị huỷ) — controller tự chờ rồi đếm.
  (ref) => ref.read(reviewOutboxProvider.notifier).pendingCountWhenRestored(),
);

/// Phụ thuộc cho `AuthController` của app tiếng Trung — `main.dart`/test: `authDepsProvider.overrideWith(buildChineseAuthDeps)`.
AuthDeps buildChineseAuthDeps(Ref ref) {
  return AuthDeps(
    identity: ref.watch(identityClientProvider),
    session: ref.watch(authSessionProvider),
    loadMe: () => loadMe(ref.read(chineseDioProvider)),
    installGuard: InstallGuard(prefs: ref.watch(keyValueStoreProvider), tokenStore: ref.watch(tokenStoreProvider)),
    deviceName: deviceNameForPlatform(),
    // Sau đăng xuất/mất phiên: provider theo người dùng tự huỷ qua `userScopeProvider`; outbox ôn thẻ KHÔNG xoá ở
    // đây (mất phiên/đổi mật khẩu ⇒ giữ kho theo userId, cùng người đăng nhập lại sẽ gửi tiếp — RK-M1). Xoá chỉ khi
    // chủ động đăng xuất có xác nhận: `signOutFlow`. Tóm tắt SRS tự chặn khi user đổi (không gọi API không token).
    onSignedOut: (userId) async {},
  );
}
