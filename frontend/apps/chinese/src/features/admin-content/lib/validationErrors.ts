// Lỗi 400 `VALIDATION` của API admin trả `details` theo ĐƯỜNG DẪN (`blocks[2].payload.lines[0].pinyin`: ["…"]) — hàm
// thuần chuẩn hoá khoá rồi tách phần gắn được vào ô nhập / phần liệt kê trong Alert (hợp đồng §5.3.3).

export type PathErrors = Record<string, string[]>

/** `Blocks[2].Payload.Lines[0].Pinyin` ⇒ `blocks[2].payload.lines[0].pinyin` (ModelState của ASP.NET viết hoa đầu đoạn). */
export function normalizeErrorPath(key: string): string {
  return key
    .replace(/^\$\./, '')
    .split('.')
    .map((seg) => (seg.length ? seg[0]!.toLowerCase() + seg.slice(1) : seg))
    .join('.')
}

/**
 * `details` thô ⇒ `{ path: [thông điệp] }`. Chấp nhận cả `ValidationProblemDetails.errors` của ASP.NET và giá trị chuỗi
 * đơn. Khoá không phải lỗi theo trường (vd `problems`, `wordIds`) bị bỏ qua nếu không phải mảng chuỗi.
 */
export function flattenValidationDetails(details: Record<string, unknown> | undefined | null): PathErrors {
  if (!details) return {}
  const source =
    details.errors && typeof details.errors === 'object' && !Array.isArray(details.errors)
      ? (details.errors as Record<string, unknown>)
      : details
  const out: PathErrors = {}
  for (const [key, value] of Object.entries(source)) {
    const list = Array.isArray(value) ? value : typeof value === 'string' ? [value] : null
    if (!list) continue
    const messages = list.filter((m): m is string => typeof m === 'string' && m.length > 0)
    if (messages.length === 0) continue
    const path = normalizeErrorPath(key)
    out[path] = [...(out[path] ?? []), ...messages]
  }
  return out
}

/**
 * Tách lỗi theo tiền tố: `stripPrefix(errors, 'blocks[2].payload.')` ⇒ `{ lines[0].pinyin: [...] }` (chỉ khoá bắt đầu
 * bằng tiền tố; phần còn lại bỏ). Component con nhận lỗi tương đối, không cần biết mình là khối thứ mấy.
 */
export function stripPrefix(errors: PathErrors, prefix: string): PathErrors {
  const out: PathErrors = {}
  for (const [path, messages] of Object.entries(errors)) {
    if (path.startsWith(prefix)) out[path.slice(prefix.length)] = messages
  }
  return out
}

/** Chia lỗi thành phần GẮN ĐƯỢC vào ô (theo `isKnown(path)`) và phần còn lại để liệt kê trong Alert. */
export function partitionErrors(errors: PathErrors, isKnown: (path: string) => boolean): { mapped: PathErrors; unmapped: PathErrors } {
  const mapped: PathErrors = {}
  const unmapped: PathErrors = {}
  for (const [path, messages] of Object.entries(errors)) {
    if (isKnown(path)) mapped[path] = messages
    else unmapped[path] = messages
  }
  return { mapped, unmapped }
}

/** Gộp lỗi thành danh sách dòng `đường dẫn: thông điệp` để hiện trong Alert. */
export function listErrorLines(errors: PathErrors): string[] {
  return Object.entries(errors).flatMap(([path, messages]) => messages.map((m) => (path ? `${path}: ${m}` : m)))
}

/** Thông điệp đầu tiên của một đường dẫn (helperText của ô). */
export function firstError(errors: PathErrors | undefined, path: string): string | undefined {
  return errors?.[path]?.[0]
}
