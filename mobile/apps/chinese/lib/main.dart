import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'config/app_config_provider.dart';
import 'core/config_error_app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Cấu hình máy chủ từ --dart-define-from-file; web dev suy từ origin trang (proxy cùng origin).
  final AppConfig config;
  try {
    config = AppConfig.fromDartDefine(isWeb: kIsWeb, base: kIsWeb ? Uri.base : null);
  } on AppConfigException catch (e) {
    runApp(ConfigErrorApp(message: e.message));
    return;
  }

  // Header X-AF-Client: phiên bản từ package_info_plus (lỗi ⇒ fallback), nền tảng android|ios|web.
  final clientHeader = await resolveClientHeader(appSlug: kAppSlug, fallbackVersion: kAppVersionFallback);
  afLog('Khởi động $config · $clientHeader');

  runApp(
    ProviderScope(
      overrides: [appConfigProvider.overrideWithValue(config), clientHeaderProvider.overrideWithValue(clientHeader)],
      retry: afNoRetry,
      child: const ChineseApp(),
    ),
  );
}
