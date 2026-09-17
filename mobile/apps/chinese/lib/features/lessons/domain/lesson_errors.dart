// Thông điệp lỗi cho các màn bài học — cùng tinh thần `QueryErrorAlert.tsx` web.
import 'package:af_core/af_core.dart';

/// Tiêu đề khối lỗi 503 (học liệu chưa nạp).
const kLessonsUnavailableTitle = 'Học liệu chưa sẵn sàng';

/// Thông điệp 503 của bài học.
const kLessonsUnavailableMessage =
    'Bài học chưa được nạp dữ liệu. Vui lòng thử lại sau ít phút hoặc báo quản trị viên.';

/// Thông điệp hiển thị cho lỗi tải bài học: 503 ⇒ câu cố định; còn lại ⇒ thông điệp tiếng Việt của `ApiError`.
String lessonErrorMessage(ApiError error) => error.status == 503 ? kLessonsUnavailableMessage : error.message;

/// 503 là câu trả lời "thật" của server ⇒ không hiện nút Thử lại (giống web); lỗi khác thì có.
bool lessonErrorRetryable(ApiError error) => error.status != 503;

/// Mã lỗi 422 khi nộp quiz mà admin vừa sửa bài / bài không còn câu hỏi ⇒ tải lại bài, về màn mở đầu (R-LS8).
const kQuizChangedCode = 'QUIZ_CHANGED';
const kQuizEmptyCode = 'QUIZ_EMPTY';

/// Lỗi nộp quiz buộc tải lại bài (không thể "Thử lại" cùng đáp án).
bool isQuizReloadError(ApiError error) => error.code == kQuizChangedCode || error.code == kQuizEmptyCode;
