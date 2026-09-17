import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Nhãn 5 nhánh — thứ tự giống web (Trang chủ, Ôn tập, Bài học, Luyện viết, còn lại vào "Thêm").
const kShellDestinations = <AfNavDestination>[
  AfNavDestination(label: 'Trang chủ', icon: Icons.home_outlined, selectedIcon: Icons.home),
  AfNavDestination(label: 'Ôn tập', icon: Icons.style_outlined, selectedIcon: Icons.style),
  AfNavDestination(label: 'Bài học', icon: Icons.menu_book_outlined, selectedIcon: Icons.menu_book),
  AfNavDestination(label: 'Luyện viết', icon: Icons.draw_outlined, selectedIcon: Icons.draw),
  AfNavDestination(label: 'Thêm', icon: Icons.more_horiz),
];

/// Khung 5 nhánh của `StatefulShellRoute.indexedStack`. Mỗi trang tự dựng `AppBar` riêng (shell không có tiêu đề)
/// để nút hành động của từng trang (làm mới, lọc...) nằm đúng chỗ.
///
/// Badge "Ôn tập" (`dueNow + newAvailableToday`) do M6 điền; M0 chưa có.
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.navigationShell, this.reviewBadge = 0});

  final StatefulNavigationShell navigationShell;
  final int reviewBadge;

  @override
  Widget build(BuildContext context) {
    final destinations = [
      for (var i = 0; i < kShellDestinations.length; i++)
        if (i == 1 && reviewBadge > 0)
          AfNavDestination(
            label: kShellDestinations[i].label,
            icon: kShellDestinations[i].icon,
            selectedIcon: kShellDestinations[i].selectedIcon,
            badgeCount: reviewBadge,
          )
        else
          kShellDestinations[i],
    ];
    return AfShellScaffold(
      destinations: destinations,
      selectedIndex: navigationShell.currentIndex,
      // Chọn lại nhánh đang mở ⇒ về trang gốc của nhánh (initialLocation) — giống bấm lại tab trên web.
      onDestinationSelected: (i) => navigationShell.goBranch(i, initialLocation: i == navigationShell.currentIndex),
      body: navigationShell,
    );
  }
}
