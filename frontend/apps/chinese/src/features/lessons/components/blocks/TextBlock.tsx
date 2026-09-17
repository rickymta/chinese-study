import { Stack } from '@mui/material'
import type { TextBlockPayload } from '../../types'
import { InlineZh } from '../InlineZh'

/** Khối văn bản: mỗi đoạn một `<p>`, chữ Hán nội dòng dạng ruby (bấm để nghe). */
export function TextBlock({ payload }: { payload: TextBlockPayload }) {
  return (
    <Stack sx={{ gap: 1 }}>
      {payload.paragraphs.map((p, i) => (
        <InlineZh key={i} text={p} />
      ))}
    </Stack>
  )
}
