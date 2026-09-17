import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../config/app_config_provider.dart';
import '../config/links.dart';
import '../features/auth/application/sign_out.dart';
import '../router/routes.dart';

/// Quyền quản trị — có một trong hai ⇒ hiện dòng giải thích dùng bản web (RM-S6). App KHÔNG có màn quản trị.
const kAdminPermissions = {'content.manage', 'users.manage'};

/// Trang "Thêm": tài khoản, Pinyin, Từ điển, Hồ sơ, Giấy phép & nguồn, chế độ giao diện (tạm tới khi có Hồ sơ — M4),
/// dòng giải thích quản trị (RM-S6), Đăng xuất (RM-S7).
class MorePage extends ConsumerWidget {
  const MorePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final mode = ref.watch(themeModeProvider);
    final config = ref.watch(appConfigProvider);
    final clientHeader = ref.watch(clientHeaderProvider);
    final account = ref.watch(currentAccountProvider);
    final permissions = ref.watch(permissionsProvider);
    final isAdmin = permissions.any(kAdminPermissions.contains);

    return Scaffold(
      appBar: AppBar(title: const Text('Thêm')),
      body: AfPageBody(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (account != null)
              ListTile(
                leading: CircleAvatar(child: Text(_initial(account.displayName, account.email))),
                title: Text(account.displayName.isEmpty ? account.email : account.displayName),
                subtitle: Text(account.email),
                minTileHeight: 64,
                onTap: () => context.push(AppRoutes.profile),
              ),
            if (isAdmin)
              const Padding(
                padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
                child: AuthBanner(message: 'Quản trị nội dung và người dùng dùng bản web: $kWebAppUrl'),
              ),
            const Divider(height: 8),
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
            // Giọng đọc tạm đặt ở đây (M3); M4 chuyển vào Hồ sơ → tab Giao diện.
            _NavTile(
              icon: Icons.volume_up_outlined,
              title: 'Giọng đọc',
              subtitle: 'Chọn giọng tiếng Trung, tốc độ, nghe thử',
              onTap: () => context.push(AppRoutes.voice),
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
                  Text('Giao diện', style: theme.textTheme.titleSmall),
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
            ListTile(
              leading: Icon(Icons.logout, color: theme.colorScheme.error),
              title: Text('Đăng xuất', style: TextStyle(color: theme.colorScheme.error)),
              minTileHeight: 56,
              onTap: () => signOutFlow(context, ref),
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Text(
                'AntFarm · Tiếng Trung — $clientHeader · môi trường ${config.env.name}',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ),
          ],
        ),
      ),
    );
  }

  static String _initial(String displayName, String email) {
    final source = displayName.trim().isNotEmpty ? displayName.trim() : email;
    return source.isEmpty ? '?' : source.characters.first.toUpperCase();
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
