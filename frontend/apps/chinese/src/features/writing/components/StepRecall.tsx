import type { Ref } from 'react'
import { StepWrite } from './StepWrite'
import type { AttemptSummary, HanziWriterBoardHandle } from './HanziWriterBoard'

export interface StepRecallProps {
  hanzi: string
  size: number
  onStart: () => void
  onComplete: (summary: AttemptSummary) => void
  finished: boolean
  ref?: Ref<HanziWriterBoardHandle>
}

/** Bước Tự viết (`recall`, R-W2): không khung, không chữ mẫu; sai 3 lần thì tự hiện, hoặc bấm "Gợi ý nét". */
export function StepRecall({ hanzi, size, onStart, onComplete, finished, ref }: StepRecallProps) {
  return (
    <StepWrite
      ref={ref}
      hanzi={hanzi}
      mode="recall"
      size={size}
      onStart={onStart}
      onComplete={onComplete}
      finished={finished}
      instruction="Tự viết từ trí nhớ, không có nét mờ. Sai 3 lần ở một nét thì nét đúng tự hiện; cần thì bấm “Gợi ý nét”. Viết sạch (0 lỗi, 0 gợi ý) ở 2 ngày khác nhau là thuộc chữ."
    />
  )
}
