import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

// Kiểu dữ liệu từ điển theo hợp đồng F6/F7 §6.1 (port `features/dictionary/types.ts` web) — M6 chỉ cần chi tiết từ
// cho sheet "Xem chi tiết" của phiên ôn; M8 bổ sung tìm kiếm/chi tiết chữ. Trường null bị lược khỏi JSON ⇒ đọc "dễ tính".

/// Chữ cấu thành trong chi tiết từ.
@immutable
class WordCharacter {
  const WordCharacter({required this.hanzi, this.pinyinReadings = const [], this.hanViet = const [], this.strokeCount});

  static WordCharacter? fromJson(JsonMap? json) {
    final hanzi = readString(json, 'hanzi');
    if (hanzi == null || hanzi.isEmpty) return null;
    return WordCharacter(
      hanzi: hanzi,
      pinyinReadings: readPrimitiveList<String>(json, 'pinyinReadings'),
      hanViet: readPrimitiveList<String>(json, 'hanViet'),
      strokeCount: readInt(json, 'strokeCount'),
    );
  }

  final String hanzi;
  final List<String> pinyinReadings;
  final List<String> hanViet;
  final int? strokeCount;
}

/// Trạng thái thẻ SRS của người dùng cho từ này — null khi chưa có thẻ.
@immutable
class WordSrsInfo {
  const WordSrsInfo({required this.cardId, required this.state, required this.isSuspended, this.dueAt});

  static WordSrsInfo? fromJson(JsonMap? json) {
    final cardId = readString(json, 'cardId');
    if (json == null || cardId == null || cardId.isEmpty) return null;
    return WordSrsInfo(
      cardId: cardId,
      state: readStringOr(json, 'state'),
      dueAt: readDateTime(json, 'dueAt'),
      isSuspended: readBoolOr(json, 'isSuspended'),
    );
  }

  final String cardId;

  /// `new` | `learning` | `review` | `relearning` (chuỗi thô của server).
  final String state;
  final DateTime? dueAt;
  final bool isSuspended;
}

/// Chi tiết từ (`GET /dictionary/words/{id}`).
@immutable
class WordDetail {
  const WordDetail({
    required this.id,
    required this.simplified,
    required this.pinyin,
    this.traditional,
    this.variants = const [],
    this.hsk3Level,
    this.hsk2Level,
    this.pos = const [],
    this.usageNote,
    this.meaningsEn = const [],
    this.meaningsVi = const [],
    this.meaningViStatus,
    this.meaningViSource,
    this.hanViet,
    this.hanVietStatus,
    this.sources = const [],
    this.characters = const [],
    this.srs,
  });

  factory WordDetail.fromJson(JsonMap? json) => WordDetail(
    id: readStringOr(json, 'id'),
    simplified: readStringOr(json, 'simplified'),
    pinyin: readStringOr(json, 'pinyin'),
    traditional: readString(json, 'traditional'),
    variants: readPrimitiveList<String>(json, 'variants').where((v) => v.isNotEmpty).toList(),
    hsk3Level: readInt(json, 'hsk3Level'),
    hsk2Level: readInt(json, 'hsk2Level'),
    pos: readPrimitiveList<String>(json, 'pos'),
    usageNote: readString(json, 'usageNote'),
    meaningsEn: readPrimitiveList<String>(json, 'meaningsEn'),
    meaningsVi: readPrimitiveList<String>(json, 'meaningsVi'),
    meaningViStatus: readString(json, 'meaningViStatus'),
    meaningViSource: readString(json, 'meaningViSource'),
    hanViet: readString(json, 'hanViet'),
    hanVietStatus: readString(json, 'hanVietStatus'),
    sources: readPrimitiveList<String>(json, 'sources'),
    characters: readList(json, 'characters', WordCharacter.fromJson),
    srs: WordSrsInfo.fromJson(readMap(json, 'srs')),
  );

  final String id;
  final String simplified;

  /// Pinyin dạng SỐ.
  final String pinyin;
  final String? traditional;
  final List<String> variants;
  final int? hsk3Level;
  final int? hsk2Level;

  /// Mã từ loại (`n`, `v`, `a`…) — nhãn Việt qua `domain/pos.dart`, mã lạ ẩn.
  final List<String> pos;
  final String? usageNote;
  final List<String> meaningsEn;
  final List<String> meaningsVi;

  /// `machine` | `reviewed`.
  final String? meaningViStatus;

  /// `cvdict` | `machine` | `manual`.
  final String? meaningViSource;
  final String? hanViet;

  /// `derived` | `reviewed`.
  final String? hanVietStatus;
  final List<String> sources;
  final List<WordCharacter> characters;
  final WordSrsInfo? srs;

  /// Phồn thể chỉ hiện khi khác giản thể.
  bool get showTraditional => traditional != null && traditional!.isNotEmpty && traditional != simplified;

  WordDetail copyWith({WordSrsInfo? srs, bool clearSrs = false}) => WordDetail(
    id: id,
    simplified: simplified,
    pinyin: pinyin,
    traditional: traditional,
    variants: variants,
    hsk3Level: hsk3Level,
    hsk2Level: hsk2Level,
    pos: pos,
    usageNote: usageNote,
    meaningsEn: meaningsEn,
    meaningsVi: meaningsVi,
    meaningViStatus: meaningViStatus,
    meaningViSource: meaningViSource,
    hanViet: hanViet,
    hanVietStatus: hanVietStatus,
    sources: sources,
    characters: characters,
    srs: clearSrs ? null : (srs ?? this.srs),
  );
}
