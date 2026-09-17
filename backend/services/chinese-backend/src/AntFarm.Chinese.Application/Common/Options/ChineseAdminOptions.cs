namespace AntFarm.Chinese.Application.Common.Options;

/// <summary>
/// R-P6: email được gán vai trò admin (a) NGAY lúc provision lần đầu (xử lý ở
/// <c>UserProvisioningService</c>), (b) lúc KHỞI ĐỘNG — <c>AccessSeeder</c> kiểm tra + gán bù cho
/// MỌI user đã tồn tại có email trong danh sách mà đang thiếu admin. "Gán một lần" nghĩa là
/// idempotent theo TỪNG user (đã có admin thì bỏ qua), KHÔNG PHẢI seeder chỉ chạy một lần duy
/// nhất — thao tác kiểm tra + gán bù này lặp lại ở MỖI LẦN khởi động service, và KHÔNG tự gỡ admin
/// khi email bị xoá khỏi cấu hình sau đó. Bind từ section "ChineseAdmin" — POCO singleton INSTANCE
/// giống <see cref="ChineseAccessOptions"/>.
/// </summary>
public sealed class ChineseAdminOptions
{
    public string[] BootstrapEmails { get; init; } = [];
}
