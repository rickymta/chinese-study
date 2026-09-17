import type { ProgressOverview } from '../types'

export type TodayTaskKind = 'review' | 'new-cards' | 'lesson' | 'writing' | 'tone' | 'pinyin'

export interface TodayTask {
  kind: TodayTaskKind
  title: string
  /** Dòng phụ (số lượng, tên bài…). */
  subtitle?: string
  /** Đường dẫn trong app. */
  to: string
}

/** Ngưỡng "chưa học pinyin đủ" (R-PG9 mục 6). */
export const PINYIN_MIN_ANSWERED = 40

/** Đường dẫn tab luyện thanh của trang Pinyin (`?tab=luyen` — F5). */
export const TONE_DRILL_PATH = '/pinyin?tab=luyen'

/**
 * Danh sách "Việc hôm nay" theo R-PG9 — đúng thứ tự, chỉ mục còn việc; khối dữ liệu vắng (`null`) thì bỏ qua các
 * mục dựa vào khối đó. Nhãn dùng số thẻ theo `srs` (không dùng `dailyGoal`, vì mục tiêu ngày hiển thị riêng — D8).
 */
export function buildTodayTasks(overview: ProgressOverview): TodayTask[] {
  const tasks: TodayTask[] = []
  const { srs, lessons, tone } = overview

  if (srs && srs.dueToday > 0) {
    tasks.push({ kind: 'review', title: `Ôn ${srs.dueToday} thẻ đến hạn`, to: '/on-tap' })
  }
  if (srs && srs.newAvailableToday > 0) {
    tasks.push({ kind: 'new-cards', title: `Học ${srs.newAvailableToday} thẻ mới`, to: '/on-tap' })
  }
  if (lessons?.next) {
    tasks.push({
      kind: 'lesson',
      title: lessons.completed === 0 && lessons.inProgress === 0 ? 'Bắt đầu bài học đầu tiên' : 'Bài tiếp theo',
      subtitle: lessons.next.title,
      to: `/bai-hoc/${encodeURIComponent(lessons.next.slug)}`,
    })
  }
  if (lessons?.lastCompleted && lessons.lastCompleted.unpracticedChars > 0) {
    const last = lessons.lastCompleted
    tasks.push({
      kind: 'writing',
      title: `Luyện viết chữ bài "${last.title}"`,
      subtitle: `${last.unpracticedChars} chữ chưa luyện`,
      to: `/luyen-viet?tab=bai-hoc&bai=${encodeURIComponent(last.slug)}`,
    })
  }
  if (tone && tone.recommendedFocus.length > 0) {
    tasks.push({
      kind: 'tone',
      title: `Luyện thanh ${tone.recommendedFocus.join(', ')}`,
      subtitle: 'Thanh bạn nghe chưa vững',
      to: TONE_DRILL_PATH,
    })
  }
  if (tone && tone.totalAnswered < PINYIN_MIN_ANSWERED) {
    tasks.push({
      kind: 'pinyin',
      title: 'Học pinyin trước',
      subtitle:
        tone.totalAnswered === 0
          ? 'Nghe và phân biệt 4 thanh điệu trước khi học từ'
          : `Mới trả lời ${tone.totalAnswered}/${PINYIN_MIN_ANSWERED} câu luyện thanh`,
      to: '/pinyin',
    })
  }
  return tasks
}
