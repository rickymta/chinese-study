import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('AfShellScaffold: 5 nhãn đúng thứ tự, huy hiệu, chọn đích', (tester) async {
    var selected = 0;
    await tester.pumpWidget(
      StatefulBuilder(
        builder: (context, setState) => MaterialApp(
          theme: buildAfTheme(brightness: Brightness.light),
          home: AfShellScaffold(
            title: 'AntFarm · Tiếng Trung',
            destinations: const [
              AfNavDestination(label: 'Trang chủ', icon: Icons.home_outlined),
              AfNavDestination(label: 'Ôn tập', icon: Icons.style_outlined, badgeCount: 120),
              AfNavDestination(label: 'Bài học', icon: Icons.menu_book_outlined),
              AfNavDestination(label: 'Luyện viết', icon: Icons.draw_outlined),
              AfNavDestination(label: 'Thêm', icon: Icons.more_horiz),
            ],
            selectedIndex: selected,
            onDestinationSelected: (i) => setState(() => selected = i),
            body: Center(child: Text('nhánh $selected')),
          ),
        ),
      ),
    );
    expect(find.text('AntFarm · Tiếng Trung'), findsOneWidget);
    final labels = tester.widgetList<NavigationDestination>(find.byType(NavigationDestination)).map((d) => d.label);
    expect(labels, ['Trang chủ', 'Ôn tập', 'Bài học', 'Luyện viết', 'Thêm']);
    expect(find.text('99+'), findsWidgets); // huy hiệu tối đa 99 ⇒ "99+"
    await tester.tap(find.text('Bài học'));
    await tester.pumpAndSettle();
    expect(find.text('nhánh 2'), findsOneWidget);
  });

  testWidgets('StickyActionBar chia đều nút, có SafeArea', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: const SizedBox(),
          bottomNavigationBar: StickyActionBar(
            children: [
              OutlinedButton(onPressed: () {}, child: const Text('Huỷ')),
              FilledButton(onPressed: () {}, child: const Text('Nộp')),
            ],
          ),
        ),
      ),
    );
    expect(find.byType(SafeArea), findsWidgets);
    final a = tester.getSize(find.text('Huỷ').hitTestable().first);
    expect(a.height, greaterThan(0));
    final w1 = tester.getSize(find.byType(OutlinedButton)).width;
    final w2 = tester.getSize(find.byType(FilledButton)).width;
    expect((w1 - w2).abs(), lessThan(1));
  });
}
