import { Box, Card, CardActionArea, CardContent, Chip, Typography } from '@mui/material'
import { Link, useLocation } from 'react-router-dom'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import PlayCircleOutlinedIcon from '@mui/icons-material/PlayCircleOutlined'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import { linkState } from '@af/ui'
import type { LessonSummary } from '@af/chinese-kit'

export const UNREVIEWED_LESSON_LABEL = 'Nội dung chưa được duyệt'

/** Chip trạng thái học của bài: Chưa học / Đang học / Hoàn thành {best}%. */
export function LessonStatusChip({ lesson, size = 'small' }: { lesson: Pick<LessonSummary, 'progress'>; size?: 'small' | 'medium' }) {
  const p = lesson.progress
  if (!p) return <Chip size={size} variant="outlined" label="Chưa học" />
  if (p.status === 'completed') {
    return (
      <Chip
        size={size}
        color="success"
        icon={<CheckCircleOutlinedIcon />}
        label={p.bestScorePercent != null ? `Hoàn thành ${p.bestScorePercent}%` : 'Hoàn thành'}
      />
    )
  }
  return <Chip size={size} color="primary" variant="outlined" icon={<PlayCircleOutlinedIcon />} label="Đang học" />
}

export interface LessonCardProps {
  lesson: LessonSummary
  /** `true` ⇒ viền nổi bật (bài tiếp theo). */
  highlighted?: boolean
}

/**
 * Thẻ bài học (§5.3.1): số thứ tự, tiêu đề, tóm tắt (≤ 3 dòng), số từ · số câu · ~phút, chip trạng thái, chip
 * "Nội dung chưa được duyệt" khi `reviewStatus = machine` (R-LS2). Cả thẻ là link tới `/bai-hoc/:slug`.
 */
export function LessonCard({ lesson, highlighted = false }: LessonCardProps) {
  const location = useLocation()
  return (
    <Card
      variant="outlined"
      sx={{
        height: '100%',
        borderColor: highlighted ? 'primary.main' : 'divider',
        borderWidth: highlighted ? 2 : 1,
      }}
    >
      <CardActionArea component={Link} to={`/bai-hoc/${lesson.slug}`} state={linkState(location)} sx={{ height: '100%', alignItems: 'stretch' }}>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1, height: '100%' }}>
          <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
            <Typography
              component="span"
              variant="h6"
              sx={{ fontWeight: 700, color: 'primary.main', fontVariantNumeric: 'tabular-nums', flexShrink: 0, lineHeight: 1.4 }}
            >
              {String(lesson.orderIndex).padStart(2, '0')}
            </Typography>
            <Typography component="h2" variant="h6" sx={{ fontWeight: 700, lineHeight: 1.4, flex: 1, minWidth: 0 }}>
              {lesson.title}
            </Typography>
          </Box>
          {lesson.summary && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
            >
              {lesson.summary}
            </Typography>
          )}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: 'text.secondary', mt: 'auto' }}>
            <ScheduleOutlinedIcon sx={{ fontSize: 16 }} />
            <Typography variant="caption">
              {lesson.wordCount} từ · {lesson.questionCount} câu hỏi · ~{lesson.estimatedMinutes} phút
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap' }}>
            <LessonStatusChip lesson={lesson} />
            {lesson.reviewStatus === 'machine' && <Chip size="small" variant="outlined" color="warning" label={UNREVIEWED_LESSON_LABEL} />}
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  )
}
