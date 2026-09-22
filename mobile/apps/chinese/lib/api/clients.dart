import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config_provider.dart';
import '../features/auth/application/auth_providers.dart';
import '../router/router.dart';

/// Adapter HTTP tuỳ chọn cho test (widget test override bằng `FakeAdapter`); null ⇒ adapter mặc định của dio.
/// Giữ nguyên chuỗi interceptor (làm mới 401, JSON guard, chuẩn hoá lỗi) trong test — không override cả Dio.
final identityAdapterProvider = Provider<HttpClientAdapter?>((ref) => null);
final chineseAdapterProvider = Provider<HttpClientAdapter?>((ref) => null);

/// Client gọi identity-service (`<AF_IDENTITY_API_URL>`), luôn kèm header `X-AF-Client` (RM-A5).
///
/// Phiên (`AuthSession`) đọc LƯỜI qua `ref.read` trong callback — tránh vòng phụ thuộc lúc dựng provider
/// (session cần client này để gọi `/auth/mobile/refresh`). Bốn endpoint `/auth/mobile/{login,register,refresh,logout}`
/// không kéo theo làm mới khi 401 (af_core loại trừ theo URL); `/auth/mobile/password`, `/account` thì có.
// Kiểu biến khai tường minh để phá vòng suy luận kiểu (identityDio → authSession → identityClient → identityDio).
final Provider<Dio> identityDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  final dio = createApiClient(
    baseUrl: config.identityApiUrl,
    headers: {kClientHeaderName: ref.watch(clientHeaderProvider)},
    getAccessToken: () => ref.read(authSessionProvider).accessToken,
    refresh: () => ref.read(authSessionProvider).refresh(),
    onAuthLost: () => ref.read(authSessionProvider).handleAuthLost(),
  );
  final adapter = ref.watch(identityAdapterProvider);
  if (adapter != null) dio.httpClientAdapter = adapter;
  return dio;
});

/// Client gọi chinese-backend (`<AF_CHINESE_API_URL>`), Bearer từ `AuthSession`; 401 ⇒ làm mới rồi gửi lại một lần;
/// làm mới bị từ chối ⇒ mất phiên (về `/dang-nhap?reason=expired`).
///
/// GET 403 ⇒ `go('/403')`; GET 404 ⇒ `push('/404')` (giữ nút quay lại — DB-M14). Lời gọi nền dùng
/// `afOptions(skipErrorRedirect: true)` để không kéo người dùng khỏi trang đang xem.
final Provider<Dio> chineseDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  final dio = createApiClient(
    baseUrl: config.chineseApiUrl,
    getAccessToken: () => ref.read(authSessionProvider).accessToken,
    refresh: () => ref.read(authSessionProvider).refresh(),
    onAuthLost: () => ref.read(authSessionProvider).handleAuthLost(),
    onErrorRedirect: (status, req) {
      final router = ref.read(routerProvider);
      if (status == 403) {
        router.go(AppRoutes.forbidden);
      } else if (status == 404) {
        router.push(AppRoutes.notFound);
      }
    },
  );
  final adapter = ref.watch(chineseAdapterProvider);
  if (adapter != null) dio.httpClientAdapter = adapter;
  return dio;
});
