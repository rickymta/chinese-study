/// Hằng đường dẫn (slug tiếng Việt không dấu, giống web). KHÔNG route nào bắt đầu bằng `/chinese` hay `/identity`
/// — proxy dev nuốt hai tiền tố đó (hợp đồng mobile §5.3.8).
abstract final class AppRoutes {
  static const home = '/';
  static const review = '/on-tap';
  static const reviewSession = '/on-tap/phien';
  static const lessons = '/bai-hoc';

  /// Mẫu route bài học chi tiết (`:slug`), M9 — M5 dẫn tới `ComingSoonPage`.
  static const lessonPattern = '/bai-hoc/:slug';

  /// `/bai-hoc/<slug>` (slug đã `Uri.encodeComponent`, giống web).
  static String lesson(String slug) => '$lessons/${Uri.encodeComponent(slug)}';
  static const writing = '/luyen-viet';
  static const more = '/them';
  static const pinyin = '/pinyin';
  static const dictionary = '/tu-dien';
  static const profile = '/ho-so';
  static const licenses = '/giay-phep';
  static const login = '/dang-nhap';
  static const register = '/dang-ky';
  static const unauthorized = '/401';
  static const forbidden = '/403';
  static const notFound = '/404';
}
