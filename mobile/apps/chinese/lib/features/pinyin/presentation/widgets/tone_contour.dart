import 'package:flutter/material.dart';

/// Mô tả một thanh trên thang Chao 5 mức (port `CONTOURS` của `ToneContour.tsx`).
class ToneContourSpec {
  const ToneContourSpec({required this.label, required this.points, required this.name});

  final String label;
  final List<int> points;
  final String name;
}

/// 1 = 55 · 2 = 35 · 3 = 214 · 4 = 51 · 5 (thanh nhẹ) = chấm ngắn ở mức 3.
const kToneContours = <int, ToneContourSpec>{
  1: ToneContourSpec(label: '55', points: [5, 5], name: 'Thanh 1 — cao, bằng'),
  2: ToneContourSpec(label: '35', points: [3, 5], name: 'Thanh 2 — đi lên'),
  3: ToneContourSpec(label: '214', points: [2, 1, 4], name: 'Thanh 3 — xuống rồi lên'),
  4: ToneContourSpec(label: '51', points: [5, 1], name: 'Thanh 4 — xuống mạnh'),
  5: ToneContourSpec(label: '·', points: [3, 3], name: 'Thanh nhẹ — ngắn'),
};

const double _w = 96;
const double _h = 72;
const double _pad = 8;

/// Đường nét cao độ 5 mức của từng thanh — vẽ bằng `CustomPainter` (hợp đồng M7), không thư viện.
class ToneContour extends StatelessWidget {
  const ToneContour({super.key, required this.tones});

  final List<int> tones;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Wrap(
      spacing: 12,
      runSpacing: 12,
      children: [
        for (final t in tones)
          if (kToneContours[t] case final spec?)
            Semantics(
              label: spec.name,
              child: Container(
                constraints: const BoxConstraints(minWidth: 112),
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  border: Border.all(color: theme.colorScheme.outlineVariant),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    CustomPaint(
                      size: const Size(_w, _h),
                      painter: _ContourPainter(
                        points: spec.points,
                        neutral: t == 5,
                        gridColor: theme.colorScheme.outlineVariant,
                        lineColor: theme.colorScheme.primary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${t == 5 ? 'Thanh nhẹ' : 'Thanh $t'} · ${spec.label}',
                      style: theme.textTheme.labelMedium?.copyWith(fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
              ),
            ),
      ],
    );
  }
}

class _ContourPainter extends CustomPainter {
  const _ContourPainter({
    required this.points,
    required this.neutral,
    required this.gridColor,
    required this.lineColor,
  });

  final List<int> points;
  final bool neutral;
  final Color gridColor;
  final Color lineColor;

  double _y(num level) => _h - _pad - ((level - 1) / 4) * (_h - _pad * 2);

  @override
  void paint(Canvas canvas, Size size) {
    final grid = Paint()
      ..color = gridColor
      ..strokeWidth = 1;
    // 5 mức cao độ.
    for (var lv = 1; lv <= 5; lv++) {
      final y = _y(lv);
      canvas.drawLine(Offset(_pad, y), Offset(_w - _pad, y), grid);
    }
    final stepX = (_w - _pad * 2) / (points.length - 1).clamp(1, 99);
    final line = Paint()
      ..color = lineColor
      ..strokeWidth = neutral ? 6 : 4
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..style = PaintingStyle.stroke;
    if (neutral) {
      // Thanh nhẹ: chấm ngắn ở mức 3 (web: `strokeDasharray="2 10"`).
      final y = _y(3);
      for (var x = _pad; x <= _w - _pad; x += 12) {
        canvas.drawLine(Offset(x, y), Offset(x + 2, y), line);
      }
      return;
    }
    final path = Path();
    for (var i = 0; i < points.length; i++) {
      final x = _pad + i * stepX;
      final y = _y(points[i]);
      if (i == 0) {
        path.moveTo(x, y);
      } else {
        path.lineTo(x, y);
      }
    }
    canvas.drawPath(path, line);
  }

  @override
  bool shouldRepaint(_ContourPainter old) =>
      old.points != points || old.neutral != neutral || old.gridColor != gridColor || old.lineColor != lineColor;
}
