using System.Reflection;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Dictionary;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Application.Pinyin;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Chinese.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các dịch vụ tầng Application (use case, validator...). F3: provisioning
    /// (R-P4/R-P5/R-P6), PermissionResolver — hiện thực cổng <see cref="IPermissionResolver"/> của
    /// AntFarm.Auth trên DB access.* của chính service (R-P1), MeService (§6.3). F5: sổ hoạt động
    /// học dùng chung (<see cref="IStudyActivityRecorder"/>) + bài luyện thanh (§5.2.1). F4:
    /// UserAdminService (§5.2.3) — quản trị người dùng/vai trò cục bộ.</summary>
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

        return services;
    }
}
