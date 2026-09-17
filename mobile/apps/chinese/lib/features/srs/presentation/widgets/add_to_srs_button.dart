import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../api/clients.dart';
import '../../../dictionary/application/providers.dart';
import '../../../dictionary/data/models.dart';
import '../../../progress/application/providers.dart';
import '../../application/providers.dart';
import '../../data/srs_api.dart';

/// `dd/MM` theo giờ máy (Dart không có CSDL múi giờ — BA-mặc định, RK-M23; web dùng múi giờ hồ sơ).
String formatDueDate(DateTime dueAt) {
  final d = dueAt.toLocal();
  return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}';
}

/// Nút/chip trạng thái SRS ở chi tiết từ (hợp đồng §5.3.2, port `AddToSrsButton.tsx`; M6 tạo sẵn, M8 dùng ở
/// `/tu-dien/:id`): chưa có thẻ ⇒ "Thêm vào ôn tập"; có thẻ ⇒ chip "Chờ học" (`new`) / "Đang ôn · đến hạn dd/MM";
/// tạm dừng ⇒ chip "Đã tạm dừng" + "Tiếp tục ôn". Lỗi ghi báo tại chỗ (không điều hướng). Thành công ⇒ làm mới tóm tắt
/// SRS + tổng quan + chi tiết từ (để `word.srs` đổi thành "Chờ học").
class AddToSrsButton extends ConsumerStatefulWidget {
  const AddToSrsButton({super.key, required this.word});

  final WordDetail word;

  @override
  ConsumerState<AddToSrsButton> createState() => _AddToSrsButtonState();
}

class _AddToSrsButtonState extends ConsumerState<AddToSrsButton> {
  bool _busy = false;
  String? _error;

  void _afterChange() {
    ref.invalidateSrsSummary();
    ref.invalidateProgressOverview();
    ref.invalidate(wordDetailProvider(widget.word.id));
  }

  Future<void> _add() async {
    if (_busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final res = await addSrsCards(ref.read(chineseDioProvider), [widget.word.id]);
      if (!mounted) return;
      _afterChange();
      showAfToast(
        context,
        res.createdFor(widget.word.id) ? 'Đã thêm — thẻ sẽ xuất hiện trong lượt từ mới' : 'Từ này đã có trong ôn tập',
        kind: AfToastKind.success,
      );
    } on Object catch (err) {
      final e = ApiError.from(err);
      if (!mounted) return;
      setState(() {
        _error = e.code == 'UNKNOWN_WORD' ? 'Từ này chưa có trong kho học liệu — không thêm được.' : e.message;
      });
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _suspend(bool suspended) async {
    final srs = widget.word.srs;
    if (_busy || srs == null) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await setSrsCardSuspension(ref.read(chineseDioProvider), cardId: srs.cardId, suspended: suspended);
      if (!mounted) return;
      _afterChange();
      showAfToast(context, suspended ? 'Đã tạm dừng thẻ này' : 'Đã tiếp tục ôn thẻ này', kind: AfToastKind.success);
    } on Object catch (err) {
      if (!mounted) return;
      setState(() => _error = ApiError.from(err).message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final srs = widget.word.srs;
    final scheme = Theme.of(context).colorScheme;
    final Widget row;
    if (srs == null) {
      row = FilledButton.icon(
        onPressed: _busy ? null : _add,
        icon: _busy
            ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2))
            : const Icon(Icons.add),
        label: const Text('Thêm vào ôn tập'),
        style: FilledButton.styleFrom(minimumSize: const Size(0, afMinTapHeight)),
      );
    } else if (srs.isSuspended) {
      row = Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          const Chip(avatar: Icon(Icons.pause_circle_outline, size: 18), label: Text('Đã tạm dừng')),
          OutlinedButton.icon(
            onPressed: _busy ? null : () => _suspend(false),
            icon: const Icon(Icons.play_arrow),
            label: const Text('Tiếp tục ôn'),
          ),
        ],
      );
    } else {
      final dueAt = srs.dueAt;
      final label = srs.state == 'new'
          ? 'Chờ học'
          : dueAt != null
          ? 'Đang ôn · đến hạn ${formatDueDate(dueAt)}'
          : 'Đang ôn';
      row = Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          Chip(
            avatar: Icon(Icons.style_outlined, size: 18, color: scheme.primary),
            label: Text(label),
            side: BorderSide(color: scheme.primary),
            labelStyle: TextStyle(color: scheme.primary),
          ),
          TextButton.icon(
            onPressed: _busy ? null : () => _suspend(true),
            icon: const Icon(Icons.pause_circle_outline),
            label: const Text('Tạm dừng'),
          ),
        ],
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        row,
        if (_error != null) ...[const SizedBox(height: 8), AuthBanner(message: _error!, isError: true)],
      ],
    );
  }
}
