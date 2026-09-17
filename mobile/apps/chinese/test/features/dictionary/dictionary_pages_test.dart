import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/core/widgets/meaning_status_chip.dart';
import 'package:af_chinese/core/widgets/speak_button.dart';
import 'package:af_chinese/features/auth/presentation/pages/error_pages.dart';
import 'package:af_chinese/features/dictionary/application/providers.dart';
import 'package:af_chinese/features/dictionary/data/models.dart';
import 'package:af_chinese/features/dictionary/presentation/pages/character_detail_page.dart';
import 'package:af_chinese/features/dictionary/presentation/pages/dictionary_search_page.dart';
import 'package:af_chinese/features/dictionary/presentation/pages/word_detail_page.dart';
import 'package:af_chinese/features/dictionary/presentation/widgets/word_detail_view.dart';
import 'package:af_chinese/features/dictionary/presentation/widgets/word_list_tile.dart';
import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';
import '../../helpers/test_app.dart';

/// Mọi cách gõ phải ra 爱 (hợp đồng M8 — tiêu chí web-dev, mô phỏng ở server giả).
const kAiQueries = ['爱', 'ai4', 'ài', 'ai', 'yêu', 'yeu', 'ái'];

/// Máy chủ từ điển giả: `q` rỗng ⇒ 500 từ lộ trình (20 mỗi trang, id `w-<n>`); `q` ∈ [kAiQueries] ⇒ 爱; khác ⇒ rỗng.
/// `/dictionary/words/{id}` từ fixture (`srs` xuất hiện sau khi POST `/srs/cards`), id `xxx` ⇒ 404; chữ 爱 từ fixture.
/// [unavailable] ⇒ 503 `CONTENT_UNAVAILABLE`; [failPage2Once] ⇒ trang 2 lỗi 500 một lần.
class FakeDictionaryServer {
  bool unavailable = false;
  bool failPage2Once = false;

  /// Trang 2 trả `items` rỗng nhưng `totalCount` vẫn 500 (học liệu đổi giữa lúc phân trang) ⇒ app phải dừng tải.
  bool page2Empty = false;

  /// Trang 2 trả lại đúng 20 dòng của trang 1 (trùng id) ⇒ app khử trùng + dừng.
  bool page2Duplicate = false;
  bool added = false;
  int summaryCalls = 0;
  final List<Map<String, String>> searchCalls = [];
  final List<Object?> addBodies = [];

  Map<String, Object?> _browseItem(int n) => {
    'id': 'w-$n',
    'simplified': '字$n',
    'pinyin': 'zi4',
    'hsk3Level': 1,
    'hanViet': 'tự',
    'meaningsVi': ['chữ số $n'],
    'meaningViStatus': n.isEven ? 'machine' : 'reviewed',
    'matchKind': 'browse',
  };

