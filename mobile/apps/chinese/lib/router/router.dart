import 'package:af_auth/af_auth.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/application/sign_out.dart';
import '../features/auth/presentation/pages/error_pages.dart';
import '../features/speech/presentation/pages/voice_settings_page.dart';
import '../features/system/presentation/pages/home_page.dart';
import '../shell/app_shell.dart';
import '../shell/coming_soon_page.dart';
import '../shell/more_page.dart';
import 'routes.dart';

export 'routes.dart';

/// Tên app hiện trên màn đăng nhập/đăng ký.
const kBrand = 'AntFarm · Tiếng Trung';

/// Navigator gốc — màn toàn màn hình (phiên ôn, bảng viết, trang lỗi, đăng nhập) đẩy lên đây để ẩn bottom nav.
final rootNavigatorKey = GlobalKey<NavigatorState>(debugLabel: 'root');

/// GoRouter của app: `redirect` = `authRedirect` (hợp đồng mobile §5.3.8), chạy lại mỗi khi `AuthState` đổi qua
/// `refreshListenable`. Cây cần đăng nhập bọc trong `AuthGate` (splash / không kết nối / nội dung).
///
/// Web dev dùng hash URL mặc định (DB-M16) — không gọi `usePathUrlStrategy()`.
final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    navigatorKey: rootNavigatorKey,
    initialLocation: AppRoutes.home,
    refreshListenable: authRefreshListenable(ref),
    redirect: (context, state) => authRedirect(ref.read(authControllerProvider), state.uri),
    routes: [
      GoRoute(
        path: AppRoutes.login,
        parentNavigatorKey: rootNavigatorKey,
        builder: (_, _) => const LoginPage(brand: kBrand, registerPath: AppRoutes.register),
      ),
      GoRoute(
        path: AppRoutes.register,
        parentNavigatorKey: rootNavigatorKey,
        builder: (_, _) => const RegisterPage(brand: kBrand, loginPath: AppRoutes.login),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => AuthGate(
          onLogout: signOutFlow,
          child: AppShell(navigationShell: navigationShell),
        ),
        branches: [
          StatefulShellBranch(
            routes: [GoRoute(path: AppRoutes.home, builder: (_, _) => const HomePage())],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.review,
                builder: (_, _) => const ComingSoonPage(title: 'Ôn tập', icon: Icons.style_outlined),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.lessons,
                builder: (_, _) => const ComingSoonPage(title: 'Bài học', icon: Icons.menu_book_outlined),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.writing,
                builder: (_, _) => const ComingSoonPage(title: 'Luyện viết', icon: Icons.draw_outlined),
              ),
            ],
          ),
          // Nhánh "Thêm": các trang con là đường dẫn cấp cao (`/pinyin?tab=luyen` liên kết được từ "Việc hôm nay"),
          // `push` trong nhánh này để giữ bottom nav (hợp đồng §5.3.8).
          StatefulShellBranch(
            initialLocation: AppRoutes.more,
            routes: [
              GoRoute(path: AppRoutes.more, builder: (_, _) => const MorePage()),
              GoRoute(
                path: AppRoutes.pinyin,
                builder: (_, _) => const ComingSoonPage(title: 'Pinyin', icon: Icons.record_voice_over_outlined),
              ),
              GoRoute(
                path: AppRoutes.dictionary,
                builder: (_, _) => const ComingSoonPage(title: 'Tra từ', icon: Icons.search),
              ),
              GoRoute(
                path: AppRoutes.profile,
                builder: (_, _) => const ComingSoonPage(title: 'Hồ sơ', icon: Icons.person_outline),
              ),
              GoRoute(
                path: AppRoutes.licenses,
                builder: (_, _) => const ComingSoonPage(title: 'Giấy phép & nguồn', icon: Icons.gavel_outlined),
              ),
              GoRoute(path: AppRoutes.voice, builder: (_, _) => const VoiceSettingsPage()),
            ],
          ),
        ],
      ),
      // Trang lỗi thống nhất (toàn màn hình, ngoài shell).
      GoRoute(
        path: AppRoutes.unauthorized,
        parentNavigatorKey: rootNavigatorKey,
        builder: (_, _) => const UnauthorizedPage(),
      ),
      GoRoute(
        path: AppRoutes.forbidden,
        parentNavigatorKey: rootNavigatorKey,
        builder: (_, _) => const ForbiddenPage(),
      ),
      GoRoute(path: AppRoutes.notFound, parentNavigatorKey: rootNavigatorKey, builder: (_, _) => const NotFoundPage()),
    ],
    errorBuilder: (context, state) => const NotFoundPage(),
  );
  ref.onDispose(router.dispose);
  return router;
});
