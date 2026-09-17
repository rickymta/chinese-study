import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ApiError', () {
    test('tự tạo không cần requestOptions, message không null', () {
      final e = ApiError('Lỗi thử', status: 400, code: 'X', details: {'a': 'b'});
      expect(e.message, 'Lỗi thử');
      expect(e.status, 400);
      expect(e.code, 'X');
      expect(isApiError(e), isTrue);
      expect(isApiError(Exception()), isFalse);
      expect(e, isA<DioException>());
      expect(e.toString(), 'ApiError(400 X): Lỗi thử');
    });

    test('from() giữ nguyên ApiError, bọc lỗi lạ bằng thông điệp chung', () {
      final e = ApiError('a');
      expect(ApiError.from(e), same(e));
      final other = ApiError.from(StateError('x'));
      expect(other.message, 'Đã xảy ra lỗi không xác định.');
      expect(other.status, isNull);
    });

    test('from(DioException) chuẩn hoá theo thân lỗi', () {
      final req = RequestOptions(path: '/x');
      final dioErr = DioException(
        requestOptions: req,
        type: DioExceptionType.badResponse,
        response: Response<Object?>(
          requestOptions: req,
          statusCode: 409,
          data: {'error': 'Email đã được dùng.', 'code': 'EMAIL_TAKEN'},
        ),
      );
      final e = ApiError.from(dioErr);
      expect(e.message, 'Email đã được dùng.');
      expect(e.code, 'EMAIL_TAKEN');
      expect(e.status, 409);
    });

    test('isNetwork chỉ đúng với lỗi dio loại connectionError/timeout không response', () {
      final req = RequestOptions(path: '/x');
      expect(
        ApiError.fromDioException(DioException.connectionError(requestOptions: req, reason: 'x')).isNetwork,
        isTrue,
      );
      expect(
        ApiError.fromDioException(DioException.receiveTimeout(timeout: Duration.zero, requestOptions: req)).isNetwork,
        isTrue,
      );
      expect(ApiError.network().isNetwork, isTrue);
      // Lỗi lập trình bọc qua from(), lỗi tự tạo không type, DioException.unknown ⇒ KHÔNG phải lỗi mạng.
      expect(ApiError.from(TypeError()).isNetwork, isFalse);
      expect(ApiError('x').isNetwork, isFalse);
      expect(ApiError.fromDioException(DioException(requestOptions: req, error: StateError('y'))).isNetwork, isFalse);
      // Có response ⇒ không phải lỗi mạng dù type lạ.
      final withRes = DioException(
        requestOptions: req,
        type: DioExceptionType.connectionError,
        response: Response<Object?>(requestOptions: req, statusCode: 500),
      );
      expect(ApiError.fromDioException(withRes).isNetwork, isFalse);
    });

    test('fieldErrors đọc chuỗi đơn lẫn danh sách', () {
      final e = ApiError(
        'x',
        details: {
          'a': 'một',
          'b': ['hai', 'ba'],
          'c': 5,
        },
      );
      expect(e.fieldErrors('a'), ['một']);
      expect(e.fieldErrors('b'), ['hai', 'ba']);
      expect(e.fieldErrors('c'), isEmpty);
    });
  });
}
