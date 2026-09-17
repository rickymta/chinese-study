using System.Reflection;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Dictionary;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Srs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Chinese.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các dịch vụ tầng Application (use case, validator...). F3: provisioning
    /// (R-P4/R-P5/R-P6), PermissionResolver — hiện thực cổng <see cref="IPermissionResolver"/> của
    /// AntFarm.Auth trên DB access.* của chính service (R-P1), MeService (§6.3). F5: sổ hoạt động
    /// học dùng chung (<see cref="IStudyActivityRecorder"/>) + bài luyện thanh (§5.2.1). F4:
    /// UserAdminService (§5.2.3) — quản trị người dùng/vai trò cục bộ. F7: "hôm nay" của người học
    /// (<see cref="IUserDayContext"/>), cài đặt học tập, SRS (tóm tắt/hàng đợi/chấm thẻ/thêm thẻ —
    /// §5.2.8).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<UserProvisioningService>();
        services.AddScoped<MeService>();
        // Đăng ký CỤ THỂ (không chỉ qua interface) — UserAdminService cần gọi PermissionResolver.Invalidate
        // (R4-9), phương thức KHÔNG có trong IPermissionResolver của AntFarm.Auth (chỉ có GetPermissionsAsync).
        services.AddScoped<PermissionResolver>();
        services.AddScoped<IPermissionResolver>(sp => sp.GetRequiredService<PermissionResolver>());
        services.AddScoped<UserAdminService>();

        services.AddScoped<IStudyActivityRecorder, StudyActivityRecorder>();
        services.AddScoped<ToneDrillService>();
        services.AddScoped<ToneStatsService>();

        // F6: tra từ (§5.2.3) — DictionaryQueryParser dùng IPinyinCatalog (đăng ký Singleton ở
        // Infrastructure). AddMemoryCache: DictionaryService cache danh sách 500 từ HSK1 trong
        // tiến trình, khoá theo id lượt nạp gần nhất (review F6.2) — an toàn gọi nhiều lần
        // (idempotent, chỉ đăng ký nếu chưa có IMemoryCache nào khác).
        services.AddMemoryCache();
        services.AddScoped<DictionaryQueryParser>();
        services.AddScoped<DictionaryService>();

        // F7: SRS FSRS-6 (§5.2.8) — IUserDayContext cắt "hôm nay" theo múi giờ người học dùng
        // chung cho summary/queue/review; ISrsCardService đăng ký CỤ THỂ (không chỉ qua interface)
        // để SrsReviewService/SrsQueueService gọi thẳng phương thức tĩnh ToDto (internal, cùng assembly).
        services.AddScoped<IUserDayContext, UserDayContext>();
        services.AddScoped<LearnerSettingsService>();
        services.AddScoped<SrsSummaryService>();
        services.AddScoped<SrsCardService>();
        services.AddScoped<ISrsCardService>(sp => sp.GetRequiredService<SrsCardService>());
        services.AddScoped<SrsQueueService>();
        services.AddScoped<SrsReviewService>();

        // F9: bài học + quiz (§5.2.1.2) — QuizSubmissionService cần TimeProvider trực tiếp (chấm +
        // ghi study_events/srs_cards trong một transaction, K12) nên KHÔNG qua IUserDayContext (dùng
        // "bây giờ" của server, không phải ngày lịch người học — R-LS7 chỉ cần occurredAtUtc).
        services.AddScoped<LessonQueryService>();
        services.AddScoped<LessonProgressService>();
        services.AddScoped<QuizSubmissionService>();

        return services;
    }
}
