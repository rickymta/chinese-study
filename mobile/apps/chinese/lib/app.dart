import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

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
      // Làm mới token khi app trở lại foreground (RM-S3); M5/M6 nối thêm làm mới tổng quan + gửi outbox.
      builder: (context, child) => AuthLifecycleObserver(child: child ?? const SizedBox.shrink()),
    );
  }
}
