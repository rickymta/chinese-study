import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/routes.dart';
import '../../data/models.dart';
import '../widgets/drill/drill_tab.dart';
import '../widgets/guide_view.dart';
import '../widgets/pinyin_chart_view.dart';

/// Các tab của `/pinyin?tab=` (giá trị URL giống web).
enum PinyinTab {
  guide('huong-dan', 'Hướng dẫn'),
  chart('bang', 'Bảng'),
  drill('luyen', 'Luyện');

  const PinyinTab(this.slug, this.label);

  final String slug;
  final String label;

  static PinyinTab fromSlug(String? slug) {
    for (final t in values) {
      if (t.slug == slug) return t;
    }
    // D33: mặc định Hướng dẫn — người số 0 cần đọc trước khi bấm bảng.
    return PinyinTab.guide;
  }
}

/// Key nút "Cài đặt giọng đọc" (test).
const kVoiceSettingsKey = ValueKey('pinyin-voice-settings');

/// `/pinyin?tab=huong-dan|bang|luyen&che-do=mot|cap` (hợp đồng M7, cần `study.use`): 3 tab Hướng dẫn · Bảng · Luyện.
/// Tab khởi đầu + chế độ luyện lấy từ query lúc mở, sau đó state cục bộ, KHÔNG ghi lại URL (RM-L6).
/// `VoiceMissingNotice` ở đầu (chung cho cả ba tab) khi `noVoice`/`unsupported`; nút "Cài đặt giọng đọc" ⇒
/// `/ho-so?tab=giao-dien`. Đang làm bài luyện mà rời trang ⇒ `PopScope` + hỏi (RM-L7).
class PinyinPage extends ConsumerStatefulWidget {
  const PinyinPage({super.key});

  @override
  ConsumerState<PinyinPage> createState() => _PinyinPageState();
}

class _PinyinPageState extends ConsumerState<PinyinPage> with SingleTickerProviderStateMixin {
  TabController? _tabs;
  DrillMode _initialMode = DrillMode.listenTone;
  final GlobalKey<DrillTabState> _drillKey = GlobalKey<DrillTabState>();
  bool _drillRunning = false;
  bool _confirming = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Đọc query MỘT lần lúc mở (`GoRouterState.of` phụ thuộc InheritedWidget nên không gọi được trong initState);
    // đổi tab về sau là state cục bộ, không ghi lại URL (RM-L6).
    if (_tabs != null) return;
    final query = GoRouterState.of(context).uri.queryParameters;
    _tabs = TabController(
      length: PinyinTab.values.length,
      vsync: this,
      initialIndex: PinyinTab.fromSlug(query['tab']).index,
    );
    _initialMode = DrillMode.fromParam(query['che-do']);
  }

  @override
  void dispose() {
    _tabs?.dispose();
    super.dispose();
  }

  Future<void> _confirmLeave() async {
    if (_confirming) return;
    _confirming = true;
    try {
      final ok = await showAfConfirm(
        context: context,
        title: 'Bỏ bài đang làm?',
        message: 'Kết quả sẽ không được lưu. Chỉ những bài làm đủ 20 câu mới được tính vào thống kê.',
        confirmLabel: 'Bỏ bài',
        cancelLabel: 'Tiếp tục làm',
        destructive: true,
      );
      if (!ok || !mounted) return;
      _drillKey.currentState?.abandon();
      if (context.canPop()) {
        context.pop();
      } else {
        context.go(AppRoutes.more);
      }
    } finally {
      _confirming = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    final speech = ref.watch(speechControllerProvider);
    final showVoiceNotice = speech.status == SpeechStatus.noVoice || speech.status == SpeechStatus.unsupported;
    final tabs = _tabs!;

    return PopScope(
      canPop: !_drillRunning,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) unawaited(_confirmLeave());
      },
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Pinyin & thanh điệu'),
          actions: [
            IconButton(
              key: kVoiceSettingsKey,
              tooltip: 'Cài đặt giọng đọc',
              onPressed: () => context.push('${AppRoutes.profile}?tab=giao-dien'),
              icon: const Icon(Icons.settings_voice_outlined),
            ),
          ],
          bottom: TabBar(
            controller: tabs,
            tabs: [for (final t in PinyinTab.values) Tab(text: t.label)],
          ),
        ),
        body: Column(
          children: [
            if (showVoiceNotice)
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                child: VoiceMissingNotice(
                  status: speech.status,
                  onRetry: () => unawaited(ref.read(speechControllerProvider.notifier).refreshVoices()),
                ),
              ),
            Expanded(
              child: TabBarView(
                controller: tabs,
                children: [
                  GuideView(onGoToChart: () => tabs.animateTo(PinyinTab.chart.index)),
                  const PinyinChartView(),
                  DrillTab(
                    key: _drillKey,
                    initialMode: _initialMode,
                    onRunningChanged: (running) {
                      if (mounted && running != _drillRunning) setState(() => _drillRunning = running);
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
