import 'package:flutter/material.dart';

/// Khung trang đăng nhập/đăng ký (tương đương `AuthShell` web): một cột, rộng tối đa 480, tên app + tiêu đề,
/// cuộn được khi bàn phím hiện.
class AuthScaffold extends StatelessWidget {
  const AuthScaffold({super.key, required this.brand, required this.title, required this.child, this.logo});

  /// Tên app hiện trên đầu, vd "AntFarm · Tiếng Trung".
  final String brand;
  final String title;
  final Widget child;
  final Widget? logo;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: Align(
          alignment: Alignment.topCenter,
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(20, 32, 20, 24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (logo != null) ...[Center(child: logo), const SizedBox(height: 12)],
                  Text(
                    brand,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.primary),
                  ),
                  const SizedBox(height: 4),
                  Text(title, textAlign: TextAlign.center, style: theme.textTheme.headlineSmall),
                  const SizedBox(height: 24),
                  child,
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Dải thông báo trong biểu mẫu (thay `Alert` của MUI): `info` hoặc `error`.
class AuthBanner extends StatelessWidget {
  const AuthBanner({super.key, required this.message, this.isError = false});

  final String message;
  final bool isError;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final bg = isError ? scheme.errorContainer : scheme.secondaryContainer;
    final fg = isError ? scheme.onErrorContainer : scheme.onSecondaryContainer;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(10)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(isError ? Icons.error_outline : Icons.info_outline, size: 20, color: fg),
          const SizedBox(width: 8),
          Expanded(
            child: Text(message, style: TextStyle(color: fg)),
          ),
        ],
      ),
    );
  }
}
