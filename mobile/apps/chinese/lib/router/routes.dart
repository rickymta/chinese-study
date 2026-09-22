/// Hằng đường dẫn (slug tiếng Việt không dấu, giống web). KHÔNG route nào bắt đầu bằng `/chinese` hay `/identity`
/// — proxy dev nuốt hai tiền tố đó (hợp đồng mobile §5.3.8).
abstract final class AppRoutes {
  static const home = '/';
  static const review = '/on-tap';
  static const reviewSession = '/on-tap/phien';
  static const lessons = '/bai-hoc';

  /// Mẫu route bài học chi tiết (`:slug`, M9): `/bai-hoc/:slug?tab=noi-dung|tu-vung|quiz`.
  static const lessonPattern = '/bai-hoc/:slug';

  /// `/bai-hoc/<slug>` (slug đã `Uri.encodeComponent`, giống web).
  static String lesson(String slug) => '$lessons/${Uri.encodeComponent(slug)}';
  static const writing = '/luyen-viet';
  static const more = '/them';
  static const pinyin = '/pinyin';
  static const dictionary = '/tu-dien';

  /// Mẫu route chi tiết từ / chi tiết chữ (M8). `/tu-dien/chu/:hanzi` khai TRƯỚC `/tu-dien/:id` trong router.
  static const wordPattern = '/tu-dien/:id';
  static const characterPattern = '/tu-dien/chu/:hanzi';

  /// `/tu-dien/<id>`.
  static String word(String id) => '$dictionary/${Uri.encodeComponent(id)}';

  /// `/tu-dien/chu/<hanzi>` — chữ Hán luôn `Uri.encodeComponent` (`爱` ⇒ `%E7%88%B1`); go_router giải mã lại ở
  /// `pathParameters`.
  static String character(String hanzi) => '$dictionary/chu/${Uri.encodeComponent(hanzi)}';

  /// `/tu-dien?q=<q>` — mở trang tìm với từ khoá sẵn (liên kết từ bài học M9…).
  static String dictionarySearch(String q) =>
      q.trim().isEmpty ? dictionary : '$dictionary?q=${Uri.encodeQueryComponent(q.trim())}';
  static const profile = '/ho-so';
  static const licenses = '/giay-phep';
  static const login = '/dang-nhap';
  static const register = '/dang-ky';
  static const unauthorized = '/401';
  static const forbidden = '/403';
  static const notFound = '/404';
}
