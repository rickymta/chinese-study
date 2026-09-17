import 'package:af_core/af_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'error_view.dart';

/// Hiển thị `AsyncValue<T>` chuẩn (tương đương khung loading/lỗi của React Query ở web):
/// loading ⇒ [loading] hoặc vòng xoay; lỗi ⇒ [ErrorView] với thông điệp `ApiError` + Thử lại; data ⇒ [data].
///
/// Khi đang làm mới (`isRefreshing`/`isReloading`) mà đã có dữ liệu cũ ⇒ vẫn hiện dữ liệu cũ (không nháy).
class AsyncValueView<T> extends StatelessWidget {
  const AsyncValueView({
    super.key,
    required this.value,
    required this.data,
    this.onRetry,
    this.loading,
    this.error,
    this.compact = true,
  });

  final AsyncValue<T> value;
  final Widget Function(T data) data;
  final VoidCallback? onRetry;

  /// Widget lúc tải; null ⇒ vòng xoay căn giữa.
  final Widget? loading;

  /// Widget lỗi tuỳ biến; null ⇒ [ErrorView].
  final Widget Function(ApiError error)? error;

  /// `ErrorView` gọn (trong thẻ) hay toàn trang.
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final v = value;
    if (v.hasValue && (v.isLoading || !v.hasError)) return data(v.requireValue);
    if (v.hasError) {
      final apiErr = ApiError.from(v.error!);
      if (error != null) return error!(apiErr);
      return ErrorView(
        kind: apiErr.isNetwork
            ? ErrorViewKind.network
            : apiErr.isForbidden
            ? ErrorViewKind.forbidden
            : apiErr.isNotFound
            ? ErrorViewKind.notFound
            : ErrorViewKind.unknown,
        message: apiErr.message,
        onRetry: onRetry,
        compact: compact,
      );
    }
    return loading ??
        const Padding(
          padding: EdgeInsets.all(24),
          child: Center(child: CircularProgressIndicator()),
        );
  }
}
