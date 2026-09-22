import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/fake_adapter.dart';

void main() {
  const base = 'http://localhost:5280/chinese/api';

  Dio client(
    FakeHttpClientAdapter adapter, {
    String? Function()? getAccessToken,
    Future<String> Function()? refresh,
    void Function()? onAuthLost,
    void Function(int status, RequestOptions req)? onErrorRedirect,
    Map<String, String> headers = const {},
  }) {
    final dio = createApiClient(
      baseUrl: base,
      headers: headers,
      getAccessToken: getAccessToken,
      refresh: refresh,
      onAuthLost: onAuthLost,
      onErrorRedirect: onErrorRedirect,
    );
    dio.httpClientAdapter = adapter;
    return dio;
  }

  group('createApiClient — header & token', () {
    test('gửi Accept JSON, header cố định và Bearer khi có token', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(200, {'ok': true}));
      final dio = client(
        adapter,
        headers: {'X-AF-Client': 'chinese-mobile/0.1.0+1 (web)'},
        getAccessToken: () => 'tok',
      );
      final res = await dio.get<Map<String, Object?>>('/me');
      expect(res.data, {'ok': true});
      final req = adapter.requests.single;
      expect(req.headers['Accept'], 'application/json');
      expect(req.headers['X-AF-Client'], 'chinese-mobile/0.1.0+1 (web)');
      expect(req.headers['Authorization'], 'Bearer tok');
      expect(req.uri.toString(), '$base/me');
    });

    test('không gắn Authorization khi không có token', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(200, {}));
      await client(adapter, getAccessToken: () => null).get<Object?>('/system/info');
      expect(adapter.requests.single.headers.containsKey('Authorization'), isFalse);
    });
  });

  group('createApiClient — làm mới phiên khi 401', () {
    test('request skipAuthHeader bị 401 ⇒ KHÔNG gửi lại bằng token hiện tại (đi thẳng đường làm mới)', () async {
      var refreshCalls = 0;
      final adapter = FakeHttpClientAdapter((req, _) async {
        if (req.headers['Authorization'] == 'Bearer new') return FakeResponse.json(200, {'ok': true});
        return FakeResponse.json(401, {'error': 'Cần đăng nhập'});
      });
      final dio = client(
        adapter,
        getAccessToken: () => 'tok',
        refresh: () async {
          refreshCalls++;
          return 'new';
        },
      );
      await dio.get<Object?>('/public', options: afOptions(skipAuthHeader: true));
      expect(refreshCalls, 1);
      expect(adapter.requests.map((r) => r.headers['Authorization']), [null, 'Bearer new']);
    });

    test('refresh bị từ chối nhưng token hiện tại đã đổi (request mang token cũ) ⇒ KHÔNG onAuthLost', () async {
      var lost = 0;
      var token = 'cu';
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'Cần đăng nhập'}));
      final dio = client(
        adapter,
        getAccessToken: () => token,
        refresh: () async {
          token = 'moi'; // phiên khác đã đăng nhập trong lúc chờ
          throw ApiError('mất', status: 401);
        },
        onAuthLost: () => lost++,
      );
      await expectLater(
        dio.get<Object?>('/me', options: Options(headers: {'Authorization': 'Bearer cu'})),
        throwsA(isA<ApiError>()),
      );
      expect(lost, 0);
    });

    test('gửi lại vẫn 401 nhưng token hiện tại đã đổi ⇒ KHÔNG onAuthLost; token trùng ⇒ có', () async {
      var lost = 0;
      var token = 'cu';
      final adapter = FakeHttpClientAdapter((req, i) async {
        if (i == 1) token = 'khac'; // đổi phiên đúng lúc gửi lại
        return FakeResponse.json(401, {'error': 'Cần đăng nhập'});
      });
      final dio = client(adapter, getAccessToken: () => token, refresh: () async => 'new', onAuthLost: () => lost++);
      await expectLater(
        dio.get<Object?>('/me', options: Options(headers: {'Authorization': 'Bearer cu'})),
        throwsA(isA<ApiError>()),
      );
      expect(lost, 0);

      var token2 = 'cu';
      final adapter2 = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'Cần đăng nhập'}));
      var lost2 = 0;
      final dio2 = client(
        adapter2,
        getAccessToken: () => token2,
        refresh: () async {
          token2 = 'new';
          return 'new';
        },
        onAuthLost: () => lost2++,
      );
      await expectLater(dio2.get<Object?>('/me'), throwsA(isA<ApiError>()));
      expect(lost2, 1);
    });

    test('401 mang token CŨ ⇒ gửi lại bằng token hiện tại, KHÔNG gọi refresh', () async {
      var refreshCalls = 0;
      final adapter = FakeHttpClientAdapter((req, _) async {
        if (req.headers['Authorization'] == 'Bearer moi') return FakeResponse.json(200, {'ok': true});
        return FakeResponse.json(401, {'error': 'Cần đăng nhập'});
      });
      var token = 'cu';
      final dio = client(
        adapter,
        getAccessToken: () => token,
        refresh: () async {
          refreshCalls++;
          return 'refreshed';
        },
      );
      // Request đi với token cũ; trước khi 401 về, phiên đã đổi sang token mới.
      final future = dio.get<Object?>('/me', options: Options(headers: {'Authorization': 'Bearer cu'}));
      token = 'moi';
      final res = await future;
      expect(res.data, {'ok': true});
      expect(refreshCalls, 0);
      expect(adapter.requests, hasLength(2));
      expect(adapter.requests.last.headers['Authorization'], 'Bearer moi');
    });

    test('skipAuthHeader ⇒ không gắn Authorization dù có token', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(200, {}));
      await client(adapter, getAccessToken: () => 'tok').post<Object?>(
        '/auth/mobile/refresh',
        data: {'refreshToken': 'x'},
        options: afOptions(skipAuthRefresh: true, skipAuthHeader: true),
      );
      expect(adapter.requests.single.headers.containsKey('Authorization'), isFalse);
    });

    test('401 → refresh → gửi lại một lần thành công', () async {
      final adapter = FakeHttpClientAdapter((req, i) async {
        if (req.headers['Authorization'] == 'Bearer new') return FakeResponse.json(200, {'id': 1});
        return FakeResponse.json(401, {'error': 'Cần đăng nhập'});
      });
      var refreshCalls = 0;
      var lost = 0;
      final dio = client(
        adapter,
        getAccessToken: () => 'old',
        refresh: () async {
          refreshCalls++;
          return 'new';
        },
        onAuthLost: () => lost++,
      );
      final res = await dio.get<Map<String, Object?>>('/me');
      expect(res.data, {'id': 1});
      expect(refreshCalls, 1);
      expect(lost, 0);
      expect(adapter.requests, hasLength(2));
      expect(adapter.requests[1].skipAuthRefresh, isTrue);
    });

    test('hai request 401 đồng thời ⇒ refresh đúng 1 lần', () async {
      final refreshCompleter = Completer<String>();
      var refreshCalls = 0;
      final adapter = FakeHttpClientAdapter((req, i) async {
        if (req.headers['Authorization'] == 'Bearer new') return FakeResponse.json(200, {'p': req.path});
        return FakeResponse.json(401, {'error': 'x'});
      });
      final dio = client(
        adapter,
        getAccessToken: () => 'old',
        refresh: () {
          refreshCalls++;
          return refreshCompleter.future;
        },
      );
      final f1 = dio.get<Map<String, Object?>>('/a');
      final f2 = dio.get<Map<String, Object?>>('/b');
      // Chờ cả hai request đã nhận 401 và vào interceptor (dio có nhiều bước bất đồng bộ) rồi mới trả token.
      while (adapter.requests.length < 2) {
        await Future<void>.delayed(const Duration(milliseconds: 1));
      }
      await Future<void>.delayed(const Duration(milliseconds: 20));
      refreshCompleter.complete('new');
      final results = await Future.wait([f1, f2]);
      expect(refreshCalls, 1);
      expect(results.map((r) => r.data?['p']), ['/a', '/b']);
    });

    test('refresh trả 401 ⇒ onAuthLost đúng 1 lần, lỗi trả về là 401 gốc', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'Cần đăng nhập'}));
      var lost = 0;
      final dio = client(
        adapter,
        getAccessToken: () => 'old',
        refresh: () async => throw ApiError('Phiên hết hạn', status: 401, code: 'REFRESH_INVALID'),
        onAuthLost: () => lost++,
      );
      await expectLater(
        dio.get<Object?>('/me'),
        throwsA(
          isA<ApiError>().having((e) => e.status, 'status', 401).having((e) => e.message, 'message', 'Cần đăng nhập'),
        ),
      );
      expect(lost, 1);
      expect(adapter.requests, hasLength(1));
    });

    test('refresh lỗi mạng ⇒ KHÔNG onAuthLost (giữ phiên), trả 401 gốc', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'x'}));
      var lost = 0;
      final dio = client(
        adapter,
        refresh: () async => throw ApiError('Không kết nối được máy chủ.'),
        onAuthLost: () => lost++,
      );
      await expectLater(dio.get<Object?>('/me'), throwsA(isA<ApiError>().having((e) => e.status, 'status', 401)));
      expect(lost, 0);
    });

    test('gửi lại vẫn 401 ⇒ onAuthLost', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'x'}));
      var lost = 0;
      final dio = client(adapter, refresh: () async => 'new', onAuthLost: () => lost++);
      await expectLater(dio.get<Object?>('/me'), throwsA(isA<ApiError>()));
      expect(lost, 1);
      expect(adapter.requests, hasLength(2));
    });

    test('URL /auth/mobile/login 401 ⇒ không refresh', () async {
      final adapter = FakeHttpClientAdapter(
        (_, _) async =>
            FakeResponse.json(401, {'error': 'Email hoặc mật khẩu không đúng.', 'code': 'INVALID_CREDENTIALS'}),
      );
      var refreshCalls = 0;
      final dio = client(
        adapter,
        refresh: () async {
          refreshCalls++;
          return 'x';
        },
      );
      await expectLater(
        dio.post<Object?>('/auth/mobile/login', data: {'email': 'a@b.c'}),
        throwsA(isA<ApiError>().having((e) => e.code, 'code', 'INVALID_CREDENTIALS')),
      );
      expect(refreshCalls, 0);
    });

    test('isAuthUrl khớp đúng 4 endpoint phiên, không khớp /auth/mobile/password', () {
      expect(isAuthUrl('/auth/login'), isTrue);
      expect(isAuthUrl('/auth/mobile/refresh'), isTrue);
      expect(isAuthUrl('http://x/identity/api/auth/mobile/logout?x=1'), isTrue);
      expect(isAuthUrl('/auth/mobile/password'), isFalse);
      expect(isAuthUrl('/account'), isFalse);
      expect(isAuthUrl(null), isFalse);
    });
  });

  group('createApiClient — điều hướng lỗi GET', () {
    test('GET 404 ⇒ onErrorRedirect(404)', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(404, {'error': 'Không có'}));
      final calls = <int>[];
      final dio = client(adapter, onErrorRedirect: (s, _) => calls.add(s));
      await expectLater(dio.get<Object?>('/lessons/x'), throwsA(isA<ApiError>()));
      expect(calls, [404]);
    });

    test('GET 403 ⇒ onErrorRedirect(403)', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(403, {'error': 'Cấm'}));
      final calls = <int>[];
      final dio = client(adapter, onErrorRedirect: (s, _) => calls.add(s));
      await expectLater(dio.get<Object?>('/x'), throwsA(isA<ApiError>()));
      expect(calls, [403]);
    });

    test('POST 404 ⇒ KHÔNG điều hướng', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(404, {'error': 'Không có'}));
      final calls = <int>[];
      final dio = client(adapter, onErrorRedirect: (s, _) => calls.add(s));
      await expectLater(dio.post<Object?>('/x', data: {}), throwsA(isA<ApiError>()));
      expect(calls, isEmpty);
    });

    test('skipErrorRedirect ⇒ KHÔNG điều hướng', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(404, {'error': 'Không có'}));
      final calls = <int>[];
      final dio = client(adapter, onErrorRedirect: (s, _) => calls.add(s));
      await expectLater(
        dio.get<Object?>('/system/info', options: afOptions(skipErrorRedirect: true)),
        throwsA(isA<ApiError>()),
      );
      expect(calls, isEmpty);
    });
  });

  group('createApiClient — JSON guard', () {
    test('200 text/html (proxy rơi về index.html) ⇒ ApiError thông điệp rõ', () async {
      final adapter = FakeHttpClientAdapter(
        (_, _) async => const FakeResponse(200, body: '<!DOCTYPE html><html></html>', contentType: 'text/html'),
      );
      await expectLater(
        client(adapter).get<Object?>('/system/info'),
        throwsA(isA<ApiError>().having((e) => e.message, 'message', contains('Phản hồi máy chủ không hợp lệ'))),
      );
    });

    test('204 không thân ⇒ qua', () async {
      final adapter = FakeHttpClientAdapter((_, _) async => const FakeResponse(204, contentType: null));
      final res = await client(adapter).post<Object?>('/auth/mobile/logout', data: {'refreshToken': 'a'});
      expect(res.statusCode, 204);
    });

    test('200 JSON đúng content-type ⇒ qua', () async {
      final adapter = FakeHttpClientAdapter(
        (_, _) async => const FakeResponse(200, body: '{"a":1}', contentType: 'application/json; charset=utf-8'),
      );
      final res = await client(adapter).get<Map<String, Object?>>('/x');
      expect(res.data, {'a': 1});
    });
  });

  group('createApiClient — thông điệp tiếng Việt', () {
    Future<ApiError> errorOf(FakeHttpClientAdapter adapter) async {
      try {
        await client(adapter).get<Object?>('/x');
      } on ApiError catch (e) {
        return e;
      }
      fail('Phải ném ApiError');
    }

    test('thân lỗi §6.0 ⇒ dùng error/code/details', () async {
      final e = await errorOf(
        FakeHttpClientAdapter(
          (_, _) async => FakeResponse.json(422, {
            'error': 'Dữ liệu không hợp lệ.',
            'code': 'VALIDATION',
            'details': {
              'email': ['Email đã được dùng.'],
            },
          }),
        ),
      );
      expect(e.message, 'Dữ liệu không hợp lệ.');
      expect(e.code, 'VALIDATION');
      expect(e.status, 422);
      expect(e.fieldErrors('email'), ['Email đã được dùng.']);
      expect(e.fieldErrors('khac'), isEmpty);
    });

    test('timeout', () async {
      final e = await errorOf(
        FakeHttpClientAdapter(
          (req, _) async => throw DioException.connectionTimeout(timeout: Duration.zero, requestOptions: req),
        ),
      );
      expect(e.message, 'Máy chủ phản hồi quá lâu, vui lòng thử lại.');
      expect(e.status, isNull);
    });

    test('mất mạng', () async {
      final e = await errorOf(
        FakeHttpClientAdapter(
          (req, _) async => throw DioException.connectionError(requestOptions: req, reason: 'socket'),
        ),
      );
      expect(e.message, 'Không kết nối được máy chủ. Kiểm tra mạng hoặc dịch vụ chưa chạy.');
      expect(e.isNetwork, isTrue);
    });

    test('502/503/504', () async {
      for (final s in [502, 503, 504]) {
        final e = await errorOf(FakeHttpClientAdapter((_, _) async => FakeResponse(s, body: '', contentType: null)));
        expect(e.message, 'Dịch vụ chưa sẵn sàng (gateway không tới được service).');
        expect(e.status, s);
      }
    });

    test('429 / 404 / 403 / 401 không thân', () async {
      final expected = {
        429: 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.',
        404: 'Không tìm thấy tài nguyên.',
        403: 'Bạn không có quyền thực hiện thao tác này.',
        401: 'Cần đăng nhập để tiếp tục.',
      };
      for (final entry in expected.entries) {
        final e = await errorOf(FakeHttpClientAdapter((_, _) async => FakeResponse(entry.key, contentType: null)));
        expect(e.message, entry.value, reason: '${entry.key}');
      }
    });
  });
}
