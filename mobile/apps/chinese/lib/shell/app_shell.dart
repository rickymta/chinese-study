import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/progress/application/providers.dart';
import '../features/srs/application/providers.dart';

/// Nhãn 5 nhánh — thứ tự giống web (Trang chủ, Ôn tập, Bài học, Luyện viết, còn lại vào "Thêm").
const kShellDestinations = <AfNavDestination>[
  AfNavDestination(label: 'Trang chủ', icon: Icons.home_outlined, selectedIcon: Icons.home),
  AfNavDestination(label: 'Ôn tập', icon: Icons.style_outlined, selectedIcon: Icons.style),
  AfNavDestination(label: 'Bài học', icon: Icons.menu_book_outlined, selectedIcon: Icons.menu_book),
  AfNavDestination(label: 'Luyện viết', icon: Icons.draw_outlined, selectedIcon: Icons.draw),
  AfNavDestination(label: 'Thêm', icon: Icons.more_horiz),
];

/// Chỉ số nhánh Trang chủ / Ôn tập trong [kShellDestinations].
const kHomeBranch = 0;
const kReviewBranch = 1;

/// Khung 5 nhánh của `StatefulShellRoute.indexedStack`. Mỗi trang tự dựng `AppBar` riêng (shell không có tiêu đề)
/// để nút hành động của từng trang (làm mới, lọc...) nằm đúng chỗ.
///
/// Huy hiệu "Ôn tập" = `dueNow + newAvailableToday` từ tóm tắt SRS (`reviewBadgeProvider` ở `features/srs`, M6 chốt một
/// nguồn; `AfShellScaffold` cắt "99+"); [reviewBadge] truyền tường minh để test/ghi đè, null ⇒ đọc provider. Chọn lại tab Trang chủ đang mở ⇒ về
/// gốc nhánh + làm mới tổng quan (hợp đồng §5.3.8).
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell, this.reviewBadge});

  final StatefulNavigationShell navigationShell;
  final int? reviewBadge;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final int badge = reviewBadge ?? ref.watch<int>(reviewBadgeProvider);
    final destinations = [
      for (var i = 0; i < kShellDestinations.length; i++)
        if (i == kReviewBranch && badge > 0)
          AfNavDestination(
            label: kShellDestinations[i].label,
            icon: kShellDestinations[i].icon,
            selectedIcon: kShellDestinations[i].selectedIcon,
            badgeCount: badge,
          )
        else
          kShellDestinations[i],
    ];
    return AfShellScaffold(
      destinations: destinations,
      selectedIndex: navigationShell.currentIndex,
      onDestinationSelected: (i) {
        final reselect = i == navigationShell.currentIndex;
        if (reselect && i == kHomeBranch) ref.invalidateProgressOverview();
        if (reselect && i == kReviewBranch) ref.invalidateSrsSummary();
        // Chọn lại nhánh đang mở ⇒ về trang gốc của nhánh (initialLocation) — giống bấm lại tab trên web.
        navigationShell.goBranch(i, initialLocation: reselect);
      },
      body: navigationShell,
    );
  }
}
