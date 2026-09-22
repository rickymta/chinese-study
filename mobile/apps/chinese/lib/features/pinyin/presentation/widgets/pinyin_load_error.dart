import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

/// Thông điệp lỗi tải học liệu pinyin: 503 `CONTENT_UNAVAILABLE` ⇒ câu cố định như web; còn lại thông điệp `ApiError`.
String pinyinErrorMessage(ApiError error) =>
    error.status == 503 ? 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.' : error.message;

/// Khối lỗi gọn trong tab (thay `Alert` + "Thử lại" của web) — 503 KHÔNG điều hướng, chỉ báo tại chỗ.
class PinyinLoadError extends StatelessWidget {
  const PinyinLoadError({super.key, required this.error, required this.onRetry, this.title});

  final ApiError error;
  final VoidCallback onRetry;
  final String? title;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ErrorView(
        kind: error.isNetwork
            ? ErrorViewKind.network
            : error.isForbidden
            ? ErrorViewKind.forbidden
            : ErrorViewKind.unknown,
        title: title ?? (error.status == 503 ? 'Học liệu chưa sẵn sàng' : 'Không tải được học liệu pinyin'),
        message: pinyinErrorMessage(error),
        onRetry: onRetry,
        compact: true,
      ),
    );
  }
}

/// Khung xương lúc tải (thay `Skeleton` web): vài thanh bo góc mờ.
class PinyinSkeleton extends StatelessWidget {
  const PinyinSkeleton({super.key, this.rows = 4, this.height = 56});

  final int rows;
  final double height;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.surfaceContainerHighest;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < rows; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Container(
              height: height,
              decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
            ),
          ),
      ],
    );
  }
}
