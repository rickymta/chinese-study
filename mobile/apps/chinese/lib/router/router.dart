import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/system/presentation/pages/home_page.dart';
import '../shell/app_shell.dart';
import '../shell/coming_soon_page.dart';
import '../shell/more_page.dart';
import 'routes.dart';

export 'routes.dart';

/// Navigator gốc — màn toàn màn hình (phiên ôn, bảng viết, trang lỗi) đẩy lên đây để ẩn bottom nav.
final rootNavigatorKey = GlobalKey<NavigatorState>(debugLabel: 'root');

/// GoRouter của app. M0: chưa có redirect xác thực (M2 thêm `authRedirect` + `refreshListenable`).
///
/// Web dev dùng hash URL mặc định (DB-M16) — không gọi `usePathUrlStrategy()`.
final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    navigatorKey: rootNavigatorKey,
    initialLocation: AppRoutes.home,
    routes: [
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => AppShell(navigationShell: navigationShell),
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
            ],
          ),
        ],
      ),
      // Trang lỗi thống nhất (toàn màn hình, ngoài shell). M2 bổ sung nút Đăng xuất ở /403.
      GoRoute(
        path: AppRoutes.unauthorized,
        parentNavigatorKey: rootNavigatorKey,
        builder: (context, _) => const _ErrorPage(kind: ErrorViewKind.unauthorized),
      ),
      GoRoute(
        path: AppRoutes.forbidden,
        parentNavigatorKey: rootNavigatorKey,
        builder: (context, _) => const _ErrorPage(kind: ErrorViewKind.forbidden),
      ),
      GoRoute(
        path: AppRoutes.notFound,
        parentNavigatorKey: rootNavigatorKey,
        builder: (context, _) => const _ErrorPage(kind: ErrorViewKind.notFound),
      ),
    ],
    errorBuilder: (context, state) => const _ErrorPage(kind: ErrorViewKind.notFound),
  );
  ref.onDispose(router.dispose);
  return router;
});

/// Trang lỗi toàn màn: `ErrorView` + nút về trang chủ.
class _ErrorPage extends StatelessWidget {
  const _ErrorPage({required this.kind});

  final ErrorViewKind kind;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(ErrorView.defaultTitle(kind))),
      body: ErrorView(
        kind: kind,
        actions: [
          OutlinedButton.icon(
            onPressed: () => context.go(AppRoutes.home),
            icon: const Icon(Icons.home_outlined),
            label: const Text('Về trang chủ'),
          ),
        ],
      ),
    );
  }
}
