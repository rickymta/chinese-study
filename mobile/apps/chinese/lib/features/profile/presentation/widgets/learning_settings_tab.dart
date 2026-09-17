import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/speech/chinese_speech.dart';
import '../../../../core/widgets/pinyin_text.dart';
import '../../../../core/widgets/speak_button.dart';
import '../../../srs/application/providers.dart';
import '../../../srs/data/models.dart';
import '../pages/profile_page.dart';

/// `1,2` · `0,80` — số thực kiểu Việt.
String viNumber(double n, [int digits = 1]) => n.toStringAsFixed(digits).replaceAll('.', ',');

/// Tab "Học tập" (port `LearningSettingsTab.tsx`, hợp đồng M4): hạn mức từ mới/lượt ôn, độ nhớ mục tiêu, tốc độ đọc
/// (nghe thử theo giá trị ĐANG kéo), tự đọc khi hiện thẻ. Lưu ⇒ `PUT /me/learning-settings` ⇒ state provider đổi
/// (TTS đọc `ttsRate` từ đây); 400 hiện dưới ô theo `details`.
class LearningSettingsTab extends ConsumerWidget {
  const LearningSettingsTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // `unwrapPrevious`: lúc đổi tài khoản provider đang tải vẫn giữ `.value` của người trước ⇒ phải hiện vòng xoay,
    // không hiện số của người trước (review M4 C1).
    final settings = ref.watch(learningSettingsProvider).unwrapPrevious();
    return ProfileTabBody(
      child: AsyncValueView<LearningSettings>(
        value: settings,
        onRetry: () => ref.invalidate(learningSettingsProvider),
        data: (s) => _SettingsForm(settings: s),
      ),
    );
  }
}

class _SettingsForm extends ConsumerStatefulWidget {
  const _SettingsForm({required this.settings});

  final LearningSettings settings;

  @override
  ConsumerState<_SettingsForm> createState() => _SettingsFormState();
}

