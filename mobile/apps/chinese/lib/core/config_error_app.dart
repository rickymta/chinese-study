import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

/// App tối giản hiện khi cấu hình máy chủ thiếu/không hợp lệ (`AppConfigException`) — không chạy tiếp vì mọi lời gọi
/// API sẽ hỏng. Không dùng Riverpod/router để chắc chắn luôn hiện được.
class ConfigErrorApp extends StatelessWidget {
  const ConfigErrorApp({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AntFarm · Tiếng Trung',
      debugShowCheckedModeBanner: false,
      theme: buildAfTheme(brightness: Brightness.light),
      darkTheme: buildAfTheme(brightness: Brightness.dark),
      home: Scaffold(
        body: SafeArea(
          child: ErrorView(kind: ErrorViewKind.unknown, title: 'Thiếu cấu hình máy chủ', message: message),
        ),
      ),
    );
  }
}
