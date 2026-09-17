import 'dart:async';

import 'package:flutter/material.dart';

import '../../data/models.dart';
import '../../domain/dates.dart';
import '../../domain/heatmap.dart';
import 'dashboard_card.dart';

/// Ô 16 + khe 3 ⇒ 14 cột ≈ 263 + cột nhãn thứ 22: vừa 360 px sau đệm trang (32) + đệm thẻ (32) = 296 khả dụng.
const kHeatmapCell = 16.0;
const kHeatmapGap = 3.0;
const kHeatmapLabelCol = 22.0;

/// Thời gian giữ nhãn sau khi chạm một ô (như tooltip ghim của web).
const kHeatmapPinDuration = Duration(seconds: 3);

/// Key lưới ô (để test đo bề rộng không tràn 360 px).
const kHeatmapGridKey = ValueKey('activity-heatmap-grid');

/// Lịch hoạt động 90 ngày (R-PG6, port `ActivityHeatmap.tsx`) — lưới tự vẽ, không thư viện biểu đồ. Cột = tuần
/// (Thứ Hai đầu), nhãn tháng phía trên, nhãn T2/T4/T6 bên trái. Màu = alpha của `primary` theo mức 1–4 nên đủ tương
/// phản cả sáng/tối; ô hôm nay có viền màu accent. Điện thoại không có hover ⇒ CHẠM ô ⇒ ghim nhãn "dd/MM: N lượt" dưới
/// lưới [kHeatmapPinDuration] (chạm lại ô đó ⇒ bỏ ghim). Màn hẹp hơn lưới ⇒ cuộn ngang, mở ở tuần hiện tại.
class ActivityHeatmap extends StatefulWidget {
  const ActivityHeatmap({super.key, required this.activity, required this.today});

  final List<ActivityDay> activity;

  /// `localDate` của server.
  final String today;

  @override
  State<ActivityHeatmap> createState() => _ActivityHeatmapState();
}

class _ActivityHeatmapState extends State<ActivityHeatmap> {
  late HeatmapGrid _grid = buildHeatmap(widget.activity, widget.today);
  HeatmapCell? _pinned;
  Timer? _pinTimer;

  @override
  void didUpdateWidget(ActivityHeatmap oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.activity != widget.activity || oldWidget.today != widget.today) {
      _grid = buildHeatmap(widget.activity, widget.today);
      _unpin();
    }
  }

  @override
  void dispose() {
    _pinTimer?.cancel();
    super.dispose();
  }

  void _unpin() {
    _pinTimer?.cancel();
    _pinTimer = null;
    _pinned = null;
  }

  void _onCellTap(HeatmapCell cell) {
    setState(() {
      if (_pinned == cell) {
        _unpin();
        return;
      }
      _pinTimer?.cancel();
      _pinned = cell;
      _pinTimer = Timer(kHeatmapPinDuration, () {
        if (mounted) setState(_unpin);
      });
    });
  }

  Color _colorOf(BuildContext context, HeatLevel level) {
    final scheme = Theme.of(context).colorScheme;
    return switch (level) {
      0 => scheme.onSurface.withValues(alpha: 0.08),
      1 => scheme.primary.withValues(alpha: 0.3),
      2 => scheme.primary.withValues(alpha: 0.55),
      3 => scheme.primary.withValues(alpha: 0.8),
      _ => scheme.primary,
    };
  }

  static String labelOf(HeatmapCell cell) => '${formatDdMm(cell.date)}: ${cell.count} lượt';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final grid = _grid;
    final columns = grid.weeks.length;
    final gridWidth = columns * kHeatmapCell + (columns > 0 ? columns - 1 : 0) * kHeatmapGap;
    final caption = theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant, fontSize: 10);
    final pinned = _pinned;

    return DashboardCard(
      title: '90 ngày qua',
      icon: Icons.calendar_month_outlined,
      aside: MutedText('${grid.activeDays} ngày có học · ${grid.total} lượt', small: true),
      children: [
        // `reverse: true` ⇒ khi phải cuộn (chỉ dưới ~290 px), mở sẵn ở tuần hiện tại (bên phải).
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          reverse: true,
          padding: const EdgeInsets.only(bottom: 4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Hàng nhãn tháng: đặt theo chỉ số cột.
              Padding(
                padding: const EdgeInsets.only(left: kHeatmapLabelCol),
                child: SizedBox(
                  width: gridWidth,
                  height: 16,
                  child: Stack(
                    clipBehavior: Clip.none,
                    children: [
                      for (final m in grid.monthLabels)
                        Positioned(
                          left: m.weekIndex * (kHeatmapCell + kHeatmapGap),
                          top: 0,
                          child: Text(m.label, style: caption, softWrap: false),
                        ),
                    ],
                  ),
                ),
              ),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Cột nhãn thứ — chỉ T2/T4/T6 cho thoáng.
                  SizedBox(
                    width: kHeatmapLabelCol - kHeatmapGap,
                    child: Column(
                      children: [
                        for (var i = 0; i < 7; i++)
                          Container(
                            height: kHeatmapCell,
                            margin: EdgeInsets.only(bottom: i < 6 ? kHeatmapGap : 0),
                            alignment: Alignment.centerLeft,
                            child: i.isEven ? Text(kWeekdayShortVi[i], style: caption, softWrap: false) : null,
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(width: kHeatmapGap),
                  Semantics(
                    label: 'Lịch hoạt động 90 ngày: ${grid.activeDays} ngày có học, ${grid.total} lượt',
                    child: Row(
                      key: kHeatmapGridKey,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        for (var wi = 0; wi < columns; wi++) ...[
                          if (wi > 0) const SizedBox(width: kHeatmapGap),
                          Column(
                            children: [
                              for (var di = 0; di < 7; di++) ...[
                                if (di > 0) const SizedBox(height: kHeatmapGap),
                                _cell(context, grid.weeks[wi][di], pinned),
                              ],
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        Row(
          children: [
            Expanded(
              child: pinned == null
                  ? const SizedBox(height: 18)
                  : Text(
                      labelOf(pinned),
                      key: const ValueKey('activity-heatmap-pinned'),
                      style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
            ),
            Text('Ít ', style: caption),
            for (var lv = 0; lv <= 4; lv++)
              Container(
                width: 12,
                height: 12,
                margin: const EdgeInsets.symmetric(horizontal: 2),
                decoration: BoxDecoration(color: _colorOf(context, lv), borderRadius: BorderRadius.circular(3)),
              ),
            Text(' Nhiều', style: caption),
          ],
        ),
      ],
    );
  }

  Widget _cell(BuildContext context, HeatmapCell? cell, HeatmapCell? pinned) {
    if (cell == null) return const SizedBox(width: kHeatmapCell, height: kHeatmapCell);
    final scheme = Theme.of(context).colorScheme;
    final isPinned = pinned == cell;
    return Semantics(
      label: labelOf(cell),
      button: true,
      child: GestureDetector(
        key: ValueKey('heatmap-${cell.date}'),
        behavior: HitTestBehavior.opaque,
        onTap: () => _onCellTap(cell),
        child: Container(
          width: kHeatmapCell,
          height: kHeatmapCell,
          decoration: BoxDecoration(
            color: _colorOf(context, cell.level),
            borderRadius: BorderRadius.circular(3),
            border: isPinned
                ? Border.all(color: scheme.onSurface, width: 2)
                : cell.isToday
                ? Border.all(color: scheme.secondary, width: 2)
                : null,
          ),
        ),
      ),
    );
  }
}
