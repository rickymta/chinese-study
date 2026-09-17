import 'package:flutter/material.dart';

import 'af_page_body.dart';

/// Thanh hành động dính đáy màn (nút chính của phiên ôn, nộp quiz, "Gửi lại"...), có `SafeArea` cho tai thỏ/home bar.
///
/// Đặt vào `Scaffold.bottomNavigationBar` hoặc `persistentFooterButtons`; nội dung rộng tối đa 600 như trang.
class StickyActionBar extends StatelessWidget {
  const StickyActionBar({super.key, required this.children, this.padding = const EdgeInsets.fromLTRB(16, 8, 16, 8)});

  /// Các nút — xếp ngang, mỗi nút `Expanded` để đều nhau; một nút ⇒ chiếm hết chiều ngang.
  final List<Widget> children;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.surface,
      child: DecoratedBox(
        decoration: BoxDecoration(
          border: Border(top: BorderSide(color: scheme.outlineVariant)),
        ),
        child: SafeArea(
          top: false,
          // `heightFactor: 1`: cao ĐÚNG bằng nội dung. `Center` trần trong `Scaffold.bottomNavigationBar` (ràng buộc
          // lỏng) sẽ phình ra toàn màn hình và đè lên thân trang (phát hiện ở M6 — chạm vào thẻ không ăn).
          child: Align(
            heightFactor: 1,
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
              child: Padding(
                padding: padding,
                child: Row(
                  children: [
                    for (var i = 0; i < children.length; i++) ...[
                      if (i > 0) const SizedBox(width: 12),
                      Expanded(child: children[i]),
                    ],
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
