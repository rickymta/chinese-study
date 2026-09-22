import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/features/srs/presentation/pages/review_home_page.dart';
import 'package:af_chinese/features/srs/presentation/pages/review_session_page.dart';
import 'package:af_chinese/features/srs/presentation/widgets/flashcard.dart';
import 'package:af_chinese/features/srs/presentation/widgets/rating_bar.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';
import '../../helpers/test_app.dart';

/// Máy chủ SRS giả cho widget test: hàng đợi từ fixture (lần 2 trở đi trống ⇒ hết thẻ), chấm thẻ ghi log theo
/// `clientReviewId` (đếm số lần nhận — DB không được nhân đôi), [offline] ⇒ ném lỗi kết nối cho lời chấm.
class FakeSrsServer {
  FakeSrsServer({this.dueNowAfterReview = 4});

  bool offline = false;
  int queueCalls = 0;
  int dueNowAfterReview;
  final List<Map<String, Object?>> reviewBodies = [];
  final Map<String, int> received = {};

  Map<String, Object?> summary({int dueNow = 2, int newAvailable = 1}) => {
    'localDate': '2026-09-17',
    'timeZone': 'Asia/Ho_Chi_Minh',
    'dueToday': dueNow,
    'dueNow': dueNow,
    'reviewedToday': received.length,
    'reviewsDoneToday': 0,
    'reviewLimitRemaining': 200,
    'dailyReviewLimit': 200,
    'newIntroducedToday': 0,
    'newAvailableToday': newAvailable,
    'dailyNewCards': 10,
    'totalCards': 5,
    'matureCards': 1,
  };

