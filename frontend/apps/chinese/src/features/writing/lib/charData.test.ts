import { afterEach, describe, expect, it, vi } from 'vitest'
import { codePointHex, hasStrokeData, isJsonResponse, loadCharData, loadManifest, resetManifestCache, type HanziDataManifest } from './charData'

const manifest: HanziDataManifest = {
  dataset: 'hanzi-writer-data',
  version: '2.0.1',
  license: 'Arphic Public License (ARPHICPL.TXT)',
  naming: 'codepoint-hex-lower',
  count: 2,
  characters: ['爱', '你'],
  missing: ['𠀀'],
}

function jsonResponse(body: unknown, init?: { status?: number; contentType?: string }): Response {
  const status = init?.status ?? 200
  return new Response(typeof body === 'string' ? body : JSON.stringify(body), {
    status,
    headers: { 'content-type': init?.contentType ?? 'application/json; charset=utf-8' },
  })
}

describe('codePointHex — tên file dữ liệu nét', () => {
  it('爱 (U+7231) ⇒ 7231', () => {
    expect(codePointHex('爱')).toBe('7231')
  })
  it('chữ mở rộng B (cặp surrogate) ⇒ 5 hex, không dùng charCodeAt', () => {
    expect(codePointHex('𠀀')).toBe('20000')
    expect(codePointHex('𪛔')).toBe('2a6d4')
  })
  it('chỉ lấy code point đầu; hex chữ thường', () => {
    expect(codePointHex('爱我')).toBe('7231')
    expect(codePointHex('丽')).toBe('4e3d')
  })
  it('chuỗi rỗng ⇒ ném lỗi', () => {
    expect(() => codePointHex('')).toThrow()
  })
})

describe('hasStrokeData', () => {
  it('theo danh mục giả', () => {
    expect(hasStrokeData('爱', manifest)).toBe(true)
    expect(hasStrokeData('你', manifest)).toBe(true)
    expect(hasStrokeData('𠀀', manifest)).toBe(false)
    expect(hasStrokeData('丽', manifest)).toBe(false)
  })
  it('chưa có danh mục ⇒ false', () => {
    expect(hasStrokeData('爱', undefined)).toBe(false)
  })
})

describe('isJsonResponse — không parse index.html như JSON', () => {
  it('200 + application/json ⇒ true', () => {
    expect(isJsonResponse(jsonResponse({}))).toBe(true)
  })
  it('200 + text/html (SPA fallback) ⇒ false', () => {
    expect(isJsonResponse(jsonResponse('<!doctype html>', { contentType: 'text/html' }))).toBe(false)
  })
  it('404 ⇒ false dù content-type là json', () => {
    expect(isJsonResponse(jsonResponse({}, { status: 404 }))).toBe(false)
  })
})

describe('loadCharData / loadManifest', () => {
  afterEach(() => resetManifestCache())

  it('gọi đúng đường dẫn /hanzi-data/<hex>.json và trả strokes/medians', async () => {
    const fetchImpl = vi.fn(async (url: string | URL | Request) => {
      expect(String(url)).toBe('/hanzi-data/7231.json')
      return jsonResponse({ strokes: ['M 1 1'], medians: [[[1, 1]]] })
    }) as unknown as typeof fetch
    const data = await loadCharData('爱', { fetchImpl })
    expect(data.strokes).toHaveLength(1)
  })

  it('file thiếu mà server trả index.html ⇒ reject (không nổ lúc parse)', async () => {
    const fetchImpl = vi.fn(async () => jsonResponse('<!doctype html><html></html>', { contentType: 'text/html' })) as unknown as typeof fetch
    await expect(loadCharData('丽', { fetchImpl })).rejects.toThrow(/Chưa có dữ liệu nét/)
  })

  it('JSON thiếu strokes ⇒ reject', async () => {
    const fetchImpl = vi.fn(async () => jsonResponse({ foo: 1 })) as unknown as typeof fetch
    await expect(loadCharData('爱', { fetchImpl })).rejects.toThrow(/không hợp lệ/)
  })

  it('manifest cache một lần; lỗi thì lần sau thử lại', async () => {
    const failing = vi.fn(async () => jsonResponse('', { status: 500, contentType: 'text/plain' })) as unknown as typeof fetch
    await expect(loadManifest({ fetchImpl: failing })).rejects.toThrow()
    const ok = vi.fn(async () => jsonResponse(manifest)) as unknown as typeof fetch
    const m1 = await loadManifest({ fetchImpl: ok })
    const m2 = await loadManifest({ fetchImpl: ok })
    expect(m1).toBe(m2)
    expect(ok).toHaveBeenCalledTimes(1)
    expect(m1.characters).toEqual(['爱', '你'])
  })
})
