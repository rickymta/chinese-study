import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../api/clients.dart';
import '../../../../progress/application/providers.dart';
import '../../../application/providers.dart';
import '../../../data/models.dart';
import '../../../data/pinyin_api.dart';
import '../../../domain/drill_types.dart';
import '../../../domain/generate_drill.dart';
import '../pinyin_load_error.dart';
import '../tone_stats_card.dart';
import 'drill_result.dart';
import 'drill_runner.dart';
import 'drill_setup.dart';

/// Số câu mỗi bài.
const kDrillCount = 20;

enum _Phase { setup, running, result }

/// Tab Luyện (port `DrillTab.tsx`): `setup` (chọn chế độ + thẻ thống kê) → `running` (20 câu) → `result` (nộp + kết
/// quả). `clientSessionId = uuidV4()` sinh lúc bắt đầu, giữ trong bộ nhớ (RM-L2) — "Gửi lại" dùng cùng id. Nộp xong ⇒
/// invalidate `tone-stats` + tổng quan trang chủ. Trạng thái sống qua việc đổi tab (`AutomaticKeepAliveClientMixin`);
/// rời TRANG khi đang làm ⇒ trang hỏi xác nhận qua [onRunningChanged] (RM-L7).
class DrillTab extends ConsumerStatefulWidget {
  const DrillTab({super.key, required this.initialMode, required this.onRunningChanged});

  /// Chế độ khởi đầu từ `?che-do=mot|cap` (sau đó state cục bộ, không ghi lại URL — RM-L6).
  final DrillMode initialMode;

  /// Báo cho trang biết đang làm dở (để chặn pop + hỏi).
  final ValueChanged<bool> onRunningChanged;

  @override
  ConsumerState<DrillTab> createState() => DrillTabState();
}

class DrillTabState extends ConsumerState<DrillTab> with AutomaticKeepAliveClientMixin {
  late DrillMode _mode = widget.initialMode;
  _Phase _phase = _Phase.setup;
  DrillSession? _session;

  /// Bảng pinyin đã dùng để sinh bài — giữ riêng để `running` không phụ thuộc `chart.value` của provider (có thể mất
  /// khi provider bị làm mới/lỗi giữa chừng — review M7).
  PinyinChart? _chart;
  DrillOutcome? _outcome;
  SubmitToneDrillRequest? _request;
  bool _submitting = false;
  SubmitToneDrillResponse? _serverResult;
  ApiError? _submitError;

  @override
  bool get wantKeepAlive => true;

  bool get running => _phase == _Phase.running;

  void _setPhase(_Phase next) {
    final wasRunning = running;
    setState(() => _phase = next);
    if (wasRunning != running) widget.onRunningChanged(running);
  }

  void _start(PinyinChart chart, List<int> focus) {
    final items = generateDrill(mode: _mode, chart: chart, focus: focus, count: kDrillCount);
    if (items.isEmpty) return;
    _session = DrillSession(clientSessionId: uuidV4(), mode: _mode, items: items, startedAt: DateTime.now().toUtc());
    _chart = chart;
    _outcome = null;
    _request = null;
    _serverResult = null;
    _submitError = null;
    _setPhase(_Phase.running);
  }

  void _finish(List<AnsweredItem> answers, DateTime finishedAt) {
    final session = _session;
    if (session == null || _phase != _Phase.running) return;
    final outcome = DrillOutcome(session: session, answers: answers, finishedAt: finishedAt);
    _outcome = outcome;
    _request = buildSubmitRequest(outcome);
    _setPhase(_Phase.result);
    unawaited(_submit());
  }

  /// Nộp bài (lần đầu và "Gửi lại" — cùng body, cùng `clientSessionId`).
  Future<void> _submit() async {
    final request = _request;
    if (request == null || _submitting) return;
    setState(() {
      _submitting = true;
      _submitError = null;
    });
    try {
      final res = await submitToneDrill(ref.read(chineseDioProvider), request);
      if (!mounted) return;
      setState(() => _serverResult = res);
      // Cùng danh sách invalidate với `useSubmitToneDrill` web: thống kê thanh + tổng quan (chuỗi ngày, thanh yếu).
      ref.invalidateToneStats();
      ref.invalidateProgressOverview();
    } on Object catch (e) {
      if (!mounted) return;
      setState(() => _submitError = ApiError.from(e));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// Bỏ bài đang làm (đã xác nhận ở trang) — về thiết lập, không lưu gì (R5-10).
  void abandon() {
    if (!running) return;
    _session = null;
    _chart = null;
    _setPhase(_Phase.setup);
  }

  /// Phòng hờ: đang làm mà mất bảng (không xảy ra vì `_chart` giữ từ lúc bắt đầu) ⇒ về thiết lập + báo, không crash.
  void _abortMissingChart() {
    if (!mounted || !running) return;
    _backToSetup();
    showAfToast(context, 'Mất dữ liệu bảng pinyin — bài luyện đã dừng, hãy bắt đầu lại.', kind: AfToastKind.error);
  }

  void _backToSetup() {
    _session = null;
    _chart = null;
    _outcome = null;
    _request = null;
    _serverResult = null;
    _submitError = null;
    _setPhase(_Phase.setup);
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final chart = ref.watch(pinyinChartProvider);
    final stats = ref.watch(ref.watch(toneStatsProvider));
    final speech = ref.watch(speechControllerProvider);
    final focus = stats.value?.recommendedFocus ?? const <int>[];

    switch (_phase) {
      case _Phase.setup:
        final chartErr = chart.hasError ? ApiError.from(chart.error!) : null;
        return AfPageBody(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (chart.isLoading && !chart.hasValue)
                const PinyinSkeleton(rows: 1, height: 220)
              else
                DrillSetup(
                  mode: _mode,
                  onModeChange: (m) => setState(() => _mode = m),
                  focus: focus,
                  onStart: () {
                    final c = chart.value;
                    if (c != null) _start(c, focus);
                  },
                  speechStatus: speech.status,
                  disabledReason: chartErr == null
                      ? null
                      : chartErr.status == 503
                      ? pinyinErrorMessage(chartErr)
                      : 'Không tải được bảng pinyin: ${chartErr.message}',
                ),
              if (chartErr != null && chartErr.status != 503) ...[
                const SizedBox(height: 12),
                PinyinLoadError(error: chartErr, onRetry: () => ref.invalidate(pinyinChartProvider)),
              ],
              const SizedBox(height: 12),
              const ToneStatsCard(),
            ],
          ),
        );
      case _Phase.running:
        final session = _session;
        final usedChart = _chart;
        if (session == null || usedChart == null) {
          WidgetsBinding.instance.addPostFrameCallback((_) => _abortMissingChart());
          return const AfPageBody(child: PinyinSkeleton(rows: 1, height: 220));
        }
        return DrillRunner(
          key: ValueKey(session.clientSessionId),
          session: session,
          chart: usedChart,
          onFinish: _finish,
        );
      case _Phase.result:
        return AfPageBody(
          child: DrillResult(
            outcome: _outcome!,
            serverResult: _serverResult,
            submitting: _submitting,
            submitError: _submitError,
            onRetry: () => unawaited(_submit()),
            onNewDrill: _backToSetup,
            onViewStats: _backToSetup,
          ),
        );
    }
  }
}
