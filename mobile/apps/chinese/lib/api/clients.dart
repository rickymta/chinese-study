import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config_provider.dart';
import '../router/router.dart';

/// Client gọi identity-service (`<AF_IDENTITY_API_URL>`), luôn kèm header `X-AF-Client` (RM-A5).
///
/// M0: chưa có phiên — `getAccessToken`/`refresh`/`onAuthLost` để trống, M2 (af_auth) gắn `AuthSession`.
final identityDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  return createApiClient(baseUrl: config.identityApiUrl, headers: {kClientHeaderName: ref.watch(clientHeaderProvider)});
});

/// Client gọi chinese-backend (`<AF_CHINESE_API_URL>`).
///
/// GET 403 ⇒ `go('/403')`; GET 404 ⇒ `push('/404')` (giữ nút quay lại — DB-M14). Lời gọi nền dùng
/// `afOptions(skipErrorRedirect: true)` để không kéo người dùng khỏi trang đang xem.
final chineseDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  return createApiClient(
    baseUrl: config.chineseApiUrl,
    onErrorRedirect: (status, req) {
      final router = ref.read(routerProvider);
      if (status == 403) {
        router.go(AppRoutes.forbidden);
      } else if (status == 404) {
        router.push(AppRoutes.notFound);
      }
    },
  );
});
