import { Box, Card, CardActionArea, CardContent, Checkbox, Chip, Skeleton, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography, useMediaQuery, useTheme } from '@mui/material'
import { Hanzi } from '@/components/Hanzi'
import { numberedToMarked } from '@/lib/pinyin'
import { MeaningStatusChip } from '@/features/dictionary/components/MeaningStatusChip'
import type { AdminWord } from '../types'

export interface WordReviewListProps {
  words: AdminWord[]
  selected: ReadonlySet<string>
  onToggle: (id: string) => void
  onToggleAll: (ids: string[], checked: boolean) => void
  onOpen: (word: AdminWord) => void
  loading?: boolean
  /** `true` khi đang tải trang kế (giữ dữ liệu cũ mờ đi). */
  stale?: boolean
}

function HanVietCell({ word }: { word: AdminWord }) {
  if (!word.hanViet) return <Typography variant="body2" color="text.secondary">—</Typography>
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexWrap: 'wrap' }}>
      <Typography variant="body2" sx={{ textTransform: 'uppercase', letterSpacing: 0.3 }}>
        {word.hanViet}
      </Typography>
      {word.hanVietStatus === 'derived' && <Chip size="small" variant="outlined" label="Suy ra" sx={{ height: 20, fontSize: 11 }} />}
    </Box>
  )
}

/**
 * Danh sách từ cần duyệt: desktop bảng (checkbox, chữ, pinyin dấu, Hán Việt, nghĩa, trạng thái); điện thoại thẻ.
 * Bấm dòng ⇒ mở `WordEditDrawer`; checkbox chọn nhiều để duyệt hàng loạt (không kích hoạt mở).
 */
export function WordReviewList({ words, selected, onToggle, onToggleAll, onOpen, loading = false, stale = false }: WordReviewListProps) {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const ids = words.map((w) => w.id)
  const allChecked = ids.length > 0 && ids.every((id) => selected.has(id))
  const someChecked = ids.some((id) => selected.has(id))

  if (loading) {
    return (
      <Stack spacing={1}>
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} variant="rounded" height={isXs ? 88 : 48} />
        ))}
      </Stack>
    )
  }
  if (words.length === 0) return <Typography color="text.secondary">Không có từ nào khớp bộ lọc.</Typography>

  if (isXs) {
    return (
      <Stack spacing={1} sx={{ opacity: stale ? 0.6 : 1 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Checkbox checked={allChecked} indeterminate={!allChecked && someChecked} onChange={(_e, c) => onToggleAll(ids, c)} slotProps={{ input: { 'aria-label': 'Chọn tất cả trang này' } }} />
          <Typography variant="body2" color="text.secondary">
            Chọn cả trang
          </Typography>
        </Box>
        {words.map((w) => (
          <Card key={w.id} variant="outlined" sx={{ display: 'flex', alignItems: 'stretch' }}>
            <Checkbox checked={selected.has(w.id)} onChange={() => onToggle(w.id)} sx={{ alignSelf: 'center' }} slotProps={{ input: { 'aria-label': `Chọn ${w.simplified}` } }} />
            <CardActionArea onClick={() => onOpen(w)} sx={{ flex: 1, minWidth: 0 }}>
              <CardContent sx={{ display: 'flex', gap: 1.5, alignItems: 'center', py: 1.5, pl: 0 }}>
                <Hanzi size="md" sx={{ fontSize: 30, minWidth: 44, textAlign: 'center', flexShrink: 0 }}>
                  {w.simplified}
                </Hanzi>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, flexWrap: 'wrap' }}>
                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                      {numberedToMarked(w.pinyin)}
                    </Typography>
                    {w.hanViet && (
                      <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                        {w.hanViet}
                      </Typography>
                    )}
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                    {(w.meaningsVi ?? []).join('; ') || '—'}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 0.5, mt: 0.5, flexWrap: 'wrap' }}>
                    <MeaningStatusChip status={w.meaningViStatus} />
                    {w.meaningViStatus === 'reviewed' && <Chip size="small" color="success" variant="outlined" label="Đã duyệt" />}
                    {w.hsk3Level != null && <Chip size="small" variant="outlined" label={`HSK ${w.hsk3Level}`} />}
                  </Box>
                </Box>
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Stack>
    )
  }

  return (
    <Box sx={{ overflowX: 'auto', border: 1, borderColor: 'divider', borderRadius: 1, opacity: stale ? 0.6 : 1 }}>
      <Table size="small" aria-label="Danh sách từ cần duyệt">
        <TableHead>
          <TableRow>
            <TableCell padding="checkbox">
              <Checkbox checked={allChecked} indeterminate={!allChecked && someChecked} onChange={(_e, c) => onToggleAll(ids, c)} slotProps={{ input: { 'aria-label': 'Chọn tất cả trang này' } }} />
            </TableCell>
            <TableCell>Chữ</TableCell>
            <TableCell>Pinyin</TableCell>
            <TableCell>Hán Việt</TableCell>
            <TableCell>Nghĩa tiếng Việt</TableCell>
            <TableCell>Trạng thái</TableCell>
            <TableCell align="right">Lộ trình</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {words.map((w) => (
            <TableRow key={w.id} hover selected={selected.has(w.id)} onClick={() => onOpen(w)} sx={{ cursor: 'pointer' }}>
              <TableCell padding="checkbox" onClick={(e) => e.stopPropagation()}>
                <Checkbox checked={selected.has(w.id)} onChange={() => onToggle(w.id)} slotProps={{ input: { 'aria-label': `Chọn ${w.simplified}` } }} />
              </TableCell>
              <TableCell>
                <Hanzi size="md" sx={{ fontSize: 24 }}>
                  {w.simplified}
                </Hanzi>
              </TableCell>
              <TableCell sx={{ whiteSpace: 'nowrap', fontWeight: 600 }}>{numberedToMarked(w.pinyin)}</TableCell>
              <TableCell>
                <HanVietCell word={w} />
              </TableCell>
              <TableCell sx={{ maxWidth: 360 }}>
                <Typography variant="body2" sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                  {(w.meaningsVi ?? []).join('; ') || '—'}
                </Typography>
              </TableCell>
              <TableCell>
                {w.meaningViStatus === 'machine' ? <MeaningStatusChip status="machine" /> : <Chip size="small" color="success" variant="outlined" label="Đã duyệt" />}
              </TableCell>
              <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                {w.pathOrder ?? '—'}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  )
}
