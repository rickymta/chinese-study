// Bản nháp bài học phía trình soạn (F10): chuyển DTO ⇄ state form, chuẩn hoá trước khi gửi, so sánh "có thay đổi?",
// kiểm điều kiện xuất bản (R-CA4) trước khi gọi server, đường dẫn lỗi 400 gắn được vào ô. Hàm thuần — test ở
// `lessonDraft.test.ts`. Server vẫn là nguồn sự thật (validator dùng chung F9/F10); đây chỉ để báo sớm.

import type { GlossaryEntry, LessonBlock, QuizOptionLang, QuizPromptLang, QuizQuestionType, TipVariant } from '@/features/lessons/types'
import type {
  AdminLesson,
  AdminLessonBlock,
  AdminLessonWord,
  AdminQuizQuestion,
  BlockRequestItem,
  QuizQuestionRequestItem,
  UpdateLessonMetaRequest,
} from '../types'
import { pinyinProblem } from './zhText'

export const BLOCKS_MAX = 30
export const WORDS_MAX = 30
export const WORDS_WARN_OVER = 15
export const QUIZ_MAX = 30
export const QUIZ_MIN_PUBLISH = 3
export const QUIZ_WARN_UNDER = 5
export const OPTIONS_MIN = 2
export const OPTIONS_MAX = 4
export const OPTION_IDS = ['a', 'b', 'c', 'd'] as const
export const GLOSSARY_MAX = 10
export const OBJECTIVES_MAX = 6

// ─── Kiểu bản nháp ───

export interface ZhLineDraft {
  speaker: string
  hanzi: string
  pinyin: string
  vi: string
  note: string
}

export type BlockDraft =
  | { localId: string; type: 'text'; paragraphs: string[] }
  | { localId: string; type: 'dialogue'; title: string; lines: ZhLineDraft[] }
  | { localId: string; type: 'grammar'; title: string; pattern: string; explanation: string; examples: ZhLineDraft[] }
  | { localId: string; type: 'tip'; variant: TipVariant | ''; text: string }

export type BlockDraftType = BlockDraft['type']

export interface QuizOptionDraft {
  text: string
  lang: QuizOptionLang
}

export interface QuizQuestionDraft {
  localId: string
  /** Có ⇒ câu cũ trên server (giữ `key`); không ⇒ câu mới. */
  id?: string
  key?: string
  type: QuizQuestionType
  prompt: string
  promptLang: QuizPromptLang
  promptPinyin: string
  audioText: string
  options: QuizOptionDraft[]
  correctIndex: number
  explanation: string
}

export interface MetaDraft {
  title: string
  slug: string
  topic: string
  orderIndex: number
  summary: string
  /** Mỗi dòng một mục tiêu. */
  objectivesText: string
  estimatedMinutes: number
  glossary: GlossaryEntry[]
}

const newId = () => crypto.randomUUID()

// ─── Khối ───

export function emptyLine(): ZhLineDraft {
  return { speaker: '', hanzi: '', pinyin: '', vi: '', note: '' }
}

function lineFromDto(l: { speaker?: string | null; hanzi: string; pinyin: string; vi: string; note?: string | null }): ZhLineDraft {
  return { speaker: l.speaker ?? '', hanzi: l.hanzi, pinyin: l.pinyin, vi: l.vi, note: l.note ?? '' }
}

export function newBlockDraft(type: BlockDraftType): BlockDraft {
  switch (type) {
    case 'text':
      return { localId: newId(), type, paragraphs: [''] }
    case 'dialogue':
      return { localId: newId(), type, title: '', lines: [emptyLine(), emptyLine()] }
    case 'grammar':
      return { localId: newId(), type, title: '', pattern: '', explanation: '', examples: [emptyLine()] }
    case 'tip':
      return { localId: newId(), type, variant: '', text: '' }
  }
}

