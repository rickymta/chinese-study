import 'dart:convert';

import 'package:af_chinese/features/auth/data/me_api.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  test('loadMe: GET /me với skipErrorRedirect, parse fixture me.json', () async {
    final adapter = FakeAdapter((_) async => (200, jsonEncode(loadFixture('me.json'))));
    final dio = createApiClient(baseUrl: testConfig.chineseApiUrl, getAccessToken: () => 'tok')
      ..httpClientAdapter = adapter;
    final me = await loadMe(dio);
    expect(me.id, '0192');
    expect(me.roles, ['learner']);
    expect(me.permissions, {'study.use'});
    expect(me.timeZone, 'Asia/Ho_Chi_Minh');
    final req = adapter.requests.single;
    expect(req.uri.path, endsWith('/me'));
    expect(req.skipErrorRedirect, isTrue);
    expect(req.headers['Authorization'], 'Bearer tok');
  });

  test('loadMe fail-closed: thân thiếu permissions ⇒ 0 quyền; 403 ⇒ ném ApiError 403', () async {
    final noPerm = FakeAdapter((_) async => (200, '{"id":"x"}'));
    final dio = createApiClient(baseUrl: testConfig.chineseApiUrl)..httpClientAdapter = noPerm;
    expect((await loadMe(dio)).permissions, isEmpty);

    final forbidden = FakeAdapter((_) async => (403, '{"error":"Không có quyền","code":"FORBIDDEN"}'));
    final dio2 = createApiClient(baseUrl: testConfig.chineseApiUrl)..httpClientAdapter = forbidden;
    await expectLater(loadMe(dio2), throwsA(isA<ApiError>().having((e) => e.status, 'status', 403)));
  });
}
