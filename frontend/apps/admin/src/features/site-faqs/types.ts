/** Hợp đồng §6.2 W3b — FAQ trên website. */
export interface FaqDto {
  id: string
  question: string
  answerMarkdown: string
  groupKey: string
  sortOrder: number
  isPublished: boolean
  /** `xmin` dạng chuỗi số — PUT gửi lại nguyên để kiểm concurrency. */
  version: string
  updatedAt: string
}

export interface FaqInput {
  question: string
  answerMarkdown: string
  groupKey: string
  isPublished: boolean
}

export const DEFAULT_GROUP_KEY = 'general'
