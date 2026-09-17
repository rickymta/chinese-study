import type { CharDataLoaderFn, CharacterJson } from 'hanzi-writer'

/**
 * Dữ liệu nét chữ (R-W1): tập con `hanzi-writer-data@2.0.1` đóng gói sẵn ở `public/hanzi-data/` (sinh bằng
 * `content/chinese/scripts/build-hanzi-data.mjs`, commit vào git). Tên file = mã code point hex THƯỜNG không đệm
 * (`爱` U+7231 ⇒ `7231.json`; chữ mở rộng B ⇒ 5 hex). CẤM để hanzi-writer dùng loader mặc định (gọi cdn.jsdelivr.net).
 */
export const HANZI_DATA_BASE = '/hanzi-data'

export interface HanziDataManifest {
  dataset: string
  version: string
  license: string
  /** `codepoint-hex-lower` */
  naming: string
  count: number
  /** Chữ có file dữ liệu nét. */
  characters: string[]
  /** Chữ trong kho từ nhưng nguồn không có dữ liệu nét (R-W9: hiện mờ + nhãn). */
  missing: string[]
}

export interface FetchOptions {
  /** Cho test: thay `fetch` toàn cục. */
  fetchImpl?: typeof fetch
  /** Cho test: đổi gốc đường dẫn. */
  base?: string
}

/** Mã code point hex thường, không đệm, của code point ĐẦU TIÊN (chữ mở rộng B là cặp surrogate ⇒ không dùng `charCodeAt`). */
export function codePointHex(ch: string): string {
  const cp = ch.codePointAt(0)
  if (cp === undefined) throw new Error('codePointHex: chuỗi rỗng')
  return cp.toString(16).toLowerCase()
}

/**
 * Phản hồi hợp lệ = 2xx VÀ content-type có "json". Dev Vite (và SPA fallback nếu nginx cấu hình sai) trả
 * `index.html` 200 `text/html` cho file thiếu — parse HTML như JSON sẽ nổ khó hiểu trong hanzi-writer.
 */
export function isJsonResponse(res: Pick<Response, 'ok' | 'headers'>): boolean {
  if (!res.ok) return false
  const type = res.headers.get('content-type') ?? ''
  return type.toLowerCase().includes('json')
}

let manifestPromise: Promise<HanziDataManifest> | null = null

/** Tải `index.json` một lần cho cả app (cache mức module); lỗi ⇒ xoá cache để lần gọi sau thử lại. */
export function loadManifest(opts?: FetchOptions): Promise<HanziDataManifest> {
  if (manifestPromise) return manifestPromise
  const doFetch = opts?.fetchImpl ?? fetch
  const base = opts?.base ?? HANZI_DATA_BASE
  manifestPromise = doFetch(`${base}/index.json`)
    .then(async (res) => {
      if (!isJsonResponse(res)) throw new Error(`Không tải được danh mục dữ liệu nét (${res.status})`)
      const raw = (await res.json()) as Partial<HanziDataManifest>
      if (!Array.isArray(raw.characters)) throw new Error('Danh mục dữ liệu nét không hợp lệ')
      return { ...raw, characters: raw.characters, missing: raw.missing ?? [] } as HanziDataManifest
    })
    .catch((err: unknown) => {
      manifestPromise = null
      throw err
    })
  return manifestPromise
}

/** Cho test: xoá cache danh mục. */
export function resetManifestCache(): void {
  manifestPromise = null
}

const charSetCache = new WeakMap<HanziDataManifest, Set<string>>()

/** Chữ có dữ liệu nét trong danh mục? (`Set` cache theo từng manifest — danh sách 300+ chữ tra nhiều lần khi vẽ lưới). */
export function hasStrokeData(ch: string, manifest: HanziDataManifest | undefined): boolean {
  if (!manifest) return false
  let set = charSetCache.get(manifest)
  if (!set) {
    set = new Set(manifest.characters)
    charSetCache.set(manifest, set)
  }
  return set.has(ch)
}

/** Tải JSON nét của một chữ từ file cục bộ; lỗi mạng / 404 / không phải JSON ⇒ reject. */
export async function loadCharData(ch: string, opts?: FetchOptions): Promise<CharacterJson> {
  const doFetch = opts?.fetchImpl ?? fetch
  const base = opts?.base ?? HANZI_DATA_BASE
  const res = await doFetch(`${base}/${codePointHex(ch)}.json`)
  if (!isJsonResponse(res)) throw new Error(`Chưa có dữ liệu nét cho chữ ${ch} (${res.status})`)
  const data = (await res.json()) as Partial<CharacterJson>
  if (!Array.isArray(data.strokes) || !Array.isArray(data.medians)) throw new Error(`Dữ liệu nét của chữ ${ch} không hợp lệ`)
  return data as CharacterJson
}

/** Loader truyền vào MỌI `HanziWriter.create` (R-W1) — không bao giờ để rơi về loader mặc định. */
export const charDataLoader: CharDataLoaderFn = (char, onLoad, onError) => {
  loadCharData(char).then(onLoad, onError)
}
