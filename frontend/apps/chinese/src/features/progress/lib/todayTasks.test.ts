import { describe, expect, it } from 'vitest'
import type { ProgressOverview } from '../types'
import { buildTodayTasks } from './todayTasks'

/** Tổng quan mẫu theo §6.4 — mọi khối có việc để kiểm thứ tự. */
function overview(patch: Partial<ProgressOverview> = {}): ProgressOverview {
  return {
    localDate: '2026-09-18',
    timeZone: 'Asia/Ho_Chi_Minh',
    streak: { current: 5, longest: 12, studiedToday: false },
    today: { srsReviews: 0, newCards: 0, toneDrillItems: 0, writingAttempts: 0, quizzes: 0, lessonsCompleted: 0, activityCount: 0 },
    srs: { dueToday: 23, dueNow: 20, newAvailableToday: 10, newIntroducedToday: 0, reviewedToday: 0, nextDueAt: null },
    dailyGoal: { done: 0, total: 33, achieved: false },
    vocabulary: { totalInPath: 500, introduced: 64, learning: 52, mature: 12 },
    lessons: {
      published: 5,
      completed: 2,
      inProgress: 1,
      next: { slug: 'so-dem', title: 'Số đếm' },
      lastCompleted: { slug: 'ban-than', title: 'Giới thiệu bản thân', completedAt: '2026-09-17T10:00:00Z', unpracticedChars: 9 },
    },
    writing: { practicedChars: 24, masteredChars: 5, weakChars: 3, totalChars: 297 },
    tone: { totalAnswered: 30, accuracy: 0.82, recommendedFocus: [2, 3] },
    activity: [],
    ...patch,
  }
}

describe('buildTodayTasks — thứ tự R-PG9', () => {
  it('đủ 6 mục theo đúng thứ tự khi mọi khối còn việc', () => {
    const tasks = buildTodayTasks(overview())
    expect(tasks.map((t) => t.kind)).toEqual(['review', 'new-cards', 'lesson', 'writing', 'tone', 'pinyin'])
    expect(tasks[0]).toMatchObject({ title: 'Ôn 23 thẻ đến hạn', to: '/on-tap' })
    expect(tasks[1]).toMatchObject({ title: 'Học 10 thẻ mới', to: '/on-tap' })
    expect(tasks[2]).toMatchObject({ title: 'Bài tiếp theo', subtitle: 'Số đếm', to: '/bai-hoc/so-dem' })
    expect(tasks[3]).toMatchObject({ subtitle: '9 chữ chưa luyện', to: '/luyen-viet?tab=bai-hoc&bai=ban-than' })
    expect(tasks[3]!.title).toContain('Giới thiệu bản thân')
    expect(tasks[4]).toMatchObject({ title: 'Luyện thanh 2, 3', to: '/pinyin?tab=luyen' })
    expect(tasks[5]).toMatchObject({ title: 'Học pinyin trước', to: '/pinyin' })
  })

  it('mục bằng 0 thì ẩn', () => {
    const tasks = buildTodayTasks(
      overview({
        srs: { dueToday: 0, dueNow: 0, newAvailableToday: 0, newIntroducedToday: 3, reviewedToday: 8 },
        lessons: { published: 5, completed: 5, inProgress: 0, lastCompleted: { slug: 'x', title: 'X', completedAt: '', unpracticedChars: 0 } },
        tone: { totalAnswered: 240, accuracy: 0.9, recommendedFocus: [] },
      }),
    )
    expect(tasks).toEqual([])
  })

  it('khối null/vắng bị bỏ qua, các mục khác vẫn có', () => {
    const tasks = buildTodayTasks(overview({ srs: null, lessons: undefined, tone: null }))
    expect(tasks).toEqual([])
    const only = buildTodayTasks(overview({ srs: null, tone: null }))
    expect(only.map((t) => t.kind)).toEqual(['lesson', 'writing'])
  })

  it('người mới: bài đầu + học pinyin trước', () => {
    const tasks = buildTodayTasks(
      overview({
        srs: { dueToday: 0, dueNow: 0, newAvailableToday: 0, newIntroducedToday: 0, reviewedToday: 0 },
        lessons: { published: 5, completed: 0, inProgress: 0, next: { slug: 'chao-hoi', title: 'Chào hỏi' } },
        tone: { totalAnswered: 0, accuracy: null, recommendedFocus: [] },
      }),
    )
    expect(tasks.map((t) => t.kind)).toEqual(['lesson', 'pinyin'])
    expect(tasks[0]!.title).toBe('Bắt đầu bài học đầu tiên')
  })

  it('slug bài được encode trong đường dẫn', () => {
    const tasks = buildTodayTasks(
      overview({
        srs: null,
        tone: null,
        lessons: { published: 1, completed: 0, inProgress: 0, next: { slug: 'a b', title: 'Lạ' }, lastCompleted: { slug: 'c&d', title: 'T', completedAt: '', unpracticedChars: 1 } },
      }),
    )
    expect(tasks[0]!.to).toBe('/bai-hoc/a%20b')
    expect(tasks[1]!.to).toBe('/luyen-viet?tab=bai-hoc&bai=c%26d')
  })
})
