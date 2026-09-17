import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../application/providers.dart';
import '../../data/models.dart';
import '../widgets/system_status_card.dart';

/// Trang chủ tạm của M0: thẻ "Trạng thái hệ thống". M5 thay bằng bảng tổng quan tiến độ (F11).
class HomePage extends ConsumerWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('AntFarm · Tiếng Trung')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(systemInfoProvider(SystemService.chinese));
          ref.invalidate(systemInfoProvider(SystemService.identity));
          // Chờ cả hai lời gọi xong để vòng xoay kéo-làm-mới không tắt sớm; lỗi đã hiện trên chip.
          await Future.wait([
            for (final s in SystemService.values)
              ref.read(systemInfoProvider(s).future).then<void>((_) {}, onError: (Object _) {}),
          ]);
        },
        child: AfPageBody(
          // AlwaysScrollable để kéo-làm-mới hoạt động cả khi nội dung ngắn hơn màn.
          scrollable: false,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: EdgeInsets.zero,
            children: [
              Text('Xin chào!', style: theme.textTheme.headlineSmall),
              const SizedBox(height: 4),
              Text(
                'Bảng tổng quan tiến độ (việc hôm nay, chuỗi ngày học) sẽ xuất hiện ở đây sau khi đăng nhập.',
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: 16),
              const SystemStatusCard(),
            ],
          ),
        ),
      ),
    );
  }
}