export function blocksFromLesson(blocks: readonly AdminLessonBlock[]): BlockDraft[] {
  return blocks.map((b) => {
    const localId = b.id || newId()
    switch (b.type) {
      case 'text':
        return { localId, type: 'text', paragraphs: b.payload.paragraphs?.length ? [...b.payload.paragraphs] : [''] }
      case 'dialogue':
        return { localId, type: 'dialogue', title: b.payload.title ?? '', lines: (b.payload.lines ?? []).map(lineFromDto) }
      case 'grammar':
        return {
          localId,
          type: 'grammar',
          title: b.payload.title ?? '',
          pattern: b.payload.pattern ?? '',
          explanation: b.payload.explanation ?? '',
          examples: (b.payload.examples ?? []).map(lineFromDto),
        }
      case 'tip':
        return { localId, type: 'tip', variant: b.payload.variant ?? '', text: b.payload.text ?? '' }
    }
  })
}

const trimLine = (l: ZhLineDraft) => ({
  ...(l.speaker.trim() ? { speaker: l.speaker.trim() } : {}),
  hanzi: l.hanzi.trim(),
  pinyin: l.pinyin.trim(),
  vi: l.vi.trim(),
  ...(l.note.trim() ? { note: l.note.trim() } : {}),
})

/**
 * Chuẩn hoá bản nháp ⇒ request: trim mọi chuỗi, trường tuỳ chọn rỗng ⇒ bỏ khỏi payload. Đoạn văn rỗng GIỮ NGUYÊN
 * vị trí (không lọc) để lỗi 400 `paragraphs[i]` của server gắn đúng ô — client chặn gửi khi còn đoạn rỗng
 * (`emptyParagraphErrors`).
 */
export function blocksToRequest(drafts: readonly BlockDraft[]): BlockRequestItem[] {
  return drafts.map((d) => {
    switch (d.type) {
      case 'text':
        return { type: 'text', payload: { paragraphs: d.paragraphs.map((p) => p.trim()) } }
      case 'dialogue':
        return {
          type: 'dialogue',
          payload: { ...(d.title.trim() ? { title: d.title.trim() } : {}), lines: d.lines.map(trimLine) },
        }
      case 'grammar':
        return {
          type: 'grammar',
          payload: {
            title: d.title.trim(),
            ...(d.pattern.trim() ? { pattern: d.pattern.trim() } : {}),
            explanation: d.explanation.trim(),
            examples: d.examples.map(trimLine),
          },
        }
      case 'tip':
        return { type: 'tip', payload: { ...(d.variant ? { variant: d.variant } : {}), text: d.text.trim() } }
    }
  })
}

/** Khối cho `LessonContent` xem trước (id = localId). */
export function blocksToPreview(drafts: readonly BlockDraft[]): LessonBlock[] {
  return blocksToRequest(drafts).map((b, i) => ({ ...b, id: drafts[i]!.localId }) as LessonBlock)
}

// ─── Quiz ───

export function newQuestionDraft(): QuizQuestionDraft {
  return {
    localId: newId(),
    type: 'single_choice',
    prompt: '',
    promptLang: 'vi',
    promptPinyin: '',
    audioText: '',
    options: [
      { text: '', lang: 'vi' },
      { text: '', lang: 'vi' },
      { text: '', lang: 'vi' },
    ],
    correctIndex: 0,
    explanation: '',
  }
}

export function quizFromLesson(quiz: readonly AdminQuizQuestion[]): QuizQuestionDraft[] {
  return quiz.map((q) => {
    const idx = q.options.findIndex((o) => o.id === q.correctOptionId)
    return {
      localId: q.id || newId(),
      id: q.id,
      key: q.key,
      type: q.type,
      prompt: q.prompt,
      promptLang: q.promptLang,
      promptPinyin: q.promptPinyin ?? '',
      audioText: q.audioText ?? '',
      options: q.options.map((o) => ({ text: o.text, lang: o.lang })),
      correctIndex: idx >= 0 ? idx : 0,
      explanation: q.explanation ?? '',
    }
  })
}

