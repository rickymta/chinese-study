// Sinh câu hỏi luyện thanh Ở CLIENT từ catalog (R5-7, R5-8, D28). Hàm thuần, nhận `random` để test tất định.
import { DRILL_TONES, type DrillMode, type DrillTone, type PinyinChart, type ToneKey } from '../types'

export interface DrillPart {
  syllable: string
  hanzi: string
  tone: DrillTone
  meaningVi: string
}

export interface DrillItem {
  /** 1 phần (`listen_tone`) hoặc 2 phần (`tone_pair`). */
  parts: DrillPart[]
}

export interface GenerateDrillOptions {
  mode: DrillMode
  chart: Pick<PinyinChart, 'syllables'>
  /** Thanh yếu từ `tone-stats.recommendedFocus` — 50% câu dồn vào đây khi khác rỗng. */
  focus?: readonly DrillTone[]
  count?: number
  random?: () => number
}

function shuffle<T>(arr: T[], random: () => number): T[] {
  const out = [...arr]
  for (let i = out.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1))
    ;[out[i], out[j]] = [out[j]!, out[i]!]
  }
  return out
}

const pickIndex = (n: number, random: () => number) => Math.min(n - 1, Math.floor(random() * n))

/** Mọi cặp (âm tiết, thanh) có chữ minh hoạ, gom theo thanh. */
export function buildTonePools(chart: Pick<PinyinChart, 'syllables'>): Record<DrillTone, DrillPart[]> {
  const pools: Record<DrillTone, DrillPart[]> = { 1: [], 2: [], 3: [], 4: [] }
  for (const s of chart.syllables) {
    for (const t of DRILL_TONES) {
      const ex = s.tones[String(t) as ToneKey]
      if (ex?.hanzi) pools[t].push({ syllable: s.syllable, hanzi: ex.hanzi, tone: t, meaningVi: ex.meaningVi ?? '' })
    }
  }
  return pools
}

/** 15 tổ hợp thanh cho `tone_pair` — loại 3-3 (TTS sẽ biến điệu thành 2-3, chấm sai oan — RK35). */
export const TONE_PAIR_COMBOS: readonly [DrillTone, DrillTone][] = DRILL_TONES.flatMap((a) =>
  DRILL_TONES.filter((b) => !(a === 3 && b === 3)).map((b) => [a, b] as [DrillTone, DrillTone]),
)

/**
 * Dãy thanh cho `listen_tone`: có focus ⇒ nửa đầu lấy từ focus (xoay vòng), nửa sau ngẫu nhiên đều; không focus ⇒
 * mỗi thanh `count/4` câu (dư chia ngẫu nhiên). Kết quả được xáo.
 */
function toneSequence(count: number, focus: readonly DrillTone[], random: () => number): DrillTone[] {
  const seq: DrillTone[] = []
  if (focus.length > 0) {
    const half = Math.floor(count / 2)
    for (let i = 0; i < half; i++) seq.push(focus[i % focus.length]!)
    for (let i = half; i < count; i++) seq.push(DRILL_TONES[pickIndex(4, random)]!)
  } else {
    const per = Math.floor(count / 4)
    for (const t of DRILL_TONES) for (let i = 0; i < per; i++) seq.push(t)
    while (seq.length < count) seq.push(DRILL_TONES[pickIndex(4, random)]!)
  }
  return shuffle(seq, random)
}

function pairSequence(count: number, focus: readonly DrillTone[], random: () => number): [DrillTone, DrillTone][] {
  const seq: [DrillTone, DrillTone][] = []
  const focused = TONE_PAIR_COMBOS.filter(([a, b]) => focus.includes(a) || focus.includes(b))
  if (focus.length > 0 && focused.length > 0) {
    const half = Math.floor(count / 2)
    const f = shuffle([...focused], random)
    for (let i = 0; i < half; i++) seq.push(f[i % f.length]!)
    const all = shuffle([...TONE_PAIR_COMBOS], random)
    for (let i = half; i < count; i++) seq.push(all[(i - half) % all.length]!)
  } else {
    // Không focus: đi hết 15 tổ hợp (xáo) rồi lặp — phân bố đều nhất có thể với 20 câu.
    const all = shuffle([...TONE_PAIR_COMBOS], random)
    for (let i = 0; i < count; i++) seq.push(all[i % all.length]!)
  }
  return shuffle(seq, random)
}

/**
 * Sinh `count` câu (mặc định 20). Không lặp cùng chữ minh hoạ trong một phiên khi còn lựa chọn khác (RK39: hết
 * lựa chọn thì cho lặp). `tone_pair`: hai âm tiết KHÁC nhau. Catalog không có chữ cho thanh nào ⇒ bỏ thanh đó
 * (có thể trả ít hơn `count` — chỉ xảy ra với học liệu thiếu).
 */
export function generateDrill({ mode, chart, focus = [], count = 20, random = Math.random }: GenerateDrillOptions): DrillItem[] {
  const pools = buildTonePools(chart)
  const usedHanzi = new Set<string>()

  const pick = (tone: DrillTone, excludeSyllable?: string): DrillPart | null => {
    const pool = pools[tone].filter((p) => p.syllable !== excludeSyllable)
    if (pool.length === 0) return null
    const fresh = pool.filter((p) => !usedHanzi.has(p.hanzi))
    const from = fresh.length > 0 ? fresh : pool
    const chosen = from[pickIndex(from.length, random)]!
    usedHanzi.add(chosen.hanzi)
    return chosen
  }

  const items: DrillItem[] = []
  if (mode === 'listen_tone') {
    for (const tone of toneSequence(count, focus, random)) {
      const part = pick(tone)
      if (part) items.push({ parts: [part] })
    }
  } else {
    for (const [a, b] of pairSequence(count, focus, random)) {
      const first = pick(a)
      if (!first) continue
      const second = pick(b, first.syllable)
      if (!second) continue
      items.push({ parts: [first, second] })
    }
  }
  return items
}
