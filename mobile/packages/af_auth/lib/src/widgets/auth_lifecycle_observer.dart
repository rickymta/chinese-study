import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth_controller.dart';

/// Nghe `AppLifecycleState.resumed` để làm mới token khi app trở lại foreground còn ≤ 60 s hạn (RM-S3).
/// Đặt một lần quanh `MaterialApp.router` (app truyền qua `builder:`). [onResumed] để app nối thêm việc (outbox M6,
/// làm mới tổng quan M5).
class AuthLifecycleObserver extends ConsumerStatefulWidget {
  const AuthLifecycleObserver({super.key, required this.child, this.onResumed});

  final Widget child;
  final VoidCallback? onResumed;

  @override
  ConsumerState<AuthLifecycleObserver> createState() => _AuthLifecycleObserverState();
}

class _AuthLifecycleObserverState extends ConsumerState<AuthLifecycleObserver> {
  late final AppLifecycleListener _listener;

  @override
  void initState() {
    super.initState();
    _listener = AppLifecycleListener(onResume: _onResume);
  }

  void _onResume() {
    ref.read(authDepsProvider).session.onAppResumed();
    widget.onResumed?.call();
  }

  @override
  void dispose() {
    _listener.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
