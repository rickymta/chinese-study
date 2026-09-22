import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Widget _host(Future<void> Function(BuildContext ctx) onTap) {
  return MaterialApp(
    home: Scaffold(
      body: Builder(
        builder: (ctx) => Center(
          child: TextButton(onPressed: () => onTap(ctx), child: const Text('mở')),
        ),
      ),
    ),
  );
}

void main() {
  testWidgets('showAfConfirm: chạm ngoài KHÔNG đóng; Huỷ ⇒ false; Đồng ý ⇒ true', (tester) async {
    bool? result;
    await tester.pumpWidget(
      _host((ctx) async {
        result = await showAfConfirm(context: ctx, title: 'Đăng xuất?', message: 'Còn 3 đánh giá chưa gửi.');
      }),
    );
    await tester.tap(find.text('mở'));
    await tester.pumpAndSettle();
    expect(find.text('Đăng xuất?'), findsOneWidget);

    // Chạm ra ngoài hộp thoại (góc màn) ⇒ vẫn mở.
    await tester.tapAt(const Offset(5, 5));
    await tester.pumpAndSettle();
    expect(find.text('Đăng xuất?'), findsOneWidget);

    await tester.tap(find.text('Huỷ'));
    await tester.pumpAndSettle();
    expect(result, isFalse);

    await tester.tap(find.text('mở'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Đồng ý'));
    await tester.pumpAndSettle();
    expect(result, isTrue);
  });

  testWidgets('showAfDialog closeOnBarrier: true ⇒ chạm ngoài đóng', (tester) async {
    await tester.pumpWidget(
      _host(
        (ctx) => showAfDialog<void>(
          context: ctx,
          closeOnBarrier: true,
          builder: (_) => const AlertDialog(title: Text('Chỉ đọc')),
        ),
      ),
    );
    await tester.tap(find.text('mở'));
    await tester.pumpAndSettle();
    expect(find.text('Chỉ đọc'), findsOneWidget);
    await tester.tapAt(const Offset(5, 5));
    await tester.pumpAndSettle();
    expect(find.text('Chỉ đọc'), findsNothing);
  });

  testWidgets('showAfBottomSheet mặc định không đóng khi chạm ngoài', (tester) async {
    await tester.pumpWidget(
      _host(
        (ctx) => showAfBottomSheet<void>(
          context: ctx,
          builder: (_) => const SizedBox(height: 120, child: Center(child: Text('Nội dung sheet'))),
        ),
      ),
    );
    await tester.tap(find.text('mở'));
    await tester.pumpAndSettle();
    expect(find.text('Nội dung sheet'), findsOneWidget);
    await tester.tapAt(const Offset(5, 5));
    await tester.pumpAndSettle();
    expect(find.text('Nội dung sheet'), findsOneWidget);
  });

  testWidgets('showAfToast hiện SnackBar', (tester) async {
    await tester.pumpWidget(_host((ctx) async => showAfToast(ctx, 'Đã lưu', kind: AfToastKind.success)));
    await tester.tap(find.text('mở'));
    await tester.pump();
    expect(find.text('Đã lưu'), findsOneWidget);
  });
}
