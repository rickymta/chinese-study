import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../config/app_config_provider.dart';
import '../router/routes.dart';

/// Trang "Thêm": Pinyin, Từ điển, Hồ sơ, Giấy phép & nguồn, chế độ giao diện (tạm ở đây tới khi có Hồ sơ — M4).
///
/// M2 bổ sung: dòng giải thích quản trị dùng bản web (RM-S6) và nút Đăng xuất (RM-S7).
class MorePage extends ConsumerWidget {
  const MorePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final mode = ref.watch(themeModeProvider);
    final config = ref.watch(appConfigProvider);
    final clientHeader = ref.watch(clientHeaderProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Thêm')),
      body: AfPageBody(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _NavTile(
              icon: Icons.record_voice_over_outlined,
              title: 'Pinyin & luyện thanh',
              subtitle: 'Hướng dẫn, bảng âm, luyện nghe thanh',
              onTap: () => context.push(AppRoutes.pinyin),
            ),
            _NavTile(
              icon: Icons.search,
              title: 'Tra từ',
              subtitle: 'Tìm theo chữ Hán, pinyin hoặc nghĩa',
              onTap: () => context.push(AppRoutes.dictionary),
            ),
            _NavTile(
              icon: Icons.person_outline,
              title: 'Hồ sơ',
              subtitle: 'Tên, múi giờ, mật khẩu, cài đặt học tập',
              onTap: () => context.push(AppRoutes.profile),
            ),
            _NavTile(
              icon: Icons.gavel_outlined,
              title: 'Giấy phép & nguồn',
              subtitle: 'Nguồn học liệu và giấy phép phần mềm',
              onTap: () => context.push(AppRoutes.licenses),
            ),
            const Divider(height: 24),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Giao diện', style: Theme.of(context).textTheme.titleSmall),
                  const SizedBox(height: 8),
                  // Chế độ tối tạm đặt ở đây (M0); M4 chuyển vào Hồ sơ → tab Giao diện.
                  SegmentedButton<ThemeMode>(
                    segments: [
                      for (final m in ThemeMode.values)
                        ButtonSegment(
                          value: m,
                          label: Text(themeModeLabel(m)),
                          icon: Icon(switch (m) {
                            ThemeMode.system => Icons.brightness_auto_outlined,
                            ThemeMode.light => Icons.light_mode_outlined,
                            ThemeMode.dark => Icons.dark_mode_outlined,
                          }),
                        ),
                    ],
                    selected: {mode},
                    showSelectedIcon: false,
                    onSelectionChanged: (s) => ref.read(themeModeProvider.notifier).setMode(s.first),
                  ),
                ],
              ),
            ),
            const Divider(height: 24),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Text(
                'AntFarm · Tiếng Trung — $clientHeader · môi trường ${config.env.name}',
                style: Theme.of(context).textTheme.bodySmall
                    ?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _NavTile extends StatelessWidget {
  const _NavTile({required this.icon, required this.title, required this.subtitle, required this.onTap});

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon),
      title: Text(title),
      subtitle: Text(subtitle),
      trailing: const Icon(Icons.chevron_right),
      minTileHeight: 56,
      onTap: onTap,
    );
  }
}
