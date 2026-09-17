// Kiểu dữ liệu F10 theo hợp đồng F8–F11 §6.3 (chinese-backend `/api/admin/lessons*`, `/api/admin/words*`).
// Backend làm song song — bám shape hợp đồng; serializer bật `WhenWritingNull` ⇒ trường null có thể bị LƯỢC (`?:`).
// Pinyin luôn dạng SỐ THANH (`ni3 hao3`); hiển thị dạng dấu qua `lib/pinyin.ts`.

import type { HanVietStatus, MeaningViSource, MeaningViStatus } from '@/features/dictionary/types'
import type {
  DialogueBlockPayload,
  GlossaryEntry,
  GrammarBlockPayload,
  LessonReviewStatus,
  QuizOptionLang,
  QuizPromptLang,
  QuizQuestionType,
  TextBlockPayload,
  TipBlockPayload,
} from '@/features/lessons/types'

export type LessonStatus = 'draft' | 'published' | 'archived'
export type LessonSource = 'seed' | 'admin'

/** Khối nội dung phía admin — cùng payload với học viên (§5.4.3), có `id` do server gán. */
export type AdminLessonBlock =
  | { id: string; type: 'text'; payload: TextBlockPayload }
  | { id: string; type: 'dialogue'; payload: DialogueBlockPayload }
  | { id: string; type: 'grammar'; payload: GrammarBlockPayload }
  | { id: string; type: 'tip'; payload: TipBlockPayload }

export interface AdminLessonWord {
  id: string
  simplified: string
  pinyin: string
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
}

export interface AdminQuizOption {
  id: string
  text: string
  lang: QuizOptionLang
}

/** Câu hỏi phía admin — CÓ `correctOptionId` + `explanation` (khác `QuizQuestion` của học viên). */
export interface AdminQuizQuestion {
  id: string
  key: string
  type: QuizQuestionType
  prompt: string
  promptLang: QuizPromptLang
  promptPinyin?: string | null
  audioText?: string | null
  options: AdminQuizOption[]
  correctOptionId: string
  explanation?: string | null
}

/** Toàn bộ bài (mọi lệnh ghi đều trả lại kiểu này kèm `version` mới — R-CA3). */
export interface AdminLesson {
  id: string
  /** `xmin` của dòng — gửi kèm mọi lệnh ghi; lệch ⇒ 409 `CONCURRENCY_CONFLICT`. */
  version: number
  slug: string
  title: string
  topic: string
  level: string
  orderIndex: number
  summary?: string | null
  objectives?: string[] | null
  estimatedMinutes: number
  glossary?: GlossaryEntry[] | null
  status: LessonStatus
  reviewStatus: LessonReviewStatus
  source: LessonSource
  /** Có giá trị ⇒ bài ĐÃ TỪNG xuất bản ⇒ slug khoá (R-CA8), kể cả đang `draft`. */
  publishedAt?: string | null
  reviewedAt?: string | null
  editedAt?: string | null
  editedByName?: string | null
  createdAt: string
  updatedAt: string
  /** `true` ⇒ đã có người làm quiz ⇒ xoá sẽ thành lưu trữ (R-CA7). */
  hasAttempts: boolean
  blocks: AdminLessonBlock[]
  words: AdminLessonWord[]
  quiz: AdminQuizQuestion[]
  /** Cảnh báo không chặn (R-CA4) — backend tính ở MỌI phản hồi để màn soạn gợi ý sớm. */
  warnings?: string[] | null
}

export interface AdminLessonListItem {
  id: string
  slug: string
  title: string
  orderIndex: number
  status: LessonStatus
  reviewStatus: LessonReviewStatus
  source: LessonSource
  wordCount: number
  questionCount: number
  editedAt?: string | null
  publishedAt?: string | null
}

export interface AdminLessonsPage {
  items: AdminLessonListItem[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AdminLessonsQuery {
  /** Bỏ trống ⇒ mọi trạng thái TRỪ `archived` (server mặc định ẩn lưu trữ). */
  status?: LessonStatus
  q?: string
  page?: number
  pageSize?: number
}

// ─── Request ghi bài ───

export interface CreateLessonRequest {
  slug: string
  title: string
  topic: string
  orderIndex?: number
  summary?: string
}

export interface UpdateLessonMetaRequest {
  version: number
  slug: string
  title: string
  topic: string
  orderIndex: number
  /** Chuỗi (rỗng khi không có) — backend khai không null. */
  summary: string
  objectives: string[]
  estimatedMinutes: number
  glossary: GlossaryEntry[]
}

export type BlockRequestItem =
  | { type: 'text'; payload: TextBlockPayload }
  | { type: 'dialogue'; payload: DialogueBlockPayload }
  | { type: 'grammar'; payload: GrammarBlockPayload }
  | { type: 'tip'; payload: TipBlockPayload }

export interface ReplaceBlocksRequest {
  version: number
  /** 0–30 khối, thứ tự = mảng. */
  blocks: BlockRequestItem[]
}

export interface ReplaceWordsRequest {
  version: number
  /** 0–30 id từ, thứ tự = mảng, không trùng. */
  wordIds: string[]
}

export interface QuizQuestionRequestItem {
  /** Có ⇒ cập nhật câu cũ (giữ `key`); không ⇒ tạo mới. */
  id?: string
  type: QuizQuestionType
  prompt: string
  promptLang: QuizPromptLang
  promptPinyin?: string
  audioText?: string
  options: { text: string; lang: QuizOptionLang }[]
  /** 0-based — server gán id `a..d` theo vị trí. */
  correctIndex: number
  explanation?: string
}

export interface ReplaceQuizRequest {
  version: number
  questions: QuizQuestionRequestItem[]
}

export interface VersionRequest {
  version: number
}

/** `DELETE /api/admin/lessons/{id}` — 204 xoá cứng · 200 lưu trữ (R-CA7). */
export type DeleteLessonResult = { result: 'deleted' } | { result: 'archived'; lesson: AdminLesson }

// ─── Duyệt từ vựng ───

export interface AdminWord {
  id: string
  version: number
  simplified: string
  traditional?: string | null
  pinyin: string
  hsk3Level?: number | null
  pathOrder?: number | null
  pos?: string[] | null
  meaningsEn?: string[] | null
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
  meaningViSource?: MeaningViSource | null
  hanViet?: string | null
  hanVietStatus?: HanVietStatus | null
  editedAt?: string | null
  editedByName?: string | null
}

export interface AdminWordsPage {
  items: AdminWord[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AdminWordsQuery {
  meaningViStatus?: MeaningViStatus
  hanVietStatus?: HanVietStatus
  /** Cấp HSK 3.0 (server mặc định 1 khi vắng). */
  hsk?: number
  q?: string
  page?: number
  pageSize?: number
}

export interface UpdateWordRequest {
  version: number
  meaningsVi: string[]
  meaningViStatus: MeaningViStatus
  hanViet: string | null
  hanVietStatus: HanVietStatus
}

export interface BulkReviewRequest {
  /** 1–100 mục. */
  items: { id: string; version: number }[]
}

export interface BulkReviewResult {
  updated: number
  /** Id lệch version — không được cập nhật (xử lý từng mục, không nguyên tử). */
  conflicts: string[]
  notFound: string[]
}
