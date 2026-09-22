import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../time_zones.dart';

/// Danh sách múi giờ để chọn (đã chuẩn hoá, có dự phòng) — `FutureProvider` để bottom sheet dùng lại giữa các lần
/// mở. Test override bằng `availableTimeZonesProvider.overrideWith((_) async => kFallbackTimeZones)`.
final availableTimeZonesProvider = FutureProvider<List<String>>((ref) => listTimeZones());

/// Ô chọn múi giờ (tương đương `TimeZoneAutocomplete` web, hợp đồng mobile M4): ô chỉ đọc hiện ID IANA, chạm ⇒ mở
/// bottom sheet có ô tìm (không phân biệt hoa thường, `_` ≡ khoảng trắng) trên danh sách múi giờ của máy; ghim đầu
/// "Múi giờ của máy: X" khi có [deviceTimeZone]. Nhãn chỉ là ID (không tính offset) [BA-mặc định].
class TimeZoneField extends StatelessWidget {
  const TimeZoneField({
    super.key,
    required this.value,
    required this.onChanged,
    this.deviceTimeZone,
    this.errorText,
    this.helperText,
    this.enabled = true,
    this.label = 'Múi giờ',
  });

  /// ID đang chọn (đã quy bí danh); rỗng ⇒ chưa chọn.
  final String value;
  final ValueChanged<String> onChanged;

  /// Múi giờ máy (ghim đầu danh sách); null ⇒ không ghim.
  final String? deviceTimeZone;

  /// Lỗi từ máy chủ (`422 INVALID_TIME_ZONE`) hoặc validator.
  final String? errorText;
  final String? helperText;
  final bool enabled;
  final String label;

  Future<void> _open(BuildContext context) async {
    final picked = await showAfBottomSheet<String>(
      context: context,
      // Sheet chỉ để CHỌN (không có dữ liệu đang nhập cần giữ) ⇒ cho phép chạm ngoài/kéo xuống để đóng nhanh.
      closeOnBarrier: true,
      builder: (_) => TimeZonePickerSheet(selected: value, deviceTimeZone: deviceTimeZone),
    );
    if (picked != null && picked != value) onChanged(picked);
  }

  @override
  Widget build(BuildContext context) {
    // Ô chỉ đọc: không cần controller — đổi [value] ⇒ đổi key ⇒ dựng lại field với initialValue mới (đổi
    // `controller.text` trong build/didUpdateWidget làm Form setState sai lúc).
    return TextFormField(
      key: ValueKey('tz:$value'),
      initialValue: value,
      readOnly: true,
      enabled: enabled,
      onTap: enabled ? () => _open(context) : null,
      decoration: InputDecoration(
        labelText: label,
        errorText: errorText,
        helperText: helperText,
        helperMaxLines: 3,
        suffixIcon: const Icon(Icons.arrow_drop_down),
      ),
    );
  }
}

/// Nội dung bottom sheet chọn múi giờ: ô tìm + danh sách; `Navigator.pop(id)` khi chọn.
class TimeZonePickerSheet extends ConsumerStatefulWidget {
  const TimeZonePickerSheet({super.key, required this.selected, this.deviceTimeZone});

  final String selected;
  final String? deviceTimeZone;

  @override
  ConsumerState<TimeZonePickerSheet> createState() => _TimeZonePickerSheetState();
}

class _TimeZonePickerSheetState extends ConsumerState<TimeZonePickerSheet> {
  final _query = TextEditingController();

  @override
  void dispose() {
    _query.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final zones = ref.watch(availableTimeZonesProvider);
    final device = widget.deviceTimeZone;
    final maxHeight = MediaQuery.sizeOf(context).height * 0.8;

    return SizedBox(
      height: maxHeight,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 4, 0),
            child: Row(
              children: [
                Expanded(child: Text('Chọn múi giờ', style: theme.textTheme.titleMedium)),
                IconButton(
                  tooltip: 'Đóng',
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            child: TextField(
              controller: _query,
              autofocus: true,
              autocorrect: false,
              textInputAction: TextInputAction.search,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                hintText: 'Tìm: ho chi, tokyo, europe…',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _query.text.isEmpty
                    ? null
                    : IconButton(
                        tooltip: 'Xoá',
                        onPressed: () => setState(_query.clear),
                        icon: const Icon(Icons.clear),
                      ),
              ),
            ),
          ),
          Expanded(
            child: AsyncValueView<List<String>>(
              value: zones,
              onRetry: () => ref.invalidate(availableTimeZonesProvider),
              data: (all) {
                // Múi giờ máy và giá trị đang chọn luôn có trong danh sách (kể cả khi plugin không liệt kê).
                final list = normalizeTimeZoneList(all, extra: [?device, widget.selected]);
                final q = _query.text;
                final pinned = device != null && matchesTimeZoneQuery(device, q) ? device : null;
                final rest = list.where((z) => z != pinned && matchesTimeZoneQuery(z, q)).toList();
                if (pinned == null && rest.isEmpty) {
                  return const EmptyState(
                    title: 'Không có múi giờ khớp',
                    message: 'Thử gõ tên thành phố hoặc châu lục.',
                  );
                }
                return ListView.builder(
                  itemCount: rest.length + (pinned == null ? 0 : 1),
                  itemBuilder: (context, i) {
                    if (pinned != null && i == 0) {
                      return _ZoneTile(
                        id: pinned,
                        subtitle: 'Múi giờ của máy',
                        selected: pinned == widget.selected,
                        onTap: () => Navigator.of(context).pop(pinned),
                      );
                    }
                    final id = rest[pinned == null ? i : i - 1];
                    return _ZoneTile(
                      id: id,
                      selected: id == widget.selected,
                      onTap: () => Navigator.of(context).pop(id),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _ZoneTile extends StatelessWidget {
  const _ZoneTile({required this.id, required this.selected, required this.onTap, this.subtitle});

  final String id;
  final String? subtitle;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(id),
      subtitle: subtitle == null ? null : Text(subtitle!),
      leading: Icon(subtitle == null ? Icons.public : Icons.phone_android),
      trailing: selected ? const Icon(Icons.check) : null,
      selected: selected,
      minTileHeight: 48,
      onTap: onTap,
    );
  }
}