export function quizToRequest(drafts: readonly QuizQuestionDraft[]): QuizQuestionRequestItem[] {
  return drafts.map((q) => ({
    ...(q.id ? { id: q.id } : {}),
    type: q.type,
    prompt: q.prompt.trim(),
    promptLang: q.promptLang,
    ...(q.promptLang === 'zh' && q.promptPinyin.trim() ? { promptPinyin: q.promptPinyin.trim() } : {}),
    ...(q.type === 'listen_choice' && q.audioText.trim() ? { audioText: q.audioText.trim() } : {}),
    options: q.options.map((o) => ({ text: o.text.trim(), lang: o.lang })),
    correctIndex: q.correctIndex,
    ...(q.explanation.trim() ? { explanation: q.explanation.trim() } : {}),
  }))
}

/** Câu hỏi cho xem trước (id lựa chọn `a..d` theo vị trí, như server sẽ gán). */
export function quizToPreview(drafts: readonly QuizQuestionDraft[]): AdminQuizQuestion[] {
  return quizToRequest(drafts).map((q, i) => ({
    id: drafts[i]!.localId,
    key: drafts[i]!.key ?? `m-${i + 1}`,
    type: q.type,
    prompt: q.prompt,
    promptLang: q.promptLang,
    promptPinyin: q.promptPinyin ?? null,
    audioText: q.audioText ?? null,
    options: q.options.map((o, j) => ({ id: OPTION_IDS[j] ?? String(j), ...o })),
    correctOptionId: OPTION_IDS[q.correctIndex] ?? String(q.correctIndex),
    explanation: q.explanation ?? null,
  }))
}

// ─── Thông tin chung ───

export function metaFromLesson(lesson: Pick<AdminLesson, 'title' | 'slug' | 'topic' | 'orderIndex' | 'summary' | 'objectives' | 'estimatedMinutes' | 'glossary'>): MetaDraft {
  return {
    title: lesson.title,
    slug: lesson.slug,
    topic: lesson.topic,
    orderIndex: lesson.orderIndex,
    summary: lesson.summary ?? '',
    objectivesText: (lesson.objectives ?? []).join('\n'),
    estimatedMinutes: lesson.estimatedMinutes,
    glossary: (lesson.glossary ?? []).map((g) => ({ hanzi: g.hanzi, pinyin: g.pinyin, vi: g.vi })),
  }
}

/** Mỗi dòng một mục tiêu — trim, bỏ dòng rỗng. */
export function parseObjectives(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((s) => s.trim())
    .filter(Boolean)
}

export function metaToRequest(meta: MetaDraft, version: number): UpdateLessonMetaRequest {
  return {
    version,
    slug: meta.slug.trim(),
    title: meta.title.trim(),
    topic: meta.topic.trim(),
    orderIndex: meta.orderIndex,
    // Backend khai `Summary: string` (không null) — rỗng gửi `''`.
    summary: meta.summary.trim(),
    objectives: parseObjectives(meta.objectivesText),
    estimatedMinutes: meta.estimatedMinutes,
    glossary: meta.glossary
      .map((g) => ({ hanzi: g.hanzi.trim(), pinyin: g.pinyin.trim(), vi: g.vi.trim() }))
      .filter((g) => g.hanzi || g.pinyin || g.vi),
  }
}

// ─── So sánh thay đổi ───

const stable = (v: unknown) => JSON.stringify(v)

export function blocksDirty(current: readonly BlockDraft[], base: readonly BlockDraft[]): boolean {
  return stable(blocksToRequest(current)) !== stable(blocksToRequest(base))
}

export function quizDirty(current: readonly QuizQuestionDraft[], base: readonly QuizQuestionDraft[]): boolean {
  return stable(quizToRequest(current)) !== stable(quizToRequest(base))
}

export function wordsDirty(current: readonly AdminLessonWord[], base: readonly AdminLessonWord[]): boolean {
  return stable(current.map((w) => w.id)) !== stable(base.map((w) => w.id))
}

export function metaDirty(current: MetaDraft, base: MetaDraft): boolean {
  return stable(metaToRequest(current, 0)) !== stable(metaToRequest(base, 0))
}

// ─── Điều kiện xuất bản (R-CA4) — kiểm trên bản nháp hiện tại để báo sớm ───

export interface PublishCheck {
  /** Lỗi chặn xuất bản. */
  problems: string[]
  /** Cảnh báo không chặn. */
  warnings: string[]
}

