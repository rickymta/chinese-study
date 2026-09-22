import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';

/// Trang tạm cho tính năng chưa lên mobile (được thay dần ở M5–M10).
class ComingSoonPage extends StatelessWidget {
  const ComingSoonPage({super.key, required this.title, this.icon = Icons.construction_outlined});

  final String title;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: AfPageBody(
        child: EmptyState(
          icon: icon,
          title: 'Tính năng này sắp có trên ứng dụng',
          message: 'Trong lúc chờ, dùng bản web tại chinese.antfarms.xyz — tiến độ học được tính chung.',
        ),
      ),
    );
  }
}
