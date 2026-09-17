import { Box, Typography } from '@mui/material'
import { Hanzi } from '@/components/Hanzi'
import { Pinyin } from '@/components/Pinyin'
import { SpeakButton } from '@/components/speech/SpeakButton'
import type { ZhLine } from '../types'
import { useLessonDisplay } from './LessonDisplayContext'

export interface ZhLineRowProps {
  line: ZhLine
  /** Màu tên người nói (hội thoại luân phiên hai màu để dễ theo dõi). */
  speakerColor?: string
  /** Cỡ chữ Hán (px). Hội thoại 24, ví dụ ngữ pháp 22. */
  hanziSize?: number
  /** Đánh dấu dòng đang được đọc trong "Nghe cả đoạn". */
  active?: boolean
}

/**
 * Một dòng chữ Hán dùng chung cho hội thoại và ví dụ ngữ pháp: [tên người nói] · chữ Hán lớn · pinyin dấu
 * (công tắc) · nghĩa Việt (công tắc) · ghi chú · nút nghe dòng. `minWidth: 0` để câu dài xuống dòng ở 375px.
 */
export function ZhLineRow({ line, speakerColor, hanziSize = 24, active = false }: ZhLineRowProps) {
  const { showPinyin, showVi } = useLessonDisplay()
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 1,
        py: 0.75,
        px: 1,
        borderRadius: 1,
        bgcolor: active ? 'action.selected' : 'transparent',
        transition: 'background-color 150ms',
      }}
    >
      <Box sx={{ flex: 1, minWidth: 0 }}>
        {line.speaker && (
          <Typography variant="caption" sx={{ fontWeight: 700, color: speakerColor ?? 'text.secondary', letterSpacing: 0.3 }}>
            {line.speaker}
          </Typography>
        )}
        <Hanzi component="p" size="md" sx={{ fontSize: hanziSize, m: 0, lineHeight: 1.4 }}>
          {line.hanzi}
        </Hanzi>
        {showPinyin && (
          <Pinyin value={line.pinyin} hanzi={line.hanzi} component="p" variant="body2" sx={{ color: 'primary.main', fontWeight: 500 }} />
        )}
        {showVi && (
          <Typography variant="body2" color="text.secondary">
            {line.vi}
          </Typography>
        )}
        {line.note && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontStyle: 'italic', mt: 0.25 }}>
            {line.note}
          </Typography>
        )}
      </Box>
      <SpeakButton text={line.hanzi} size="small" ariaLabel="Nghe" sx={{ mt: line.speaker ? 1.5 : 0 }} />
    </Box>
  )
}
