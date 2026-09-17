/// Đọc JSON "dễ tính" cho model viết tay (không codegen — hợp đồng mobile DB-M3).
///
/// Backend dùng `WhenWritingNull` ⇒ trường null bị LƯỢC khỏi JSON (RK-M21): thiếu khoá phải coi như null/mặc định,
/// KHÔNG ném. Kiểu sai (vd chuỗi thay số) cũng trả null để màn hình không trắng vì một trường lạ.
library;

/// Kiểu JSON object đã parse.
typedef JsonMap = Map<String, Object?>;

/// Ép một giá trị bất kỳ về [JsonMap] (Map có khoá không phải String ⇒ chuyển khoá sang String). Không phải Map ⇒ null.
JsonMap? asJsonMap(Object? value) {
  if (value == null) return null;
  if (value is JsonMap) return value;
  if (value is Map) return value.map((k, v) => MapEntry(k.toString(), v));
  return null;
}

String? readString(JsonMap? json, String key) {
  final v = json?[key];
  if (v == null) return null;
  if (v is String) return v;
  if (v is num || v is bool) return v.toString();
  return null;
}

/// Như [readString] nhưng thiếu ⇒ [fallback] (mặc định chuỗi rỗng).
String readStringOr(JsonMap? json, String key, [String fallback = '']) => readString(json, key) ?? fallback;

int? readInt(JsonMap? json, String key) {
  final v = json?[key];
  if (v is int) return v;
  if (v is double && v.isFinite && v == v.roundToDouble()) return v.toInt();
  if (v is String) return int.tryParse(v);
  return null;
}

int readIntOr(JsonMap? json, String key, [int fallback = 0]) => readInt(json, key) ?? fallback;

double? readDouble(JsonMap? json, String key) {
  final v = json?[key];
  if (v is num) return v.toDouble();
  if (v is String) return double.tryParse(v);
  return null;
}

double readDoubleOr(JsonMap? json, String key, [double fallback = 0]) => readDouble(json, key) ?? fallback;

bool? readBool(JsonMap? json, String key) {
  final v = json?[key];
  if (v is bool) return v;
  if (v is String) {
    final s = v.toLowerCase();
    if (s == 'true') return true;
    if (s == 'false') return false;
  }
  return null;
}

bool readBoolOr(JsonMap? json, String key, [bool fallback = false]) => readBool(json, key) ?? fallback;

/// Đọc mốc thời gian ISO-8601 (`2026-09-16T08:00:00Z`). Chuỗi không hợp lệ ⇒ null. Luôn trả về UTC.
DateTime? readDateTime(JsonMap? json, String key) {
  final v = json?[key];
  if (v is! String || v.isEmpty) return null;
  final parsed = DateTime.tryParse(v);
  return parsed?.toUtc();
}

/// Đọc mảng, mỗi phần tử qua [convert]; phần tử không phải Map (hoặc convert trả null) bị bỏ qua. Thiếu ⇒ rỗng.
List<T> readList<T>(JsonMap? json, String key, T? Function(JsonMap item) convert) {
  final v = json?[key];
  if (v is! List) return const [];
  final out = <T>[];
  for (final item in v) {
    final m = asJsonMap(item);
    if (m == null) continue;
    final converted = convert(m);
    if (converted != null) out.add(converted);
  }
  return out;
}

/// Đọc mảng giá trị nguyên thuỷ (chuỗi/số...) — phần tử null hoặc sai kiểu bị bỏ qua. Thiếu ⇒ rỗng.
List<T> readPrimitiveList<T>(JsonMap? json, String key) {
  final v = json?[key];
  if (v is! List) return const [];
  return v.whereType<T>().toList();
}

/// Đọc object con. Thiếu/sai kiểu ⇒ null.
JsonMap? readMap(JsonMap? json, String key) => asJsonMap(json?[key]);
