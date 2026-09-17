import 'package:flutter/material.dart';

import '../../../data/models.dart';
import '../syllable_sheet.dart' show kToneGlyph;

/// Key nút thanh (test): `tone-<hàng>-<thanh>` — hàng 0 (một âm tiết / chữ thứ nhất), hàng 1 (chữ thứ hai).
Key toneButtonKey(int row, int tone) => ValueKey('tone-$row-$tone');

/// 4 nút thanh lớn (cao ≥ 64, port `ToneButtons.tsx`) — nằm trong tầm ngón cái; nhãn "1 ˉ", "2 ˊ", "3 ˇ", "4 ˋ".
/// Ở trạng thái chấm ([correctTone] khác null): tô xanh thanh đúng, đỏ thanh chọn sai.
class ToneButtons extends StatelessWidget {
  const ToneButtons({
    super.key,
    required this.value,
    required this.onSelect,
    this.disabled = false,
    this.correctTone,
    this.label,
    this.row = 0,
  });

  /// Thanh đã chọn (nếu có).
  final int? value;
  final ValueChanged<int> onSelect;
  final bool disabled;
  final int? correctTone;
  final String? label;
  final int row;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final graded = correctTone != null;
    // Xanh "đúng" = primary (xanh lá AntFarm), đỏ "sai" = error.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        if (label != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: Text(label!, style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant)),
          ),
        Row(
          children: [
            for (var i = 0; i < kDrillTones.length; i++) ...[
              if (i > 0) const SizedBox(width: 8),
              Expanded(
                child: Builder(
                  builder: (context) {
                    final t = kDrillTones[i];
                    final isChosen = value == t;
                    final isCorrect = graded && correctTone == t;
                    final isWrongChoice = graded && isChosen && correctTone != t;
                    final Color bg = isCorrect
                        ? scheme.primary
                        : isWrongChoice
                        ? scheme.error
                        : isChosen
                        ? scheme.primary
                        : Colors.transparent;
                    final Color border = isCorrect
                        ? scheme.primary
                        : isWrongChoice
                        ? scheme.error
                        : isChosen
                        ? scheme.primary
                        : scheme.outlineVariant;
                    final Color fg = isCorrect || isChosen
                        ? (isWrongChoice ? scheme.onError : scheme.onPrimary)
                        : scheme.onSurface;
                    final enabled = !disabled;
                    return Semantics(
                      button: true,
                      label: 'Thanh $t',
                      selected: isChosen,
                      child: Opacity(
                        opacity: enabled || graded ? 1 : 0.5,
                        child: Material(
                          color: bg,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                            side: BorderSide(color: border, width: 2),
                          ),
                          child: InkWell(
                            key: toneButtonKey(row, t),
                            borderRadius: BorderRadius.circular(10),
                            onTap: enabled ? () => onSelect(t) : null,
                            child: ConstrainedBox(
                              constraints: const BoxConstraints(minHeight: 64),
                              child: Column(
                                mainAxisAlignment: MainAxisAlignment.center,
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Text(
                                    '$t',
                                    style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: fg, height: 1.1),
                                  ),
                                  ExcludeSemantics(
                                    child: Text(kToneGlyph[t]!, style: TextStyle(fontSize: 20, color: fg, height: 1)),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ],
        ),
      ],
    );
  }
}
