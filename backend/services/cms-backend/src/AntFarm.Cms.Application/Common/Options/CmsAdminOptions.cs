namespace AntFarm.Cms.Application.Common.Options;

/// <summary>
/// R-W10 (mượn R-P6 gốc): email được gán vai trò admin (a) NGAY lúc provision lần đầu (xử lý ở
/// <c>UserProvisioningService</c>), (b) lúc KHỞI ĐỘNG — <c>AccessSeeder</c> kiểm tra + gán bù cho
/// MỌI user đã tồn tại có email trong danh sách mà đang thiếu admin. "Gán một lần" nghĩa là
/// idempotent theo TỪNG user (đã có admin thì bỏ qua), KHÔNG PHẢI seeder chỉ chạy một lần duy
/// nhất — thao tác kiểm tra + gán bù lặp lại ở MỖI LẦN khởi động service, và KHÔNG tự gỡ admin khi
/// email bị xoá khỏi cấu hình sau đó. Bind từ section "CmsAdmin" — POCO singleton INSTANCE giống
/// <see cref="CmsAccessOptions"/>.
/// </summary>
public sealed class CmsAdminOptions
{
    public string[] BootstrapEmails { get; init; } = [];
}
