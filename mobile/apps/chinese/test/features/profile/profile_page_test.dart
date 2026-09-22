import 'dart:async';
import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/core/speech/chinese_speech.dart';
import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';
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

  /// Mở app đã đăng nhập rồi vào Thêm → Hồ sơ (+ chọn tab).
  Future<void> openProfile(WidgetTester tester, {String? tab}) async {
    await tester.pumpAndSettle();
    // Chỉ tìm trong thanh nav: khi đang ở trang "Thêm", tiêu đề AppBar cũng là "Thêm".
    await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Hồ sơ'));
    await tester.tap(find.text('Hồ sơ'));
    await tester.pumpAndSettle();
    if (tab != null) {
      // TabBar cuộn được ⇒ nhãn cuối có thể nằm ngoài 360 px.
      final tabFinder = find.descendant(of: find.byType(TabBar), matching: find.text(tab));
      await tester.ensureVisible(tabFinder);
      await tester.tap(tabFinder);
      await tester.pumpAndSettle();
    }
  }

  /// Adapter identity: `GET/PUT /account` (PUT ⇒ ghi lại tài khoản, hoặc trả [putStatus]/[putBody]).
  FakeAdapter identityWithAccount({
    required Map<String, Object?> account,
    int? putStatus,
    String? putBody,
    List<Map<String, Object?>>? puts,
  }) => FakeAdapter((req) async {
    if (req.uri.path.endsWith('/account')) {
      if (req.method == 'PUT') {
        final body = asJsonMap(req.data)!;
        puts?.add(body);
        if (putStatus != null) return (putStatus, putBody ?? '');
        account['displayName'] = body['displayName'];
        account['timeZone'] = body['timeZone'];
      }
      return (200, jsonEncode(account));
    }
    return okSystemInfo('identity-service').handler(req);
  });

  group('tab Thông tin', () {
    testWidgets('đổi tên + múi giờ (gõ "ho chi") ⇒ PUT /account ⇒ refresh ⇒ /me tải lại ⇒ toast, tên mới hiện ngay', (
      tester,
    ) async {
      useSmallPhone(tester);
      final account = <String, Object?>{
        ...testAccount.toJson(),
        'timeZone': 'Asia/Tokyo',
        'createdAt': '2026-09-01T00:00:00Z',
      };
      final puts = <Map<String, Object?>>[];
      final log = <RequestOptions>[];
      var putDone = false;
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: identityWithAccount(account: account, puts: puts),
          tokenStore: InMemoryTokenStore(
            StoredSession(
              refreshToken: 'rt-0',
              refreshTokenExpiresAt: DateTime.utc(2099),
              account: testAccount.copyWith(timeZone: 'Asia/Tokyo'),
            ),
          ),
          requestLog: log,
          // Sau khi PUT, token mới mang claim mới (R4-4) — giả bằng cách theo dõi puts.
          authHandler: (req) {
            if (req.uri.path.endsWith('/refresh')) {
              return putDone
                  ? Future.value((200, tokenBody(name: 'Quân Đỗ', timeZone: 'Asia/Ho_Chi_Minh')))
                  : Future.value((200, tokenBody(timeZone: 'Asia/Tokyo')));
            }
            return defaultAuthResponse(req);
          },
        ),
      );
      await openProfile(tester);
      expect(find.text('Hồ sơ'), findsWidgets);
      expect(find.text('ban@vidu.com'), findsOneWidget);
      expect(find.text('Không đổi được email trong phiên bản này.'), findsOneWidget);
      expect(find.text('Asia/Tokyo'), findsOneWidget);
      // Máy (Asia/Ho_Chi_Minh) ≠ hồ sơ (Asia/Tokyo) ⇒ banner + nút.
      expect(find.textContaining('Thiết bị của bạn đang ở múi giờ Asia/Ho_Chi_Minh'), findsOneWidget);
      expect(find.text('Chưa có thay đổi.'), findsOneWidget);
      expect(tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Lưu')).onPressed, isNull);

      await tester.enterText(find.widgetWithText(TextFormField, 'Tên hiển thị'), 'Quân Đỗ');
      await tester.pumpAndSettle();
      expect(find.text('Chưa có thay đổi.'), findsNothing);

      // Chọn múi giờ qua sheet: gõ "ho chi".
      await tester.tap(find.widgetWithText(TextFormField, 'Múi giờ'));
      await tester.pumpAndSettle();
      expect(find.text('Chọn múi giờ'), findsOneWidget);
      await tester.enterText(find.widgetWithText(TextField, 'Tìm: ho chi, tokyo, europe…'), 'ho chi');
      await tester.pumpAndSettle();
      await tester.tap(find.text('Asia/Ho_Chi_Minh').last);
      await tester.pumpAndSettle();
      expect(find.text('Chọn múi giờ'), findsNothing);
      expect(find.textContaining('Thiết bị của bạn đang ở múi giờ'), findsNothing); // đã trùng máy

      final meCallsBefore = log.where((r) => r.uri.path.endsWith('/me')).length;
      final refreshBefore = log.where((r) => r.uri.path.endsWith('/refresh')).length;
      final identityPut = puts; // ghi trước khi bấm để closure không đổi
      await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
      putDone = true;
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();

      expect(identityPut.single, {'displayName': 'Quân Đỗ', 'timeZone': 'Asia/Ho_Chi_Minh'});
      expect(log.where((r) => r.uri.path.endsWith('/refresh')).length, refreshBefore + 1); // refreshSession
      expect(log.where((r) => r.uri.path.endsWith('/me')).length, meCallsBefore + 1); // reloadMe
      expect(find.text('Đã lưu hồ sơ'), findsOneWidget);
      expect(find.text('Chưa có thay đổi.'), findsOneWidget); // form về "không đổi" với giá trị mới

      // Tên mới hiện ngay ở trang Thêm (tài khoản trong AuthState đã đổi).
      await tester.tap(find.byType(BackButton));
      await tester.pumpAndSettle();
      expect(find.text('Quân Đỗ'), findsOneWidget);
    });

    testWidgets('422 INVALID_TIME_ZONE ⇒ lỗi dưới ô múi giờ, không có dải lỗi; lỗi khác ⇒ dải lỗi', (tester) async {
      useSmallPhone(tester);
      final account = <String, Object?>{...testAccount.toJson()};
      var status = 422;
      var body = jsonEncode({'error': 'Múi giờ không hợp lệ.', 'code': 'INVALID_TIME_ZONE'});
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: FakeAdapter((req) async {
            if (req.uri.path.endsWith('/account') && req.method == 'PUT') return (status, body);
            if (req.uri.path.endsWith('/account')) return (200, jsonEncode(account));
            return okSystemInfo('identity-service').handler(req);
          }),
        ),
      );
      await openProfile(tester);
      await tester.enterText(find.widgetWithText(TextFormField, 'Tên hiển thị'), 'Tên mới');
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();
      expect(find.text('Múi giờ không hợp lệ.'), findsOneWidget);
      expect(find.byType(AuthBanner), findsNothing);

      status = 500;
      body = jsonEncode({'error': 'Lỗi máy chủ.', 'code': 'INTERNAL'});
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();
      expect(find.byType(AuthBanner), findsOneWidget);
      expect(find.text('Lỗi máy chủ.'), findsOneWidget);
      expect(find.text('Múi giờ không hợp lệ.'), findsNothing);
    });

    testWidgets('nút "Dùng múi giờ này" đặt múi giờ máy; Hoàn tác trả về giá trị cũ', (tester) async {
      useSmallPhone(tester);
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          deviceTimeZone: 'Asia/Tokyo',
        ),
      );
      await openProfile(tester);
      expect(find.text('Asia/Ho_Chi_Minh'), findsOneWidget);
      await tester.tap(find.text('Dùng múi giờ này'));
      await tester.pumpAndSettle();
      expect(find.text('Asia/Tokyo'), findsOneWidget);
      expect(find.text('Dùng múi giờ này'), findsNothing);
      await tester.tap(find.text('Hoàn tác'));
      await tester.pumpAndSettle();
      expect(find.text('Asia/Ho_Chi_Minh'), findsOneWidget);
      expect(find.text('Chưa có thay đổi.'), findsOneWidget);
    });
  });

  group('tab Mật khẩu', () {
    Future<void> fill(
      WidgetTester tester, {
      required String current,
      required String next,
      required String confirm,
    }) async {
      await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu hiện tại'), current);
      await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu mới (8–128 ký tự)'), next);
      await tester.enterText(find.widgetWithText(TextFormField, 'Nhập lại mật khẩu mới'), confirm);
      await tester.pumpAndSettle();
    }

    testWidgets('nhập lại không khớp / trùng mật khẩu cũ ⇒ lỗi validator, không gọi API', (tester) async {
      useSmallPhone(tester);
      final log = <RequestOptions>[];
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          requestLog: log,
        ),
      );
      await openProfile(tester, tab: 'Mật khẩu');
      await fill(tester, current: 'cu-cu-cu-cu', next: 'moi-moi-moi', confirm: 'khac-khac-khac');
      await tester.tap(find.widgetWithText(FilledButton, 'Đổi mật khẩu'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu nhập lại không khớp'), findsOneWidget);

      await fill(tester, current: 'cu-cu-cu-cu', next: 'cu-cu-cu-cu', confirm: 'cu-cu-cu-cu');
      await tester.tap(find.widgetWithText(FilledButton, 'Đổi mật khẩu'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu mới phải khác mật khẩu hiện tại'), findsOneWidget);
      expect(log.where((r) => r.uri.path.endsWith('/password')), isEmpty);
    });

    testWidgets('422 WRONG_PASSWORD ⇒ lỗi dưới ô hiện tại; thành công giữ phiên ⇒ toast + form trống', (tester) async {
      useSmallPhone(tester);
      var wrong = true;
      final log = <RequestOptions>[];
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          requestLog: log,
          authHandler: (req) {
            if (req.uri.path.endsWith('/password')) {
              if (wrong) {
                return Future.value((422, jsonEncode({'error': 'Sai mật khẩu.', 'code': 'WRONG_PASSWORD'})));
              }
              return Future.value((200, jsonEncode({'otherSessionsRevoked': 2, 'currentSessionKept': true})));
            }
            return defaultAuthResponse(req);
          },
        ),
      );
      await openProfile(tester, tab: 'Mật khẩu');
      await fill(tester, current: 'sai-sai-sai', next: 'moi-moi-moi', confirm: 'moi-moi-moi');
      await tester.tap(find.widgetWithText(FilledButton, 'Đổi mật khẩu'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu hiện tại không đúng.'), findsOneWidget);
      expect(find.byType(AuthBanner), findsNothing);
      // Gửi kèm refresh token hiện tại (đã xoay lúc mở app ⇒ rt-1).
      final sent = asJsonMap(log.lastWhere((r) => r.uri.path.endsWith('/password')).data)!;
      expect(sent['refreshToken'], 'rt-1');
      expect(sent['currentPassword'], 'sai-sai-sai');

      wrong = false;
      await fill(tester, current: 'dung-dung-dung', next: 'moi-moi-moi', confirm: 'moi-moi-moi');
      await tester.tap(find.widgetWithText(FilledButton, 'Đổi mật khẩu'));
      await tester.pumpAndSettle();
      expect(find.text('Đã đổi mật khẩu. Các thiết bị khác đã bị đăng xuất.'), findsOneWidget);
      expect(find.byType(NavigationBar), findsOneWidget); // vẫn đăng nhập
      final current = tester.widget<TextFormField>(find.widgetWithText(TextFormField, 'Mật khẩu hiện tại'));
      expect(current.controller?.text, isEmpty);
      // Form trống KHÔNG được hiện lỗi validator (lỗi phát hiện khi chạy thật: clear() sau reset() ⇒ ba ô đỏ).
      expect(find.text('Vui lòng nhập mật khẩu hiện tại'), findsNothing);
      expect(find.text('Vui lòng nhập mật khẩu'), findsNothing);
      expect(find.text('Vui lòng nhập lại mật khẩu mới'), findsNothing);
    });

    testWidgets('currentSessionKept=false ⇒ đăng xuất cục bộ, về đăng nhập với banner đổi mật khẩu', (tester) async {
      useSmallPhone(tester);
      final tokens = InMemoryTokenStore();
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          tokenStore: tokens,
          authHandler: (req) {
            if (req.uri.path.endsWith('/password')) {
              return Future.value((200, jsonEncode({'otherSessionsRevoked': 3, 'currentSessionKept': false})));
            }
            return defaultAuthResponse(req);
          },
        ),
      );
      await openProfile(tester, tab: 'Mật khẩu');
      await fill(tester, current: 'cu-cu-cu-cu', next: 'moi-moi-moi', confirm: 'moi-moi-moi');
      await tester.tap(find.widgetWithText(FilledButton, 'Đổi mật khẩu'));
      await tester.pumpAndSettle();
      expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
      expect(find.text('Bạn vừa đổi mật khẩu. Vui lòng đăng nhập lại bằng mật khẩu mới.'), findsOneWidget);
      expect(tokens.session, isNull);
      expect(find.byType(NavigationBar), findsNothing);
    });
  });

  group('tab Học tập', () {
    testWidgets('tải GET, banner mặc định; sửa + Lưu ⇒ PUT đủ 5 trường ⇒ toast; ttsRate đồng bộ sang SpeakButton', (
      tester,
    ) async {
      useSmallPhone(tester);
      final saved = <LearningSettings>[];
      final tts = FakeAfTts(voices: zhVoices);
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(saved: saved),
          identityAdapter: okSystemInfo('identity-service'),
          tts: tts,
        ),
      );
      await openProfile(tester, tab: 'Học tập');
      expect(find.textContaining('Đang dùng cài đặt mặc định'), findsOneWidget);
      expect(find.text('Từ mới mỗi ngày: '), findsNothing); // Text.rich gộp
      expect(find.textContaining('Từ mới mỗi ngày: 10'), findsOneWidget);
      expect(find.textContaining('Tốc độ đọc: 0,80'), findsOneWidget);
      expect(tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Lưu')).onPressed, isNull);

      // Kéo thanh tốc độ hết bên phải ⇒ 1,20; Nghe thử đọc theo giá trị ĐANG kéo (chưa lưu).
      final sliders = find.byType(Slider);
      await tester.ensureVisible(sliders.at(2));
      await tester.drag(sliders.at(2), const Offset(300, 0));
      await tester.pumpAndSettle();
      expect(find.textContaining('Tốc độ đọc: 1,20'), findsOneWidget);
      await tester.ensureVisible(find.widgetWithText(OutlinedButton, 'Nghe thử'));
      await tester.tap(find.widgetWithText(OutlinedButton, 'Nghe thử'));
      await tester.pumpAndSettle();
      expect(tts.spoken, ['你好']);
      expect(tts.rate, 1.2);
      expect(saved, isEmpty);

      await tester.enterText(find.widgetWithText(TextFormField, 'Giới hạn lượt ôn/ngày'), '150');
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();
      expect(find.text('Đã lưu cài đặt học tập'), findsOneWidget);
      expect(saved.single.dailyReviewLimit, 150);
      expect(saved.single.ttsRate, 1.2);
      expect(saved.single.dailyNewCards, 10);
      expect(saved.single.autoPlayAudio, isTrue);
      expect(find.textContaining('Đang dùng cài đặt mặc định'), findsNothing);
      expect(tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Lưu')).onPressed, isNull);

      // Nguồn sự thật ttsRate = server ⇒ provider và cache đồng bộ 1,2.
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      expect(container.read(ttsRateProvider), 1.2);
      expect(await container.read(keyValueStoreProvider).getString(ttsRateCacheKey('u-1')), '1.2');
    });

    testWidgets('validator giới hạn lượt ôn; 400 VALIDATION details ⇒ lỗi dưới đúng ô', (tester) async {
      useSmallPhone(tester);
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(
            onPut: (_) => Future.value((
              400,
              jsonEncode({
                'error': 'Dữ liệu không hợp lệ.',
                'code': 'VALIDATION',
                'details': {
                  'desiredRetention': ['desiredRetention tối đa 2 chữ số thập phân.'],
                },
              }),
            )),
          ),
          identityAdapter: okSystemInfo('identity-service'),
        ),
      );
      await openProfile(tester, tab: 'Học tập');
      await tester.enterText(find.widgetWithText(TextFormField, 'Giới hạn lượt ôn/ngày'), '5');
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();
      expect(find.text('Ít nhất 10'), findsOneWidget);

      await tester.enterText(find.widgetWithText(TextFormField, 'Giới hạn lượt ôn/ngày'), '300');
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.tap(find.widgetWithText(FilledButton, 'Lưu'));
      await tester.pumpAndSettle();
      expect(find.text('desiredRetention tối đa 2 chữ số thập phân.'), findsOneWidget);
      expect(find.byType(AuthBanner), findsNothing);
    });

    testWidgets('thiếu study.use ⇒ không có tab Học tập, có dải giải thích; ?tab=hoc-tap rơi về Thông tin', (
      tester,
    ) async {
      useSmallPhone(tester);
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          permissions: const {},
        ),
      );
      await tester.pumpAndSettle();
      // Thiếu study.use ⇒ /403; Hồ sơ vẫn vào được qua điều hướng trực tiếp.
      final container = ProviderScope.containerOf(tester.element(find.text('Đăng xuất')));
      container.read(routerProviderForTest).go('/ho-so?tab=hoc-tap');
      await tester.pumpAndSettle();
      expect(find.descendant(of: find.byType(TabBar), matching: find.text('Học tập')), findsNothing);
      expect(find.textContaining('chỉ hiện khi tài khoản có quyền Học tập'), findsOneWidget);
      expect(find.text('Không đổi được email trong phiên bản này.'), findsOneWidget);
    });
  });

  group('tab Giao diện', () {
    testWidgets('?tab=giao-dien mở đúng tab; đổi Tối/Sáng đổi brightness và lưu af.themeMode', (tester) async {
      useSmallPhone(tester, textScale: 1.3);
      final store = InMemoryKeyValueStore({kInstallFlagKey: true});
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(),
          identityAdapter: okSystemInfo('identity-service'),
          store: store,
          tts: FakeAfTts(voices: zhVoices),
        ),
      );
      await tester.pumpAndSettle();
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go('/ho-so?tab=giao-dien');
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.text('Chế độ giao diện'), findsOneWidget);

      BuildContext ctx() => tester.element(find.text('Chế độ giao diện'));
      expect(Theme.of(ctx()).brightness, Brightness.light);
      await tester.tap(find.text('Tối'));
      await tester.pumpAndSettle();
      expect(Theme.of(ctx()).brightness, Brightness.dark);
      expect(store.snapshot[kThemeModeKey], 'dark');
      expect(tester.takeException(), isNull);
      await tester.tap(find.text('Sáng'));
      await tester.pumpAndSettle();
      expect(Theme.of(ctx()).brightness, Brightness.light);
    });

    testWidgets('thả thanh tốc độ ⇒ PUT learning-settings với ttsRate mới, các trường khác giữ nguyên', (tester) async {
      useSmallPhone(tester);
      final saved = <LearningSettings>[];
      const initial = LearningSettings(
        dailyNewCards: 20,
        dailyReviewLimit: 300,
        desiredRetention: 0.85,
        ttsRate: 0.7,
        autoPlayAudio: false,
      );
      await tester.pumpWidget(
        buildTestApp(
          chineseAdapter: chineseWithSettings(initial: initial, saved: saved),
          identityAdapter: okSystemInfo('identity-service'),
          tts: FakeAfTts(voices: zhVoices),
        ),
      );
      await tester.pumpAndSettle();
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go('/ho-so?tab=giao-dien');
      await tester.pumpAndSettle();
      expect(find.text('0,70'), findsOneWidget); // từ server
      await tester.ensureVisible(find.byType(Slider));
      await tester.drag(find.byType(Slider), const Offset(300, 0));
      await tester.pumpAndSettle();
      expect(find.text('1,20'), findsOneWidget);
      expect(saved.single, initial.copyWith(ttsRate: 1.2));
      expect(container.read(ttsRateProvider), 1.2);
    });
  });
  group('đổi tài khoản trên cùng máy (review M4 C1)', () {
    testWidgets(
      'A tải xong → đăng xuất → B đăng nhập với GET treo ⇒ tab Học tập xoay, ttsRate mặc định, thả thanh không PUT trường của A; GET về ⇒ PUT B + tốc độ chờ',
      (tester) async {
        useSmallPhone(tester);
        const settingsA = LearningSettings(
          dailyNewCards: 33,
          dailyReviewLimit: 333,
          desiredRetention: 0.93,
          ttsRate: 1.1,
          autoPlayAudio: false,
        );
        const settingsB = LearningSettings(
          dailyNewCards: 5,
          dailyReviewLimit: 50,
          desiredRetention: 0.85,
          ttsRate: 0.7,
          autoPlayAudio: true,
        );
        var currentUser = 'u-1';
        final gateB = Completer<void>();
        final puts = <LearningSettings>[];
        final chinese = FakeAdapter((req) async {
          if (req.uri.path.endsWith('/me/learning-settings')) {
            if (req.method == 'PUT') {
              final body = LearningSettings.fromJson(asJsonMap(req.data)).copyWith(isDefault: false);
              puts.add(body);
              return (200, learningSettingsBody(body));
            }
            if (currentUser == 'u-2') await gateB.future; // GET của B treo
            return (200, learningSettingsBody(currentUser == 'u-1' ? settingsA : settingsB));
          }
          return okSystemInfo('chinese-backend').handler(req);
        });
        final store = InMemoryKeyValueStore({kInstallFlagKey: true});
        await tester.pumpWidget(
          buildTestApp(
            chineseAdapter: chinese,
            identityAdapter: okSystemInfo('identity-service'),
            store: store,
            tts: FakeAfTts(voices: zhVoices),
            meId: () => currentUser,
            authHandler: (req) {
              if (req.uri.path.endsWith('/login')) {
                return Future.value((200, tokenBody(withAccount: true, sub: 'u-2', email: 'b@vidu.com', name: 'B')));
              }
              return defaultAuthResponse(req);
            },
          ),
        );
        // A: mở tab Học tập ⇒ số của A; ttsRate = 1,1 (server) ⇒ cache theo người dùng A.
        await openProfile(tester, tab: 'Học tập');
        expect(find.textContaining('Từ mới mỗi ngày: 33'), findsOneWidget);
        final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
        expect(container.read(ttsRateProvider), 1.1);
        expect(store.snapshot[ttsRateCacheKey('u-1')], '1.1');
        expect(store.snapshot.containsKey(ttsRateCacheKey('u-2')), isFalse);

        // Đăng xuất (không màn nào theo dõi learningSettingsProvider) ⇒ B đăng nhập.
        await tester.tap(find.byType(BackButton));
        await tester.pumpAndSettle();
        await tester.ensureVisible(find.text('Đăng xuất'));
        await tester.tap(find.text('Đăng xuất'));
        await tester.pumpAndSettle();
        currentUser = 'u-2';
        await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'b@vidu.com');
        await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'matkhau-b');
        await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
        await tester.pumpAndSettle();
        // Đăng xuất từ "Thêm" ⇒ returnTo=/them ⇒ đăng nhập xong quay lại "Thêm" với tài khoản B.
        expect(find.text('b@vidu.com'), findsOneWidget);
        expect(find.byType(NavigationBar), findsOneWidget);

        // Tab Học tập của B: GET treo ⇒ vòng xoay (không pumpAndSettle được), KHÔNG hiện số của A.
        await openProfile(tester);
        final learningTab = find.descendant(of: find.byType(TabBar), matching: find.text('Học tập'));
        await tester.ensureVisible(learningTab);
        await tester.tap(learningTab);
        await tester.pump(const Duration(milliseconds: 400));
        await tester.pump(const Duration(milliseconds: 300));
        expect(find.byType(CircularProgressIndicator), findsWidgets);
        expect(find.textContaining('Từ mới mỗi ngày: 33'), findsNothing);
        expect(find.textContaining('Từ mới mỗi ngày'), findsNothing);

        // ttsRate của B: không lấy 1,1 của A (provider cũ / cache của A) ⇒ mặc định 0,8.
        final container2 = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
        expect(container2.read(ttsRateProvider), 0.8);
        expect(container2.read(autoPlayAudioProvider), isTrue); // mặc định, không phải false của A

        // Thả thanh tốc độ ở tab Giao diện khi GET còn treo ⇒ KHÔNG PUT (giữ chờ).
        final tab = find.descendant(of: find.byType(TabBar), matching: find.text('Giao diện'));
        await tester.ensureVisible(tab);
        await tester.tap(tab);
        // Hoạt ảnh chuyển tab 300 ms; không pumpAndSettle được vì vòng xoay của tab Học tập (GET treo) chạy mãi.
        await tester.pump(const Duration(milliseconds: 400));
        await tester.pump(const Duration(milliseconds: 400));
        await tester.ensureVisible(find.byType(Slider));
        await tester.drag(find.byType(Slider), const Offset(300, 0));
        await tester.pump(const Duration(milliseconds: 300));
        expect(puts, isEmpty);
        expect(container2.read(ttsRateProvider), 1.2);
        expect(store.snapshot[ttsRateCacheKey('u-2')], '1.2');

        // GET của B về ⇒ giá trị chờ thắng và được đẩy lên với CÁC TRƯỜNG CỦA B (không phải của A).
        gateB.complete();
        await tester.pumpAndSettle();
        expect(puts.single, settingsB.copyWith(ttsRate: 1.2, isDefault: false));
        expect(container2.read(ttsRateProvider), 1.2);
      },
    );
  });
}
