using Microsoft.Extensions.Caching.Memory;

namespace AntFarm.Chinese.Application.Dictionary;

/// <summary>
/// F10 (R-CA9 "cache từ điển được làm mới sau khi sửa") — <see cref="DictionaryService"/> khoá cache
/// danh sách 500 từ theo id lượt nạp <c>hsk-words</c> gần nhất THÀNH CÔNG (F6); admin sửa một từ qua
/// <c>WordReviewService</c> KHÔNG tạo lượt nạp mới nên khoá đó không tự đổi. Singleton này: (1) đếm
/// số lần admin lưu, ghép thêm vào khoá cache để buộc đọc lại — tuyến phòng thủ CHÍNH (đúng ngay cả
/// khi <see cref="TrackKey"/> chưa từng được gọi); (2) nhớ khoá cache GẦN NHẤT <c>DictionaryService</c>
/// dùng để <see cref="Invalidate"/> xoá THẲNG mục đó khỏi <see cref="IMemoryCache"/> luôn (dọn gọn,
/// không chờ hết hạn dự phòng 1 giờ). KHÔNG cần bền qua khởi động lại (cache trong tiến trình cũng
/// mất theo).
/// </summary>
public sealed class DictionaryCacheVersion(IMemoryCache cache)
{
    private long _value;
    private string? _lastKey;

    public long Current => Interlocked.Read(ref _value);

    /// <summary>Gọi bởi <see cref="DictionaryService"/> mỗi lần tính khoá cache — để <see cref="Invalidate"/> biết chính xác mục nào cần xoá.</summary>
    public void TrackKey(string key) => Volatile.Write(ref _lastKey, key);

    public void Invalidate()
    {
        Interlocked.Increment(ref _value);

        var key = Volatile.Read(ref _lastKey);
        if (key is not null)
            cache.Remove(key);
    }
}
