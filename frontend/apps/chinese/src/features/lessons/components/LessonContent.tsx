import { Stack, Typography } from '@mui/material'
import type { GlossaryEntry, LessonBlock } from '../types'
import { TextBlock } from './blocks/TextBlock'
import { DialogueBlock } from './blocks/DialogueBlock'
import { GrammarBlock } from './blocks/GrammarBlock'
import { TipBlock } from './blocks/TipBlock'
import { GlossaryList } from './GlossaryList'

export interface LessonContentProps {
  blocks: LessonBlock[]
  /** Từ bổ sung — hiện cuối nội dung (F10 xem trước cũng dùng). Bỏ trống ⇒ không vẽ. */
  glossary?: GlossaryEntry[] | null
}

function renderBlock(block: LessonBlock) {
  switch (block.type) {
    case 'text':
      return <TextBlock payload={block.payload} />
    case 'dialogue':
      return <DialogueBlock payload={block.payload} />
    case 'grammar':
      return <GrammarBlock payload={block.payload} />
    case 'tip':
      return <TipBlock payload={block.payload} />
    default:
      // Kiểu khối lạ (backend thêm sau) ⇒ báo nhẹ, không vỡ trang.
      return (
        <Typography variant="body2" color="text.secondary">
          (Khối nội dung chưa hỗ trợ: {(block as { type: string }).type})
        </Typography>
      )
  }
}

/**
 * Danh sách khối nội dung bài theo thứ tự — DÙNG LẠI ở xem trước bài của quản trị (F10). Cần nằm trong
 * `ChineseSpeechProvider` (nút nghe) và tuỳ chọn `LessonDisplayProvider` (công tắc pinyin/nghĩa; không bọc ⇒ bật).
 */
export function LessonContent({ blocks, glossary }: LessonContentProps) {
  return (
    <Stack sx={{ gap: 2 }}>
      {blocks.length === 0 && (
        <Typography color="text.secondary">Bài này chưa có nội dung.</Typography>
      )}
      {blocks.map((b) => (
        <div key={b.id}>{renderBlock(b)}</div>
      ))}
      {glossary && glossary.length > 0 && <GlossaryList entries={glossary} />}
    </Stack>
  )
}
