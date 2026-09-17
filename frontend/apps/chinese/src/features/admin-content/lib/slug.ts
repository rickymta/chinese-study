// Sinh/kiểm slug bài học (R-CA8): `^[a-z0-9]+(-[a-z0-9]+)*$`, 3–64 ký tự. Hàm thuần — test ở `slug.test.ts`.

export const SLUG_RE = /^[a-z0-9]+(-[a-z0-9]+)*$/
export const SLUG_MIN = 3
export const SLUG_MAX = 64

/**
 * `slugify('Chào hỏi & Giới thiệu')` ⇒ `'chao-hoi-gioi-thieu'`: bỏ dấu tiếng Việt (NFD), `đ` ⇒ `d`, chữ thường, ký tự
 * ngoài `[a-z0-9]` ⇒ `-`, gộp `-` liên tiếp, cắt ≤ 64 (không để `-` cuối). Chuỗi không còn ký tự nào ⇒ `''`.
 */
export function slugify(title: string): string {
  const ascii = title
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
  const dashed = ascii.replace(/[^a-z0-9]+/g, '-').replace(/-+/g, '-').replace(/^-|-$/g, '')
  if (dashed.length <= SLUG_MAX) return dashed
  return dashed.slice(0, SLUG_MAX).replace(/-+$/, '')
}

export function isValidSlug(slug: string): boolean {
  return slug.length >= SLUG_MIN && slug.length <= SLUG_MAX && SLUG_RE.test(slug)
}

/** Thông điệp lỗi tiếng Việt cho ô slug; hợp lệ ⇒ `null`. */
export function slugProblem(slug: string): string | null {
  if (slug.length < SLUG_MIN) return `Slug cần ít nhất ${SLUG_MIN} ký tự.`
  if (slug.length > SLUG_MAX) return `Slug tối đa ${SLUG_MAX} ký tự.`
  if (!SLUG_RE.test(slug)) return 'Slug chỉ gồm chữ thường a–z, số và dấu gạch ngang giữa các từ (vd chao-hoi).'
  return null
}
