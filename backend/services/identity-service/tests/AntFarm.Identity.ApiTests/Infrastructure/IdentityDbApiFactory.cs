using AntFarm.Testing;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>Factory cho test [DbFact] (cookie kiểu DEV) — trỏ tới af_identity_test thật (§9.2). Schema đã được <see cref="IdentityDbFixture"/> di trú TRƯỚC khi bất kỳ test nào trong collection chạy.</summary>
public class IdentityDbApiFactory : IdentityApiFactory
{
    public IdentityDbApiFactory() => Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestDatabase.BuildConnectionString("af_identity_test"));
}

/// <summary>Như <see cref="IdentityDbApiFactory"/> nhưng cấu hình cookie kiểu PRODUCTION (RK6) — dùng cho test kiểm Domain/Path/Secure khi triển khai thật.</summary>
public sealed class IdentityDbApiFactoryProdCookies : IdentityDbApiFactory
{
    public IdentityDbApiFactoryProdCookies() => ApplyCommonEnvironment(prodCookieConfig: true);
}
