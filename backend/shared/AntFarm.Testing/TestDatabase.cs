namespace AntFarm.Testing;

/// <summary>Tiện ích dựng chuỗi kết nối test từ biến môi trường <c>AF_TEST_PG</c> (§5.2.0.8).</summary>
public static class TestDatabase
{
    /// <summary>
    /// <c>AF_TEST_PG</c> là chuỗi kết nối KHÔNG có "Database=" (vd
    /// "Host=localhost;Port=5432;Username=postgres;Password=..."); hàm này gắn thêm
    /// tên database test của từng service (vd "af_chinese_test").
    /// </summary>
    public static string BuildConnectionString(string dbName)
    {
        var basePg = Environment.GetEnvironmentVariable("AF_TEST_PG");
        if (string.IsNullOrWhiteSpace(basePg))
            throw new InvalidOperationException("Biến môi trường AF_TEST_PG chưa được đặt.");

        return $"{basePg.TrimEnd(';')};Database={dbName}";
    }
}
