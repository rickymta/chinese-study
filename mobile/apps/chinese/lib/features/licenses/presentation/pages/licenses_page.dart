import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../config/app_config_provider.dart';
import '../../data/sources.dart';

/// `/giay-phep` — Giấy phép & nguồn (hợp đồng mobile §5.4.1): ghi công nguồn học liệu (nghĩa vụ CC BY-SA 4.0: ghi
/// công, ghi đã chỉnh sửa, cùng giấy phép), dữ liệu nét chữ (Arphic), thuật toán FSRS (MIT) + nút "Giấy phép phần
/// mềm" mở `showLicensePage` (có Arphic + hanzi-writer đã đăng ký ở `registerAntFarmLicenses`).
///
/// Không có `url_launcher` (không trong danh sách gói ghim §5.3.2) ⇒ URL hiện dạng chữ (URL giấy phép chọn-sao-chép
/// được).
class LicensesPage extends ConsumerWidget {
  const LicensesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final clientHeader = ref.watch(clientHeaderProvider);
    final muted = theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    return Scaffold(
      appBar: AppBar(title: const Text('Giấy phép & nguồn')),
      body: AfPageBody(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SectionCard(
              title: 'Nguồn học liệu',
              subtitle: 'Dữ liệu đã được chỉnh sửa',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  for (final s in kDictionarySources) _SourceTile(entry: s),
                  const SizedBox(height: 8),
                  Text(kOriginalContentNote, style: muted),
                ],
              ),
            ),
            const SizedBox(height: 12),
            SectionCard(
              title: 'Dữ liệu nét chữ & luyện viết',
              child: Column(children: [for (final s in kWritingSources) _SourceTile(entry: s)]),
            ),
            const SizedBox(height: 12),
            SectionCard(
              title: 'Thuật toán ôn tập',
              child: Column(children: [for (final s in kAlgorithmSources) _SourceTile(entry: s)]),
            ),
            const SizedBox(height: 16),
            FilledButton.tonalIcon(
              onPressed: () => showLicensePage(
                context: context,
                applicationName: 'AntFarm · Tiếng Trung',
                applicationVersion: clientHeader,
                applicationLegalese: 'Giấy phép của phần mềm và dữ liệu đóng gói trong ứng dụng.',
                useRootNavigator: true,
              ),
              icon: const Icon(Icons.gavel_outlined),
              label: const Text('Giấy phép phần mềm'),
            ),
            const SizedBox(height: 8),
            Text(
              'Gồm giấy phép các gói Flutter/Dart, Arphic Public License (dữ liệu nét chữ) và MIT (hanzi-writer).',
              style: muted,
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _SourceTile extends StatelessWidget {
  const _SourceTile({required this.entry});

  final SourceEntry entry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(entry.label, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
          Text('Nguồn: ${entry.url}', style: muted),
          // Nghĩa vụ CC BY-SA 4.0 §3(a)(1)(C): kèm liên kết tới giấy phép cho MỌI nguồn (review M4 C2).
          Text('Giấy phép: ${entry.license}', style: theme.textTheme.bodySmall),
          SelectableText(entry.licenseUrl, style: muted),
          if (entry.note != null) Text(entry.note!, style: muted),
        ],
      ),
    );
  }
}
