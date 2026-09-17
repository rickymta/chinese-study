// Dựng URLSearchParams mới từ bản cũ + patch — hàm thuần dùng cho MỌI trang có nhiều tham số lọc.
//
// ⚠️ `setSearchParams` của react-router KHÔNG xếp hàng: gọi hai lần liên tiếp (`setStatus(v); setPage(1)`) thì lần
// thứ hai dựng từ `searchParams` CŨ ⇒ xoá mất giá trị lần thứ nhất (review F10). Luôn gộp mọi thay đổi vào MỘT
// lời gọi `setParams(prev => patchSearchParams(prev, {...}), { replace: true })`.

export type SearchParamsPatch = Record<string, string | number | null | undefined>

/**
 * Trả bản sao `prev` đã áp `patch`: giá trị `null`/`undefined`/`''` ⇒ xoá khoá; số ⇒ chuỗi; còn lại ⇒ đặt.
 * Không đụng khoá không có trong `patch`. Không thay đổi `prev`.
 */
export function patchSearchParams(prev: URLSearchParams, patch: SearchParamsPatch): URLSearchParams {
  const out = new URLSearchParams(prev)
  for (const [key, value] of Object.entries(patch)) {
    if (value === null || value === undefined || value === '') out.delete(key)
    else out.set(key, String(value))
  }
  return out
}

/**
 * Giá trị cho một tham số kiểu enum có mặc định (cùng quy ước với `useTabParam` của `@af/ui`): bằng mặc định ⇒
 * `null` (xoá khỏi URL), khác ⇒ giữ. Dùng khi phải đổi tham số enum CÙNG LÚC với tham số khác (vd về trang 1).
 */
export function enumParam<T extends string>(value: T, defaultValue: T): T | null {
  return value === defaultValue ? null : value
}
