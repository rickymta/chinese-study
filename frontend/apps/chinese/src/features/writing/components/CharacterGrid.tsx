import { Link as RouterLink, useLocation } from 'react-router-dom'
import { Box, ButtonBase, Tooltip, Typography } from '@mui/material'
import { linkState } from '@af/ui'
import { Hanzi, numberedToMarked } from '@af/chinese-kit'
import { practicePath } from '../lib/setParam'
import type { MasteryStatus, WritingCharacterItem, WritingSet } from '../types'

export const NO_STROKE_DATA_LABEL = 'Chưa có dữ liệu nét'

/** Chấm màu trạng thái: new xám, practicing cam, mastered xanh. */
const DOT_COLOR: Record<MasteryStatus, string> = { new: 'text.disabled', practicing: 'warning.main', mastered: 'success.main' }

export interface CharacterGridProps {
  items: WritingCharacterItem[]
  set: WritingSet
  hasData: (hanzi: string) => boolean
}

function Tile({ item, set, hasData }: { item: WritingCharacterItem; set: WritingSet; hasData: boolean }) {
  const location = useLocation()
  const reading = item.pinyinReadings?.[0]
  const content = (
    <>
      <Box
        aria-hidden
        sx={{ position: 'absolute', top: 6, right: 6, width: 8, height: 8, borderRadius: '50%', bgcolor: DOT_COLOR[item.masteryStatus] }}
      />
      <Hanzi size="md" sx={{ fontSize: 30, lineHeight: 1.15 }}>
        {item.hanzi}
      </Hanzi>
      {hasData ? (
        <Typography variant="caption" color="text.secondary" noWrap sx={{ maxWidth: '100%', lineHeight: 1.2 }}>
          {reading ? numberedToMarked(reading) : ' '}
        </Typography>
      ) : (
        <Typography variant="caption" color="text.disabled" sx={{ fontSize: 9, lineHeight: 1.1, textAlign: 'center', px: 0.25 }}>
          {NO_STROKE_DATA_LABEL}
        </Typography>
      )}
    </>
  )
  const baseSx = {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 0.25,
    minHeight: 72,
    borderRadius: 1.5,
    border: 1,
    borderColor: 'divider',
    bgcolor: 'background.paper',
    px: 0.5,
    py: 0.75,
    width: '100%',
  } as const

  if (!hasData) {
    // R-W9: hiện trong danh sách nhưng mờ + nhãn; không mở được trang luyện.
    return (
      <Tooltip title={`${item.hanzi}: ${NO_STROKE_DATA_LABEL}`} enterTouchDelay={0}>
        <Box role="listitem" aria-disabled sx={{ ...baseSx, opacity: 0.45 }}>
          {content}
        </Box>
      </Tooltip>
    )
  }
  return (
    <ButtonBase
      component={RouterLink}
      to={practicePath(item.hanzi, set)}
      state={linkState(location)}
      focusRipple
      aria-label={`Luyện viết chữ ${item.hanzi}`}
      sx={{ ...baseSx, '&:hover': { borderColor: 'primary.main' } }}
    >
      {content}
    </ButtonBase>
  )
}

/**
 * Lưới ô chữ (§5.3.2): ô ≥ 64px, tự xếp 4–5 cột ở 375px; chữ + pinyin (cách đọc đầu) + chấm màu trạng thái; ô mờ
 * kèm nhãn khi chưa có dữ liệu nét (R-W9). Bấm ô ⇒ `/luyen-viet/:hanzi?tu=<bộ>` mang `state.from` để quay về đúng tab/trang.
 */
export function CharacterGrid({ items, set, hasData }: CharacterGridProps) {
  return (
    <Box
      role="list"
      sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(64px, 1fr))', gap: { xs: 0.75, sm: 1 } }}
    >
      {items.map((it) => (
        <Tile key={it.hanzi} item={it} set={set} hasData={hasData(it.hanzi)} />
      ))}
    </Box>
  )
}
