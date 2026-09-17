import type { Ref } from 'react'
import { StepWrite } from './StepWrite'
import type { AttemptSummary, HanziWriterBoardHandle } from './HanziWriterBoard'

export interface StepGuidedProps {
  hanzi: string
  size: number
  onStart: () => void
  onComplete: (summary: AttemptSummary) => void
  finished: boolean
  ref?: Ref<HanziWriterBoardHandle>
}

/** Bước Tô theo (`guided`, R-W2): viết đè lên nét mờ, sai 2 lần ở một nét thì nét đúng tự sáng lên. */
export function StepGuided({ hanzi, size, onStart, onComplete, finished, ref }: StepGuidedProps) {
  return (
    <StepWrite
      ref={ref}
      hanzi={hanzi}
      mode="guided"
      size={size}
      onStart={onStart}
      onComplete={onComplete}
      finished={finished}
      instruction="Tô theo nét mờ, đúng thứ tự và hướng. Sai 2 lần ở một nét thì nét đúng sẽ tự sáng lên."
    />
  )
}
