import 'package:flutter/material.dart';

/// Màu "cảnh báo" (MUI `warning.main`) — theme AntFarm không có slot warning; sáng/tối lấy từ MUI mặc định.
Color warningColorOf(BuildContext context) =>
    Theme.of(context).brightness == Brightness.dark ? const Color(0xFFFFA726) : const Color(0xFFED6C02);

/// Màu "thành công" = primary xanh lá của theme (web dùng `success.main` cùng tông với primary).
Color successColorOf(BuildContext context) => Theme.of(context).colorScheme.primary;

/// Số liệu lớn dùng chữ số đều bề rộng để không nhảy khi làm mới.
const kTabularNumbers = [FontFeature.tabularFigures()];

/// Khung thẻ thống nhất cho các khối của trang tổng quan (port `DashboardCard.tsx`): tiêu đề + icon nhỏ + phần tử
/// bên phải ([aside]: chip, chú thích); không vừa một dòng (360 px, chữ 1,3×) thì [aside] xuống dòng thay vì tràn.
class DashboardCard extends StatelessWidget {
  const DashboardCard({super.key, required this.title, this.icon, this.aside, required this.children});

  final String title;
  final IconData? icon;
  final Widget? aside;

  /// Nội dung theo cột, cách nhau 12 (giống `gap: 1.5` web).
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final heading = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 20, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: 8),
        ],
        Flexible(
          child: Semantics(
            header: true,
            child: Text(title, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          ),
        ),
      ],
    );
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            if (aside == null)
              heading
            else
              Wrap(
                alignment: WrapAlignment.spaceBetween,
                crossAxisAlignment: WrapCrossAlignment.center,
                spacing: 8,
                runSpacing: 4,
                children: [heading, aside!],
              ),
            for (final child in children) ...[const SizedBox(height: 12), child],
          ],
        ),
      ),
    );
  }
}

/// Số lớn + nhãn phụ (vd `64 / 500 từ trong lộ trình`) — dùng ở nhiều thẻ.
class BigNumber extends StatelessWidget {
  const BigNumber({super.key, required this.value, this.suffix, this.color, this.semanticsLabel});

  final String value;
  final String? suffix;
  final Color? color;
  final String? semanticsLabel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Text.rich(
      TextSpan(
        text: value,
        style: theme.textTheme.headlineSmall?.copyWith(
          fontWeight: FontWeight.w700,
          fontFeatures: kTabularNumbers,
          color: color,
        ),
        children: [
          if (suffix != null)
            TextSpan(
              text: ' $suffix',
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
        ],
      ),
      semanticsLabel: semanticsLabel,
    );
  }
}

/// Dòng chữ phụ (màu `onSurfaceVariant`), tuỳ chọn đậm/màu khác.
class MutedText extends StatelessWidget {
  const MutedText(this.text, {super.key, this.color, this.bold = false, this.small = false});

  final String text;
  final Color? color;
  final bool bold;
  final bool small;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final base = small ? theme.textTheme.bodySmall : theme.textTheme.bodyMedium;
    return Text(
      text,
      style: base?.copyWith(
        color: color ?? theme.colorScheme.onSurfaceVariant,
        fontWeight: bold ? FontWeight.w600 : null,
      ),
    );
  }
}

/// Thanh tiến độ bo tròn (`LinearProgress` web), cao [height].
class RoundedProgressBar extends StatelessWidget {
  const RoundedProgressBar({super.key, required this.value, this.color, this.height = 10, this.semanticsLabel});

  /// 0..1.
  final double value;
  final Color? color;
  final double height;
  final String? semanticsLabel;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(height / 2),
      child: LinearProgressIndicator(
        value: value.clamp(0.0, 1.0),
        minHeight: height,
        color: color,
        backgroundColor: Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.08),
        semanticsLabel: semanticsLabel,
      ),
    );
  }
}
