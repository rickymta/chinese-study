import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

import '../../domain/search_errors.dart';

/// Khối lỗi cho các màn từ điển (port `QueryErrorAlert.tsx`): 503 ⇒ "Học liệu chưa sẵn sàng" không nút thử lại (trạng
/// thái server, không điều hướng); 400 ⇒ thông điệp `details`; còn lại ⇒ thông điệp `ApiError` + Thử lại. 404 (id/chữ
/// không có) đã được interceptor đưa sang `/404`, ở đây chỉ còn khối "Không tìm thấy" phía dưới khi quay lại.
class DictionaryErrorView extends StatelessWidget {
  const DictionaryErrorView({super.key, required this.error, this.onRetry, this.compact = true});

  final ApiError error;
  final VoidCallback? onRetry;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final retryable = dictionaryErrorRetryable(error);
    return ErrorView(
      kind: error.status == 503
          ? ErrorViewKind.unknown
          : error.isNetwork
          ? ErrorViewKind.network
          : error.isForbidden
          ? ErrorViewKind.forbidden
          : error.isNotFound
          ? ErrorViewKind.notFound
          : ErrorViewKind.unknown,
      title: error.status == 503
          ? kContentUnavailableTitle
          : error.status == 400
          ? 'Yêu cầu không hợp lệ'
          : error.isNotFound
          ? 'Không tìm thấy'
          : null,
      message: dictionaryErrorMessage(error),
      onRetry: retryable ? onRetry : null,
      compact: compact,
    );
  }
}
