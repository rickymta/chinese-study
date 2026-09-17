import { useState, type MouseEvent } from 'react'
import { Alert, Box, Button, Card, CardContent, Chip, Collapse, IconButton, Menu, MenuItem, Stack, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import ExpandLessIcon from '@mui/icons-material/ExpandLess'
import { useConfirm } from '@af/ui'
import {
  BLOCKS_MAX,
  isKnownBlockPath,
  moveItem,
  newBlockDraft,
  removeAt,
  replaceAt,
  splitBlockPath,
  type BlockDraft,
  type BlockDraftType,
} from '../lib/lessonDraft'
import { listErrorLines, stripPrefix, type PathErrors } from '../lib/validationErrors'
import { TextBlockForm } from './blocks/TextBlockForm'
import { DialogueBlockForm } from './blocks/DialogueBlockForm'
import { GrammarBlockForm } from './blocks/GrammarBlockForm'
import { TipBlockForm } from './blocks/TipBlockForm'

export const BLOCK_TYPE_LABELS: Record<BlockDraftType, string> = {
  text: 'Văn bản',
  dialogue: 'Hội thoại',
  grammar: 'Ngữ pháp',
  tip: 'Mẹo',
}

const BLOCK_TYPES: BlockDraftType[] = ['text', 'dialogue', 'grammar', 'tip']

/** Một dòng tóm tắt khối khi thu gọn. */
function blockSummary(b: BlockDraft): string {
  switch (b.type) {
    case 'text':
      return b.paragraphs.find((p) => p.trim())?.slice(0, 60) || '(trống)'
    case 'dialogue':
      return b.title.trim() || `${b.lines.length} dòng`
    case 'grammar':
      return b.title.trim() || '(chưa có tiêu đề)'
    case 'tip':
      return b.text.trim().slice(0, 60) || '(trống)'
  }
}

export interface BlockListEditorProps {
  blocks: BlockDraft[]
  onChange: (blocks: BlockDraft[]) => void
  /** Lỗi 400 tuyệt đối (`blocks[2].payload.lines[0].pinyin`). Phần không gắn được vào ô ⇒ liệt kê ở Alert đầu. */
  errors?: PathErrors
  disabled?: boolean
}

/**
 * Danh sách khối nội dung (tab Nội dung): thêm (menu 4 loại), xoá (useConfirm), lên/xuống bằng nút, thu gọn/mở từng
 * khối. Mỗi khối là một form theo loại; lỗi 400 được cắt theo tiền tố `blocks[i].payload.` rồi giao cho form con.
 */
export function BlockListEditor({ blocks, onChange, errors = {}, disabled = false }: BlockListEditorProps) {
  const confirm = useConfirm()
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)

  // Lỗi không gắn được (khối ngoài chỉ số, trường lạ, lỗi cấp `blocks`) ⇒ Alert.
  const unmapped: PathErrors = {}
  for (const [path, messages] of Object.entries(errors)) {
    const split = splitBlockPath(path)
    const block = split ? blocks[split.index] : undefined
    if (!split || !block || !isKnownBlockPath(block, split.rel)) unmapped[path] = messages
  }
  const unmappedLines = listErrorLines(unmapped)

  const toggle = (id: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const add = (type: BlockDraftType) => {
    setMenuAnchor(null)
    onChange([...blocks, newBlockDraft(type)])
  }

  const remove = async (i: number) => {
    const ok = await confirm({
      title: `Xoá khối ${i + 1} (${BLOCK_TYPE_LABELS[blocks[i]!.type]})?`,
      message: 'Khối sẽ bị bỏ khỏi bản nháp. Bấm "Hoàn tác" ở thanh dưới nếu đổi ý trước khi lưu.',
      confirmText: 'Xoá khối',
      tone: 'danger',
    })
    if (ok) onChange(removeAt(blocks, i))
  }

  const update = (i: number, block: BlockDraft) => onChange(replaceAt(blocks, i, block))

  return (
    <Stack sx={{ gap: 2 }}>
      {unmappedLines.length > 0 && (
        <Alert severity="error">
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            Máy chủ báo lỗi:
          </Typography>
          <Box component="ul" sx={{ pl: 2.5, my: 0.5 }}>
            {unmappedLines.map((l) => (
              <li key={l}>{l}</li>
            ))}
          </Box>
        </Alert>
      )}

      {blocks.length === 0 && <Typography color="text.secondary">Bài chưa có khối nội dung nào. Thêm hội thoại, ngữ pháp, văn bản hoặc mẹo.</Typography>}

      {blocks.map((block, i) => {
        const isCollapsed = collapsed.has(block.localId)
        const blockErrors = stripPrefix(errors, `blocks[${i}].payload.`)
        const hasError = Object.keys(blockErrors).length > 0
        return (
          <Card key={block.localId} variant="outlined" sx={{ borderColor: hasError ? 'error.main' : 'divider' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, px: { xs: 1, sm: 2 }, py: 1, bgcolor: 'action.hover' }}>
              <Tooltip title={isCollapsed ? 'Mở' : 'Thu gọn'}>
                <IconButton size="small" aria-label={isCollapsed ? 'Mở khối' : 'Thu gọn khối'} onClick={() => toggle(block.localId)}>
                  {isCollapsed ? <ExpandMoreIcon fontSize="inherit" /> : <ExpandLessIcon fontSize="inherit" />}
                </IconButton>
              </Tooltip>
              <Chip size="small" label={`${i + 1} · ${BLOCK_TYPE_LABELS[block.type]}`} color={hasError ? 'error' : 'default'} />
              <Typography variant="body2" color="text.secondary" noWrap sx={{ flex: 1, minWidth: 0 }}>
                {blockSummary(block)}
              </Typography>
              <Box sx={{ display: 'flex', flexShrink: 0 }}>
                <Tooltip title="Lên">
                  <span>
                    <IconButton size="small" aria-label="Chuyển khối lên" disabled={disabled || i === 0} onClick={() => onChange(moveItem(blocks, i, i - 1))}>
                      <ExpandLessIcon fontSize="inherit" />
                    </IconButton>
                  </span>
                </Tooltip>
                <Tooltip title="Xuống">
                  <span>
                    <IconButton
                      size="small"
                      aria-label="Chuyển khối xuống"
                      disabled={disabled || i >= blocks.length - 1}
                      onClick={() => onChange(moveItem(blocks, i, i + 1))}
                    >
                      <ExpandMoreIcon fontSize="inherit" />
                    </IconButton>
                  </span>
                </Tooltip>
                <Button size="small" color="error" disabled={disabled} onClick={() => void remove(i)} sx={{ minWidth: 0, px: 1 }}>
                  Xoá
                </Button>
              </Box>
            </Box>
            <Collapse in={!isCollapsed} unmountOnExit={false}>
              <CardContent sx={{ px: { xs: 1.5, sm: 2 } }}>
                {block.type === 'text' && <TextBlockForm block={block} onChange={(b) => update(i, b)} errors={blockErrors} disabled={disabled} />}
                {block.type === 'dialogue' && <DialogueBlockForm block={block} onChange={(b) => update(i, b)} errors={blockErrors} disabled={disabled} />}
                {block.type === 'grammar' && <GrammarBlockForm block={block} onChange={(b) => update(i, b)} errors={blockErrors} disabled={disabled} />}
                {block.type === 'tip' && <TipBlockForm block={block} onChange={(b) => update(i, b)} errors={blockErrors} disabled={disabled} />}
              </CardContent>
            </Collapse>
          </Card>
        )
      })}

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          disabled={disabled || blocks.length >= BLOCKS_MAX}
          onClick={(e: MouseEvent<HTMLButtonElement>) => setMenuAnchor(e.currentTarget)}
          aria-haspopup="menu"
          aria-expanded={!!menuAnchor}
        >
          Thêm khối
        </Button>
        <Typography variant="caption" color="text.secondary">
          {blocks.length}/{BLOCKS_MAX} khối
        </Typography>
        <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
          {BLOCK_TYPES.map((t) => (
            <MenuItem key={t} onClick={() => add(t)}>
              {BLOCK_TYPE_LABELS[t]}
            </MenuItem>
          ))}
        </Menu>
      </Box>
    </Stack>
  )
}
