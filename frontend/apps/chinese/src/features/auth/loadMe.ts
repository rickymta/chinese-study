import type { MeInfo } from '@af/auth'

/**
 * TODO(F3): thay bằng `GET /chinese/api/me` (hợp đồng §6.3) qua `chineseApi` — review F3 kiểm đã gỡ stub này.
 *
 * F2 chưa có chinese-backend nhận JWT, nên tạm coi mọi tài khoản đã đăng nhập là học viên có `study.use`
 * (hợp đồng §5.3.1: "chỉ trong nhánh F2"). KHÔNG suy quyền từ claim JWT — quyền luôn do service ngôn ngữ trả.
 */
export async function loadMe(): Promise<MeInfo> {
  return { permissions: ['study.use'] }
}
