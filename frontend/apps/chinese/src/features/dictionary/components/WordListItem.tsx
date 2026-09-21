import { Box, ListItem, ListItemButton, Typography } from '@mui/material'
import { Link, useLocation } from 'react-router-dom'
import { linkState } from '@af/ui'
import { Hanzi, numberedToMarked, MeaningStatusChip } from '@af/chinese-kit'
import type { WordSummary } from '@af/chinese-kit'

export interface WordListItemProps {
  word: WordSummary
}

/**
 * Một dòng kết quả tra từ (§5.3.1): trái chữ Hán 28px · giữa pinyin dấu + Hán Việt in hoa nhỏ, dưới là ≤ 2 nghĩa
 * Việt cắt một dòng · phải chip "Chưa duyệt". Cao tối thiểu 64px, `minWidth: 0` để không tràn ngang ở 375px.
 * Link mang `state.from` để trang chi tiết quay về đúng danh sách + query.
 */
export function WordListItem({ word }: WordListItemProps) {
  const location = useLocation()
  const meanings = (word.meaningsVi ?? []).slice(0, 2).join('; ')
  return (
    <ListItem disablePadding divider>
      <ListItemButton
        component={Link}
        to={`/tu-dien/${word.id}`}
        state={linkState(location)}
        sx={{ minHeight: 64, gap: 1.5, alignItems: 'center', px: { xs: 1, sm: 2 } }}
      >
        <Hanzi size="md" sx={{ fontSize: 28, flexShrink: 0, minWidth: 40, textAlign: 'center' }}>
          {word.simplified}
        </Hanzi>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, minWidth: 0 }}>
            <Typography component="span" variant="body1" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }} title={word.pinyin}>
              {numberedToMarked(word.pinyin)}
            </Typography>
            {word.hanViet && (
              <Typography
                component="span"
                variant="caption"
                color="text.secondary"
                noWrap
                sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}
              >
                {word.hanViet}
              </Typography>
            )}
          </Box>
          <Typography variant="body2" color="text.secondary" noWrap>
            {meanings || '—'}
          </Typography>
        </Box>
        <MeaningStatusChip status={word.meaningViStatus} sx={{ flexShrink: 0 }} />
      </ListItemButton>
    </ListItem>
  )
}
