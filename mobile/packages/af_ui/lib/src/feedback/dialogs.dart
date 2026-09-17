import 'package:flutter/material.dart';

/// Hộp thoại dùng chung (tương đương `AppDialog` web): mặc định KHÔNG đóng khi chạm ngoài (`barrierDismissible`
/// false) để tránh mất dữ liệu đang nhập. Hộp thoại chỉ đọc cần đóng nhanh thì truyền `closeOnBarrier: true`
/// tường minh kèm bình luận lý do.
///
/// Đây là NƠI DUY NHẤT trong `apps/` được gọi `showDialog` (luật `raw-dialog` của `tool/check_conventions.dart`).
Future<T?> showAfDialog<T>({
  required BuildContext context,
  required WidgetBuilder builder,
  bool closeOnBarrier = false,
  bool useRootNavigator = true,
}) {
  return showDialog<T>(
    context: context,
    builder: builder,
    barrierDismissible: closeOnBarrier,
    useRootNavigator: useRootNavigator,
  );
}

/// Hộp xác nhận: trả `true` khi bấm [confirmLabel], `false` khi huỷ/đóng. [destructive] ⇒ nút xác nhận màu lỗi.
Future<bool> showAfConfirm({
  required BuildContext context,
  required String title,
  String? message,
  String confirmLabel = 'Đồng ý',
  String cancelLabel = 'Huỷ',
  bool destructive = false,
}) async {
  final result = await showAfDialog<bool>(
    context: context,
    builder: (ctx) {
      final scheme = Theme.of(ctx).colorScheme;
      return AlertDialog(
        title: Text(title),
        content: message == null ? null : Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.of(ctx).pop(false), child: Text(cancelLabel)),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: destructive
                ? FilledButton.styleFrom(backgroundColor: scheme.error, foregroundColor: scheme.onError)
                : null,
            child: Text(confirmLabel),
          ),
        ],
      );
    },
  );
  return result ?? false;
}

/// Hộp thông báo một nút (thay `window.alert`).
Future<void> showAfAlert({
  required BuildContext context,
  required String title,
  String? message,
  String okLabel = 'Đóng',
}) {
  return showAfDialog<void>(
    context: context,
    // Chỉ đọc, không có dữ liệu nhập ⇒ cho phép chạm ngoài để đóng nhanh.
    closeOnBarrier: true,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: message == null ? null : Text(message),
      actions: [FilledButton(onPressed: () => Navigator.of(ctx).pop(), child: Text(okLabel))],
    ),
  );
}

/// Bottom sheet dùng chung: mặc định KHÔNG đóng khi chạm ngoài/kéo xuống; [closeOnBarrier] tường minh cho sheet
/// chỉ đọc. Nội dung tự cuộn được khi bàn phím hiện (`isScrollControlled` + đệm `viewInsets`).
Future<T?> showAfBottomSheet<T>({
  required BuildContext context,
  required WidgetBuilder builder,
  bool closeOnBarrier = false,
  bool useRootNavigator = true,
}) {
  return showModalBottomSheet<T>(
    context: context,
    isDismissible: closeOnBarrier,
    enableDrag: closeOnBarrier,
    isScrollControlled: true,
    useRootNavigator: useRootNavigator,
    useSafeArea: true,
    builder: (ctx) => Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(ctx).bottom),
      child: builder(ctx),
    ),
  );
}
