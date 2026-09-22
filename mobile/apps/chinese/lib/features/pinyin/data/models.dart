/// Kiểu dữ liệu pinyin & luyện thanh (chinese-backend `/api/pinyin/*`, HĐ45 §6.1) — port `features/pinyin/types.ts`
/// + `api.ts#normalizeToneStats`. Serializer backend bật `WhenWritingNull` ⇒ trường `null` có thể bị LƯỢC khỏi JSON
/// (RK41): mọi `fromJson` đọc qua `json_read` (thiếu ⇒ null/mặc định, không ném).
library;

import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

/// Thanh 1–4 dùng trong bảng/bài luyện (thanh nhẹ không có chữ minh hoạ — R-L1).
const kDrillTones = <int>[1, 2, 3, 4];

/// Chế độ luyện — giá trị API `snake_case` (`JsonStringEnumConverter(SnakeCaseLower)` phía backend).
enum DrillMode {
  listenTone('listen_tone', 'Một âm tiết'),
  tonePair('tone_pair', 'Cặp thanh');

  const DrillMode(this.apiValue, this.label);

  final String apiValue;
  final String label;

  /// Giá trị URL `?che-do=mot|cap` (giống web).
  String get param => this == DrillMode.tonePair ? 'cap' : 'mot';

  static DrillMode fromApi(String? value) =>
      value == DrillMode.tonePair.apiValue ? DrillMode.tonePair : DrillMode.listenTone;

  static DrillMode fromParam(String? value) => value == 'cap' ? DrillMode.tonePair : DrillMode.listenTone;
}

/// Một ví dụ pinyin số + chữ Hán (trong `initials[].examples`).
@immutable
class PinyinExampleRef {
  const PinyinExampleRef({required this.pinyin, required this.hanzi});

  factory PinyinExampleRef.fromJson(JsonMap json) =>
      PinyinExampleRef(pinyin: readStringOr(json, 'pinyin'), hanzi: readStringOr(json, 'hanzi'));

  final String pinyin;
  final String hanzi;
}

/// Thanh mẫu (phụ âm đầu). [code] rỗng = không thanh mẫu (cột Ø).
@immutable
class PinyinInitial {
  const PinyinInitial({
    required this.code,
    required this.group,
    required this.display,
    this.ipa = '',
    this.aspirated = false,
    this.noteVi = '',
    this.examples = const [],
  });

  factory PinyinInitial.fromJson(JsonMap json) => PinyinInitial(
    code: readStringOr(json, 'code'),
    group: readStringOr(json, 'group'),
    display: readStringOr(json, 'display'),
    ipa: readStringOr(json, 'ipa'),
    aspirated: readBoolOr(json, 'aspirated'),
    noteVi: readStringOr(json, 'noteVi'),
    examples: readList(json, 'examples', PinyinExampleRef.fromJson),
  );

  final String code;

  /// `khong | moi | dau-luoi | cuong-luoi | mat-luoi | dau-luoi-truoc | uon-luoi`.
  final String group;
  final String display;
  final String ipa;
  final bool aspirated;
  final String noteVi;
  final List<PinyinExampleRef> examples;
}

/// Vận mẫu (phần vần). `v` = ü; `-i` = vận mẫu sau z/c/s/zh/ch/sh/r.
@immutable
class PinyinFinal {
  const PinyinFinal({
    required this.code,
    required this.group,
    required this.display,
    this.standaloneSpelling,
    this.noteVi = '',
  });

  factory PinyinFinal.fromJson(JsonMap json) => PinyinFinal(
    code: readStringOr(json, 'code'),
    group: readStringOr(json, 'group'),
    display: readStringOr(json, 'display'),
    standaloneSpelling: readString(json, 'standaloneSpelling'),
    noteVi: readStringOr(json, 'noteVi'),
  );

  final String code;

  /// `don | kep | mui | i | u | v | dac-biet`.
  final String group;
  final String display;

  /// Cách viết khi đứng một mình (`yan`, `wu`, `yu`); null nếu không tự đứng.
  final String? standaloneSpelling;
  final String noteVi;
}

/// Chữ minh hoạ cho một (âm tiết, thanh) — R5-3.
@immutable
class ToneExample {
  const ToneExample({required this.hanzi, this.meaningVi = ''});

  factory ToneExample.fromJson(JsonMap json) =>
      ToneExample(hanzi: readStringOr(json, 'hanzi'), meaningVi: readStringOr(json, 'meaningVi'));

  final String hanzi;
  final String meaningVi;

  @override
  bool operator ==(Object other) => other is ToneExample && other.hanzi == hanzi && other.meaningVi == meaningVi;

  @override
  int get hashCode => Object.hash(hanzi, meaningVi);
}

