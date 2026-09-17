namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>
/// Cấu hình đường dẫn học liệu (§5.2.1). Dev local: đường dẫn TƯƠNG ĐỐI mặc định
/// <c>content/chinese</c> ghép với <see cref="AppContext.BaseDirectory"/> (thư mục
/// <c>bin/Debug/net10.0/</c> — <c>.csproj</c> copy học liệu vào đó, xem `AntFarm.Chinese.Api.csproj`).
/// Docker: đặt <c>Content__RootPath</c> TUYỆT ĐỐI trỏ nơi <c>Dockerfile</c> đã <c>COPY</c> vào ảnh
/// (RK24) để không phụ thuộc thư mục publish.
/// </summary>
public sealed class ContentOptions
{
    public const string SectionName = "Content";

    public string RootPath { get; set; } = "content/chinese";

    /// <summary>F6: nạp từ vựng/chữ Hán lúc khởi động (§5.2.1) — mặc định <c>true</c>; tắt ở factory test không cần DB thật (<c>ChineseApiFactory</c>).</summary>
    public bool ImportOnStartup { get; set; } = true;

    /// <summary>Quy đường dẫn tương đối về tuyệt đối dựa trên thư mục chạy ứng dụng; đường dẫn tuyệt đối giữ nguyên.</summary>
    public string ResolveRootPath() =>
        Path.IsPathRooted(RootPath) ? RootPath : Path.Combine(AppContext.BaseDirectory, RootPath);
}
