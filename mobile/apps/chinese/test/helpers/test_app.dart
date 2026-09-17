import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:af_chinese/api/clients.dart';
import 'package:af_chinese/app.dart';
import 'package:af_chinese/config/app_config_provider.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Đọc JSON mẫu trong `test/fixtures/` (chạy từ thư mục app).
Map<String, Object?> loadFixture(String name) {
  final file = File('test/fixtures/$name');
  return jsonDecode(file.readAsStringSync()) as Map<String, Object?>;
}

/// Adapter HTTP giả cho widget test: trả theo [handler]; ném lỗi ⇒ dio bọc thành lỗi mạng.
class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.handler);

  final Future<(int, String)> Function(RequestOptions req) handler;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final (status, body) = await handler(options);
    return ResponseBody.fromString(
      body,
      status,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

const testConfig = AppConfig(
  env: AfEnv.dev,
  identityApiUrl: 'http://test.local/identity/api',
  chineseApiUrl: 'http://test.local/chinese/api',
  isWeb: false,
);

/// Dựng app đầy đủ (router + theme + provider) với client giả — dùng cho widget test shell/trang.
Widget buildTestApp({required FakeAdapter chineseAdapter, required FakeAdapter identityAdapter, KeyValueStore? store}) {
  Dio withAdapter(String baseUrl, FakeAdapter adapter) {
    final dio = createApiClient(baseUrl: baseUrl);
    dio.httpClientAdapter = adapter;
    return dio;
  }

  return ProviderScope(
    overrides: [
      appConfigProvider.overrideWithValue(testConfig),
      keyValueStoreProvider.overrideWithValue(store ?? InMemoryKeyValueStore()),
      chineseDioProvider.overrideWithValue(withAdapter(testConfig.chineseApiUrl, chineseAdapter)),
      identityDioProvider.overrideWithValue(withAdapter(testConfig.identityApiUrl, identityAdapter)),
    ],
    retry: afNoRetry,
    child: const ChineseApp(),
  );
}

/// Adapter luôn trả `system/info` thành công cho một service.
FakeAdapter okSystemInfo(String service) => FakeAdapter(
  (_) async => (
    200,
    jsonEncode({
      'service': service,
      'version': '0.1.0',
      'environment': 'Development',
      'serverTimeUtc': '2026-09-17T08:00:00Z',
    }),
  ),
);

/// Adapter giả lập service tắt (gateway 502).
FakeAdapter downSystemInfo() => FakeAdapter((_) async => (502, ''));
