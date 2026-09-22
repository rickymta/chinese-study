import 'package:flutter/material.dart';

import '../layout/af_page_body.dart';

/// Loại lỗi — tương đương các trang `/401`, `/403`, `/404` + lỗi mạng của `ErrorPage` web.
enum ErrorViewKind { unauthorized, forbidden, notFound, network, unknown }

/// Màn/khối lỗi thống nhất: biểu tượng + tiêu đề + thông điệp + Thử lại + hành động tuỳ chọn.
///
/// Dùng cả toàn trang (route `/401`, `/403`, `/404`) lẫn trong khối (`AsyncValueView` khi lỗi).
class ErrorView extends StatelessWidget {
  const ErrorView({
    super.key,
    this.kind = ErrorViewKind.unknown,
    this.title,
    this.message,
    this.onRetry,
    this.actions = const [],
    this.compact = false,
  });

  final ErrorViewKind kind;

  /// Tiêu đề; null ⇒ theo [kind].
  final String? title;

  /// Thông điệp chi tiết (thường là `ApiError.message`); null ⇒ theo [kind].
  final String? message;
  final VoidCallback? onRetry;

  /// Hành động thêm (vd nút Đăng xuất ở `/403`, Về trang chủ ở `/404`).
  final List<Widget> actions;

  /// Bố cục gọn (trong thẻ), không chiếm cả màn.
  final bool compact;

  static String defaultTitle(ErrorViewKind kind) => switch (kind) {
    ErrorViewKind.unauthorized => 'Cần đăng nhập',
    ErrorViewKind.forbidden => 'Không có quyền truy cập',
    ErrorViewKind.notFound => 'Không tìm thấy trang',
    ErrorViewKind.network => 'Không kết nối được máy chủ',
    ErrorViewKind.unknown => 'Đã xảy ra lỗi',
  };

  static String defaultMessage(ErrorViewKind kind) => switch (kind) {
    ErrorViewKind.unauthorized => 'Bạn cần đăng nhập để xem nội dung này.',
    ErrorViewKind.forbidden => 'Tài khoản của bạn không có quyền xem nội dung này.',
    ErrorViewKind.notFound => 'Đường dẫn không tồn tại hoặc đã bị xoá.',
    ErrorViewKind.network => 'Kiểm tra mạng hoặc dịch vụ chưa chạy, rồi thử lại.',
    ErrorViewKind.unknown => 'Vui lòng thử lại. Nếu vẫn lỗi, báo quản trị viên.',
  };

  static IconData iconOf(ErrorViewKind kind) => switch (kind) {
    ErrorViewKind.unauthorized => Icons.lock_outline,
    ErrorViewKind.forbidden => Icons.block_outlined,
    ErrorViewKind.notFound => Icons.search_off_outlined,
    ErrorViewKind.network => Icons.wifi_off_outlined,
    ErrorViewKind.unknown => Icons.error_outline,
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final column = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Icon(iconOf(kind), size: compact ? 32 : 56, color: theme.colorScheme.error),
        SizedBox(height: compact ? 8 : 16),
        Text(
          title ?? defaultTitle(kind),
          style: compact ? theme.textTheme.titleMedium : theme.textTheme.titleLarge,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 8),
        Text(
          message ?? defaultMessage(kind),
          style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          textAlign: TextAlign.center,
        ),
        if (onRetry != null || actions.isNotEmpty) ...[
          const SizedBox(height: 16),
          Wrap(
            spacing: 12,
            runSpacing: 8,
            alignment: WrapAlignment.center,
            children: [
              if (onRetry != null)
                FilledButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: const Text('Thử lại')),
              ...actions,
            ],
          ),
        ],
      ],
    );
    if (compact) return Padding(padding: const EdgeInsets.all(16), child: column);
    return AfPageBody(
      scrollable: true,
      child: Padding(padding: const EdgeInsets.symmetric(vertical: 48), child: column),
    );
  }
}
