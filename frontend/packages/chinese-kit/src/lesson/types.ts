// Kiểu dữ liệu F9 theo hợp đồng chi tiết F8–F11 §6.1 (chinese-backend `/api/lessons/*`).
// Serializer backend bật `WhenWritingNull` ⇒ trường có thể `null` bị LƯỢC khỏi JSON: khai `?:`/`| null`.
// Pinyin luôn dạng SỐ THANH (`ni3 hao3`) — hiển thị dạng dấu qua `lib/pinyin.ts`.

import type { MeaningViStatus } from '../dictionary/types'

/** `machine`: bài do agent soạn/nhập, chưa người duyệt (R-LS2) ⇒ hiện chip "Nội dung chưa được duyệt". */
export type LessonReviewStatus = 'machine' | 'reviewed'

export type LessonProgressStatus = 'in_progress' | 'completed'

export interface LessonProgress {
  status: LessonProgressStatus
  /** Điểm cao nhất (%) trong các lần nộp; vắng khi chưa nộp lần nào. */
  bestScorePercent?: number | null
  attemptsCount: number
  startedAt: string
  /** Lần nộp quiz gần nhất — vắng khi chưa nộp. */
  lastAttemptAt?: string | null
  completedAt?: string | null
}

export interface LessonSummary {
  id: string
  slug: string
  title: string
  topic: string
  orderIndex: number
  summary?: string | null
  estimatedMinutes: number
  wordCount: number
  questionCount: number
  reviewStatus: LessonReviewStatus
  /** Vắng khi người học chưa bắt đầu bài. */
  progress?: LessonProgress | null
}

export interface LessonListResponse {
  items: LessonSummary[]
  /** Bài `published` có `orderIndex` nhỏ nhất chưa `completed` (R-LS4); `null` khi đã xong hết. */
  nextLessonSlug?: string | null
}

// ─── Khối nội dung (§5.4.3) ───

/** Một dòng hội thoại / ví dụ ngữ pháp: chữ Hán + pinyin số thanh + nghĩa Việt. */
export interface ZhLine {
  speaker?: string | null
  hanzi: string
  pinyin: string
  vi: string
  /** Chỉ ở ví dụ ngữ pháp. */
  note?: string | null
}

export interface TextBlockPayload {
  /** Mỗi đoạn có thể chứa token nội dòng `[[chữ Hán|pinyin]]` (parse bằng `lib/inlineZh.ts`). */
  paragraphs: string[]
}

export interface DialogueBlockPayload {
  title?: string | null
  lines: ZhLine[]
}

export interface GrammarBlockPayload {
  title: string
  pattern?: string | null
  explanation: string
  examples: ZhLine[]
}

export type TipVariant = 'pronunciation' | 'culture' | 'memory' | 'grammar'

export interface TipBlockPayload {
  variant?: TipVariant | null
  text: string
}

export type LessonBlock =
  | { id: string; type: 'text'; payload: TextBlockPayload }
  | { id: string; type: 'dialogue'; payload: DialogueBlockPayload }
  | { id: string; type: 'grammar'; payload: GrammarBlockPayload }
  | { id: string; type: 'tip'; payload: TipBlockPayload }

export type LessonBlockType = LessonBlock['type']

/** Từ bổ sung của bài (tên riêng, địa danh...) — KHÔNG vào ôn tập. */
export interface GlossaryEntry {
  hanzi: string
  pinyin: string
  vi: string
}

/** Từ của bài (thuộc `content.words`) — hoàn thành bài ⇒ thành thẻ SRS (R-LS5). */
export interface LessonWord {
  id: string
  simplified: string
  traditional?: string | null
  pinyin: string
  hanViet?: string | null
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
  /** `true` ⇒ người học đã có thẻ SRS cho từ này (bất kể nguồn). */
  inSrs: boolean
}

// ─── Quiz ───

export type QuizQuestionType = 'listen_choice' | 'single_choice'
export type QuizPromptLang = 'vi' | 'zh'
export type QuizOptionLang = 'vi' | 'zh' | 'pinyin'

export interface QuizOption {
  id: string
  text: string
  lang: QuizOptionLang
}

/** Câu hỏi cho học viên — KHÔNG có `correctOptionId`/`explanation` (R-LS10). */
export interface QuizQuestion {
  id: string
  type: QuizQuestionType
  prompt: string
  promptLang: QuizPromptLang
  /** Chỉ khi `promptLang = 'zh'`. */
  promptPinyin?: string | null
  /** Chỉ ở `listen_choice`: chữ Hán để đọc TTS. */
  audioText?: string | null
  /** Không có trong hợp đồng §6.1 — khai phòng backend bổ sung; có thì hiện kèm khi "Hiện chữ". */
  audioPinyin?: string | null
  options: QuizOption[]
}

export interface LessonDetail {
  id: string
  slug: string
  title: string
  topic: string
  orderIndex: number
  summary?: string | null
  objectives?: string[] | null
  estimatedMinutes: number
  reviewStatus: LessonReviewStatus
  glossary?: GlossaryEntry[] | null
  blocks: LessonBlock[]
  words: LessonWord[]
  quiz: QuizQuestion[]
  progress?: LessonProgress | null
}

export interface QuizAnswer {
  questionId: string
  optionId: string
}

export interface SubmitQuizRequest {
  /** UUID sinh bằng `crypto.randomUUID()` LÚC BẮT ĐẦU lượt; gửi lại cùng id ⇒ server trả kết quả cũ (R-LS9). */
  clientAttemptId: string
  /** ISO-8601 UTC lúc bắt đầu lượt. */
  startedAt: string
  answers: QuizAnswer[]
}

export interface QuizQuestionResult {
  questionId: string
  optionId: string
  correct: boolean
  correctOptionId: string
  /** Có thể chứa token `[[chữ Hán|pinyin]]`. */
  explanation?: string | null
}

export interface QuizResult {
  attemptId: string
  submittedAt: string
  total: number
  correct: number
  /** `floor(correct * 100 / total)` (R-LS3). */
  scorePercent: number
  passed: boolean
  passThresholdPercent: number
  /** `true` chỉ ở lần nộp đầu tiên đạt ngưỡng (kể cả khi phát lại cùng `clientAttemptId`). */
  firstCompletion: boolean
  /** Số thẻ SRS mới thêm — phát lại ⇒ 0; chỉ hiện "Đã thêm N từ" khi > 0. */
  srsCardsAdded: number
  results: QuizQuestionResult[]
  progress: LessonProgress
}

export interface QuizAttemptSummary {
  attemptId: string
  submittedAt: string
  total: number
  correct: number
  scorePercent: number
  passed: boolean
  durationMs?: number | null
}

export interface QuizAttemptsResponse {
  items: QuizAttemptSummary[]
}

/** Ngưỡng hoàn thành bài (R-LS3) — server là nguồn sự thật (`passThresholdPercent`), đây chỉ để hiện trước khi nộp. */
export const PASS_THRESHOLD_PERCENT = 80
