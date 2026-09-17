/// Xáo lựa chọn quiz — port 1-1 `features/lessons/lib/shuffle.ts` web (Fisher–Yates + LCG tất định cho test).
library;

import 'dart:math';

import '../data/models.dart';

/// Nguồn ngẫu nhiên `[0, 1)` — mặc định `Random()`; test truyền hàm giả ([seededRng]) để có kết quả tất định.
typedef Rng = double Function();

final Random _random = Random();

double _defaultRng() => _random.nextDouble();

/// Fisher–Yates, KHÔNG sửa mảng vào; [rng] trả `[0, 1)`.
List<T> shuffle<T>(List<T> items, [Rng? rng]) {
  final r = rng ?? _defaultRng;
  final out = [...items];
  for (var i = out.length - 1; i > 0; i--) {
    // Kẹp về [0, i] phòng rng trả 1 (hoặc giá trị lạ) ⇒ không bao giờ đọc ngoài mảng.
    final j = (r() * (i + 1)).floor().clamp(0, i);
    final tmp = out[i];
    out[i] = out[j];
    out[j] = tmp;
  }
  return out;
}

/// Thứ tự HIỂN THỊ lựa chọn theo từng câu — xáo MỘT LẦN lúc bắt đầu lượt, giữ nguyên khi quay lại câu trước.
typedef OptionOrder = Map<String, List<QuizOption>>;

OptionOrder shuffleOptions(List<QuizQuestion> questions, [Rng? rng]) {
  final r = rng ?? _defaultRng;
  final out = <String, List<QuizOption>>{};
  for (final q in questions) {
    out[q.id] = shuffle(q.options, r);
  }
  return out;
}

/// Rng tất định (LCG 32-bit — Numerical Recipes, cùng hằng số với web) cho test và "làm lại cùng thứ tự" nếu cần.
/// Không dùng cho mục đích bảo mật.
Rng seededRng(int seed) {
  var state = seed & 0xFFFFFFFF;
  return () {
    state = (state * 1664525 + 1013904223) & 0xFFFFFFFF;
    return state / 0x100000000;
  };
}