/// Một hàng bảng âm tiết. [tones] chỉ có khoá 1..4 và chỉ những thanh CÓ chữ minh hoạ (rỗng ⇒ không nghe được).
@immutable
class PinyinSyllable {
  const PinyinSyllable({required this.syllable, required this.initial, required this.final_, this.tones = const {}});

  factory PinyinSyllable.fromJson(JsonMap json) {
    final rawTones = readMap(json, 'tones') ?? const {};
    final tones = <int, ToneExample>{};
    for (final t in kDrillTones) {
      final ex = asJsonMap(rawTones['$t']);
      if (ex == null) continue;
      final example = ToneExample.fromJson(ex);
      if (example.hanzi.isNotEmpty) tones[t] = example;
    }
    return PinyinSyllable(
      syllable: readStringOr(json, 'syllable'),
      initial: readStringOr(json, 'initial'),
      final_: readStringOr(json, 'final'),
      tones: tones,
    );
  }

  /// Khoá R5-1: chữ thường, không thanh, `v` chỉ ở `nv|lv|nve|lve`.
  final String syllable;
  final String initial;

  /// Mã vận mẫu (`final` là từ khoá Dart nên đặt tên `final_`).
  final String final_;
  final Map<int, ToneExample> tones;

  bool get hasAnyTone => tones.isNotEmpty;
}

/// `GET /pinyin/chart` — [version] là ETag phía server.
@immutable
class PinyinChart {
  const PinyinChart({required this.version, required this.initials, required this.finals, required this.syllables});

  factory PinyinChart.fromJson(JsonMap? json) => PinyinChart(
    version: readStringOr(json, 'version'),
    initials: readList(json, 'initials', PinyinInitial.fromJson),
    finals: readList(json, 'finals', PinyinFinal.fromJson),
    syllables: readList(json, 'syllables', PinyinSyllable.fromJson),
  );

  final String version;
  final List<PinyinInitial> initials;
  final List<PinyinFinal> finals;
  final List<PinyinSyllable> syllables;

  /// Tra âm tiết theo khoá (`ma`) — dùng khi chấm "Nghe thanh bạn chọn".
  PinyinSyllable? syllableByKey(String key) {
    for (final s in syllables) {
      if (s.syllable == key) return s;
    }
    return null;
  }
}

/// Ví dụ trong hướng dẫn: pinyin số + chữ + nghĩa.
@immutable
class GuideExample {
  const GuideExample({required this.pinyin, required this.hanzi, this.meaningVi = ''});

  factory GuideExample.fromJson(JsonMap json) => GuideExample(
    pinyin: readStringOr(json, 'pinyin'),
    hanzi: readStringOr(json, 'hanzi'),
    meaningVi: readStringOr(json, 'meaningVi'),
  );

  final String pinyin;
  final String hanzi;
  final String meaningVi;
}

/// Khối nội dung hướng dẫn (`guide.json`, đa hình theo `type`). Kiểu lạ ⇒ bỏ qua (không ném).
@immutable
sealed class GuideBlock {
  const GuideBlock();

  static GuideBlock? fromJson(JsonMap json) => switch (readString(json, 'type')) {
    'paragraph' => GuideParagraph(readStringOr(json, 'text')),
    'tip' => GuideTip(readStringOr(json, 'text')),
    'tone_contour' => GuideToneContour(readPrimitiveList<int>(json, 'tones')),
    'examples' => GuideExamples(readList(json, 'items', GuideExample.fromJson)),
    'compare' => GuideCompare(
      title: readStringOr(json, 'title'),
      pairs: readList(json, 'pairs', GuideComparePair.fromJson),
    ),
    _ => null,
  };
}

class GuideParagraph extends GuideBlock {
  const GuideParagraph(this.text);

  final String text;
}

class GuideTip extends GuideBlock {
  const GuideTip(this.text);

  final String text;
}

/// Đường nét cao độ các thanh (1–4, 5 = thanh nhẹ).
class GuideToneContour extends GuideBlock {
  const GuideToneContour(this.tones);

  final List<int> tones;
}

class GuideExamples extends GuideBlock {
  const GuideExamples(this.items);

  final List<GuideExample> items;
}

@immutable
class GuideComparePair {
  const GuideComparePair({required this.left, required this.right, this.noteVi});

  static GuideComparePair? fromJson(JsonMap json) {
    final left = readMap(json, 'left');
    final right = readMap(json, 'right');
    if (left == null || right == null) return null;
    return GuideComparePair(
      left: GuideExample.fromJson(left),
      right: GuideExample.fromJson(right),
      noteVi: readString(json, 'noteVi'),
    );
  }

  final GuideExample left;
  final GuideExample right;
  final String? noteVi;
}

class GuideCompare extends GuideBlock {
  const GuideCompare({required this.title, required this.pairs});

  final String title;
  final List<GuideComparePair> pairs;
}

@immutable
class GuideTopic {
  const GuideTopic({required this.id, required this.title, required this.order, required this.blocks});

