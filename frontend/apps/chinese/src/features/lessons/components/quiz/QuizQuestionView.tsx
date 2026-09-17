import { useState } from 'react'
import { Alert, Box, Button, Card, CardContent, Stack, Typography } from '@mui/material'
import VolumeUpIcon from '@mui/icons-material/VolumeUp'
import { Hanzi } from '@/components/Hanzi'
import { Pinyin } from '@/components/Pinyin'
import { useChineseSpeech } from '@/components/speech/ChineseSpeech'
import type { QuizOption, QuizQuestion } from '../../types'
import { OptionText, QuestionPrompt } from './QuestionParts'

export interface QuizQuestionViewProps {
  question: QuizQuestion
  /** Lựa chọn theo thứ tự HIỂN THỊ đã xáo (cố định trong lượt). */
  options: QuizOption[]
  selectedOptionId: string | null
  onSelect: (optionId: string) => void
  /**
   * `true` sau lần bấm "Nộp bài" đầu tiên: khoá đổi đáp án để "Thử lại" gửi ĐÚNG bộ đáp án cũ cùng
   * `clientAttemptId` (server phát lại kết quả đã lưu — đổi đáp án lúc này sẽ không được chấm).
   */
  disabled?: boolean
}

/**
 * Một câu hỏi: đề bài (+ nút loa lớn với `listen_choice`), lựa chọn dạng nút to xếp dọc (tối thiểu 56px cho ngón
 * tay). KHÔNG chấm tại chỗ — đáp án chỉ hiện ở kết quả cuối bài (hợp đồng). Không có giọng ⇒ `Alert` + "Hiện chữ".
 * Cha đặt `key={question.id}` để trạng thái "đã hiện chữ" reset theo câu.
 */
export function QuizQuestionView({ question, options, selectedOptionId, onSelect, disabled = false }: QuizQuestionViewProps) {
  const { canSpeak, speakZh, speaking } = useChineseSpeech()
  const [revealed, setRevealed] = useState(false)
  const isListen = question.type === 'listen_choice' && !!question.audioText

  const play = () => {
    if (!canSpeak || !question.audioText) return
    void speakZh(question.audioText).catch(() => undefined)
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Card>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          <QuestionPrompt question={question} />
          {isListen && (
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1 }}>
              <Button
                variant="contained"
                size="large"
                startIcon={<VolumeUpIcon />}
                onClick={play}
                disabled={!canSpeak || speaking}
                sx={{ minHeight: 64, minWidth: 200, fontSize: 18 }}
              >
                Nghe
              </Button>
              {!canSpeak && (
                <Alert
                  severity="warning"
                  sx={{ width: '100%' }}
                  action={
                    !revealed && (
                      <Button color="inherit" size="small" onClick={() => setRevealed(true)}>
                        Hiện chữ
                      </Button>
                    )
                  }
                >
                  Chưa có giọng tiếng Trung — không phát được câu hỏi. Bạn có thể xem chữ để trả lời.
                </Alert>
              )}
              {revealed && question.audioText && (
                <Box sx={{ textAlign: 'center' }}>
                  <Hanzi component="p" size="lg" sx={{ m: 0 }}>
                    {question.audioText}
                  </Hanzi>
                  {question.audioPinyin && <Pinyin value={question.audioPinyin} hanzi={question.audioText} component="p" sx={{ color: 'primary.main' }} />}
                </Box>
              )}
            </Box>
          )}
        </CardContent>
      </Card>

      <Stack sx={{ gap: 1 }} role="radiogroup" aria-label="Lựa chọn">
        {options.map((o, i) => {
          const selected = o.id === selectedOptionId
          return (
            <Button
              key={o.id}
              role="radio"
              aria-checked={selected}
              aria-disabled={disabled || undefined}
              variant={selected ? 'contained' : 'outlined'}
              color="primary"
              // Khi khoá: bỏ qua bấm nhưng KHÔNG dùng `disabled` (giữ màu lựa chọn đã chọn để người học đối chiếu).
              onClick={() => {
                if (!disabled) onSelect(o.id)
              }}
              sx={{
                cursor: disabled ? 'default' : 'pointer',
                minHeight: 56,
                justifyContent: 'flex-start',
                textAlign: 'left',
                textTransform: 'none',
                px: 2,
                gap: 1.5,
                // Chữ Hán trong lựa chọn giữ màu chữ của nút (contained ⇒ trắng).
                color: selected ? 'primary.contrastText' : 'text.primary',
                borderColor: selected ? 'primary.main' : 'divider',
                '& *': { color: 'inherit' },
              }}
            >
              <Typography
                component="span"
                variant="caption"
                sx={{
                  width: 24,
                  height: 24,
                  borderRadius: '50%',
                  border: 1,
                  borderColor: 'currentColor',
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                  fontWeight: 700,
                }}
              >
                {i + 1}
              </Typography>
              <Box sx={{ minWidth: 0, flex: 1, whiteSpace: 'normal' }}>
                <OptionText option={o} />
              </Box>
            </Button>
          )
        })}
      </Stack>
    </Stack>
  )
}
