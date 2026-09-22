// Thông điệp lỗi cho các màn từ điển — port `QueryErrorAlert.tsx` web.
import 'package:af_core/af_core.dart';

/// Tiêu đề khối lỗi 503 (học liệu chưa nạp).
const kContentUnavailableTitle = 'Học liệu chưa sẵn sàng';

/// Thông điệp 503 của từ điển (cùng chuỗi với web).
const kDictionaryUnavailableMessage =
    'Từ điển chưa được nạp dữ liệu. Vui lòng thử lại sau ít phút hoặc báo quản trị viên.';

/// Thông điệp hiển thị cho lỗi tải từ điển:
/// - 503 `CONTENT_UNAVAILABLE` ⇒ câu cố định (trạng thái server, không phải lỗi thoáng qua — không nút thử lại);
/// - 400 `VALIDATION` ⇒ thông điệp đầu tiên trong `details` (`{ q: ["…"] }` — mảng chuỗi mỗi trường, phòng cả dạng
///   chuỗi đơn), không có thì thông điệp chung;
/// - còn lại ⇒ thông điệp tiếng Việt của `ApiError`.
String dictionaryErrorMessage(ApiError error) {
  if (error.status == 503) return kDictionaryUnavailableMessage;
  if (error.status == 400) return firstDetailMessage(error) ?? error.message;
  return error.message;
}

/// Chuỗi đầu tiên trong `details` (theo thứ tự khoá của server); không có ⇒ `null`.
String? firstDetailMessage(ApiError error) {
  for (final v in (error.details ?? const {}).values) {
    if (v is String && v.isNotEmpty) return v;
    if (v is List && v.isNotEmpty && v.first is String && (v.first as String).isNotEmpty) return v.first as String;
  }
  return null;
}

/// 503 và 400 là câu trả lời "thật" của server ⇒ không hiện nút Thử lại (giống web); lỗi khác thì có.
bool dictionaryErrorRetryable(ApiError error) => error.status != 503 && error.status != 400;
