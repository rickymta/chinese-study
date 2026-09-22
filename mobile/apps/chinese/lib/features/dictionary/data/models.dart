import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

// Kiểu dữ liệu từ điển theo hợp đồng F6/F7 §6.1 (port `features/dictionary/types.ts` web): M6 tạo chi tiết từ cho
// sheet "Xem chi tiết" của phiên ôn; M8 thêm dòng kết quả tìm, trang kết quả và chi tiết chữ. Serializer backend bật
// `WhenWritingNull` ⇒ trường null bị LƯỢC khỏi JSON ⇒ đọc "dễ tính" (thiếu = null/mặc định, RK41).

/// Số dòng mỗi trang tìm (`pageSize` mặc định của web — 1..100).
const kSearchPageSize = 20;

/// Một dòng kết quả tìm (`meaningsVi` tối đa 3 phần tử). `matchKind`: `browse` khi `q` rỗng (liệt kê theo lộ trình),
/// còn lại `hanzi` | `pinyin` | `han_viet` | `meaning` (chuỗi thô của server, chỉ để tham khảo).
@immutable
class WordSummary {
  const WordSummary({
    required this.id,
    required this.simplified,
    required this.pinyin,
    this.traditional,
    this.hsk3Level,
    this.hsk2Level,
    this.hanViet,
    this.meaningsVi = const [],
    this.meaningViStatus,
    this.matchKind,
  });

  /// Dòng thiếu `id`/`simplified` (dữ liệu hỏng) ⇒ `null` để danh sách bỏ qua.
  static WordSummary? fromJson(JsonMap? json) {
    final id = readString(json, 'id');
    final simplified = readString(json, 'simplified');
    if (id == null || id.isEmpty || simplified == null || simplified.isEmpty) return null;
    return WordSummary(
      id: id,
      simplified: simplified,
      pinyin: readStringOr(json, 'pinyin'),
      traditional: readString(json, 'traditional'),
      hsk3Level: readInt(json, 'hsk3Level'),
      hsk2Level: readInt(json, 'hsk2Level'),
      hanViet: readString(json, 'hanViet'),
      meaningsVi: readPrimitiveList<String>(json, 'meaningsVi'),
      meaningViStatus: readString(json, 'meaningViStatus'),
      matchKind: readString(json, 'matchKind'),
    );
  }

  final String id;
  final String simplified;

  /// Pinyin dạng SỐ.
  final String pinyin;
  final String? traditional;
  final int? hsk3Level;
  final int? hsk2Level;
  final String? hanViet;
  final List<String> meaningsVi;

  /// `machine` | `reviewed`.
  final String? meaningViStatus;
  final String? matchKind;
}

/// Một trang kết quả `GET /dictionary/search`.
@immutable
class SearchResponse {
  const SearchResponse({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    int? rawCount,
  }) : rawCount = rawCount ?? items.length;

  factory SearchResponse.fromJson(JsonMap? json) {
    final raw = json?['items'];
    return SearchResponse(
      items: readList(json, 'items', WordSummary.fromJson),
      page: readIntOr(json, 'page', 1),
      pageSize: readIntOr(json, 'pageSize', kSearchPageSize),
      totalCount: readIntOr(json, 'totalCount'),
      rawCount: raw is List ? raw.length : 0,
    );
  }

  final List<WordSummary> items;
  final int page;
  final int pageSize;
  final int totalCount;

  /// Số dòng THÔ server trả (kể cả dòng hỏng bị [WordSummary.fromJson] bỏ) — trang cuối khi `< pageSize`.
  final int rawCount;

  /// Trang này là trang cuối: server trả ít hơn `pageSize` (hoặc rỗng). Không dựa vào `items.length` vì dòng hỏng bị
  /// lược sẽ làm số đã nối không bao giờ đuổi kịp `totalCount`.
  bool get isLastPage => rawCount < pageSize;
}

/// Chi tiết chữ (`GET /dictionary/characters/{hanzi}`): cách đọc, Hán Việt, số nét, bộ thủ, phồn thể, ≤ 20 từ chứa chữ.
@immutable
class CharacterDetail {
  const CharacterDetail({
    required this.hanzi,
    this.traditionalVariants = const [],
    this.pinyinReadings = const [],
    this.hanViet = const [],
    this.hanVietByPinyin = const {},
    this.hanVietStatus,
    this.strokeCount,
    this.radical,
    this.radicalNumber,
    this.words = const [],
  });

  factory CharacterDetail.fromJson(JsonMap? json) {
    final byPinyin = readMap(json, 'hanVietByPinyin') ?? const {};
    return CharacterDetail(
      hanzi: readStringOr(json, 'hanzi'),
      traditionalVariants: readPrimitiveList<String>(json, 'traditionalVariants').where((t) => t.isNotEmpty).toList(),
      pinyinReadings: readPrimitiveList<String>(json, 'pinyinReadings').where((r) => r.isNotEmpty).toList(),
      hanViet: readPrimitiveList<String>(json, 'hanViet').where((h) => h.isNotEmpty).toList(),
      hanVietByPinyin: {
        for (final e in byPinyin.entries)
          if (e.value is String) e.key: e.value! as String,
      },
      hanVietStatus: readString(json, 'hanVietStatus'),
      strokeCount: readInt(json, 'strokeCount'),
      radical: readString(json, 'radical'),
      radicalNumber: readInt(json, 'radicalNumber'),
      words: readList(json, 'words', WordSummary.fromJson),
    );
  }

  final String hanzi;

  /// Biến thể phồn thể — trùng giản thể bị lược khi hiển thị.
  final List<String> traditionalVariants;

  /// Pinyin dạng SỐ, mỗi cách đọc một phần tử.
  final List<String> pinyinReadings;

  /// Âm Hán Việt (đầu tiên là âm chính — in đậm).
  final List<String> hanViet;
  final Map<String, String> hanVietByPinyin;

  /// `derived` | `reviewed`.
  final String? hanVietStatus;
  final int? strokeCount;
  final String? radical;

  /// Số bộ thủ Khang Hy 1..214.
  final int? radicalNumber;

  /// Từ có chữ này — tối đa 20, sắp theo `path_order`.
  final List<WordSummary> words;

  /// Phồn thể khác giản thể (để hiện dòng "Phồn thể").
  List<String> get traditionalShown => traditionalVariants.where((t) => t != hanzi).toList();
}

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
