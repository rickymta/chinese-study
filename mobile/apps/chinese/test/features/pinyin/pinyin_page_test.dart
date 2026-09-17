import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/features/pinyin/data/models.dart';
import 'package:af_chinese/features/pinyin/presentation/pages/pinyin_page.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/drill/drill_result.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/drill/drill_runner.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/drill/drill_setup.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/drill/tone_buttons.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/guide_view.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/pinyin_chart_view.dart';
import 'package:af_chinese/features/pinyin/presentation/widgets/syllable_sheet.dart';
import 'package:af_chinese/features/progress/presentation/pages/dashboard_page.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';
import '../../helpers/test_app.dart';

/// Máy chủ pinyin giả: chart/guide từ fixture; tone-stats người mới rồi tăng `sessionsCount` theo số phiên đã nhận;
/// nộp bài: lần đầu 201, cùng `clientSessionId` lần sau 200 (idempotent); [offline] ⇒ lỗi kết nối; [reject422] ⇒ 422.
class FakePinyinServer {
  bool offline = false;
  bool reject422 = false;
  int chartCalls = 0;
  int statsCalls = 0;
  final List<Map<String, Object?>> bodies = [];
  final Map<String, int> received = {};

  Future<(int, String)> handle(RequestOptions req) async {
    final path = req.uri.path;
    if (path.endsWith('/pinyin/chart')) {
      chartCalls++;
      return (200, jsonEncode(loadFixture('pinyin_chart.json')));
    }
    if (path.endsWith('/pinyin/guide')) return (200, jsonEncode(loadFixture('pinyin_guide.json')));
    if (path.endsWith('/pinyin/tone-stats')) {
      statsCalls++;
      final stats = loadFixture('tone_stats_new_user.json');
      stats['sessionsCount'] = received.length;
      stats['totalAnswered'] = received.length * 20;
      return (200, jsonEncode(stats));
    }
    if (path.endsWith('/pinyin/tone-drills') && req.method == 'POST') {
      if (offline) throw DioException.connectionError(requestOptions: req, reason: 'offline');
      if (reject422) {
        return (
          422,
          jsonEncode({
            'error': "Âm tiết 'ma' thanh 1 không có chữ minh hoạ.",
            'code': 'TONE_NOT_AVAILABLE',
            'details': {'itemIndex': 0, 'partIndex': 0},
          }),
        );
      }
      final body = asJsonMap(req.data is String ? jsonDecode(req.data as String) : req.data) ?? const {};
      bodies.add(body);
      final id = body['clientSessionId'] as String;
      final first = !received.containsKey(id);
      received[id] = (received[id] ?? 0) + 1;
      final items = body['items'] as List;
      var correct = 0;
      for (final it in items) {
        final parts = (it as Map)['parts'] as List;
        if (parts.every((p) => (p as Map)['expectedTone'] == p['answeredTone'])) correct++;
      }
      return (
        first ? 201 : 200,
        jsonEncode({
          'id': 's-$id',
          'clientSessionId': id,
          'mode': body['mode'],
          'total': items.length,
          'correct': correct,
          'localDate': '2026-09-18',
          'byTone': {
            for (final t in [1, 2, 3, 4]) '$t': {'total': 0, 'correct': 0},
          },
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

  /// Mở app đã đăng nhập rồi vào Pinyin: qua "Thêm" (mặc định) hoặc deep link [query] (`?tab=luyen&che-do=cap`).
  Future<FakePinyinServer> openPinyin(
    WidgetTester tester, {
    FakePinyinServer? server,
    FakeAfTts? tts,
    KeyValueStore? store,
    String? query,
    List<RequestOptions>? requestLog,
  }) async {
    final srv = server ?? FakePinyinServer();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: FakeAdapter((req) {
          if (req.uri.path.contains('/pinyin/')) return srv.handle(req);
          return okSystemInfo('chinese-backend').handler(req);
        }),
        identityAdapter: okSystemInfo('identity-service'),
        tts: tts ?? FakeAfTts(voices: zhVoices),
        store: store,
        requestLog: requestLog,
      ),
    );
    await tester.pumpAndSettle();
    if (query != null) {
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go('/pinyin$query');
    } else {
      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pinyin & luyện thanh'));
    }
    await tester.pumpAndSettle();
    expect(find.byType(PinyinPage), findsOneWidget);
    return srv;
  }

  Future<void> goToTab(WidgetTester tester, String label) async {
    await tester.tap(find.descendant(of: find.byType(TabBar), matching: find.text(label)));
    await tester.pumpAndSettle();
  }

  /// Trả lời một câu (chọn thanh [tone] cho mọi phần) rồi bấm "Tiếp"/"Xem kết quả".
  Future<void> answerAndNext(WidgetTester tester, {int tone = 1}) async {
    await tester.tap(find.byKey(toneButtonKey(0, tone)));
    await tester.pump();
    if (find.byKey(toneButtonKey(1, tone)).evaluate().isNotEmpty) {
      await tester.tap(find.byKey(toneButtonKey(1, tone)));
      await tester.pump();
    }
    expect(find.byKey(kDrillNextKey), findsOneWidget);
    await tester.tap(find.byKey(kDrillNextKey));
    await tester.pump();
  }

  testWidgets(
    'mặc định tab Hướng dẫn: chủ đề đầu mở sẵn, đường nét thanh, ví dụ 妈 mā; "Sang bảng âm tiết" ⇒ tab Bảng',
    (tester) async {
      useSmallPhone(tester);
      await openPinyin(tester);
      expect(find.text('Pinyin & thanh điệu'), findsOneWidget);
      expect(find.byType(NavigationBar), findsOneWidget); // vẫn trong shell (push trong nhánh Thêm)
      expect(find.text('1. Bốn thanh điệu và thanh nhẹ'), findsOneWidget);
      expect(find.text('2. Thanh mẫu theo nhóm (phụ âm đầu)'), findsOneWidget);
      expect(find.text('Thanh 1 · 55'), findsOneWidget);
      expect(find.text('Thanh 3 · 214'), findsOneWidget);
      expect(find.text('妈'), findsWidgets);
      expect(find.text('mā'), findsOneWidget);
      expect(find.byType(VoiceMissingNotice), findsNothing);
      expect(tester.takeException(), isNull);

      await tester.ensureVisible(find.byKey(kGoToChartKey));
      await tester.tap(find.byKey(kGoToChartKey));
      await tester.pumpAndSettle();
      expect(find.byType(PinyinChartView), findsOneWidget);
      expect(find.byKey(chartCellKey('ma')), findsOneWidget);
    },
  );

  testWidgets('tab Bảng: lọc mặc định Môi, chạm ô "ma" ⇒ sheet 4 thanh + "Nghe lần lượt" đọc 妈 麻 马 骂 cách 600 ms', (
    tester,
  ) async {
    useSmallPhone(tester);
    final tts = FakeAfTts(voices: zhVoices);
    await openPinyin(tester, tts: tts);
    await goToTab(tester, 'Bảng');
    // Cột: Ø + b p m f ⇒ có "ba", "ma"; không có "da" (nhóm đầu lưỡi bị lọc).
    expect(find.byKey(chartCellKey('ma')), findsOneWidget);
    expect(find.byKey(chartCellKey('ba')), findsOneWidget);
    expect(find.byKey(chartCellKey('da')), findsNothing);
    expect(tester.takeException(), isNull); // không tràn ngang ở 360 px

    await tester.tap(find.byKey(chartCellKey('ma')));
    await tester.pumpAndSettle();
    expect(find.byType(SyllableSheet), findsOneWidget);
    expect(find.text('Thanh mẫu: m · IPA /m/'), findsOneWidget);
    for (final p in ['mā', 'má', 'mǎ', 'mà']) {
      expect(find.text(p), findsOneWidget);
    }
    for (final h in ['妈', '麻', '马', '骂']) {
      expect(find.descendant(of: find.byType(SyllableSheet), matching: find.text(h)), findsOneWidget);
    }
    expect(find.text('mẹ'), findsOneWidget);

    await tester.tap(find.byKey(kPlayAllKey));
    await tester.pump();
    expect(find.text('Dừng'), findsOneWidget);
    for (var i = 0; i < 4; i++) {
      await tester.pump(const Duration(milliseconds: 650));
    }
    expect(tts.spoken, ['妈', '麻', '马', '骂']);
    expect(find.text('Nghe lần lượt'), findsOneWidget);

    // Đóng sheet ⇒ về bảng; sheet chỉ đọc nên chạm ngoài cũng đóng được (closeOnBarrier).
    await tester.tap(find.byTooltip('Đóng'));
    await tester.pumpAndSettle();
    expect(find.byType(SyllableSheet), findsNothing);
    expect(find.byKey(chartCellKey('ma')), findsOneWidget);

    // Đổi lọc "Tất cả" ⇒ 23 cột, bảng cuộn ngang bên trong, trang không lỗi.
    await tester.tap(find.widgetWithText(ChoiceChip, 'Tất cả').first);
    await tester.pumpAndSettle();
    expect(find.byKey(chartCellKey('ma')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('deep link /pinyin?tab=luyen&che-do=cap ⇒ tab Luyện, chế độ Cặp thanh, thống kê người mới', (
    tester,
  ) async {
    useSmallPhone(tester);
    final tts = FakeAfTts(voices: zhVoices);
    await openPinyin(tester, tts: tts, query: '?tab=luyen&che-do=cap');
    expect(find.byType(DrillSetup), findsOneWidget);
    expect(find.text('Làm bài đầu tiên để xem bạn yếu thanh nào.'), findsOneWidget);
    final seg = tester.widget<SegmentedButton<DrillMode>>(find.byType(SegmentedButton<DrillMode>));
    expect(seg.selected.single, DrillMode.tonePair);
    expect(find.textContaining('Không có cặp 3-3'), findsOneWidget);

    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    expect(find.byType(DrillRunner), findsOneWidget);
    expect(find.text('Câu 1/20'), findsOneWidget);
    expect(find.text('Cặp thanh'), findsOneWidget);
    expect(find.text('Chữ thứ nhất'), findsOneWidget);
    expect(find.text('Chữ thứ hai'), findsOneWidget);
    expect(find.text('Chọn thanh cho chữ thứ 1 / 2'), findsOneWidget);
    // Tự phát câu đầu (bấm "Bắt đầu" là thao tác người dùng): 2 chữ.
    expect(tts.spoken, hasLength(1));
    expect(tts.spoken.single.characters.length, 2);
    expect(find.text('Nghe lại'), findsOneWidget);
    // Hàng hai khoá tới khi chọn xong chữ thứ nhất.
    await tester.tap(find.byKey(toneButtonKey(1, 2)));
    await tester.pump();
    expect(find.text('Chọn thanh cho chữ thứ 1 / 2'), findsOneWidget);
    await tester.tap(find.byKey(toneButtonKey(0, 2)));
    await tester.pump();
    expect(find.text('Chọn thanh cho chữ thứ 2 / 2'), findsOneWidget);
    await tester.tap(find.byKey(toneButtonKey(1, 2)));
    await tester.pump();
    expect(find.byKey(kDrillNextKey), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'luyện 20 câu một âm tiết: chọn ⇒ đúng/sai + Tiếp; xong ⇒ POST 201 với 20 items, "Đã lưu"; thống kê làm mới',
    (tester) async {
      useSmallPhone(tester);
      final tts = FakeAfTts(voices: zhVoices);
      final log = <RequestOptions>[];
      final server = await openPinyin(tester, tts: tts, query: '?tab=luyen', requestLog: log);
      final overviewBefore = log.where((r) => r.uri.path.endsWith('/progress/overview')).length;
      await tester.tap(find.byKey(kStartDrillKey));
      await tester.pumpAndSettle();
      expect(find.text('Câu 1/20'), findsOneWidget);
      expect(find.text('Một âm tiết'), findsOneWidget);
      expect(find.text('Bạn nghe được thanh nào?'), findsOneWidget);
      final buttonHeight = tester.getSize(find.byKey(toneButtonKey(0, 1))).height;
      expect(buttonHeight, greaterThanOrEqualTo(64));

      // Câu 1: chọn thanh 1 ⇒ chấm ngay; nút "Tiếp" dính đáy; nút thanh khoá.
      await tester.tap(find.byKey(toneButtonKey(0, 1)));
      await tester.pump();
      final graded = find.text('Đúng!').evaluate().isNotEmpty || find.text('Chưa đúng').evaluate().isNotEmpty;
      expect(graded, isTrue);
      expect(find.byType(StickyActionBar), findsOneWidget);
      expect(find.byKey(kDrillNextKey), findsOneWidget);
      expect(find.textContaining('Nghe thanh'), findsWidgets);
      await tester.tap(find.byKey(toneButtonKey(0, 3)));
      await tester.pump();
      expect(find.text('Câu 1/20'), findsOneWidget); // vẫn câu 1 — không nhận thêm lựa chọn
      await tester.tap(find.byKey(kDrillNextKey));
      await tester.pump();
      expect(find.text('Câu 2/20'), findsOneWidget);
      // Chuyển bằng thao tác ⇒ tự phát câu mới.
      expect(tts.spoken, hasLength(2));

      for (var i = 2; i <= 20; i++) {
        expect(find.text('Câu $i/20'), findsOneWidget);
        await answerAndNext(tester);
      }
      await tester.pumpAndSettle();

      // Kết quả: đã nộp 201, 20 câu, id UUID v4; số điểm khớp server.
      expect(find.byType(DrillResult), findsOneWidget);
      expect(server.bodies, hasLength(1));
      final body = server.bodies.single;
      final id = body['clientSessionId'] as String;
      expect(isUuidV4(id), isTrue);
      expect(body['mode'], 'listen_tone');
      expect((body['startedAt'] as String).endsWith('Z'), isTrue);
      expect((body['finishedAt'] as String).endsWith('Z'), isTrue);
      final items = body['items'] as List;
      expect(items, hasLength(20));
      for (final it in items) {
        final m = it as Map;
        expect((m['parts'] as List), hasLength(1));
        expect(m['replayCount'], 0);
        expect(m['responseMs'], anyOf(isNull, isA<int>()));
      }
      final correct = items.where((it) {
        final p = ((it as Map)['parts'] as List).single as Map;
        return p['expectedTone'] == p['answeredTone'];
      }).length;
      expect(find.text('$correct/20'), findsOneWidget);
      expect(find.text('Đã lưu · ngày học 2026-09-18'), findsOneWidget);
      expect(find.byKey(kRetrySubmitKey), findsNothing);
      if (correct < 20) expect(find.text('Câu sai (${20 - correct})'), findsOneWidget);
      expect(server.received[id], 1);
      expect(tester.takeException(), isNull);

      // "Xem thống kê" ⇒ về thiết lập, thống kê đã làm mới (server: 1 phiên), tổng quan cũng tải lại.
      await tester.ensureVisible(find.byKey(kViewStatsKey));
      await tester.tap(find.byKey(kViewStatsKey));
      await tester.pumpAndSettle();
      expect(find.byType(DrillSetup), findsOneWidget);
      expect(find.textContaining('1 phiên'), findsOneWidget);
      expect(find.text('Làm bài đầu tiên để xem bạn yếu thanh nào.'), findsNothing);
      expect(server.chartCalls, 1); // bảng giữ trong bộ nhớ (keepAlive)
      // Tổng quan đã bị invalidate nhưng Riverpod 3 TẠM DỪNG subscription của widget không hiển thị (nhánh Trang chủ
      // offstage trong IndexedStack) ⇒ chỉ tải lại khi quay về Trang chủ — không gọi API thừa khi đang ở Pinyin.
      expect(log.where((r) => r.uri.path.endsWith('/progress/overview')).length, overviewBefore);
      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Trang chủ')));
      await tester.pumpAndSettle();
      expect(find.byType(DashboardPage), findsOneWidget);
      expect(log.where((r) => r.uri.path.endsWith('/progress/overview')).length, overviewBefore + 1);
    },
  );

  testWidgets('mất mạng khi nộp ⇒ "Chưa lưu được" + Gửi lại cùng clientSessionId ⇒ 200 ⇒ "Đã lưu"', (tester) async {
    useSmallPhone(tester);
    final server = await openPinyin(tester, query: '?tab=luyen');
    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    for (var i = 1; i <= 20; i++) {
      await answerAndNext(tester, tone: 4);
    }
    await tester.pumpAndSettle();
    // Lần đầu offline: kết quả tạm tính ở client vẫn hiện, kèm banner + "Gửi lại".
    // (Đặt offline trước khi nộp: server nhận request đầu tiên khi đã offline.)
    expect(find.byType(DrillResult), findsOneWidget);
    expect(server.bodies, hasLength(1));
    final firstId = server.bodies.single['clientSessionId'];

    // Làm lại kịch bản với offline THẬT: bài mới, tắt mạng trước khi trả lời câu cuối.
    await tester.ensureVisible(find.byKey(kNewDrillKey));
    await tester.tap(find.byKey(kNewDrillKey));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    server.offline = true;
    for (var i = 1; i <= 20; i++) {
      await answerAndNext(tester, tone: 4);
    }
    await tester.pumpAndSettle();
    expect(find.byType(DrillResult), findsOneWidget);
    expect(find.textContaining('Chưa lưu được kết quả'), findsOneWidget);
    expect(find.byKey(kRetrySubmitKey), findsOneWidget);
    expect(find.byKey(const ValueKey('drill-score')), findsOneWidget); // điểm tạm tính ở client
    expect(server.bodies, hasLength(1)); // request offline không tới server

    server.offline = false;
    await tester.ensureVisible(find.byKey(kRetrySubmitKey));
    await tester.tap(find.byKey(kRetrySubmitKey));
    await tester.pumpAndSettle();
    expect(find.text('Đã lưu · ngày học 2026-09-18'), findsOneWidget);
    expect(find.byKey(kRetrySubmitKey), findsNothing);
    expect(server.bodies, hasLength(2));
    final secondId = server.bodies.last['clientSessionId'];
    expect(secondId, isNot(firstId)); // bài mới ⇒ id mới

    // Về thiết lập: server chỉ có 2 phiên (mỗi bài một id).
    await tester.ensureVisible(find.byKey(kNewDrillKey));
    await tester.tap(find.byKey(kNewDrillKey));
    await tester.pumpAndSettle();
    expect(find.byType(DrillSetup), findsOneWidget);
    expect(server.received.length, 2);
    expect(tester.takeException(), isNull);
  });

  testWidgets('422 khi nộp ⇒ "Máy chủ từ chối kết quả" + chỉ "Làm bài mới" (không Gửi lại)', (tester) async {
    useSmallPhone(tester);
    final server = await openPinyin(tester, query: '?tab=luyen');
    server.reject422 = true;
    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    for (var i = 1; i <= 20; i++) {
      await answerAndNext(tester, tone: 2);
    }
    await tester.pumpAndSettle();
    expect(find.textContaining("Máy chủ từ chối kết quả: Âm tiết 'ma' thanh 1 không có chữ minh hoạ."), findsOneWidget);
    expect(find.byKey(kRetrySubmitKey), findsNothing);
    expect(find.byKey(kNewDrillKey), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('rời trang khi đang làm ⇒ hỏi "Bỏ bài đang làm?"; Tiếp tục làm ⇒ ở lại; Bỏ bài ⇒ về Thêm', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openPinyin(tester);
    await goToTab(tester, 'Luyện');
    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    expect(find.text('Câu 1/20'), findsOneWidget);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.text('Bỏ bài đang làm?'), findsOneWidget);
    await tester.tap(find.text('Tiếp tục làm'));
    await tester.pumpAndSettle();
    expect(find.byType(DrillRunner), findsOneWidget);

    // Đổi tab trong trang KHÔNG mất bài (giữ state), quay lại vẫn ở câu 1.
    await goToTab(tester, 'Bảng');
    await goToTab(tester, 'Luyện');
    expect(find.text('Câu 1/20'), findsOneWidget);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Bỏ bài'));
    await tester.pumpAndSettle();
    expect(find.byType(PinyinPage), findsNothing);
    expect(find.text('Pinyin & luyện thanh'), findsOneWidget); // trang Thêm
    expect(server.bodies, isEmpty); // không nộp gì
    expect(tester.takeException(), isNull);
  });

  testWidgets('không có giọng ⇒ VoiceMissingNotice + "Bắt đầu" khoá; nút cài đặt giọng ⇒ /ho-so?tab=giao-dien', (
    tester,
  ) async {
    useSmallPhone(tester);
    await openPinyin(tester, tts: FakeAfTts(), query: '?tab=luyen');
    expect(find.byType(VoiceMissingNotice), findsOneWidget);
    expect(find.textContaining('nút "Bắt đầu" tạm khoá'), findsOneWidget);
    final start = tester.widget<FilledButton>(find.byKey(kStartDrillKey));
    expect(start.onPressed, isNull);

    await tester.tap(find.byKey(kVoiceSettingsKey));
    await tester.pumpAndSettle();
    expect(find.text('Hồ sơ'), findsOneWidget);
    expect(find.text('Chế độ giao diện'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'học liệu 503 ⇒ "Học liệu pinyin chưa sẵn sàng" ở Hướng dẫn/Bảng, Luyện khoá kèm lý do; Thử lại tải lại',
    (tester) async {
      useSmallPhone(tester);
      var down = true;
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: FakeAdapter((req) async {
            if (req.uri.path.contains('/pinyin/')) {
              if (down) {
                return (
                  503,
                  jsonEncode({
                    'error': 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.',
                    'code': 'CONTENT_UNAVAILABLE',
                  }),
                );
              }
              return FakePinyinServer().handle(req);
            }
            return okSystemInfo('chinese-backend').handler(req);
          }),
          identityAdapter: okSystemInfo('identity-service'),
          tts: FakeAfTts(voices: zhVoices),
        ),
      );
      await tester.pumpAndSettle();
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go('/pinyin');
      await tester.pumpAndSettle();
      expect(find.byType(PinyinPage), findsOneWidget); // 503 không điều hướng
      expect(find.text('Học liệu pinyin chưa sẵn sàng — báo quản trị viên.'), findsOneWidget);
      expect(find.byType(ErrorView), findsOneWidget);
      await goToTab(tester, 'Luyện');
      expect(tester.widget<FilledButton>(find.byKey(kStartDrillKey)).onPressed, isNull);
      expect(find.text('Học liệu pinyin chưa sẵn sàng — báo quản trị viên.'), findsWidgets);
      await goToTab(tester, 'Bảng');
      expect(find.byType(ErrorView), findsOneWidget);

      // Học liệu nạp xong ⇒ "Thử lại" tải lại (provider lỗi không keepAlive nên tab mở lại cũng tự tải mới).
      down = false;
      await tester.tap(find.text('Thử lại'));
      await tester.pumpAndSettle();
      expect(find.byKey(chartCellKey('ma')), findsOneWidget);
      await goToTab(tester, 'Hướng dẫn');
      expect(find.text('1. Bốn thanh điệu và thanh nhẹ'), findsOneWidget);
      await goToTab(tester, 'Luyện');
      expect(tester.widget<FilledButton>(find.byKey(kStartDrillKey)).onPressed, isNotNull);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('chế độ tối + 360×740 + chữ 1.3×: ba tab, sheet, bài luyện và kết quả không overflow', (tester) async {
    useSmallPhone(tester, textScale: 1.3);
    final store = InMemoryKeyValueStore({kInstallFlagKey: true, 'af.themeMode': 'dark'});
    await openPinyin(tester, store: store);
    expect(Theme.of(tester.element(find.byType(PinyinPage))).brightness, Brightness.dark);
    expect(tester.takeException(), isNull);
    await goToTab(tester, 'Bảng');
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(chartCellKey('ma')));
    await tester.pumpAndSettle();
    expect(find.byType(SyllableSheet), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.tap(find.byTooltip('Đóng'));
    await tester.pumpAndSettle();
    await goToTab(tester, 'Luyện');
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(kStartDrillKey));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(tester.getSize(find.byKey(toneButtonKey(0, 1))).height, greaterThanOrEqualTo(64));
    for (var i = 1; i <= 20; i++) {
      await answerAndNext(tester, tone: 3);
      expect(tester.takeException(), isNull);
    }
    await tester.pumpAndSettle();
    expect(find.byType(DrillResult), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
