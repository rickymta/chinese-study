import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'features/progress/application/providers.dart';
import 'features/srs/application/outbox_controller.dart';
import 'features/srs/application/providers.dart';
import 'router/router.dart';

/// Ngôn ngữ giao diện: tiếng Việt (chuỗi viết thẳng trong code — DB-M20); `en` để Material có bản dịch dự phòng.
/// Chữ Hán đặt locale `zh-CN` riêng từng widget qua `HanziText`, không đổi locale app.
const kSupportedLocales = [Locale('vi'), Locale('en')];

/// `MaterialApp.router`: theme sáng/tối AntFarm (accent đỏ son tiếng Trung), locale vi, router go_router.
class ChineseApp extends ConsumerWidget {
  const ChineseApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final themeMode = ref.watch(themeModeProvider);
    return MaterialApp.router(
      title: 'AntFarm · Tiếng Trung',
      debugShowCheckedModeBanner: false,
      theme: buildAfTheme(brightness: Brightness.light, accent: afAccentChinese),
      darkTheme: buildAfTheme(brightness: Brightness.dark, accent: afAccentChinese),
      themeMode: themeMode,
      locale: const Locale('vi'),
      supportedLocales: kSupportedLocales,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      routerConfig: router,
      // Làm mới token khi app trở lại foreground (RM-S3) + làm mới tổng quan/tóm tắt SRS (M5/M6: người học có thể vừa
      // ôn ở máy khác hoặc đã qua nửa đêm theo múi giờ hồ sơ) + gửi outbox còn dở (RM-L1).
      builder: (context, child) => AuthLifecycleObserver(
        onResumed: () {
          ref.read(reviewOutboxProvider.notifier).flushNow();
          ref.invalidateProgressOverview();
          ref.invalidateSrsSummary();
        },
        child: OutboxNotices(child: child ?? const SizedBox.shrink()),
      ),
    );
  }
}

/// Giữ `reviewOutboxProvider` sống từ lúc mở app (đọc kho + gửi ngay khi có phiên, không chờ mở phiên ôn) và hiện
/// toast khi một lượt chấm bị BỎ vì 4xx thật (409 trùng mã, 422 thẻ tạm dừng/đủ từ mới…) — thông điệp server.
/// Đặt trong `MaterialApp.builder` để có `ScaffoldMessenger` phía trên ở mọi màn (kể cả phiên ôn toàn màn hình).
class OutboxNotices extends ConsumerWidget {
  const OutboxNotices({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen<OutboxDrop?>(reviewOutboxProvider.select((s) => s.lastDrop), (prev, next) {
      if (next == null || next == prev) return;
      showAfToast(context, 'Không ghi được một lượt chấm: ${next.error.message}', kind: AfToastKind.error);
    });
    return child;
  }
}
