import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

import '../../domain/lesson_errors.dart';

/// Khối lỗi cho các màn bài học: 503 ⇒ "Học liệu chưa sẵn sàng" không nút thử lại (trạng thái server, không điều
/// hướng); còn lại ⇒ thông điệp `ApiError` + Thử lại. 403/404 đã được interceptor đưa sang trang lỗi chung.
class LessonErrorView extends StatelessWidget {
  const LessonErrorView({super.key, required this.error, this.onRetry, this.compact = true});

  final ApiError error;
  final VoidCallback? onRetry;
  final bool compact;

  @override
  Widget build(BuildContext context) {
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
          ? kLessonsUnavailableTitle
          : error.isNotFound
          ? 'Không tìm thấy bài học'
          : null,
      message: lessonErrorMessage(error),
      onRetry: lessonErrorRetryable(error) ? onRetry : null,
      compact: compact,
    );
  }
}
