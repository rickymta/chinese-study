using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>
/// Điểm vào nạp học liệu từ vựng lúc khởi động (§5.2.1, §5.2.4) — gọi từ <c>Program.cs</c> sau khi
/// schema đã migrate. Đọc <c>Content:RootPath</c>, <c>Content:ImportOnStartup</c> (mặc định
/// <c>true</c>); nạp <c>characters</c> TRƯỚC <c>hsk-words</c> (từ cần chữ đã tồn tại để đồng bộ
/// <c>word_characters</c>) — <c>characters</c> lỗi vẫn thử <c>hsk-words</c> (từ thiếu chữ sẽ vào
/// <c>invalid</c>, không sập). Bắt MỌI exception, log Error, KHÔNG NÉM (CLAUDE.md mục "Seed").
/// </summary>
public static class ContentImportRunner
{
    public static async Task RunAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ContentImportRunner");

        try
        {
            var options = serviceProvider.GetRequiredService<IOptions<ContentOptions>>().Value;
            if (!options.ImportOnStartup)
            {
                logger.LogInformation("Content:ImportOnStartup=false — bỏ qua nạp học liệu từ vựng.");
                return;
            }

            var rootPath = options.ResolveRootPath();
            var importer = serviceProvider.GetRequiredService<ContentImporter>();

            var charactersPath = Path.Combine(rootPath, "data", "characters", "characters.json");
            await importer.ImportCharactersAsync(charactersPath, "characters", ct);

            var wordsPath = Path.Combine(rootPath, "data", "vocabulary", "hsk-words.json");
            await importer.ImportWordsAsync(wordsPath, "hsk-words", ct);
        }
        catch (Exception ex)
        {
            // ContentImporter tự bắt lỗi của TỪNG dataset — đây là lưới an toàn cuối cùng cho lỗi
            // hạ tầng (vd DbContext không resolve được) để KHÔNG BAO GIỜ làm service khởi động thất bại.
            logger.LogError(ex, "Nạp học liệu từ vựng thất bại ngoài dự kiến — service vẫn khởi động, tra từ có thể thiếu dữ liệu.");
        }
    }
}