  factory GuideTopic.fromJson(JsonMap json) => GuideTopic(
    id: readStringOr(json, 'id'),
    title: readStringOr(json, 'title'),
    order: readIntOr(json, 'order'),
    blocks: readList(json, 'blocks', GuideBlock.fromJson),
  );

  final String id;
  final String title;
  final int order;
  final List<GuideBlock> blocks;
}

/// `GET /pinyin/guide` — chủ đề đã sắp theo [GuideTopic.order] (như `getGuide` web).
@immutable
class PinyinGuide {
  const PinyinGuide({required this.version, required this.topics});

  factory PinyinGuide.fromJson(JsonMap? json) {
    final topics = readList(json, 'topics', GuideTopic.fromJson)..sort((a, b) => a.order.compareTo(b.order));
    return PinyinGuide(version: readStringOr(json, 'version'), topics: topics);
  }

  final String version;
  final List<GuideTopic> topics;
}

/// Một phần của câu trong yêu cầu nộp bài.
@immutable
class SubmitDrillPart {
  const SubmitDrillPart({
    required this.syllable,
    required this.hanzi,
    required this.expectedTone,
    required this.answeredTone,
  });

  final String syllable;
  final String hanzi;
  final int expectedTone;
  final int answeredTone;

  JsonMap toJson() => {
    'syllable': syllable,
    'hanzi': hanzi,
    'expectedTone': expectedTone,
    'answeredTone': answeredTone,
  };
}

/// Một câu — 1 phần (`listen_tone`) hoặc 2 phần (`tone_pair`). [responseMs] của CÂU, null nếu không đo được.
@immutable
class SubmitDrillItem {
  const SubmitDrillItem({required this.parts, required this.responseMs, required this.replayCount});

  final List<SubmitDrillPart> parts;
  final int? responseMs;
  final int replayCount;

  JsonMap toJson() => {
    'parts': [for (final p in parts) p.toJson()],
    'responseMs': responseMs,
    'replayCount': replayCount,
  };
}

/// Body `POST /pinyin/tone-drills` (§6.1). Thời điểm gửi dạng ISO-8601 UTC có `Z` (`toUtc().toIso8601String()`) —
/// backend từ chối chuỗi không `Z` (D38). [clientSessionId] sinh lúc bắt đầu phiên — nộp lại cùng id là idempotent.
@immutable
class SubmitToneDrillRequest {
  const SubmitToneDrillRequest({
    required this.clientSessionId,
    required this.mode,
    required this.startedAt,
    required this.finishedAt,
    required this.items,
  });

  final String clientSessionId;
  final DrillMode mode;
  final DateTime startedAt;
  final DateTime finishedAt;
  final List<SubmitDrillItem> items;

  JsonMap toJson() => {
    'clientSessionId': clientSessionId,
    'mode': mode.apiValue,
    'startedAt': startedAt.toUtc().toIso8601String(),
    'finishedAt': finishedAt.toUtc().toIso8601String(),
    'items': [for (final i in items) i.toJson()],
  };
}

/// Đếm theo một thanh (`total`/`correct`).
@immutable
class ToneCount {
  const ToneCount({this.total = 0, this.correct = 0});

  factory ToneCount.fromJson(JsonMap? json) =>
      ToneCount(total: readIntOr(json, 'total'), correct: readIntOr(json, 'correct'));

  final int total;
  final int correct;

  @override
  bool operator ==(Object other) => other is ToneCount && other.total == total && other.correct == correct;

  @override
  int get hashCode => Object.hash(total, correct);

  @override
  String toString() => 'ToneCount($correct/$total)';
}

/// 201 (mới) / 200 (đã nộp trước đó) của `POST /pinyin/tone-drills`. [total]/[correct] đếm theo CÂU; [byTone] theo
/// PHẦN, luôn đủ 4 khoá; [localDate] `yyyy-MM-dd` theo múi giờ người dùng (RM-L4: hiển thị đúng chuỗi server).
@immutable
class SubmitToneDrillResponse {
  const SubmitToneDrillResponse({
    required this.id,
    required this.clientSessionId,
    required this.mode,
    required this.total,
    required this.correct,
    required this.localDate,
    required this.byTone,
  });

  factory SubmitToneDrillResponse.fromJson(JsonMap? json) {
    final raw = readMap(json, 'byTone');
    return SubmitToneDrillResponse(
      id: readStringOr(json, 'id'),
      clientSessionId: readStringOr(json, 'clientSessionId'),
      mode: DrillMode.fromApi(readString(json, 'mode')),
      total: readIntOr(json, 'total'),
      correct: readIntOr(json, 'correct'),
      localDate: readStringOr(json, 'localDate'),
      byTone: {for (final t in kDrillTones) t: ToneCount.fromJson(asJsonMap(raw?['$t']))},
    );
  }

