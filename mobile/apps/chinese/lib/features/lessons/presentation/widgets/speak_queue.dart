import 'dart:async';

import 'package:flutter/foundation.dart';

/// Đọc lần lượt nhiều câu ("Nghe cả đoạn" ở khối hội thoại — port `useSpeakQueue.ts`): gọi [speak] từng câu, chờ xong
/// mới câu kế. Dừng khi bấm "Dừng" hoặc rời màn ([dispose] của widget gọi [stop]). Lần gọi đầu nằm trong handler nút
/// (iOS mở khoá TTS); các câu sau phát tiếp trong cùng phiên đã mở khoá. `_runId` tăng mỗi lượt để lượt cũ (nếu còn
/// chờ) tự thoát.
class SpeakQueueController extends ChangeNotifier {
  SpeakQueueController({required this.speak, required this.cancel});

  /// Đọc một câu, hoàn tất khi phát xong (hoặc bị ngắt). Lỗi phát bị nuốt bên trong.
  final Future<void> Function(String text) speak;

  /// Huỷ lượt đang đọc của engine.
  final Future<void> Function() cancel;

  bool _playing = false;
  int? _activeIndex;
  int _runId = 0;
  bool _disposed = false;

  /// Đang đọc cả đoạn.
  bool get playing => _playing;

  /// Chỉ số câu đang đọc (`null` khi không đọc) — để tô nền dòng.
  int? get activeIndex => _activeIndex;

  /// Đọc lần lượt; PHẢI gọi trong handler thao tác người dùng (iOS). Bấm khi đang đọc ⇒ bên gọi dùng [stop].
  Future<void> playAll(List<String> texts) async {
    if (texts.isEmpty) return;
    final runId = ++_runId;
    _playing = true;
    notifyListeners();
    for (var i = 0; i < texts.length; i++) {
      if (_disposed || _runId != runId) return;
      _activeIndex = i;
      notifyListeners();
      // Lỗi phát một câu không chặn câu kế.
      try {
        await speak(texts[i]);
      } on Object catch (_) {}
    }
    if (_disposed || _runId != runId) return;
    _playing = false;
    _activeIndex = null;
    notifyListeners();
  }

  void stop() {
    _runId++;
    final wasPlaying = _playing;
    _playing = false;
    _activeIndex = null;
    if (wasPlaying) unawaited(cancel());
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _runId++;
    if (_playing) unawaited(cancel());
    super.dispose();
  }
}
