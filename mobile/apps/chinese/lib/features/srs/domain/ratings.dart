// Nhãn Việt + màu ngữ nghĩa của 4 mức chấm (hợp đồng §5.3.2) — port `lib/ratings.ts` web (bỏ phím tắt: điện thoại).
import '../data/models.dart';

/// Màu ngữ nghĩa (tên theo palette MUI của web; quy sang `ColorScheme` ở widget `RatingBar`).
enum RatingColor { error, warning, success, info }

class RatingMeta {
  const RatingMeta({required this.label, required this.color});

  final String label;
  final RatingColor color;
}

const Map<SrsRating, RatingMeta> kRatingMeta = {
  SrsRating.again: RatingMeta(label: 'Quên', color: RatingColor.error),
  SrsRating.hard: RatingMeta(label: 'Khó', color: RatingColor.warning),
  SrsRating.good: RatingMeta(label: 'Được', color: RatingColor.success),
  SrsRating.easy: RatingMeta(label: 'Dễ', color: RatingColor.info),
};
