namespace AntFarm.Identity.Domain.Accounts;

/// <summary>
/// Kênh phát hành refresh token (M1, RM-A3) — <c>Web</c> dùng cookie <c>af_rt</c>
/// (<see cref="AntFarm.Identity.Domain.Accounts"/> giữ nguyên hành vi trước M1), <c>Mobile</c>
/// dùng endpoint riêng <c>/api/auth/mobile/*</c> với token trả trong body JSON. Token xoay vòng
/// LUÔN kế thừa kênh của token cha — dùng SAI kênh (vd token web gửi tới endpoint mobile) bị từ
/// chối 401 REFRESH_INVALID mà KHÔNG thu hồi gì (§5.2.2 luồng refresh).
/// </summary>
public enum RefreshClientType
{
    Web,
    Mobile
}