  final String id;
  final String clientSessionId;
  final DrillMode mode;
  final int total;
  final int correct;
  final String localDate;
  final Map<int, ToneCount> byTone;
}

/// Thống kê một thanh trong cửa sổ 200 phần gần nhất. [accuracy] null khi `total = 0`.
@immutable
class ToneAccuracy {
  const ToneAccuracy({this.total = 0, this.correct = 0, this.accuracy});

  final int total;
  final int correct;
  final double? accuracy;

  @override
  bool operator ==(Object other) =>
      other is ToneAccuracy && other.total == total && other.correct == correct && other.accuracy == accuracy;

  @override
  int get hashCode => Object.hash(total, correct, accuracy);

  @override
  String toString() => 'ToneAccuracy($correct/$total, $accuracy)';
}

/// Một cặp thanh hay bị nhầm: nghe [expected] thành [answered].
@immutable
class ToneConfusion {
  const ToneConfusion({required this.expected, required this.answered, required this.count});

  static ToneConfusion? fromJson(JsonMap json) {
    final expected = readInt(json, 'expected');
    final answered = readInt(json, 'answered');
    if (expected == null || answered == null) return null;
    return ToneConfusion(expected: expected, answered: answered, count: readIntOr(json, 'count'));
  }

  final int expected;
  final int answered;
  final int count;

  @override
  bool operator ==(Object other) =>
      other is ToneConfusion && other.expected == expected && other.answered == answered && other.count == count;

  @override
  int get hashCode => Object.hash(expected, answered, count);
}

/// `GET /pinyin/tone-stats` đã CHUẨN HOÁ (port `normalizeToneStats` web): phản hồi thô có thể thiếu khoá `null`
/// (RK41) ⇒ bù về null/0; [byTone] luôn đủ 4 khoá; [windowSize] mặc định 200.
@immutable
class ToneStats {
  const ToneStats({
    this.totalAnswered = 0,
    this.sessionsCount = 0,
    this.lastSessionAt,
    this.windowSize = 200,
    this.accuracy,
    this.byTone = const {1: ToneAccuracy(), 2: ToneAccuracy(), 3: ToneAccuracy(), 4: ToneAccuracy()},
    this.confusions = const [],
    this.recommendedFocus = const [],
    this.g0Reached = false,
  });

  /// Chuẩn hoá phản hồi thô — xem [normalizeToneStats].
  factory ToneStats.fromJson(JsonMap? json) => normalizeToneStats(json);

  final int totalAnswered;
  final int sessionsCount;
  final DateTime? lastSessionAt;
  final int windowSize;

  /// Độ chính xác chung trong cửa sổ; null khi chưa có dữ liệu.
  final double? accuracy;
  final Map<int, ToneAccuracy> byTone;
  final List<ToneConfusion> confusions;

  /// Thanh yếu (độ chính xác < 0,8 với ≥ 10 câu) — 50% câu của bài kế dồn vào đây.
  final List<int> recommendedFocus;

  /// Mỗi thanh ≥ 20 câu và ≥ 85% ⇒ đã "xong G0" (§1.3).
  final bool g0Reached;
}

/// Đọc `accuracy`: chỉ nhận SỐ, còn lại (thiếu/null/chuỗi) ⇒ null — như `typeof x === 'number'` ở web.
double? _readAccuracy(JsonMap? json) {
  final v = json?['accuracy'];
  return v is num ? v.toDouble() : null;
}

/// Port 1-1 `normalizeToneStats` (`features/pinyin/api.ts`): phản hồi thô có thể THIẾU khoá `null` ⇒ bù về giá trị
/// mặc định để giao diện nhất quán; `byTone` luôn đủ 4 khoá 1..4.
ToneStats normalizeToneStats(JsonMap? raw) {
  final rawByTone = readMap(raw, 'byTone');
  final byTone = <int, ToneAccuracy>{};
  for (final t in kDrillTones) {
    final v = asJsonMap(rawByTone?['$t']);
    byTone[t] = ToneAccuracy(
      total: readIntOr(v, 'total'),
      correct: readIntOr(v, 'correct'),
      accuracy: _readAccuracy(v),
    );
  }
  return ToneStats(
    totalAnswered: readIntOr(raw, 'totalAnswered'),
    sessionsCount: readIntOr(raw, 'sessionsCount'),
    lastSessionAt: readDateTime(raw, 'lastSessionAt'),
    windowSize: readIntOr(raw, 'windowSize', 200),
    accuracy: _readAccuracy(raw),
    byTone: byTone,
    confusions: readList(raw, 'confusions', ToneConfusion.fromJson),
    recommendedFocus: readPrimitiveList<int>(raw, 'recommendedFocus'),
    g0Reached: readBool(raw, 'g0Reached') ?? false,
  );
}
