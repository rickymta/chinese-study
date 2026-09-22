import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';

/// Một phản hồi giả cho [FakeHttpClientAdapter].
class FakeResponse {
  const FakeResponse(this.status, {this.body = '', this.contentType = 'application/json'});

  factory FakeResponse.json(int status, Object? json) => FakeResponse(status, body: jsonEncode(json));

  final int status;
  final String body;
  final String? contentType;
}

/// Adapter HTTP giả: mỗi request đi qua [handler]; ghi lại mọi [RequestOptions] để test assert.
///
/// Handler có thể ném [DioException] (giả timeout) hoặc lỗi thường (giả mất mạng — dio bọc thành `unknown`,
/// `response == null`).
class FakeHttpClientAdapter implements HttpClientAdapter {
  FakeHttpClientAdapter(this.handler);

  final Future<FakeResponse> Function(RequestOptions options, int callIndex) handler;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final res = await handler(options, requests.length - 1);
    return ResponseBody.fromString(
      res.body,
      res.status,
      headers: {
        if (res.contentType != null) Headers.contentTypeHeader: [res.contentType!],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