  Future<(int, String)> handle(RequestOptions req) async {
    final path = req.uri.path;
    if (path.endsWith('/srs/summary')) {
      summaryCalls++;
      return (
        200,
        jsonEncode({
          'localDate': '2026-09-18',
          'timeZone': 'Asia/Ho_Chi_Minh',
          'dueToday': 0,
          'dueNow': 0,
          'reviewedToday': 0,
          'reviewsDoneToday': 0,
          'reviewLimitRemaining': 200,
          'dailyReviewLimit': 200,
          'newIntroducedToday': 0,
          'newAvailableToday': added ? 1 : 0,
          'dailyNewCards': 10,
          'totalCards': added ? 1 : 0,
          'matureCards': 0,
        }),
      );
    }
    if (path.endsWith('/srs/cards') && req.method == 'POST') {
      added = true;
      addBodies.add(req.data is String ? jsonDecode(req.data as String) : req.data);
      return (
        201,
        jsonEncode({
          'added': 1,
          'skipped': 0,
          'cards': [
            {'cardId': 'c1', 'wordId': 'w1', 'created': true, 'state': 'new', 'isSuspended': false},
          ],
        }),
      );
    }
    if (unavailable && path.contains('/dictionary/')) {
      return (503, jsonEncode({'error': 'Học liệu chưa sẵn sàng.', 'code': 'CONTENT_UNAVAILABLE'}));
    }
    if (path.endsWith('/dictionary/search')) {
      final params = req.uri.queryParameters;
      searchCalls.add(params);
      final q = params['q'] ?? '';
      final page = int.parse(params['page'] ?? '1');
      if (q.isEmpty) {
        if (page == 2 && failPage2Once) {
          failPage2Once = false;
          return (500, jsonEncode({'error': 'Lỗi máy chủ.'}));
        }
        if (page == 2 && page2Empty) {
          return (200, jsonEncode({'items': <Object?>[], 'page': 2, 'pageSize': 20, 'totalCount': 500}));
        }
        if (page == 2 && page2Duplicate) {
          return (
            200,
            jsonEncode({
              'items': [for (var i = 1; i <= 20; i++) _browseItem(i)],
              'page': 2,
              'pageSize': 20,
              'totalCount': 500,
            }),
          );
        }
        return (
          200,
          jsonEncode({
            'items': [for (var i = (page - 1) * 20 + 1; i <= page * 20; i++) _browseItem(i)],
            'page': page,
            'pageSize': 20,
            'totalCount': 500,
          }),
        );
      }
      if (kAiQueries.contains(q)) {
        final fixture = loadFixture('dictionary_search.json');
        fixture['items'] = [(fixture['items'] as List).first];
        fixture['totalCount'] = 1;
        return (200, jsonEncode(fixture));
      }
      return (200, jsonEncode({'items': <Object?>[], 'page': 1, 'pageSize': 20, 'totalCount': 0}));
    }
    if (path.contains('/dictionary/words/')) {
      final id = req.uri.pathSegments.last;
      if (id == 'xxx') return (404, jsonEncode({'error': 'Không tìm thấy từ.'}));
      final word = loadFixture('dictionary_word.json');
      word['id'] = id;
      if (added) {
        word['srs'] = {'cardId': 'c1', 'state': 'new', 'dueAt': '2026-09-18T00:00:00Z', 'isSuspended': false};
      }
      return (200, jsonEncode(word));
    }
    if (path.contains('/dictionary/characters/')) {
      final hanzi = req.uri.pathSegments.last; // đã giải mã URL
      if (hanzi == '爱') return (200, jsonEncode(loadFixture('dictionary_character.json')));
      if (hanzi.characters.length != 1) {
        return (
          400,
          jsonEncode({
            'error': 'Dữ liệu không hợp lệ.',
            'code': 'VALIDATION',
            'details': {
              'hanzi': ['Phải là một chữ Hán.'],
            },
          }),
        );
      }
      return (404, jsonEncode({'error': 'Không tìm thấy chữ.'}));
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

  /// Mở app đã đăng nhập rồi vào Từ điển: qua "Thêm" (mặc định) hoặc deep link [location] (`/tu-dien?q=…`,
  /// `/tu-dien/chu/%E7%88%B1`…).
  Future<FakeDictionaryServer> openDictionary(
    WidgetTester tester, {
    FakeDictionaryServer? server,
    String? location,
    KeyValueStore? store,
    List<RequestOptions>? requestLog,
  }) async {
    final srv = server ?? FakeDictionaryServer();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: FakeAdapter((req) {
          if (req.uri.path.contains('/dictionary/')) return srv.handle(req);
          return okSystemInfo('chinese-backend').handler(req);
        }),
        identityAdapter: okSystemInfo('identity-service'),
        srs: srv.handle,
        tts: FakeAfTts(voices: zhVoices),
        store: store,
        requestLog: requestLog,
      ),
    );
    await tester.pumpAndSettle();
    if (location != null) {
      final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
      container.read(routerProviderForTest).go(location);
    } else {
      await tester.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text('Thêm')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Tra từ'));
    }
    await tester.pumpAndSettle();
    return srv;
  }

