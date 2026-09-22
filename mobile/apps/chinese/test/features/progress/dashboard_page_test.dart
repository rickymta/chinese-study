import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/features/lessons/presentation/pages/lesson_detail_page.dart';
import 'package:af_chinese/features/progress/presentation/widgets/activity_heatmap.dart';
import 'package:af_chinese/features/progress/presentation/widgets/streak_card.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  void useSmallPhone(WidgetTester tester, {double textScale = 1.0}) {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = textScale;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
  }

  int overviewCalls(List<RequestOptions> log) => log.where((r) => r.uri.path.endsWith('/progress/overview')).length;

  String? streakNumber(WidgetTester tester) => tester.widget<Text>(find.byKey(kStreakCurrentKey)).data;

  Future<void> scrollTo(WidgetTester tester, Finder finder) async {
    await tester.scrollUntilVisible(finder, 200, scrollable: find.byType(Scrollable).first);
    await tester.pumpAndSettle();
  }

  testWidgets('người mới: chuỗi 0, lời mời trong từng thẻ, việc hôm nay = bài đầu + pinyin, không có huy hiệu', (
    tester,
  ) async {
    useSmallPhone(tester);
    await tester.pumpWidget(
      buildTestApp(chineseAdapter: okSystemInfo('chinese-backend'), identityAdapter: okSystemInfo('identity-service')),
    );
    await tester.pumpAndSettle();

    expect(find.text('Hôm nay, Thứ Năm 17/09'), findsOneWidget); // localDate server, không phải ngày máy
    expect(find.text('Chuỗi ngày học'), findsOneWidget);
    expect(find.text('0'), findsWidgets);
    expect(find.text('Học 1 hoạt động hôm nay để bắt đầu chuỗi.'), findsOneWidget);
    expect(find.text('Chưa có chuỗi nào'), findsOneWidget);
    expect(find.text('Hôm nay chưa học'), findsOneWidget);
    // Mục tiêu: 0/10, còn 10 thẻ mới ⇒ "Ôn ngay (10)".
    expect(find.textContaining('0/10'), findsOneWidget);
    expect(find.text('Ôn ngay (10)'), findsOneWidget);
    // Việc hôm nay theo R-PG9.
    expect(find.text('Học 10 thẻ mới'), findsOneWidget);
    expect(find.text('Bắt đầu bài học đầu tiên'), findsWidgets); // mục việc + nút thẻ Bài học
    expect(find.text('Học pinyin trước'), findsOneWidget);
    expect(find.text('Ôn 0 thẻ đến hạn'), findsNothing);
    // Huy hiệu Ôn tập = dueNow 0 + newAvailableToday 10 = 10.
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('10')), findsOneWidget);

    await scrollTo(tester, find.text('Học từ đầu tiên'));
    expect(find.text('Học từ đầu tiên'), findsOneWidget);
    await scrollTo(tester, find.text('Viết chữ đầu tiên'));
    expect(find.text('Viết chữ đầu tiên'), findsOneWidget);
    await scrollTo(tester, find.text('Làm bài luyện thanh đầu tiên'));
    expect(find.text('Làm bài luyện thanh đầu tiên'), findsOneWidget);
    // Không có quyền quản trị ⇒ không có khối trạng thái hệ thống.
    expect(find.byKey(const ValueKey('system-status-section')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('người đã học (fixture đầy đủ): số liệu, 6 việc đúng thứ tự, huy hiệu 30, bấm việc ⇒ trang đích', (
    tester,
  ) async {
    useSmallPhone(tester);
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        overviewBody: overviewFixture('progress_overview_full.json'),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Hôm nay, Thứ Sáu 18/09'), findsOneWidget);
    expect(streakNumber(tester), '5');
    expect(find.text('Học 1 hoạt động để giữ chuỗi.'), findsOneWidget);
    expect(find.text('Dài nhất: 12 ngày · hôm nay 12 lượt'), findsOneWidget);
    expect(find.textContaining('0/33'), findsOneWidget);
    expect(find.text('Ôn ngay (33)'), findsOneWidget);

    final tasks = tester
        .widgetList<ListTile>(find.byWidgetPredicate((w) => w is ListTile && w.key.toString().contains('today-task')))
        .map((t) => (t.title! as Text).data)
        .toList();
    expect(tasks, [
      'Ôn 23 thẻ đến hạn',
      'Học 10 thẻ mới',
      'Bài tiếp theo',
      'Luyện viết chữ bài "Giới thiệu bản thân"',
      'Luyện thanh 2, 3',
      'Học pinyin trước',
    ]);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('30')), findsOneWidget);

    await scrollTo(tester, find.text('Từ vựng'));
    expect(find.textContaining('64'), findsWidgets);
    expect(find.textContaining('/ 500 từ trong lộ trình'), findsOneWidget);
    await scrollTo(tester, find.text('Học tiếp'));
    expect(find.textContaining('/ 5 bài hoàn thành'), findsOneWidget);
    expect(find.text('Đang học dở 1 bài'), findsOneWidget);
    await scrollTo(tester, find.text('Luyện 3 chữ yếu'));
    expect(find.text('9 chữ bài "Giới thiệu bản thân"'), findsOneWidget);
    await scrollTo(tester, find.text('Luyện thanh 2, 3').last);
    expect(find.textContaining('82%'), findsOneWidget);
    expect(find.text('Thanh 2'), findsOneWidget);
    expect(find.text('Thanh 3'), findsOneWidget);

    // Bấm "Bài tiếp theo" ⇒ /bai-hoc/so-dem ⇒ trang bài (M9; adapter giả trả thân `system/info` ⇒ bài rỗng nhưng
    // không vỡ), bottom nav còn.
    await scrollTo(tester, find.byKey(const ValueKey('today-task-lesson')));
    await tester.tap(find.byKey(const ValueKey('today-task-lesson')));
    await tester.pumpAndSettle();
    expect(find.byType(LessonDetailPage), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'khối vắng (fixture tối thiểu) ⇒ ẩn thẻ tương ứng, "Không còn việc nào", lịch 90 ngày trống, huy hiệu 0',
    (tester) async {
      useSmallPhone(tester);
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: okSystemInfo('chinese-backend'),
          identityAdapter: okSystemInfo('identity-service'),
          overviewBody: overviewFixture('progress_overview_minimal.json'),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Chuỗi ngày học'), findsOneWidget);
      expect(find.text('Việc hôm nay'), findsOneWidget);
      expect(find.text('Không còn việc nào — bạn có thể tra từ điển hoặc luyện viết thêm.'), findsOneWidget);
      await scrollTo(tester, find.text('90 ngày qua'));
      expect(find.text('0 ngày có học · 0 lượt'), findsOneWidget);
      // Giới hạn trong Card vì thanh nav cũng có nhãn "Bài học"/"Luyện viết".
      for (final title in ['Mục tiêu hôm nay', 'Từ vựng', 'Bài học', 'Luyện viết', 'Thanh điệu']) {
        expect(
          find.descendant(of: find.byType(Card), matching: find.text(title)),
          findsNothing,
          reason: title,
        );
      }
      expect(find.byType(Badge), findsNothing);
    },
  );

  testWidgets('lịch 90 ngày: 14 cột vừa 360 px không tràn; chạm ô ⇒ nhãn "dd/MM: N lượt" ghim 3 s; ô hôm nay có viền', (
    tester,
  ) async {
    useSmallPhone(tester);
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        overviewBody: overviewFixture('progress_overview_full.json'),
      ),
    );
    await tester.pumpAndSettle();
    await scrollTo(tester, find.byKey(kHeatmapGridKey));
    // 14 cột × 16 + 13 × 3 = 263 ≤ 296 khả dụng (360 − đệm trang 32 − đệm thẻ 32) ⇒ không cần cuộn ngang.
    final gridWidth = tester.getSize(find.byKey(kHeatmapGridKey)).width;
    expect(gridWidth, 263);
    expect(gridWidth + kHeatmapLabelCol, lessThanOrEqualTo(296));
    expect(find.byKey(const ValueKey('heatmap-2026-09-18')), findsOneWidget);
    // Nhãn tháng: 21/06 (Chủ nhật) → 18/09 ⇒ T6, T7, T8, T9 (cột 0 chỉ có 21/06 nhưng cột kế vẫn tháng 6 ⇒ giữ T6).
    // "T6" trùng nhãn thứ Sáu ở cột trái (T2/T4/T6) ⇒ 2 widget.
    expect(find.text('T6'), findsNWidgets(2));
    for (final label in ['T7', 'T8', 'T9']) {
      expect(find.text(label), findsOneWidget, reason: label);
    }

    await tester.tap(find.byKey(const ValueKey('heatmap-2026-09-18')));
    await tester.pump();
    expect(find.text('18/09: 12 lượt'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('heatmap-2026-09-17')));
    await tester.pump();
    expect(find.text('17/09: 1 lượt'), findsOneWidget);
    expect(find.text('18/09: 12 lượt'), findsNothing);
    await tester.pump(kHeatmapPinDuration + const Duration(milliseconds: 10));
    expect(find.text('17/09: 1 lượt'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('lỗi tải (503 học liệu) ⇒ ErrorView trong trang + Thử lại ⇒ tải được; kéo-để-làm-mới gọi lại API', (
    tester,
  ) async {
    useSmallPhone(tester);
    var calls = 0;
    final log = <RequestOptions>[];
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        requestLog: log,
        // Huy hiệu (M6) gọi /srs/summary — stub riêng để không tiêu tốn chuỗi "lần 1 lỗi, lần 2 OK" của overview.
        srs: (_) async => (200, summaryFromOverview(defaultOverviewBody())),
        overview: (_) async {
          calls++;
          if (calls == 1) {
            return (503, jsonEncode({'error': 'Học liệu chưa được nạp.', 'code': 'CONTENT_UNAVAILABLE'}));
          }
          return (200, overviewFixture('progress_overview_new_user.json'));
        },
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Học liệu chưa sẵn sàng'), findsOneWidget);
    expect(find.textContaining('Học liệu chưa được nạp.'), findsOneWidget);
    expect(find.text('Trang chủ'), findsWidgets); // tiêu đề khi chưa có dữ liệu
    expect(find.byType(NavigationBar), findsOneWidget); // không bị đẩy sang trang lỗi (skipErrorRedirect)

    await tester.tap(find.text('Thử lại'));
    await tester.pumpAndSettle();
    expect(find.text('Hôm nay, Thứ Năm 17/09'), findsOneWidget);
    expect(calls, 2);

    // Kéo để làm mới ⇒ gọi lại; số cũ vẫn hiện trong lúc tải (không nháy về skeleton).
    await tester.fling(find.text('Chuỗi ngày học'), const Offset(0, 300), 1000);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.text('Chuỗi ngày học'), findsOneWidget);
    await tester.pumpAndSettle();
    expect(calls, 3);
    expect(overviewCalls(log), 3);
  });

  testWidgets('múi giờ máy ≠ hồ sơ ⇒ dòng nhắc, chạm ⇒ Hồ sơ; trùng ⇒ không nhắc (kể cả bí danh Asia/Saigon)', (
    tester,
  ) async {
    useSmallPhone(tester);
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        deviceTimeZone: 'Asia/Tokyo',
      ),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('Ngày học tính theo múi giờ hồ sơ (Asia/Ho_Chi_Minh)'), findsOneWidget);
    await tester.tap(find.textContaining('Ngày học tính theo múi giờ hồ sơ'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Hồ sơ'), findsOneWidget);

    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        deviceTimeZone: 'Asia/Saigon',
      ),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('Ngày học tính theo múi giờ hồ sơ'), findsNothing);
  });

  testWidgets('có users.manage ⇒ khối "Trạng thái hệ thống" thu gọn cuối trang, mở mới gọi /system/info', (
    tester,
  ) async {
    useSmallPhone(tester);
    final log = <RequestOptions>[];
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        permissions: const {'study.use', 'users.manage'},
        requestLog: log,
      ),
    );
    await tester.pumpAndSettle();
    await scrollTo(tester, find.byKey(const ValueKey('system-status-section')));
    expect(find.text('Trạng thái hệ thống (qua gateway)'), findsOneWidget);
    expect(log.where((r) => r.uri.path.endsWith('/system/info')), isEmpty);
    expect(find.text('Tiếng Trung: đang chạy'), findsNothing);

    await tester.tap(find.text('Trạng thái hệ thống (qua gateway)'));
    await tester.pumpAndSettle();
    await scrollTo(tester, find.text('Tiếng Trung: đang chạy'));
    expect(find.text('Tài khoản: đang chạy'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('lưu cài đặt học tập ⇒ tổng quan + huy hiệu làm mới', (tester) async {
    useSmallPhone(tester);
    var newCards = 10;
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: chineseWithSettings(),
        identityAdapter: okSystemInfo('identity-service'),
        overview: (_) async {
          final body = jsonDecode(overviewFixture('progress_overview_new_user.json')) as Map<String, Object?>;
          (body['srs']! as Map<String, Object?>)['newAvailableToday'] = newCards;
          return (200, jsonEncode(body));
        },
      ),
    );
    await tester.pumpAndSettle();
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('10')), findsOneWidget);

    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Hồ sơ'));
    await tester.pumpAndSettle();
    final tab = find.descendant(of: find.byType(TabBar), matching: find.text('Học tập'));
    await tester.ensureVisible(tab);
    await tester.tap(tab);
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Giới hạn lượt ôn/ngày'), '150');
    await tester.pumpAndSettle();
    newCards = 25; // server trả hạn mức mới sau khi lưu
    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
    await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
    await tester.pumpAndSettle();
    expect(find.text('Đã lưu cài đặt học tập'), findsOneWidget);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('25')), findsOneWidget);
  });

  testWidgets('đổi tài khoản trên cùng máy ⇒ tổng quan của người mới, không lộ số của người trước', (tester) async {
    useSmallPhone(tester);
    var userId = 'u-1';
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        meId: () => userId,
        authHandler: (req) {
          final path = req.uri.path;
          if (path.endsWith('/logout')) return Future.value((204, ''));
          return Future.value((200, tokenBody(withAccount: !path.endsWith('/refresh'), sub: userId)));
        },
        overview: (_) async =>
            (200, overviewFixture(userId == 'u-1' ? 'progress_overview_full.json' : 'progress_overview_new_user.json')),
      ),
    );
    await tester.pumpAndSettle();
    expect(streakNumber(tester), '5');
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('30')), findsOneWidget);

    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Đăng xuất'));
    await tester.tap(find.text('Đăng xuất'));
    await tester.pumpAndSettle();
    userId = 'u-2';
    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'hai@vidu.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'matkhau-dai');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    // Đăng xuất từ "Thêm" ⇒ returnTo=/them ⇒ sau đăng nhập về "Thêm"; huy hiệu đã là của người mới (10, không 30).
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('30')), findsNothing);
    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Trang chủ')));
    await tester.pumpAndSettle();
    expect(find.text('Học 1 hoạt động hôm nay để bắt đầu chuỗi.'), findsOneWidget);
    expect(streakNumber(tester), '0');
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('30')), findsNothing);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('10')), findsOneWidget);
  });

  testWidgets('chế độ tối + 360×740 + chữ 1.3×: cả trang (fixture đầy đủ) không overflow', (tester) async {
    useSmallPhone(tester, textScale: 1.3);
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        overviewBody: overviewFixture('progress_overview_full.json'),
        store: InMemoryKeyValueStore({kInstallFlagKey: true, kThemeModeKey: 'dark'}),
        permissions: const {'study.use', 'users.manage'},
      ),
    );
    await tester.pumpAndSettle();
    expect(Theme.of(tester.element(find.text('Chuỗi ngày học'))).brightness, Brightness.dark);
    for (final label in ['Mục tiêu hôm nay', 'Việc hôm nay', '90 ngày qua', 'Từ vựng', 'Bài học', 'Luyện viết']) {
      // Giới hạn trong Card vì thanh nav cũng có "Bài học"/"Luyện viết".
      await scrollTo(tester, find.descendant(of: find.byType(Card), matching: find.text(label)));
    }
    await scrollTo(tester, find.byKey(const ValueKey('system-status-section')));
    expect(tester.takeException(), isNull);
  });
}
