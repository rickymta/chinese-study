import { Box, Typography } from '@mui/material'
import { Hanzi } from '@/components/Hanzi'
import { Pinyin } from '@/components/Pinyin'
import { numberedToMarked } from '@/lib/pinyin'
import type { QuizOption, QuizQuestion } from '../../types'
import { useLessonDisplay } from '../LessonDisplayContext'

/**
 * Nội dung một lựa chọn theo `lang`: `zh` ⇒ chữ Hán (`lang="zh-CN"`), `pinyin` ⇒ dạng dấu, `vi` ⇒ chữ thường.
 * Dùng ở nút chọn (lúc làm) và ở bảng kết quả.
 */
export function OptionText({ option, size = 'md' }: { option: QuizOption; size?: 'sm' | 'md' }) {
  if (option.lang === 'zh') {
    return (
      <Hanzi size={size === 'sm' ? 'sm' : 'md'} sx={{ fontSize: size === 'sm' ? 20 : 26 }}>
        {option.text}
      </Hanzi>
    )
  }
  if (option.lang === 'pinyin') {
    return (
      <Typography component="span" variant={size === 'sm' ? 'body2' : 'body1'} sx={{ fontWeight: 600 }}>
        {numberedToMarked(option.text)}
      </Typography>
    )
  }
  return (
    <Typography component="span" variant={size === 'sm' ? 'body2' : 'body1'}>
      {option.text}
    </Typography>
  )
}

/**
 * Đề bài: `promptLang = 'zh'` ⇒ chữ Hán lớn + pinyin (theo công tắc Pinyin); `vi` ⇒ chữ thường.
 * Câu nghe (`listen_choice`) không hiện `audioText` ở đây (người học phải nghe) — `QuizQuestionView` lo phần loa.
 */
export function QuestionPrompt({ question, compact = false }: { question: QuizQuestion; compact?: boolean }) {
  const { showPinyin } = useLessonDisplay()
  if (question.promptLang === 'zh') {
    return (
      <Box>
        <Hanzi component="p" size="lg" sx={{ fontSize: compact ? 24 : { xs: 30, sm: 36 }, m: 0, lineHeight: 1.4 }}>
          {question.prompt}
        </Hanzi>
        {showPinyin && question.promptPinyin && (
          <Pinyin value={question.promptPinyin} hanzi={question.prompt} component="p" variant={compact ? 'body2' : 'body1'} sx={{ color: 'primary.main', fontWeight: 500 }} />
        )}
      </Box>
    )
  }
  return (
    <Typography component="p" variant={compact ? 'body1' : 'h6'} sx={{ fontWeight: compact ? 500 : 600, lineHeight: 1.4 }}>
      {question.prompt}
    </Typography>
  )
}