export interface PublishCheckInput {
  title: string
  blocks: readonly BlockDraft[]
  words: readonly AdminLessonWord[]
  quiz: readonly QuizQuestionDraft[]
}

/** Lỗi payload từng khối (rút gọn theo §5.4.3 — server kiểm đầy đủ). */
export function blockProblems(block: BlockDraft): string[] {
  const out: string[] = []
  const checkLines = (lines: readonly ZhLineDraft[], label: string) => {
    lines.forEach((l, i) => {
      if (!l.hanzi.trim()) out.push(`${label} ${i + 1}: thiếu chữ Hán.`)
      if (!l.vi.trim()) out.push(`${label} ${i + 1}: thiếu nghĩa tiếng Việt.`)
      const p = pinyinProblem(l.hanzi, l.pinyin)
      if (p) out.push(`${label} ${i + 1}: ${p}`)
    })
  }
  switch (block.type) {
    case 'text':
      if (block.paragraphs.length === 0) out.push('Khối văn bản cần ít nhất một đoạn.')
      block.paragraphs.forEach((p, i) => {
        if (!p.trim()) out.push(`Đoạn ${i + 1} trống — điền nội dung hoặc xoá đoạn.`)
      })
      break
    case 'dialogue':
      if (block.lines.length < 2) out.push('Hội thoại cần ít nhất 2 dòng.')
      checkLines(block.lines, 'Dòng')
      break
    case 'grammar':
      if (!block.title.trim()) out.push('Ngữ pháp thiếu tiêu đề.')
      if (!block.explanation.trim()) out.push('Ngữ pháp thiếu giải thích.')
      if (block.examples.length < 1) out.push('Ngữ pháp cần ít nhất 1 ví dụ.')
      checkLines(block.examples, 'Ví dụ')
      break
    case 'tip':
      if (!block.text.trim()) out.push('Mẹo thiếu nội dung.')
      break
  }
  return out
}

/** Lỗi một câu hỏi (2–4 lựa chọn không rỗng, đáp án đúng trong khoảng, câu nghe có `audioText`). */
export function questionProblems(q: QuizQuestionDraft): string[] {
  const out: string[] = []
  if (!q.prompt.trim()) out.push('thiếu đề bài.')
  if (q.options.length < OPTIONS_MIN || q.options.length > OPTIONS_MAX) out.push(`cần ${OPTIONS_MIN}–${OPTIONS_MAX} lựa chọn.`)
  if (q.options.some((o) => !o.text.trim())) out.push('có lựa chọn để trống.')
  if (q.correctIndex < 0 || q.correctIndex >= q.options.length) out.push('chưa chọn đáp án đúng.')
  if (q.type === 'listen_choice' && !q.audioText.trim()) out.push('câu nghe cần chữ Hán để đọc.')
  return out
}

export function checkPublishable(input: PublishCheckInput): PublishCheck {
  const problems: string[] = []
  const warnings: string[] = []
  if (!input.title.trim()) problems.push('Tiêu đề không được để trống.')
  if (input.blocks.length === 0) problems.push('Bài cần ít nhất một khối nội dung.')
  input.blocks.forEach((b, i) => blockProblems(b).forEach((p) => problems.push(`Khối ${i + 1}: ${p}`)))
  if (input.words.length === 0) problems.push('Bài cần ít nhất một từ vựng.')
  if (input.quiz.length < QUIZ_MIN_PUBLISH) problems.push(`Quiz cần ít nhất ${QUIZ_MIN_PUBLISH} câu (hiện ${input.quiz.length}).`)
  input.quiz.forEach((q, i) => questionProblems(q).forEach((p) => problems.push(`Câu ${i + 1}: ${p}`)))

  if (input.quiz.length > 0 && input.quiz.length < QUIZ_WARN_UNDER) warnings.push(`Quiz nên có ≥ ${QUIZ_WARN_UNDER} câu.`)
  if (input.quiz.length > 0) {
    const listen = input.quiz.filter((q) => q.type === 'listen_choice').length
    if (listen / input.quiz.length < 0.3) warnings.push('Nên có ≥ 30% câu nghe (listen_choice).')
  }
  if (input.words.length > WORDS_WARN_OVER) warnings.push(`Bài có ${input.words.length} từ — nên ≤ ${WORDS_WARN_OVER} từ mỗi bài.`)
  if (!input.blocks.some((b) => b.type === 'dialogue')) warnings.push('Bài chưa có khối hội thoại.')
  return { problems, warnings }
}

