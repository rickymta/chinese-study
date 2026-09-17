import 'package:flutter/material.dart';

/// Chiều rộng tối đa nội dung một cột (mobile-first §5.3.8): điện thoại dùng hết chiều ngang, tablet/web căn giữa.
const afMaxContentWidth = 600.0;

/// Khoảng đệm mặc định của trang.
const afPagePadding = EdgeInsets.fromLTRB(16, 12, 16, 24);

/// Bọc nội dung trang: giới hạn rộng 600, căn giữa, đệm chuẩn. [scrollable] ⇒ `SingleChildScrollView`.
class AfPageBody extends StatelessWidget {
  const AfPageBody({
    super.key,
    required this.child,
    this.padding = afPagePadding,
    this.scrollable = true,
    this.maxWidth = afMaxContentWidth,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final bool scrollable;
  final double maxWidth;

  @override
  Widget build(BuildContext context) {
    final content = Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: maxWidth),
        child: Padding(padding: padding, child: child),
      ),
    );
    if (!scrollable) return content;
    return SingleChildScrollView(child: content);
  }
}