class _SettingsFormState extends ConsumerState<_SettingsForm> {
  final _formKey = GlobalKey<FormState>();
  final _reviewLimit = TextEditingController();
  late int _dailyNewCards;
  late int _retentionPercent;
  late double _ttsRate;
  late bool _autoPlay;
  final Map<String, String?> _serverErrors = {};
  String? _formError;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _reset(widget.settings);
  }

  @override
  void didUpdateWidget(_SettingsForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Server trả bản mới (sau khi lưu ở đây hoặc kéo tốc độ ở tab Giao diện) ⇒ form về "không đổi".
    if (oldWidget.settings != widget.settings) setState(() => _reset(widget.settings));
  }

  @override
  void dispose() {
    _reviewLimit.dispose();
    super.dispose();
  }

  void _reset(LearningSettings s) {
    _dailyNewCards = s.dailyNewCards;
    _reviewLimit.text = s.dailyReviewLimit.toString();
    _retentionPercent = (s.desiredRetention * 100).round();
    _ttsRate = clampTtsRate(s.ttsRate);
    _autoPlay = s.autoPlayAudio;
    _serverErrors.clear();
    _formError = null;
  }

  int? get _reviewLimitValue => int.tryParse(_reviewLimit.text.trim());

  bool get _dirty {
    final s = widget.settings;
    return _dailyNewCards != s.dailyNewCards ||
        _reviewLimitValue != s.dailyReviewLimit ||
        _retentionPercent != (s.desiredRetention * 100).round() ||
        _ttsRate != clampTtsRate(s.ttsRate) ||
        _autoPlay != s.autoPlayAudio;
  }

  static String? _validateReviewLimit(String? v) {
    final n = int.tryParse((v ?? '').trim());
    if (n == null) return 'Nhập một số nguyên';
    if (n < 10) return 'Ít nhất 10';
    if (n > 1000) return 'Nhiều nhất 1000';
    return null;
  }

  Future<void> _submit() async {
    if (_submitting) return;
    setState(() {
      _formError = null;
      _serverErrors.clear();
    });
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _submitting = true);
    try {
      await ref
          .read(learningSettingsProvider.notifier)
          .save(
            LearningSettings(
              dailyNewCards: _dailyNewCards,
              dailyReviewLimit: _reviewLimitValue!,
              desiredRetention: _retentionPercent / 100,
              ttsRate: _ttsRate,
              autoPlayAudio: _autoPlay,
            ),
          );
      if (!mounted) return;
      // M6: ref.invalidate(srsSummaryProvider) — hạn mức thẻ mới đổi ⇒ số thẻ đến hạn đổi.
      showAfToast(context, 'Đã lưu cài đặt học tập', kind: AfToastKind.success);
    } on Object catch (err) {
      final e = ApiError.from(err);
      if (!mounted) return;
      setState(() {
        var attached = false;
        for (final field in const [
          'dailyNewCards',
          'dailyReviewLimit',
          'desiredRetention',
          'ttsRate',
          'autoPlayAudio',
        ]) {
          final msgs = e.fieldErrors(field);
          if (msgs.isNotEmpty) {
            _serverErrors[field] = msgs.first;
            attached = true;
          }
        }
        if (!attached) _formError = e.message;
      });
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dirty = _dirty;
    final helperStyle = theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_formError != null) ...[AuthBanner(message: _formError!, isError: true), const SizedBox(height: 16)],
          if (widget.settings.isDefault && !dirty) ...[
            const AuthBanner(
              message: 'Đang dùng cài đặt mặc định. Người mới bắt đầu nên giữ 10 từ mới/ngày vài tuần đầu.',
            ),
            const SizedBox(height: 16),
          ],

          // ── Từ mới mỗi ngày ──
          _SliderField(
            label: 'Từ mới mỗi ngày',
            valueLabel: '$_dailyNewCards',
            value: _dailyNewCards.toDouble(),
            min: 0,
            max: 50,
            divisions: 50,
            enabled: !_submitting,
            onChanged: (v) => setState(() => _dailyNewCards = v.round()),
            helper:
                _serverErrors['dailyNewCards'] ??
                'Thẻ mới được đưa vào ôn mỗi ngày (0 = tạm ngừng học từ mới, chỉ ôn thẻ cũ).',
            isError: _serverErrors['dailyNewCards'] != null,
          ),
          const SizedBox(height: 16),

          // ── Giới hạn lượt ôn/ngày ──
          TextFormField(
            controller: _reviewLimit,
            enabled: !_submitting,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            validator: _validateReviewLimit,
            autovalidateMode: AutovalidateMode.onUserInteraction,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: 'Giới hạn lượt ôn/ngày',
              helperText: 'Từ 10 đến 1000. Thẻ đến hạn vượt giới hạn sẽ chờ sang ngày sau.',
              helperMaxLines: 2,
              errorText: _serverErrors['dailyReviewLimit'],
            ),
          ),
          const SizedBox(height: 20),

          // ── Độ nhớ mục tiêu ──
          _SliderField(
            label: 'Độ nhớ mục tiêu',
            valueLabel: '$_retentionPercent%',
            value: _retentionPercent.toDouble(),
            min: 80,
            max: 97,
            divisions: 17,
            enabled: !_submitting,
            onChanged: (v) => setState(() => _retentionPercent = v.round()),
            helper:
                _serverErrors['desiredRetention'] ??
                'Cao hơn ⇒ ôn dày hơn, nhớ chắc hơn. Chỉ ảnh hưởng các lượt chấm sau.',
            isError: _serverErrors['desiredRetention'] != null,
          ),
          const SizedBox(height: 16),

          // ── Tốc độ đọc + nghe thử theo giá trị ĐANG kéo ──
          _SliderField(
            label: 'Tốc độ đọc',
            valueLabel: viNumber(_ttsRate, 2),
            value: _ttsRate,
            min: kTtsRateMin,
            max: kTtsRateMax,
            divisions: ((kTtsRateMax - kTtsRateMin) / 0.05).round(),
            enabled: !_submitting,
            onChanged: (v) => setState(() => _ttsRate = clampTtsRate(v)),
            helper:
                _serverErrors['ttsRate'] ??
                'Mới học nên để 0,7–0,8 để nghe rõ đường nét thanh; quen rồi tăng dần lên 1,0.',
            isError: _serverErrors['ttsRate'] != null,
            trailing: Row(
              children: [
                SpeakButton(text: kSampleHanzi, variant: SpeakButtonVariant.button, label: 'Nghe thử', rate: _ttsRate),
                const SizedBox(width: 12),
                const PinyinText(kSamplePinyin, hanzi: kSampleHanzi),
              ],
            ),
          ),
          const SizedBox(height: 8),

          // ── Tự đọc khi hiện thẻ ──
          SwitchListTile(
            value: _autoPlay,
            onChanged: _submitting ? null : (v) => setState(() => _autoPlay = v),
            contentPadding: EdgeInsets.zero,
            title: const Text('Tự đọc khi hiện thẻ'),
            subtitle: Text(
              _serverErrors['autoPlayAudio'] ??
                  'Trong phiên ôn, thẻ vừa hiện sẽ được đọc ngay (trên bản web iPhone chỉ đọc sau khi bạn chạm màn).',
              style: helperStyle,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: dirty && !_submitting ? () => setState(() => _reset(widget.settings)) : null,
                child: const Text('Hoàn tác'),
              ),
              const SizedBox(width: 8),
              FilledButton(
                onPressed: dirty && !_submitting ? _submit : null,
                child: _submitting
                    ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Lưu'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Nhãn + giá trị đậm, thanh trượt, dòng giải thích (lỗi server màu lỗi).
class _SliderField extends StatelessWidget {
  const _SliderField({
    required this.label,
    required this.valueLabel,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    required this.onChanged,
    required this.helper,
    this.isError = false,
    this.enabled = true,
    this.trailing,
  });

  final String label;
  final String valueLabel;
  final double value;
  final double min;
  final double max;
  final int divisions;
  final ValueChanged<double> onChanged;
  final String helper;
  final bool isError;
  final bool enabled;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text.rich(
          TextSpan(
            text: '$label: ',
            children: [
              TextSpan(
                text: valueLabel,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
            ],
          ),
          style: theme.textTheme.bodyLarge,
        ),
        Slider(
          value: value.clamp(min, max),
          min: min,
          max: max,
          divisions: divisions,
          label: valueLabel,
          onChanged: enabled ? onChanged : null,
        ),
        ?trailing,
        if (trailing != null) const SizedBox(height: 4),
        Text(
          helper,
          style: theme.textTheme.bodySmall?.copyWith(
            color: isError ? theme.colorScheme.error : theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
