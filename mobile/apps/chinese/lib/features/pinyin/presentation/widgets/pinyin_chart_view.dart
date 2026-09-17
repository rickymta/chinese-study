import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/pinyin/pinyin.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import 'pinyin_load_error.dart';
import 'syllable_sheet.dart';

/// Nhóm thanh mẫu (thứ tự chip như web; `all` = mọi nhóm, cột Ø luôn hiện).
const kInitialGroups = ['all', 'moi', 'dau-luoi', 'cuong-luoi', 'mat-luoi', 'dau-luoi-truoc', 'uon-luoi'];
const kFinalGroups = ['all', 'don', 'kep', 'mui', 'i', 'u', 'v', 'dac-biet'];

const kInitialGroupLabels = <String, String>{
  'all': 'Tất cả',
  'khong': 'Không thanh mẫu',
  'moi': 'Môi (b p m f)',
  'dau-luoi': 'Đầu lưỡi (d t n l)',
  'cuong-luoi': 'Cuống lưỡi (g k h)',
  'mat-luoi': 'Mặt lưỡi (j q x)',
  'dau-luoi-truoc': 'Đầu lưỡi trước (z c s)',
  'uon-luoi': 'Uốn lưỡi (zh ch sh r)',
};
const kFinalGroupLabels = <String, String>{
  'all': 'Tất cả',
  'don': 'Đơn',
  'kep': 'Kép',
  'mui': 'Mũi (-n/-ng)',
  'i': 'Nhóm i',
  'u': 'Nhóm u',
  'v': 'Nhóm ü',
  'dac-biet': 'Đặc biệt',
};

/// Kích thước ô (hợp đồng: ≥ 48×44), cột đầu, hàng tiêu đề nhóm.
const double kChartCellW = 48;
const double kChartCellH = 44;
const double kChartFirstColW = 64;
const double kChartGroupRowH = 26;

/// Key ô âm tiết (test): `chart-cell-<syllable>`.
Key chartCellKey(String syllable) => ValueKey('chart-cell-$syllable');

/// Tab Bảng (port `PinyinChart.tsx`): hàng = vận mẫu (theo nhóm), cột = Ø + thanh mẫu; chip lọc nhóm (điện thoại mặc
/// định `tm=moi` để bảng hẹp — D33). CHỈ VÙNG BẢNG cuộn hai chiều (trang không tràn ngang ở 360 px); hàng tiêu đề +
/// cột đầu dính — tự dựng bằng 2 cặp `ScrollController` đồng bộ (BA-mặc định, không thêm `two_dimensional_scrollables`).
/// Chạm ô ⇒ [showSyllableSheet]. Giữ trạng thái lọc/cuộn khi đổi tab (`AutomaticKeepAliveClientMixin`).
class PinyinChartView extends ConsumerStatefulWidget {
  const PinyinChartView({super.key});

  @override
  ConsumerState<PinyinChartView> createState() => _PinyinChartViewState();
}

class _PinyinChartViewState extends ConsumerState<PinyinChartView> with AutomaticKeepAliveClientMixin {
  String _tm = 'moi';
  String _vm = 'all';

