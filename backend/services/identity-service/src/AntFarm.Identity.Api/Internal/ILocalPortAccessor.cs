using Microsoft.AspNetCore.Http;

namespace AntFarm.Identity.Api.Internal;

/// <summary>Tách riêng để ApiTests thay bằng bản đọc header test (TestServer luôn có <c>Connection.LocalPort == 0</c>, §5.2.9).</summary>
public interface ILocalPortAccessor
{
    int GetLocalPort(HttpContext context);
}

/// <summary>Bản thật — KHÔNG đọc header <c>Host</c> (giả mạo được), chỉ đọc cổng TCP thật của kết nối.</summary>
public sealed class ConnectionLocalPortAccessor : ILocalPortAccessor
{
    public int GetLocalPort(HttpContext context) => context.Connection.LocalPort;
}
