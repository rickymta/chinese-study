import { useMemo, type KeyboardEvent } from 'react'
import { Box, Typography, type SxProps, type Theme, type TypographyProps } from '@mui/material'
import { useChineseSpeech } from '../speech/ChineseSpeech'
import { numberedToMarked } from '../pinyin/pinyin'
import { parseInlineZh } from './lib/inlineZh'
import { useLessonDisplay } from './LessonDisplayContext'

export interface InlineZhProps {
  /** Văn bản có thể chứa token `[[chữ Hán|pinyin số thanh]]`. */
  text: string
  variant?: TypographyProps['variant']
  component?: TypographyProps['component']
  color?: TypographyProps['color']
  sx?: SxProps<Theme>
}

/** Một token chữ Hán nội dòng: `<ruby>` chữ Hán + `<rt>` pinyin dấu; bấm/Enter ⇒ đọc TTS (chỉ trong handler — iOS). */
function ZhToken({ hanzi, pinyin }: { hanzi: string; pinyin: string }) {
  const { canSpeak, speakZh } = useChineseSpeech()
  const { showPinyin } = useLessonDisplay()
  const marked = useMemo(() => numberedToMarked(pinyin), [pinyin])
  const speak = () => {
    if (!canSpeak) return
    void speakZh(hanzi).catch(() => undefined)
  }
  const onKeyDown = (e: KeyboardEvent<HTMLElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      speak()
    }
  }
  return (
    <Box
      component="ruby"
      lang="zh-CN"
      role={canSpeak ? 'button' : undefined}
      tabIndex={canSpeak ? 0 : undefined}
      aria-label={canSpeak ? `Nghe ${hanzi}` : undefined}
      title={canSpeak ? `Bấm để nghe · ${marked}` : marked}
      onClick={speak}
      onKeyDown={canSpeak ? onKeyDown : undefined}
      sx={{
        fontFamily: 'var(--af-font-cjk)',
        fontSize: '1.2em',
        lineHeight: 1,
        cursor: canSpeak ? 'pointer' : 'default',
        borderRadius: 0.5,
        px: 0.25,
        // Ruby cần chỗ phía trên; khi ẩn pinyin thì `rt` không vẽ, chữ Hán về hàng.
        rubyPosition: 'over',
        '&:hover': canSpeak ? { bgcolor: 'action.hover' } : undefined,
        '&:focus-visible': { outline: '2px solid', outlineColor: 'primary.main', outlineOffset: 1 },
      }}
    >
      {hanzi}
      {showPinyin && (
        <>
          <Box component="rp">(</Box>
          <Box
            component="rt"
            lang="zh-Latn-pinyin"
            sx={{ fontFamily: (t) => t.typography.fontFamily, fontSize: '0.55em', color: 'text.secondary', letterSpacing: 0.2 }}
          >
            {marked}
          </Box>
          <Box component="rp">)</Box>
        </>
      )}
    </Box>
  )
}

/**
 * Hiển thị văn bản có token chữ Hán nội dòng (§5.4.3): chữ Hán bọc `ruby lang="zh-CN"` với pinyin DẠNG DẤU ở
 * `rt` (tắt được bằng công tắc Pinyin); bấm vào chữ ⇒ đọc. Token hỏng giữ nguyên văn (parser khoan dung).
 * `lineHeight` cao hơn bình thường để hàng ruby không chồng lên dòng trên.
 */
export function InlineZh({ text, variant = 'body1', component = 'p', color, sx }: InlineZhProps) {
  const segments = useMemo(() => parseInlineZh(text), [text])
  const { showPinyin } = useLessonDisplay()
  return (
    <Typography
      variant={variant}
      component={component}
      color={color}
      sx={[{ lineHeight: showPinyin ? 2.1 : 1.7 }, ...(Array.isArray(sx) ? sx : [sx])]}
    >
      {segments.map((s, i) => (s.kind === 'text' ? <span key={i}>{s.text}</span> : <ZhToken key={i} hanzi={s.hanzi} pinyin={s.pinyin} />))}
    </Typography>
  )
}
