/// Chính sách thử lại dùng chung cho `ProviderScope(retry:)` / `ProviderContainer(retry:)`.
///
/// Riverpod 3 mặc định tự thử lại provider lỗi (lùi luỹ thừa). App AntFarm TẮT: app tự quyết định thử lại (nút
/// "Thử lại", kéo-làm-mới, outbox ôn thẻ có lịch riêng RM-L1) — tránh gọi API dồn dập khi backend tắt.
/// Dùng cùng một hàm ở `main.dart` và test để hành vi test khớp app thật.
Duration? afNoRetry(int retryCount, Object error) => null;