  Future<(int, String)> handle(RequestOptions req) async {
    final path = req.uri.path;
    if (path.endsWith('/srs/summary')) return (200, jsonEncode(summary()));
    if (path.endsWith('/srs/queue')) {
      queueCalls++;
      final fixture = loadFixture('srs_queue.json');
      if (queueCalls > 1) fixture['cards'] = <Object?>[];
      return (200, jsonEncode(fixture));
    }
    if (path.contains('/reviews') && req.method == 'POST') {
      if (offline) throw DioException.connectionError(requestOptions: req, reason: 'offline');
      final body = asJsonMap(req.data is String ? jsonDecode(req.data as String) : req.data) ?? const {};
      reviewBodies.add(body);
      final id = body['clientReviewId'] as String;
      received[id] = (received[id] ?? 0) + 1;
      return (
        200,
        jsonEncode({
          'reviewId': 'r-$id',
          'duplicate': received[id]! > 1,
          'card': {'cardId': 'c', 'state': 'review', 'isSuspended': false},
          'summary': summary(dueNow: dueNowAfterReview, newAvailable: 0),
        }),
      );
    }
    return (404, jsonEncode({'error': 'không có'}));
  }
}

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

  Future<FakeSrsServer> openReviewTab(
    WidgetTester tester, {
    FakeSrsServer? server,
    KeyValueStore? store,
    FakeAfTts? tts,
    Future<(int, String)> Function(RequestOptions req)? srs,
  }) async {
    final srv = server ?? FakeSrsServer();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        store: store,
        srs: srs ?? srv.handle,
        tts: tts,
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Ôn tập')));
    await tester.pumpAndSettle();
    return srv;
  }

  Future<void> rateCurrent(WidgetTester tester, String rating) async {
    await tester.tap(find.byKey(kFlipButtonKey));
    await tester.pump();
    expect(find.byKey(kFlashcardBackKey), findsOneWidget);
    await tester.tap(find.byKey(ValueKey('rate-$rating')));
    await tester.pump();
    // Khoá 300 ms chống bấm đúp.
    await tester.pump(const Duration(milliseconds: 350));
  }

  testWidgets('/on-tap: 3 ô số từ /srs/summary, nút "Bắt đầu ôn (3)", huy hiệu 3, liên kết cài đặt học tập', (
    tester,
  ) async {
    useSmallPhone(tester);
    await openReviewTab(tester);
    expect(find.text('Ôn tập'), findsWidgets);
    expect(find.text('ĐẾN HẠN HÔM NAY'), findsOneWidget);
    expect(find.text('1/10'), findsOneWidget); // từ mới còn học được / hạn mức
    expect(find.text('Bắt đầu ôn (3)'), findsOneWidget);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('3')), findsOneWidget);
    expect(find.textContaining('/500'), findsOneWidget);
    expect(find.text('Cài đặt học tập'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('/on-tap: hết thẻ ⇒ "Hôm nay xong rồi!" + lượt ôn kế tiếp; hết lượt ôn ⇒ banner giới hạn', (
    tester,
  ) async {
    useSmallPhone(tester);
    await openReviewTab(
      tester,
      srs: (req) async => (
        200,
        jsonEncode({
          'localDate': '2026-09-17',
          'timeZone': 'Asia/Ho_Chi_Minh',
          'dueToday': 5,
          'dueNow': 0,
          'reviewedToday': 12,
          'reviewLimitRemaining': 0,
          'dailyReviewLimit': 12,
          'newAvailableToday': 0,
          'dailyNewCards': 10,
          'totalCards': 20,
          'matureCards': 3,
          'nextDueAt': '2026-09-18T01:30:00Z',
        }),
      ),
    );
    expect(find.text('Hôm nay xong rồi!'), findsOneWidget);
    expect(find.textContaining('Lượt ôn kế tiếp: '), findsOneWidget);
    expect(find.textContaining('Đã đạt giới hạn 12 lượt ôn hôm nay'), findsOneWidget);
    expect(find.byKey(kStartReviewKey), findsNothing);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.byType(Badge)), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('phiên ôn: lật ⇒ RatingBar; chấm ⇒ thẻ kế; hết 3 thẻ ⇒ tổng kết; POST đúng id/rating; huy hiệu đổi', (
    tester,
  ) async {
    useSmallPhone(tester);
    final tts = FakeAfTts(voices: zhVoices);
    final server = await openReviewTab(tester, tts: tts);
    await tester.tap(find.byKey(kStartReviewKey));
    await tester.pumpAndSettle();

    // Toàn màn hình: không còn bottom nav; thẻ đầu 爱, chưa lật ⇒ chưa có nút chấm; tự đọc thẻ đầu.
    expect(find.byType(NavigationBar), findsNothing);
    expect(find.text('爱'), findsOneWidget);
    expect(find.byType(RatingBar), findsNothing);
    expect(find.byKey(kFlipButtonKey), findsOneWidget);
    expect(find.text('0/3'), findsOneWidget);
    expect(tts.spoken, ['爱']);

    await tester.tap(find.byKey(kFlipButtonKey));
    await tester.pump();
    expect(find.byType(RatingBar), findsOneWidget);
    expect(find.text('ài'), findsOneWidget); // pinyin dạng dấu
    expect(find.text('ÁI'), findsOneWidget);
    expect(find.text('1. yêu'), findsOneWidget);
    expect(find.text('Chưa duyệt'), findsOneWidget);
    expect(find.text('5,5 phút'), findsOneWidget); // khoảng dự kiến nút Khó
    expect(find.text('8 ngày'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('rate-good')));
    await tester.pump();
    expect(find.text('你好'), findsOneWidget);
    expect(find.text('1/3'), findsOneWidget);
    expect(find.byType(RatingBar), findsNothing);
    expect(tts.spoken, ['爱', '你好']); // chấm ⇒ đọc thẻ kế ngay trong thao tác chạm
    await tester.pump(const Duration(milliseconds: 350));

    await rateCurrent(tester, 'again');
    expect(find.text('谢谢'), findsOneWidget);
    await rateCurrent(tester, 'easy');
    await tester.pumpAndSettle();

    expect(find.text('Xong phiên ôn!'), findsOneWidget);
    expect(find.textContaining('3 lượt'), findsOneWidget);
    expect(find.textContaining('nhớ 67%'), findsOneWidget);
    expect(server.queueCalls, 2);
    expect(server.reviewBodies.map((b) => b['rating']), ['good', 'again', 'easy']);
    for (final b in server.reviewBodies) {
      expect(isUuidV4(b['clientReviewId'] as String), isTrue);
      expect(b['durationMs'], isA<int>());
    }
    expect(server.received.values, everyElement(1));
    // Server còn thẻ (dueNow 4 sau khi chấm) ⇒ "Ôn tiếp"; về trang ôn tập ⇒ huy hiệu = summary của phản hồi chấm.
    expect(find.text('Ôn tiếp'), findsOneWidget);
    await tester.tap(find.text('Về trang ôn tập'));
    await tester.pumpAndSettle();
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('4')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mất mạng: chấm 3 thẻ ⇒ banner "Đang chờ gửi 3"; đóng ⇒ hỏi; có mạng ⇒ gửi lại cùng id, không trùng', (
    tester,
  ) async {
    useSmallPhone(tester);
    final store = InMemoryKeyValueStore({kInstallFlagKey: true});
    final server = await openReviewTab(tester, store: store);
    server.offline = true;
    await tester.tap(find.byKey(kStartReviewKey));
    await tester.pumpAndSettle();

    await rateCurrent(tester, 'good');
    await tester.pump(const Duration(milliseconds: 50));
    expect(find.byKey(const ValueKey('pending-reviews-banner')), findsOneWidget);
    expect(find.textContaining('Đang chờ gửi 1 đánh giá'), findsOneWidget);
    await rateCurrent(tester, 'good');
    await rateCurrent(tester, 'good');
    await tester.pump(const Duration(milliseconds: 50));
    expect(find.textContaining('Đang chờ gửi 3 đánh giá'), findsOneWidget);
    // Kho bền có 3 phần tử với id khác nhau (tắt app vẫn còn).
    final raw = await store.getString('af.srs.outbox.u-1');
    final stored = (jsonDecode(raw!) as List).cast<Map<String, Object?>>();
    expect(stored.map((e) => e['clientReviewId']).toSet(), hasLength(3));

    // Hết thẻ (server đã báo hết từ lô tải thêm đầu) ⇒ tổng kết vẫn hiện kèm banner chờ gửi (như web); chưa có lượt
    // nào tới server; đóng ⇒ hỏi xác nhận ⇒ "Ở lại".
    await tester.pumpAndSettle();
    expect(server.received, isEmpty);
    expect(find.text('Xong phiên ôn!'), findsOneWidget);
    expect(find.textContaining('Đang chờ gửi 3 đánh giá'), findsOneWidget);
    expect(server.queueCalls, 2); // không gọi /srs/queue dồn dập khi outbox còn kẹt
    await tester.tap(find.byType(CloseButton));
    await tester.pumpAndSettle();
    expect(find.text('Rời phiên ôn?'), findsOneWidget);
    expect(find.textContaining('Còn 3 đánh giá chưa gửi'), findsOneWidget);
    await tester.tap(find.text('Ở lại'));
    await tester.pumpAndSettle();
    expect(find.text('Rời phiên ôn?'), findsNothing);

    // Có mạng lại ⇒ bậc thang thử lại (≤ 10 s) gửi hết với CÙNG id; DB không trùng; banner biến mất; phiên kết thúc.
    server.offline = false;
    for (var i = 0; i < 12; i++) {
      await tester.pump(const Duration(seconds: 1));
    }
    await tester.pumpAndSettle();
    expect(server.received.keys.toSet(), stored.map((e) => e['clientReviewId'] as String).toSet());
    expect(server.received.values, everyElement(1));
    expect(find.byKey(const ValueKey('pending-reviews-banner')), findsNothing);
    expect(await store.containsKey('af.srs.outbox.u-1'), isFalse);
    expect(find.text('Xong phiên ôn!'), findsOneWidget);
    // Không còn gì chờ ⇒ đóng không hỏi, về /on-tap với huy hiệu từ phản hồi chấm.
    await tester.tap(find.byType(CloseButton));
    await tester.pumpAndSettle();
    expect(find.text('Rời phiên ôn?'), findsNothing);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'đăng xuất khi còn outbox kẹt ⇒ hỏi "Còn N đánh giá chưa gửi"; đồng ý ⇒ kho bị xoá, về đăng nhập (RM-S7)',
    (tester) async {
      useSmallPhone(tester);
      final store = InMemoryKeyValueStore({kInstallFlagKey: true});
      final server = await openReviewTab(tester, store: store);
      server.offline = true;
      await tester.tap(find.byKey(kStartReviewKey));
      await tester.pumpAndSettle();
      await rateCurrent(tester, 'good');
      await rateCurrent(tester, 'good');
      await tester.pumpAndSettle();
      // Rời phiên (giữ outbox) ⇒ trang ôn tập cũng hiện banner chờ gửi.
      await tester.tap(
        find.byKey(const ValueKey('session-close')).evaluate().isEmpty
            ? find.byType(CloseButton)
            : find.byKey(const ValueKey('session-close')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Rời đi'));
      await tester.pumpAndSettle();
      expect(find.byType(NavigationBar), findsOneWidget);
      expect(find.textContaining('Đang chờ gửi 2 đánh giá'), findsOneWidget);
      expect(await store.containsKey('af.srs.outbox.u-1'), isTrue);

      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Đăng xuất'));
      await tester.tap(find.text('Đăng xuất'));
      await tester.pumpAndSettle();
      expect(find.text('Còn 2 đánh giá chưa gửi — đăng xuất sẽ bỏ chúng.'), findsOneWidget);
      await tester.tap(find.widgetWithText(FilledButton, 'Đăng xuất'));
      await tester.pumpAndSettle();
      expect(find.widgetWithText(TextFormField, 'Email'), findsOneWidget); // trang đăng nhập
      expect(await store.containsKey('af.srs.outbox.u-1'), isFalse);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('phiên hết hạn (refresh 401 lúc mở app) ⇒ kho outbox của người đó VẪN CÒN, không gửi (RK-M1)', (
    tester,
  ) async {
    final server = FakeSrsServer();
    final raw = jsonEncode([
      {
        'clientReviewId': '22222222-2222-4222-8222-222222222222',
        'cardId': 'c1',
        'rating': 'good',
        'durationMs': 5,
        'attempts': 1,
      },
    ]);
    final store = InMemoryKeyValueStore({kInstallFlagKey: true, 'af.srs.outbox.u-1': raw});
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        store: store,
        srs: server.handle,
        authHandler: (_) async => (401, jsonEncode({'error': 'Phiên không hợp lệ.', 'code': 'REFRESH_INVALID'})),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại để tiếp tục.'), findsOneWidget);
    expect(server.received, isEmpty);
    expect(await store.getString('af.srs.outbox.u-1'), raw);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mở app khi kho còn outbox (tắt app giữa chừng) ⇒ tự gửi ngay, không cần mở phiên', (tester) async {
    final server = FakeSrsServer();
    final store = InMemoryKeyValueStore({
      kInstallFlagKey: true,
      'af.srs.outbox.u-1': jsonEncode([
        {
          'clientReviewId': '11111111-1111-4111-8111-111111111111',
          'cardId': 'c1',
          'rating': 'good',
          'durationMs': 5,
          'attempts': 2,
        },
      ]),
    });
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        store: store,
        srs: server.handle,
      ),
    );
    await tester.pumpAndSettle();
    expect(server.received, {'11111111-1111-4111-8111-111111111111': 1});
    expect(await store.containsKey('af.srs.outbox.u-1'), isFalse);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'chế độ tối + 360×740 + chữ 1.3×: trang ôn tập và phiên (mặt trước + mặt sau + tổng kết) không overflow',
    (tester) async {
      useSmallPhone(tester, textScale: 1.3);
      final store = InMemoryKeyValueStore({kInstallFlagKey: true, 'af.themeMode': 'dark'});
      await openReviewTab(tester, store: store);
      expect(Theme.of(tester.element(find.byType(ReviewHomePage))).brightness, Brightness.dark);
      expect(tester.takeException(), isNull);
      await tester.tap(find.byKey(kStartReviewKey));
      await tester.pumpAndSettle();
      expect(find.byType(ReviewSessionPage), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.tap(find.byKey(kFlipButtonKey));
      await tester.pump();
      expect(tester.takeException(), isNull);
      // Nút chấm cao ≥ 56.
      final size = tester.getSize(find.byKey(const ValueKey('rate-good')));
      expect(size.height, greaterThanOrEqualTo(56));
      await tester.tap(find.byKey(const ValueKey('rate-hard')));
      await tester.pump(const Duration(milliseconds: 350));
      await rateCurrent(tester, 'hard');
      await rateCurrent(tester, 'hard');
      await tester.pumpAndSettle();
      expect(find.text('Xong phiên ôn!'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('sheet "Xem chi tiết" tải /dictionary/words/{id} và hiện AddToSrsButton (chip Chờ học)', (tester) async {
    useSmallPhone(tester);
    final server = FakeSrsServer();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: FakeAdapter((req) async {
          if (req.uri.path.endsWith('/dictionary/words/w1')) return (200, jsonEncode(wordDetailBody()));
          return okSystemInfo('chinese-backend').handler(req);
        }),
        identityAdapter: okSystemInfo('identity-service'),
        srs: server.handle,
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Ôn tập')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(kStartReviewKey));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(kFlipButtonKey));
    await tester.pumpAndSettle();
    // Thanh đáy chỉ cao bằng nội dung (af_ui StickyActionBar) — không đè lên thẻ.
    expect(tester.getRect(find.byType(StickyActionBar)).height, lessThan(120));
    await tester.tap(find.widgetWithText(TextButton, 'Xem chi tiết'));
    await tester.pumpAndSettle();
    expect(find.text('Chi tiết từ'), findsOneWidget);
    expect(find.text('HSK 3.0 cấp 1'), findsOneWidget);
    expect(find.text('động từ'), findsOneWidget);
    expect(find.text('Hán Việt suy ra'), findsOneWidget);
    expect(find.text('Chờ học'), findsOneWidget);
    expect(find.textContaining('CC-CEDICT (MDBG) (CC BY-SA 4.0)'), findsOneWidget);
    expect(tester.takeException(), isNull);
    // Đóng sheet ⇒ vẫn ở phiên, thẻ vẫn lật.
    await tester.tap(find.byTooltip('Đóng'));
    await tester.pumpAndSettle();
    expect(find.byType(Flashcard), findsOneWidget);
    expect(find.byType(RatingBar), findsOneWidget);
  });
}

Map<String, Object?> wordDetailBody() => {
  'id': 'w1',
  'simplified': '爱',
  'traditional': '愛',
  'pinyin': 'ai4',
  'hsk3Level': 1,
  'pos': ['v', 'n'],
  'meaningsEn': ['to love'],
  'meaningsVi': ['yêu'],
  'meaningViStatus': 'machine',
  'meaningViSource': 'cvdict',
  'hanViet': 'ái',
  'hanVietStatus': 'derived',
  'sources': ['cc-cedict', 'cvdict'],
  'characters': [
    {
      'hanzi': '爱',
      'pinyinReadings': ['ai4'],
      'hanViet': ['ái'],
    },
  ],
  'srs': {'cardId': 'c1', 'state': 'new', 'dueAt': '2026-09-17T00:00:00Z', 'isSuspended': false},
};