  DictionarySearchState searchState(WidgetTester tester) =>
      ProviderScope.containerOf(tester.element(find.byType(NavigationBar))).read(dictionarySearchProvider);

  /// Chạm mở một dòng kết quả: chạm vào mép trái (chữ Hán) — chip "Chưa duyệt" bên phải có tooltip chạm riêng, ở
  /// chữ 1.3× chip rộng tới tâm dòng nên `tap` tâm sẽ mở tooltip thay vì trang.
  Future<void> tapTile(WidgetTester tester, String id) async {
    final tile = find.byKey(wordTileKey(id));
    await tester.ensureVisible(tile);
    await tester.tapAt(tester.getTopLeft(tile) + const Offset(24, 24));
    await tester.pumpAndSettle();
  }

  Future<void> goBack(WidgetTester tester) async {
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
  }

  testWidgets('Thêm → Tra từ: lộ trình HSK 1 (500 từ), cuộn xuống ⇒ tải trang 2; mở chi tiết rồi quay lại giữ vị trí', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openDictionary(tester);
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget); // push trong nhánh "Thêm" ⇒ còn bottom nav
    expect(find.text('Lộ trình HSK 1 (500 từ)'), findsOneWidget);
    expect(server.searchCalls, hasLength(1));
    expect(server.searchCalls.single.containsKey('q'), isFalse);
    expect(server.searchCalls.single['pageSize'], '20');
    expect(find.byKey(wordTileKey('w-1')), findsOneWidget);
    expect(tester.getSize(find.byKey(wordTileKey('w-1'))).height, greaterThanOrEqualTo(64));
    expect(find.text('Chưa duyệt'), findsWidgets); // dòng chẵn nghĩa `machine`
    expect(searchState(tester).items, hasLength(20));
    expect(tester.takeException(), isNull);

    // Cuộn tới gần cuối ⇒ tải trang 2 (còn ≤ 5 dòng cuối) ⇒ 40 dòng.
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    expect(server.searchCalls.map((c) => c['page']), contains('2'));
    expect(searchState(tester).items, hasLength(40));
    expect(searchState(tester).hasMore, isTrue);

