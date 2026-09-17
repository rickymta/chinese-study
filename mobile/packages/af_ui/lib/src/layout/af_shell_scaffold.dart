import 'package:flutter/material.dart';

/// Một đích ở thanh điều hướng đáy.
class AfNavDestination {
  const AfNavDestination({required this.label, required this.icon, this.selectedIcon, this.badgeCount = 0});

  final String label;
  final IconData icon;
  final IconData? selectedIcon;

  /// Số hiện trên huy hiệu (0 ⇒ ẩn; > 99 ⇒ "99+").
  final int badgeCount;
}

/// Khung màn chính: `AppBar` tiêu đề + nội dung + `NavigationBar` ≤ 5 đích có huy hiệu (hợp đồng §5.3.7).
///
/// App tiếng Trung dùng với `StatefulShellRoute.indexedStack` của go_router: [body] là `navigationShell`,
/// [selectedIndex] = `navigationShell.currentIndex`, [onDestinationSelected] gọi `goBranch`.
class AfShellScaffold extends StatelessWidget {
  const AfShellScaffold({
    super.key,
    required this.destinations,
    required this.selectedIndex,
    required this.onDestinationSelected,
    required this.body,
    this.title,
    this.actions,
  }) : assert(destinations.length >= 2 && destinations.length <= 5, 'NavigationBar nhận 2–5 đích');

  final List<AfNavDestination> destinations;
  final int selectedIndex;
  final ValueChanged<int> onDestinationSelected;
  final Widget body;

  /// Tiêu đề AppBar; null ⇒ không có AppBar (trang con tự dựng).
  final String? title;
  final List<Widget>? actions;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: title == null ? null : AppBar(title: Text(title!), actions: actions),
      body: body,
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex,
        onDestinationSelected: onDestinationSelected,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        destinations: [
          for (final d in destinations)
            NavigationDestination(
              label: d.label,
              icon: _badged(Icon(d.icon), d.badgeCount),
              selectedIcon: _badged(Icon(d.selectedIcon ?? d.icon), d.badgeCount),
              tooltip: d.label,
            ),
        ],
      ),
    );
  }

  static Widget _badged(Widget icon, int count) {
    if (count <= 0) return icon;
    return Badge.count(count: count, maxCount: 99, child: icon);
  }
}
