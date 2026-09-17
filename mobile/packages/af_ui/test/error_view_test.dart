import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('ErrorView: tiêu đề theo kind, Thử lại gọi onRetry, hành động thêm', (tester) async {
    var retries = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ErrorView(
            kind: ErrorViewKind.forbidden,
            message: 'Tài khoản chưa có quyền study.use.',
            onRetry: () => retries++,
            actions: [TextButton(onPressed: () {}, child: const Text('Đăng xuất'))],
          ),
        ),
      ),
    );
    expect(find.text('Không có quyền truy cập'), findsOneWidget);
    expect(find.text('Tài khoản chưa có quyền study.use.'), findsOneWidget);
    expect(find.text('Đăng xuất'), findsOneWidget);
    await tester.tap(find.text('Thử lại'));
    expect(retries, 1);
  });

  testWidgets('ErrorView 404 mặc định', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: ErrorView(kind: ErrorViewKind.notFound)),
      ),
    );
    expect(find.text('Không tìm thấy trang'), findsOneWidget);
    expect(find.text('Thử lại'), findsNothing);
  });

  testWidgets('AsyncValueView: loading → lỗi mạng (ErrorView network) → data', (tester) async {
    Widget build(AsyncValue<String> v) => MaterialApp(
      home: Scaffold(
        body: AsyncValueView<String>(value: v, data: (d) => Text('dữ liệu: $d'), onRetry: () {}),
      ),
    );
    await tester.pumpWidget(build(const AsyncValue<String>.loading()));
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    await tester.pumpWidget(
      build(AsyncValue<String>.error(ApiError.network('Không kết nối được máy chủ.'), StackTrace.empty)),
    );
    expect(find.text('Không kết nối được máy chủ'), findsOneWidget); // tiêu đề kind network
    expect(find.text('Không kết nối được máy chủ.'), findsOneWidget); // thông điệp ApiError
    expect(find.text('Thử lại'), findsOneWidget);

    await tester.pumpWidget(build(const AsyncValue<String>.data('ok')));
    expect(find.text('dữ liệu: ok'), findsOneWidget);
  });

  testWidgets('EmptyState và SectionCard render', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: buildAfTheme(brightness: Brightness.light),
        home: Scaffold(
          body: ListView(
            children: const [
              SectionCard(title: 'Việc hôm nay', subtitle: 'Thứ Tư', child: Text('nội dung')),
              EmptyState(title: 'Chưa có thẻ', message: 'Thêm từ để bắt đầu.'),
            ],
          ),
        ),
      ),
    );
    expect(find.text('Việc hôm nay'), findsOneWidget);
    expect(find.text('Thứ Tư'), findsOneWidget);
    expect(find.text('nội dung'), findsOneWidget);
    expect(find.text('Chưa có thẻ'), findsOneWidget);
  });
}