    // Mở chi tiết một dòng đang thấy ⇒ quay lại: cùng dòng ở cùng vị trí, không gọi lại API tìm.
    final visible = find.byKey(wordTileKey('w-18'));
    expect(visible, findsOneWidget);
    await tester.ensureVisible(visible); // `tapTile` cũng ensureVisible — đo vị trí SAU đó để so đúng
    await tester.pumpAndSettle();
    final before = tester.getTopLeft(visible);
    expect(before.dy, greaterThan(100)); // đã cuộn: dòng 18 nằm giữa màn, không phải đầu danh sách
    final callsBefore = server.searchCalls.length;
    await tapTile(tester, 'w-18');
    expect(find.byType(WordDetailPage), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    await goBack(tester);
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(find.byType(WordDetailPage), findsNothing);
    expect(tester.getTopLeft(find.byKey(wordTileKey('w-18'))), before);
    expect(server.searchCalls, hasLength(callsBefore));
    expect(searchState(tester).items, hasLength(40));

    // Về "Thêm" rồi mở lại: state trong nhánh còn nguyên (không gọi lại API), ô tìm rỗng.
    await goBack(tester);
    expect(find.text('Pinyin & luyện thanh'), findsOneWidget);
    await tester.tap(find.text('Tra từ'));
    await tester.pumpAndSettle();
    expect(find.text('Lộ trình HSK 1 (500 từ)'), findsOneWidget);
    expect(server.searchCalls, hasLength(callsBefore));
  });

  testWidgets(
    'gõ "ai" ⇒ debounce 300 ms một lời gọi ⇒ 爱; chi tiết từ; Thêm vào ôn tập ⇒ Chờ học + tóm tắt SRS làm mới; chạm chữ '
    '⇒ trang chữ; quay lại giữ kết quả',
    (tester) async {
      useSmallPhone(tester);
      final server = await openDictionary(tester);
      final summaryBefore = server.summaryCalls;

      await tester.enterText(find.byKey(kDictionarySearchFieldKey), 'a');
      await tester.pump(const Duration(milliseconds: 100));
      await tester.enterText(find.byKey(kDictionarySearchFieldKey), 'ai');
      await tester.pump(const Duration(milliseconds: 200));
      expect(server.searchCalls, hasLength(1)); // chưa hết 300 ms kể từ lần gõ cuối
      await tester.pump(const Duration(milliseconds: 150));
      await tester.pumpAndSettle();
      expect(server.searchCalls, hasLength(2));
      expect(server.searchCalls.last['q'], 'ai');
      expect(find.text('1 kết quả cho “ai”'), findsOneWidget);
      expect(find.byKey(wordTileKey('w1')), findsOneWidget);
      expect(find.text('ài'), findsOneWidget);
      expect(find.text('ÁI'), findsOneWidget);
      expect(find.text('yêu; thích'), findsOneWidget);
      expect(find.text('Chưa duyệt'), findsOneWidget);

      // Chi tiết từ.
      await tapTile(tester, 'w1');
      expect(find.byType(WordDetailPage), findsOneWidget);
      expect(find.text('Chi tiết từ'), findsOneWidget);
      expect(find.text('爱'), findsWidgets);
      expect(find.text('ài'), findsWidgets); // pinyin 20 + cách đọc trong ô chữ
      expect(find.text('HÁN VIỆT: ÁI'), findsOneWidget);
      expect(find.text('Hán Việt suy ra'), findsOneWidget);
      expect(find.text('HSK 3.0 cấp 1'), findsOneWidget);
      expect(find.text('động từ'), findsOneWidget);
      expect(find.text('danh từ'), findsOneWidget);
      expect(find.text('1. yêu'), findsOneWidget);
      expect(find.text('Chưa duyệt'), findsOneWidget);
      expect(find.text('Nghĩa tiếng Anh (CC-CEDICT)'), findsOneWidget);
      expect(find.text('1. to love'), findsNothing); // ExpansionTile đóng
      expect(find.text('Chữ trong từ'), findsOneWidget);
      expect(find.byKey(characterTileKey('爱')), findsOneWidget);
      expect(find.text('Thêm vào ôn tập'), findsOneWidget);
      expect(find.byType(SpeakButton), findsOneWidget);
      expect(tester.takeException(), isNull);

      // Thêm vào ôn tập ⇒ POST /srs/cards {wordIds:[w1]} ⇒ toast + chi tiết tải lại (chip "Chờ học") + tóm tắt SRS.
      await tester.ensureVisible(find.text('Thêm vào ôn tập'));
      await tester.tap(find.text('Thêm vào ôn tập'));
      await tester.pumpAndSettle();
      expect(server.addBodies, [
        {
          'wordIds': ['w1'],
        },
      ]);
      expect(find.text('Đã thêm — thẻ sẽ xuất hiện trong lượt từ mới'), findsOneWidget);
      expect(find.text('Chờ học'), findsOneWidget);
      expect(find.text('Thêm vào ôn tập'), findsNothing);
      expect(server.summaryCalls, greaterThan(summaryBefore));
      // Huy hiệu "Ôn tập" đổi theo tóm tắt mới (newAvailableToday 1).
      expect(find.descendant(of: find.byType(NavigationBar), matching: find.text('1')), findsOneWidget);

      // Chạm ô chữ ⇒ trang chữ.
      await tester.ensureVisible(find.byKey(characterTileKey('爱')));
      await tester.tap(find.byKey(characterTileKey('爱')));
      await tester.pumpAndSettle();
      expect(find.byType(CharacterDetailPage), findsOneWidget);
      expect(find.text('Chi tiết chữ'), findsOneWidget);
      expect(find.text('Cách đọc'), findsOneWidget);
      expect(find.text('ài'), findsWidgets);
      expect(find.text('Số nét'), findsOneWidget);
      expect(find.text('10'), findsOneWidget);
      expect(find.text('Bộ thủ'), findsOneWidget);
      expect(find.text('爪'), findsOneWidget);
      expect(find.text(' (bộ số 87)'), findsOneWidget);
      expect(find.text('Phồn thể'), findsOneWidget);
      expect(find.text('愛'), findsOneWidget);
      expect(find.text('Từ có chữ này'), findsOneWidget);
      expect(find.byKey(wordTileKey('w2')), findsOneWidget);
      // Nút "Luyện viết chữ này" chỉ hiện sau M10 (nhãn "Luyện viết" ở bottom nav là của shell).
      expect(
        find.descendant(of: find.byType(CharacterDetailPage), matching: find.textContaining('Luyện viết')),
        findsNothing,
      );
      expect(tester.takeException(), isNull);

      // Từ có chữ này ⇒ chi tiết từ 爱好 (push) ⇒ quay lại chữ ⇒ quay lại từ ⇒ quay lại tìm: kết quả "ai" còn nguyên.
      await tapTile(tester, 'w2');
      expect(find.byType(WordDetailPage), findsOneWidget);
      await goBack(tester);
      expect(find.byType(CharacterDetailPage), findsOneWidget);
      await goBack(tester);
      expect(find.byType(WordDetailPage), findsOneWidget);
      final calls = server.searchCalls.length;
      await goBack(tester);
      expect(find.byType(DictionarySearchPage), findsOneWidget);
      expect(find.text('1 kết quả cho “ai”'), findsOneWidget);
      expect(server.searchCalls, hasLength(calls));
      final field = tester.widget<TextField>(find.byKey(kDictionarySearchFieldKey));
      expect(field.controller?.text, 'ai');

      // Nút xoá ⇒ về lộ trình.
      await tester.tap(find.byTooltip('Xoá'));
      await tester.pumpAndSettle();
      expect(find.text('Lộ trình HSK 1 (500 từ)'), findsOneWidget);
    },
  );

  testWidgets('mọi cách gõ 爱/ai4/ài/ai/yêu/yeu/ái đều ra 爱; "zzz" ⇒ không tìm thấy + gợi ý', (tester) async {
    useSmallPhone(tester);
    final server = await openDictionary(tester);
    for (final q in kAiQueries) {
      await tester.enterText(find.byKey(kDictionarySearchFieldKey), q);
      await tester.pump(const Duration(milliseconds: 300));
      await tester.pumpAndSettle();
      expect(server.searchCalls.last['q'], q);
      expect(find.text('1 kết quả cho “$q”'), findsOneWidget, reason: q);
      expect(find.byKey(wordTileKey('w1')), findsOneWidget, reason: q);
    }
    await tester.enterText(find.byKey(kDictionarySearchFieldKey), 'zzz');
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    expect(find.text('0 kết quả cho “zzz”'), findsOneWidget);
    expect(find.text('Không tìm thấy “zzz”.'), findsOneWidget);
    expect(find.textContaining('Thử gõ theo cách khác'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('chip HSK 1 ⇒ gửi hsk=1, bật lại ⇒ bỏ; trang 2 lỗi ⇒ "Thử lại" ở cuối ⇒ tải được', (tester) async {
    useSmallPhone(tester);
    final server = await openDictionary(tester);
    await tester.tap(find.byKey(kHskChipKey));
    await tester.pumpAndSettle();
    expect(server.searchCalls.last['hsk'], '1');
    expect(searchState(tester).hsk, 1);
    await tester.tap(find.byKey(kHskChipKey));
    await tester.pumpAndSettle();
    expect(server.searchCalls.last.containsKey('hsk'), isFalse);
    expect(searchState(tester).hsk, isNull);

    server.failPage2Once = true;
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    expect(searchState(tester).loadMoreError, isNotNull);
    expect(searchState(tester).items, hasLength(20));
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    expect(find.byKey(kLoadMoreRetryKey), findsOneWidget);
    await tester.tap(find.byKey(kLoadMoreRetryKey));
    await tester.pumpAndSettle();
    expect(searchState(tester).loadMoreError, isNull);
    expect(searchState(tester).items, hasLength(40));
    expect(tester.takeException(), isNull);
  });

  testWidgets('trang 2 rỗng (totalCount vẫn 500) ⇒ đúng MỘT lời gọi trang 2, footer thành dòng ghi công; trang trùng ⇒ '
      'khử trùng + dừng', (tester) async {
    useSmallPhone(tester);
    final server = FakeDictionaryServer()..page2Empty = true;
    await openDictionary(tester, server: server);
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    expect(server.searchCalls.where((c) => c['page'] == '2'), hasLength(1));
    expect(server.searchCalls.where((c) => c['page'] == '3'), isEmpty);
    expect(searchState(tester).items, hasLength(20));
    expect(searchState(tester).hasMore, isFalse);
    expect(find.textContaining('Dữ liệu đã được chỉnh sửa'), findsOneWidget); // footer ghi công
    expect(find.byType(CircularProgressIndicator), findsNothing);
    expect(tester.takeException(), isNull);

    // Kéo-làm-mới ⇒ tải lại từ trang 1 với server trả trùng ở trang 2.
    server.page2Empty = false;
    server.page2Duplicate = true;
    await tester.drag(find.byType(ListView), const Offset(0, 2500));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListView), const Offset(0, 600));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListView), const Offset(0, -1500));
    await tester.pumpAndSettle();
    final ids = searchState(tester).items.map((w) => w.id).toList();
    expect(ids.toSet().length, ids.length); // không trùng id
    expect(ids, hasLength(20));
    expect(searchState(tester).hasMore, isFalse);
    expect(server.searchCalls.where((c) => c['page'] == '3'), isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('deep link ?q= dài hơn 64 ký tự bị cắt còn 64 (không gửi q quá dài ⇒ 400)', (tester) async {
    useSmallPhone(tester);
    final long = List.filled(70, 'a').join();
    final server = await openDictionary(tester, location: '/tu-dien?q=$long');
    expect(server.searchCalls.last['q']?.length, 64);
    expect(tester.widget<TextField>(find.byKey(kDictionarySearchFieldKey)).controller?.text.length, 64);
  });

  testWidgets('deep link /tu-dien?q=yêu ⇒ ô tìm có "yêu" và kết quả 爱; /tu-dien/chu/%E7%88%B1 ⇒ trang chữ 爱', (
    tester,
  ) async {
    useSmallPhone(tester);
    final server = await openDictionary(tester, location: '/tu-dien?q=y%C3%AAu');
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(tester.widget<TextField>(find.byKey(kDictionarySearchFieldKey)).controller?.text, 'yêu');
    expect(server.searchCalls.last['q'], 'yêu');
    expect(find.text('1 kết quả cho “yêu”'), findsOneWidget);

    final container = ProviderScope.containerOf(tester.element(find.byType(NavigationBar)));
    container.read(routerProviderForTest).go('/tu-dien/chu/%E7%88%B1');
    await tester.pumpAndSettle();
    expect(find.byType(CharacterDetailPage), findsOneWidget);
    expect(find.text('爪'), findsOneWidget);
    // Không có gì để pop (deep link) ⇒ nút quay lại đưa về /tu-dien.
    await goBack(tester);
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('id lạ ⇒ /404 (giữ nút quay lại); chữ không có ⇒ /404; 2 ký tự ⇒ 400 báo tại chỗ', (tester) async {
    useSmallPhone(tester);
    await openDictionary(tester, location: '/tu-dien/xxx');
    expect(find.byType(NotFoundPage), findsOneWidget);
    expect(tester.takeException(), isNull);

    final container = ProviderScope.containerOf(tester.element(find.byType(NotFoundPage)));
    container.read(routerProviderForTest).go('/tu-dien/chu/%E4%BD%A0%E5%A5%BD'); // 你好
    await tester.pumpAndSettle();
    expect(find.byType(CharacterDetailPage), findsOneWidget);
    expect(find.text('Yêu cầu không hợp lệ'), findsOneWidget);
    expect(find.text('Phải là một chữ Hán.'), findsOneWidget);
    expect(find.text('Thử lại'), findsNothing);
  });

  testWidgets('503 CONTENT_UNAVAILABLE ⇒ "Học liệu chưa sẵn sàng" không nút thử lại, không điều hướng', (tester) async {
    useSmallPhone(tester);
    final server = FakeDictionaryServer()..unavailable = true;
    await openDictionary(tester, server: server);
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(find.text('Học liệu chưa sẵn sàng'), findsOneWidget);
    expect(find.textContaining('Từ điển chưa được nạp dữ liệu'), findsOneWidget);
    expect(find.text('Thử lại'), findsNothing);

    // Học liệu nạp xong ⇒ kéo để làm mới ⇒ có dữ liệu.
    server.unavailable = false;
    await tester.drag(find.byType(ListView), const Offset(0, 400));
    await tester.pumpAndSettle();
    expect(find.text('Lộ trình HSK 1 (500 từ)'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('sheet "Xem chi tiết" trong phiên ôn: ô chữ KHÔNG dẫn đi (linkCharacters=false)', (tester) async {
    useSmallPhone(tester);
    await tester.pumpWidget(
      speechTestApp(
        Scaffold(
          body: SingleChildScrollView(
            child: WordDetailView(
              word: WordDetail.fromJson(loadFixture('dictionary_word.json')),
              linkCharacters: false,
            ),
          ),
        ),
        tts: FakeAfTts(voices: zhVoices),
      ),
    );
    await tester.pumpAndSettle();
    final tile = find.byKey(characterTileKey('爱'));
    expect(tile, findsOneWidget);
    final ink = tester.widget<InkWell>(find.descendant(of: tile, matching: find.byType(InkWell)));
    expect(ink.onTap, isNull);
  });

  testWidgets('360×740, chữ 1.3×, chế độ tối: tìm / chi tiết từ / chi tiết chữ không overflow', (tester) async {
    useSmallPhone(tester, textScale: 1.3);
    final store = InMemoryKeyValueStore({kInstallFlagKey: true, 'af.themeMode': 'dark'});
    await openDictionary(tester, store: store, location: '/tu-dien?q=ai');
    expect(find.byType(DictionarySearchPage), findsOneWidget);
    expect(tester.takeException(), isNull);
    expect(Theme.of(tester.element(find.byType(DictionarySearchPage))).brightness, Brightness.dark);
    expect(find.byKey(wordTileKey('w1')), findsOneWidget);
    // Chạm vào chip "Chưa duyệt" trong dòng vẫn mở từ (tooltip chỉ khi giữ lâu — review M8).
    await tester.tap(find.text('Chưa duyệt'));
    await tester.pumpAndSettle();
    expect(find.byType(WordDetailPage), findsOneWidget);
    expect(find.text(kMachineMeaningTooltip), findsNothing);
    await goBack(tester);
    // Ở trang chi tiết chip vẫn mở tooltip khi chạm.
    await tapTile(tester, 'w1');
    expect(find.byType(WordDetailPage), findsOneWidget);
    await tester.tap(find.text('Chưa duyệt'));
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.text(kMachineMeaningTooltip), findsOneWidget);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.ensureVisible(find.byKey(characterTileKey('爱')));
    await tester.tap(find.byKey(characterTileKey('爱')));
    await tester.pumpAndSettle();
    expect(find.byType(CharacterDetailPage), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