  final _headerH = ScrollController();
  final _bodyH = ScrollController();
  final _firstColV = ScrollController();
  final _bodyV = ScrollController();

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    // Thân bảng cuộn ⇒ hàng tiêu đề/cột đầu chạy theo (chỉ một chiều mỗi cặp, tránh vòng lặp). Kẹp vào biên của
    // bên nhận: thân có thể vượt biên (bounce/overscroll) hoặc hai bên chưa cùng kích thước trong một khung hình.
    _bodyH.addListener(() => _follow(_bodyH, _headerH));
    _bodyV.addListener(() => _follow(_bodyV, _firstColV));
  }

  static void _follow(ScrollController source, ScrollController target) {
    if (!source.hasClients || !target.hasClients) return;
    final max = target.position.maxScrollExtent;
    final offset = source.offset.clamp(0.0, max < 0 ? 0.0 : max);
    if (target.offset != offset) target.jumpTo(offset);
  }

  @override
  void dispose() {
    _headerH.dispose();
    _bodyH.dispose();
    _firstColV.dispose();
    _bodyV.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final chart = ref.watch(pinyinChartProvider);
    final theme = Theme.of(context);
    return AsyncValueView<PinyinChart>(
      value: chart,
      loading: const Padding(padding: EdgeInsets.all(16), child: PinyinSkeleton(rows: 1, height: 320)),
      error: (e) => Padding(
        padding: const EdgeInsets.all(16),
        child: PinyinLoadError(error: e, onRetry: () => ref.invalidate(pinyinChartProvider)),
      ),
      data: (c) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 8),
          _ChipRow(
            label: 'Thanh mẫu',
            options: kInitialGroups,
            labels: kInitialGroupLabels,
            selected: _tm,
            onSelected: (v) => setState(() => _tm = v),
          ),
          _ChipRow(
            label: 'Vận mẫu',
            options: kFinalGroups,
            labels: kFinalGroupLabels,
            selected: _vm,
            onSelected: (v) => setState(() => _vm = v),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            child: Text(
              'Bấm một ô để nghe 4 thanh. Ô chữ mờ: âm tiết có thật nhưng chưa có chữ minh hoạ đọc đúng.',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      border: Border.all(color: theme.colorScheme.outlineVariant),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(10),
                      child: _Grid(
                        chart: c,
                        tm: _tm,
                        vm: _vm,
                        headerH: _headerH,
                        bodyH: _bodyH,
                        firstColV: _firstColV,
                        bodyV: _bodyV,
                        onTapSyllable: (s) => showSyllableSheet(context, chart: c, syllable: s),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Một hàng chip lọc cuộn ngang (2 hàng × ~40 px thay vì `Wrap` chiếm nửa màn ở 360 px / chữ 1,3×).
class _ChipRow extends StatelessWidget {
  const _ChipRow({
    required this.label,
    required this.options,
    required this.labels,
    required this.selected,
    required this.onSelected,
  });

  final String label;
  final List<String> options;
  final Map<String, String> labels;
  final String selected;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        children: [
          Center(child: Text(label, style: Theme.of(context).textTheme.labelMedium)),
          const SizedBox(width: 8),
          for (final o in options) ...[
            Center(
              child: ChoiceChip(
                label: Text(labels[o] ?? o),
                selected: selected == o,
                onSelected: (_) => onSelected(o),
                visualDensity: VisualDensity.compact,
              ),
            ),
            const SizedBox(width: 6),
          ],
        ],
      ),
    );
  }
}

/// Hàng của lưới: tiêu đề nhóm hoặc một vận mẫu.
sealed class _GridRow {
  const _GridRow();
}

class _GroupRow extends _GridRow {
  const _GroupRow(this.group);

  final String group;
}

class _FinalRow extends _GridRow {
  const _FinalRow(this.fin);

  final PinyinFinal fin;
}

class _Grid extends StatelessWidget {
  const _Grid({
    required this.chart,
    required this.tm,
    required this.vm,
    required this.headerH,
    required this.bodyH,
    required this.firstColV,
    required this.bodyV,
    required this.onTapSyllable,
  });

  final PinyinChart chart;
  final String tm;
  final String vm;
  final ScrollController headerH;
  final ScrollController bodyH;
  final ScrollController firstColV;
  final ScrollController bodyV;
  final ValueChanged<PinyinSyllable> onTapSyllable;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    // Cột Ø luôn hiện; cột thanh mẫu lọc theo nhóm.
    final columns = chart.initials.where((i) => i.code.isEmpty || tm == 'all' || i.group == tm).toList();
    final finals = chart.finals.where((f) => vm == 'all' || f.group == vm).toList();
    // Nhóm hàng theo `group` (giữ thứ tự file) để chèn tiêu đề nhóm.
    final rows = <_GridRow>[];
    String? lastGroup;
    for (final f in finals) {
      if (f.group != lastGroup) {
        rows.add(_GroupRow(f.group));
        lastGroup = f.group;
      }
      rows.add(_FinalRow(f));
    }
    final lookup = <String, PinyinSyllable>{for (final s in chart.syllables) '${s.initial}|${s.final_}': s};
    final bodyWidth = columns.length * kChartCellW;
    final headerStyle = theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w700);
    final headerBg = scheme.surfaceContainerLow;
    final groupBg = scheme.surfaceContainerHighest;

    double rowHeight(_GridRow r) => r is _GroupRow ? kChartGroupRowH : kChartCellH;

    return Column(
      children: [
        // Hàng tiêu đề (dính): góc + tên thanh mẫu — cuộn ngang theo thân, không tự cuộn.
        SizedBox(
          height: kChartCellH,
          child: Row(
            children: [
              Container(
                width: kChartFirstColW,
                height: kChartCellH,
                alignment: Alignment.center,
                color: headerBg,
                child: Text(
                  'vận \\ thanh',
                  style: theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant),
                ),
              ),
              Expanded(
                child: SingleChildScrollView(
                  controller: headerH,
                  scrollDirection: Axis.horizontal,
                  physics: const NeverScrollableScrollPhysics(),
                  child: Container(
                    color: headerBg,
                    child: Row(
                      children: [
                        for (final c in columns)
                          SizedBox(
                            width: kChartCellW,
                            height: kChartCellH,
                            child: Center(child: Text(c.code.isEmpty ? 'Ø' : c.display, style: headerStyle)),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
        Divider(height: 1, color: scheme.outlineVariant),
        Expanded(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Cột đầu (dính): tên vận mẫu / nhãn nhóm — cuộn dọc theo thân.
              SizedBox(
                width: kChartFirstColW,
                child: SingleChildScrollView(
                  controller: firstColV,
                  physics: const NeverScrollableScrollPhysics(),
                  child: Column(
                    children: [
                      for (final r in rows)
                        Container(
                          width: kChartFirstColW,
                          height: rowHeight(r),
                          alignment: Alignment.center,
                          padding: const EdgeInsets.symmetric(horizontal: 4),
                          color: r is _GroupRow ? groupBg : headerBg,
                          child: switch (r) {
                            _GroupRow(:final group) => Text(
                              kFinalGroupLabels[group] ?? group,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant),
                            ),
                            _FinalRow(:final fin) => Text(fin.display, style: headerStyle),
                          },
                        ),
                    ],
                  ),
                ),
              ),
              VerticalDivider(width: 1, color: scheme.outlineVariant),
              // Thân: cuộn dọc ngoài, ngang trong. Rộng ít nhất bằng vùng còn lại để dải hàng nhóm phủ kín khi
              // bảng hẹp hơn màn (lọc Môi ⇒ 5 cột = 240 px < 360 px).
              Expanded(
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final width = bodyWidth > constraints.maxWidth ? bodyWidth : constraints.maxWidth;
                    return SingleChildScrollView(
                      controller: bodyV,
                      child: SingleChildScrollView(
                        controller: bodyH,
                        scrollDirection: Axis.horizontal,
                        child: SizedBox(
                          width: width,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              for (final r in rows)
                                switch (r) {
                                  _GroupRow() => Container(width: width, height: kChartGroupRowH, color: groupBg),
                                  _FinalRow(:final fin) => SizedBox(
                                    height: kChartCellH,
                                    child: Row(
                                      children: [
                                        for (final c in columns)
                                          _Cell(syllable: lookup['${c.code}|${fin.code}'], onTap: onTapSyllable),
                                      ],
                                    ),
                                  ),
                                },
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Một ô: âm tiết (mờ khi chưa có chữ minh hoạ); trống khi tổ hợp không tồn tại.
class _Cell extends StatelessWidget {
  const _Cell({required this.syllable, required this.onTap});

  final PinyinSyllable? syllable;
  final ValueChanged<PinyinSyllable> onTap;

  @override
  Widget build(BuildContext context) {
    final s = syllable;
    if (s == null) return const SizedBox(width: kChartCellW, height: kChartCellH);
    final scheme = Theme.of(context).colorScheme;
    final label = displaySyllableKey(s.syllable);
    return SizedBox(
      width: kChartCellW,
      height: kChartCellH,
      child: InkWell(
        key: chartCellKey(s.syllable),
        onTap: () => onTap(s),
        child: Semantics(
          button: true,
          label: 'Âm tiết $label',
          child: Center(
            child: Text(label, style: TextStyle(fontSize: 14, color: s.hasAnyTone ? scheme.onSurface : scheme.outline)),
          ),
        ),
      ),
    );
  }
}
