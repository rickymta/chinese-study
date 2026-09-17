// Kiểu dữ liệu F11 theo hợp đồng chi tiết F8–F11 §6.4 (chinese-backend `GET /api/progress/overview`).
// Serializer backend bật `WhenWritingNull` ⇒ khối/trường có thể `null` bị LƯỢC khỏi JSON: khai `?:`/`| null`.
// Mọi "ngày" (`localDate`, `activity[].date`) là `yyyy-MM-dd` theo MÚI GIỜ HỒ SƠ của người học (R-PG2) —
// frontend KHÔNG tự tính "hôm nay" bằng đồng hồ trình duyệt.

export interface ProgressStreak {
  /** Chuỗi ngày học hiện tại (R-PG3): hôm nay chưa học thì đếm từ hôm qua — chưa đứt tới hết ngày. */
  current: number
  /** Chuỗi dài nhất trong lịch sử, luôn ≥ `current`. */
  longest: number
  studiedToday: boolean
}

/** Hoạt động hôm nay (theo `kind` của sổ hoạt động). */
export interface ProgressToday {
  srsReviews: number
  newCards: number
  toneDrillItems: number
  writingAttempts: number
  quizzes: number
  lessonsCompleted: number
  /** Tổng `quantity` mọi loại hôm nay. */
  activityCount: number
}

/** Trích từ `GET /api/srs/summary` (K13). */
export interface ProgressSrs {
  dueToday: number
  dueNow: number
  newAvailableToday: number
  newIntroducedToday: number
  reviewedToday: number
  nextDueAt?: string | null
}

/** Mục tiêu ngày (R-PG5) — backend tính; vắng khi `srs` vắng. */
export interface ProgressDailyGoal {
  done: number
  total: number
  achieved: boolean
}

export interface ProgressVocabulary {
  /** Số từ trong lộ trình (`path_order IS NOT NULL`). */
  totalInPath: number
  /** Thẻ đã được giới thiệu (đã ôn ít nhất một lần). */
  introduced: number
  learning: number
  /** Thẻ `review` có `stability ≥ 21`. */
  mature: number
}

export interface ProgressLessonRef {
  slug: string
  title: string
}

export interface ProgressLastCompletedLesson extends ProgressLessonRef {
  completedAt: string
  /** Số chữ của bài chưa từng luyện viết (R-PG9 mục 4). */
  unpracticedChars: number
}

export interface ProgressLessons {
  published: number
  completed: number
  inProgress: number
  /** Bài tiếp theo (R-LS4) — vắng khi đã học hết. */
  next?: ProgressLessonRef | null
  /** Bài hoàn thành gần nhất — vắng khi chưa hoàn thành bài nào. */
  lastCompleted?: ProgressLastCompletedLesson | null
}

export interface ProgressWriting {
  practicedChars: number
  masteredChars: number
  weakChars: number
  /** Kích thước bộ `hsk1`. */
  totalChars: number
}

export interface ProgressTone {
  totalAnswered: number
  /** 0..1; `null` khi chưa trả lời câu nào. */
  accuracy?: number | null
  /** Thanh nên luyện (1–4), rỗng khi chưa đủ dữ liệu hoặc đã vững. */
  recommendedFocus: number[]
}

export interface ActivityDay {
  /** `yyyy-MM-dd` theo múi giờ hồ sơ. */
  date: string
  /** `SUM(quantity)` mọi loại hoạt động trong ngày; 0 khi không học. */
  count: number
}

export interface ProgressOverview {
  /** "Hôm nay" theo múi giờ hồ sơ. */
  localDate: string
  /** Múi giờ IANA đã dùng để cắt ngày. */
  timeZone: string
  streak: ProgressStreak
  today: ProgressToday
  /** Các khối dưới đây có thể VẮNG khi nguồn dữ liệu không dùng được (R-PG7) ⇒ frontend ẩn khối. */
  srs?: ProgressSrs | null
  dailyGoal?: ProgressDailyGoal | null
  vocabulary?: ProgressVocabulary | null
  lessons?: ProgressLessons | null
  writing?: ProgressWriting | null
  tone?: ProgressTone | null
  /** 90 phần tử `[today − 89, today]` tăng dần theo ngày (R-PG6). */
  activity: ActivityDay[]
}
