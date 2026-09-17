import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../widgets/appearance_tab.dart';
import '../widgets/change_password_tab.dart';
import '../widgets/learning_settings_tab.dart';
import '../widgets/profile_info_tab.dart';

/// Các tab của `/ho-so?tab=` (giá trị trên URL giống web + `giao-dien` riêng mobile).
enum ProfileTab {
  info('thong-tin', 'Thông tin'),
  password('mat-khau', 'Mật khẩu'),
  learning('hoc-tap', 'Học tập'),
  appearance('giao-dien', 'Giao diện');

  const ProfileTab(this.slug, this.label);

  final String slug;
  final String label;

  static ProfileTab? fromSlug(String? slug) {
    for (final t in values) {
      if (t.slug == slug) return t;
    }
    return null;
  }
}

/// Hồ sơ (hợp đồng mobile M4): 4 tab Thông tin · Mật khẩu · Học tập · Giao diện. Tab khởi đầu lấy từ `?tab=` lúc mở,
/// sau đó là state cục bộ, KHÔNG ghi lại URL (RM-L6). Người thiếu `study.use` không thấy tab Học tập (cài đặt SRS/TTS
/// vô nghĩa với họ) — kèm dải giải thích như web.
class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canStudy = ref.watch(permissionsProvider).contains(kStudyUsePermission);
    final tabs = [
      for (final t in ProfileTab.values)
        if (t != ProfileTab.learning || canStudy) t,
    ];
    final requested = ProfileTab.fromSlug(GoRouterState.of(context).uri.queryParameters['tab']);
    final initial = requested == null ? 0 : tabs.indexOf(requested).clamp(0, tabs.length - 1);

    return DefaultTabController(
      length: tabs.length,
      initialIndex: initial,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Hồ sơ'),
          bottom: TabBar(
            // Cuộn được để 4 nhãn không bị cắt ở 360 px / chữ 1,3×.
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [for (final t in tabs) Tab(text: t.label)],
          ),
        ),
        body: Column(
          children: [
            if (!canStudy)
              const Padding(
                padding: EdgeInsets.fromLTRB(16, 12, 16, 0),
                child: AuthBanner(
                  message:
                      'Tab "Học tập" (hạn mức từ mới, tốc độ đọc…) chỉ hiện khi tài khoản có quyền Học tập — liên hệ '
                      'quản trị viên nếu bạn cần dùng phần học.',
                ),
              ),
            Expanded(
              child: TabBarView(
                children: [
                  for (final t in tabs)
                    switch (t) {
                      ProfileTab.info => const ProfileInfoTab(),
                      ProfileTab.password => const ChangePasswordTab(),
                      ProfileTab.learning => const LearningSettingsTab(),
                      ProfileTab.appearance => const AppearanceTab(),
                    },
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Khung chung của một tab: thẻ bọc nội dung, cuộn được, rộng tối đa 600.
class ProfileTabBody extends StatelessWidget {
  const ProfileTabBody({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return AfPageBody(
      child: Card(
        child: Padding(padding: const EdgeInsets.all(16), child: child),
      ),
    );
  }
}
