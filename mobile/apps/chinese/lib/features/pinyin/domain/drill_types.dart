// Kiểu phiên luyện đang chạy + hàm thuần chấm/tính thời gian — port `features/pinyin/drill/drillTypes.ts` và
// `buildRequest` của `DrillTab.tsx`.
import 'package:flutter/foundation.dart';

import '../data/models.dart';
import 'generate_drill.dart';

/// Phiên luyện đang chạy — sinh ở client khi bấm "Bắt đầu" (R5-8, R5-10; RM-L2: id sinh lúc bắt đầu, giữ bộ nhớ).
@immutable
class DrillSession {
  const DrillSession({required this.clientSessionId, required this.mode, required this.items, required this.startedAt});

  /// `uuidV4()` — khoá idempotent khi nộp; giữ nguyên mọi lần "Gửi lại".
  final String clientSessionId;
  final DrillMode mode;
  final List<DrillItem> items;

  /// UTC.
  final DateTime startedAt;
}

/// Một câu đã trả lời (đủ mọi phần).
@immutable
class AnsweredItem {
  const AnsweredItem({
    required this.item,
    required this.answered,
    required this.correct,
    required this.responseMs,
    required this.replayCount,
  });

  final DrillItem item;

  /// Thanh đã chọn cho từng phần, cùng thứ tự `item.parts`.
  final List<int> answered;

  /// Đúng khi MỌI phần đúng (R5-9).
  final bool correct;

  /// Từ lúc phát xong lần đầu tới lúc chọn đủ; null nếu chưa từng phát.
  final int? responseMs;
  final int replayCount;
}

@immutable
class DrillOutcome {
  const DrillOutcome({required this.session, required this.answers, required this.finishedAt});

  final DrillSession session;
  final List<AnsweredItem> answers;

  /// UTC.
  final DateTime finishedAt;

  int get total => answers.length;
  int get correct => answers.where((a) => a.correct).length;
  List<AnsweredItem> get wrong => answers.where((a) => !a.correct).toList();
}

/// Cận trên `responseMs` backend chấp nhận (validator: 0..600000).
const kResponseMsMax = 600000;

/// Cận trên `replayCount` backend chấp nhận (0..100).
const kReplayCountMax = 100;

/// Thời gian trả lời của CÂU (ms): từ lúc phát xong lần đầu tới lúc chọn đủ. Chưa từng phát ⇒ null.
/// Quá 10 phút (để yên rồi quay lại) ⇒ null — gửi giá trị ngoài 0..600000 là 400 và mất cả phiên.
int? computeResponseMs(num? firstPlayEndAt, num now) {
  if (firstPlayEndAt == null) return null;
  final diff = now - firstPlayEndAt;
  if (!diff.isFinite) return null;
  final ms = diff.round();
  if (ms < 0) return null;
  return ms > kResponseMsMax ? null : ms;
}

/// Thống kê theo PHẦN tính ở client (hiện tạm trong lúc chờ server, và khi gửi lỗi).
Map<int, ToneCount> summarizeByTone(List<AnsweredItem> answers) {
  final total = {for (final t in kDrillTones) t: 0};
  final correct = {for (final t in kDrillTones) t: 0};
  for (final a in answers) {
    for (var i = 0; i < a.item.parts.length; i++) {
      final p = a.item.parts[i];
      if (!total.containsKey(p.tone)) continue;
      total[p.tone] = total[p.tone]! + 1;
      if (i < a.answered.length && a.answered[i] == p.tone) correct[p.tone] = correct[p.tone]! + 1;
    }
  }
  return {for (final t in kDrillTones) t: ToneCount(total: total[t]!, correct: correct[t]!)};
}

/// Body nộp bài từ kết quả phiên (port `buildRequest` của `DrillTab.tsx`).
SubmitToneDrillRequest buildSubmitRequest(DrillOutcome outcome) => SubmitToneDrillRequest(
  clientSessionId: outcome.session.clientSessionId,
  mode: outcome.session.mode,
  startedAt: outcome.session.startedAt,
  finishedAt: outcome.finishedAt,
  items: [
    for (final a in outcome.answers)
      SubmitDrillItem(
        parts: [
          for (var i = 0; i < a.item.parts.length; i++)
            SubmitDrillPart(
              syllable: a.item.parts[i].syllable,
              hanzi: a.item.parts[i].hanzi,
              expectedTone: a.item.parts[i].tone,
              answeredTone: a.answered[i],
            ),
        ],
        responseMs: a.responseMs,
        replayCount: a.replayCount,
      ),
  ],
);