/** Lỗi cục bộ theo đường dẫn tuyệt đối cho đoạn văn rỗng — chặn gửi để server không phải trả 400. */
export function emptyParagraphErrors(blocks: readonly BlockDraft[]): Record<string, string[]> {
  const out: Record<string, string[]> = {}
  blocks.forEach((b, i) => {
    if (b.type !== 'text') return
    b.paragraphs.forEach((p, j) => {
      if (!p.trim()) out[`blocks[${i}].payload.paragraphs[${j}]`] = ['Đoạn trống — điền nội dung hoặc xoá đoạn.']
    })
  })
  return out
}

// ─── Đường dẫn lỗi 400 gắn được vào ô ───

const INDEX_RE = /^(\w+)\[(\d+)\](?:\.(.+))?$/

/** `lines[0].pinyin` trên khối hội thoại có 2 dòng ⇒ true; `lines[5].pinyin` ⇒ false; trường lạ ⇒ false. */
export function isKnownBlockPath(block: BlockDraft, relPath: string): boolean {
  const m = INDEX_RE.exec(relPath)
  if (block.type === 'text') {
    if (relPath === 'paragraphs') return true
    return !!m && m[1] === 'paragraphs' && !m[3] && Number(m[2]) < block.paragraphs.length
  }
  const lineFields = new Set(['speaker', 'hanzi', 'pinyin', 'vi', 'note'])
  if (block.type === 'dialogue') {
    if (relPath === 'title' || relPath === 'lines') return true
    return !!m && m[1] === 'lines' && Number(m[2]) < block.lines.length && !!m[3] && lineFields.has(m[3])
  }
  if (block.type === 'grammar') {
    if (['title', 'pattern', 'explanation', 'examples'].includes(relPath)) return true
    return !!m && m[1] === 'examples' && Number(m[2]) < block.examples.length && !!m[3] && lineFields.has(m[3])
  }
  return relPath === 'text' || relPath === 'variant'
}

/** `blocks[2].payload.lines[0].pinyin` ⇒ `{ index: 2, rel: 'lines[0].pinyin' }`; không đúng dạng ⇒ null. */
export function splitBlockPath(path: string): { index: number; rel: string } | null {
  const m = /^blocks\[(\d+)\]\.payload\.(.+)$/.exec(path)
  return m ? { index: Number(m[1]), rel: m[2]! } : null
}

export function isKnownQuestionPath(q: QuizQuestionDraft, relPath: string): boolean {
  if (['type', 'prompt', 'promptLang', 'promptPinyin', 'audioText', 'options', 'correctIndex', 'explanation'].includes(relPath)) return true
  const m = INDEX_RE.exec(relPath)
  return !!m && m[1] === 'options' && Number(m[2]) < q.options.length && (m[3] === 'text' || m[3] === 'lang')
}

/** `questions[1].options[2].text` ⇒ `{ index: 1, rel: 'options[2].text' }`. */
export function splitQuestionPath(path: string): { index: number; rel: string } | null {
  const m = /^questions\[(\d+)\]\.(.+)$/.exec(path)
  return m ? { index: Number(m[1]), rel: m[2]! } : null
}

// ─── Thao tác danh sách dùng chung (thuần, không đổi mảng gốc) ───

export function moveItem<T>(list: readonly T[], from: number, to: number): T[] {
  if (from === to || from < 0 || to < 0 || from >= list.length || to >= list.length) return [...list]
  const out = [...list]
  const [item] = out.splice(from, 1)
  out.splice(to, 0, item!)
  return out
}

export function removeAt<T>(list: readonly T[], index: number): T[] {
  return list.filter((_, i) => i !== index)
}

export function replaceAt<T>(list: readonly T[], index: number, item: T): T[] {
  return list.map((x, i) => (i === index ? item : x))
}
