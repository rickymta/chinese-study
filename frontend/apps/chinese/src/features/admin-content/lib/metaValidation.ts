// Kiểm thông tin chung của bài trước khi gửi (`PUT /api/admin/lessons/{id}`) — luật §5.4.3 + R-CA8. Trả lỗi theo đường
// dẫn cùng dạng với 400 của server để form dùng chung một cách hiển thị. Hàm thuần — test ở `metaValidation.test.ts`.

import { GLOSSARY_MAX, OBJECTIVES_MAX, parseObjectives, type MetaDraft } from './lessonDraft'
import { slugProblem } from './slug'
import type { PathErrors } from './validationErrors'
import { isHanziOnly, pinyinProblem } from './zhText'

export const TOPIC_RE = /^[a-z0-9-]{2,64}$/

export function validateMeta(meta: MetaDraft, slugLocked: boolean, originalSlug: string): PathErrors {
  const errors: PathErrors = {}
  const add = (path: string, msg: string) => {
    errors[path] = [...(errors[path] ?? []), msg]
  }
  const title = meta.title.trim()
  if (!title) add('title', 'Tiêu đề không được để trống.')
  else if (title.length > 200) add('title', 'Tiêu đề tối đa 200 ký tự.')

  const slug = meta.slug.trim()
  const slugErr = slugProblem(slug)
  if (slugErr) add('slug', slugErr)
  else if (slugLocked && slug !== originalSlug) add('slug', 'Bài đã từng xuất bản — không đổi slug được (tránh gãy liên kết).')

  if (!TOPIC_RE.test(meta.topic.trim())) add('topic', 'Chủ đề: 2–64 ký tự, chỉ chữ thường a–z, số, gạch ngang (vd giao-tiep).')

  if (!Number.isInteger(meta.orderIndex) || meta.orderIndex < 1 || meta.orderIndex > 999) add('orderIndex', 'Thứ tự là số nguyên 1–999.')
  if (meta.summary.trim().length > 1000) add('summary', 'Tóm tắt tối đa 1000 ký tự.')

  const objectives = parseObjectives(meta.objectivesText)
  if (objectives.length > OBJECTIVES_MAX) add('objectives', `Tối đa ${OBJECTIVES_MAX} mục tiêu.`)
  if (objectives.some((o) => o.length > 200)) add('objectives', 'Mỗi mục tiêu tối đa 200 ký tự.')

  if (!Number.isInteger(meta.estimatedMinutes) || meta.estimatedMinutes < 1 || meta.estimatedMinutes > 120)
    add('estimatedMinutes', 'Số phút là số nguyên 1–120.')

  const glossary = meta.glossary.filter((g) => g.hanzi.trim() || g.pinyin.trim() || g.vi.trim())
  if (glossary.length > GLOSSARY_MAX) add('glossary', `Tối đa ${GLOSSARY_MAX} từ bổ sung.`)
  meta.glossary.forEach((g, i) => {
    if (!g.hanzi.trim() && !g.pinyin.trim() && !g.vi.trim()) return // dòng trống bị bỏ khi gửi
    if (!isHanziOnly(g.hanzi) || g.hanzi.trim().length > 10) add(`glossary[${i}].hanzi`, 'Chỉ chữ Hán, tối đa 10 chữ.')
    const p = pinyinProblem(g.hanzi, g.pinyin)
    if (p) add(`glossary[${i}].pinyin`, p)
    const vi = g.vi.trim()
    if (!vi || vi.length > 100) add(`glossary[${i}].vi`, 'Nghĩa 1–100 ký tự.')
  })
  return errors
}
