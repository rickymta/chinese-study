import { useState } from 'react'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { Box, Button, Card, CardContent, Chip, Collapse, Link, List, ListItem, ListItemButton, Typography } from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import ExpandLessIcon from '@mui/icons-material/ExpandLess'
import { LangText, linkState } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import { MeaningStatusChip } from '@/features/dictionary/components/MeaningStatusChip'
import { maskHanzi } from '../lib/maskHanzi'
import type { MasteryStatus, WritingCharacterDetail } from '../types'

export const MASTERY_LABELS: Record<MasteryStatus, string> = {
  new: 'Chưa viết',
  practicing: 'Đang luyện',
  mastered: 'Đã thuộc',
}

export const MASTERY_COLORS: Record<MasteryStatus, 'default' | 'warning' | 'success'> = {
  new: 'default',
  practicing: 'warning',
  mastered: 'success',
}

export function MasteryChip({ status, size = 'small' }: { status: MasteryStatus; size?: 'small' | 'medium' }) {
  return <Chip size={size} color={MASTERY_COLORS[status]} variant={status === 'new' ? 'outlined' : 'filled'} label={MASTERY_LABELS[status]} />
}

export interface CharacterInfoCardProps {
  ch: WritingCharacterDetail
  /**
   * Bước Tự viết đang dở: ẩn chữ lớn VÀ che chữ này trong danh sách từ (`爱人` ⇒ `＿人`, pinyin + nghĩa vẫn hiện) —
   * không để người học chép chữ mẫu rồi vẫn được tính "viết sạch" (review F8).
   */
  hideGlyph?: boolean
  /**
   * Bước Tô/Tự viết: khối "Từ có chữ này" thu gọn (mặc định đóng) để bảng viết không bị đẩy khỏi màn hình 375px.
   */
  compact?: boolean
}

/**
 * Thẻ thông tin chữ (§5.3.2): chữ lớn + nút nghe, pinyin các cách đọc (dạng dấu), Hán Việt (cầu nối nghĩa cho
 * người Việt), số nét, bộ thủ, ≤ 5 từ chứa chữ (link `/tu-dien/:id`), trạng thái thuộc chữ.
 */
export function CharacterInfoCard({ ch, hideGlyph = false, compact = false }: CharacterInfoCardProps) {
  const location = useLocation()
  const [wordsOpen, setWordsOpen] = useState(false)
  const readings = (ch.pinyinReadings ?? []).filter(Boolean)
  const hanViet = (ch.hanViet ?? []).filter(Boolean)
  const words = (ch.words ?? []).slice(0, 5)
  const status = ch.stats?.masteryStatus ?? 'new'
  const showWords = words.length > 0 && (!compact || wordsOpen)

  return (
    <Card variant="outlined">
      <CardContent sx={{ px: { xs: 1.5, sm: 2 }, py: compact ? 1.25 : 2, '&:last-child': { pb: compact ? 1.25 : 2 } }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
          {!hideGlyph && (
            <Hanzi size={compact ? 'lg' : 'xl'} sx={{ lineHeight: 1.1 }}>
              {ch.hanzi}
            </Hanzi>
          )}
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              <Typography component="span" variant="h6" sx={{ fontWeight: 600 }}>
                {readings.length > 0 ? readings.map((r) => numberedToMarked(r)).join(' · ') : '—'}
              </Typography>
              <SpeakButton text={ch.hanzi} size="small" ariaLabel="Nghe" />
            </Box>
            {hanViet.length > 0 && (
              <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                Hán Việt: {hanViet.join(' · ')}
              </Typography>
            )}
            <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 0.75, alignItems: 'center' }}>
              <MasteryChip status={status} />
              {ch.strokeCount != null && <Chip size="small" variant="outlined" label={`${ch.strokeCount} nét`} />}
              {ch.radical && (
                <Chip
                  size="small"
                  variant="outlined"
                  label={
                    <span>
                      Bộ{' '}
                      <LangText lang="zh-CN" component="span" sx={{ fontSize: 16 }}>
                        {ch.radical}
                      </LangText>
                    </span>
                  }
                />
              )}
            </Box>
          </Box>
        </Box>

        {ch.stats && !compact && (
          <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 1 }}>
            Đã viết {ch.stats.attempts} lần (tự viết {ch.stats.recallAttempts}) · tự viết sạch {ch.stats.cleanRecallDays}/2 ngày
            {ch.stats.bestRecallMistakes != null && ` · ít lỗi nhất ${ch.stats.bestRecallMistakes}`}
          </Typography>
        )}

        {words.length > 0 && compact && (
          <Button
            size="small"
            onClick={() => setWordsOpen((v) => !v)}
            endIcon={wordsOpen ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            aria-expanded={wordsOpen}
            sx={{ mt: 0.5, ml: -0.5 }}
          >
            Từ có chữ này ({words.length})
          </Button>
        )}

        <Collapse in={showWords} unmountOnExit>
          <Box sx={{ mt: compact ? 0 : 1.5 }}>
            {!compact && (
              <Typography component="h3" variant="subtitle2" sx={{ fontWeight: 700 }}>
                Từ có chữ này
              </Typography>
            )}
            <List dense disablePadding>
              {words.map((w) => (
                <ListItem key={w.id} disablePadding>
                  <ListItemButton component={RouterLink} to={`/tu-dien/${w.id}`} state={linkState(location)} sx={{ px: 0.5, gap: 1, minHeight: 40 }}>
                    <Hanzi size="md" sx={{ flexShrink: 0 }}>
                      {hideGlyph ? maskHanzi(w.simplified, ch.hanzi) : w.simplified}
                    </Hanzi>
                    <Typography component="span" variant="body2" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                      {numberedToMarked(w.pinyin)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" noWrap sx={{ minWidth: 0, flex: 1 }}>
                      {(w.meaningsVi ?? []).slice(0, 2).join('; ') || '—'}
                    </Typography>
                    <MeaningStatusChip status={w.meaningViStatus} sx={{ flexShrink: 0 }} />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Box>
        </Collapse>

        {!compact && (
          <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
            <Link component={RouterLink} to={`/tu-dien/chu/${encodeURIComponent(ch.hanzi)}`} state={linkState(location)} color="inherit">
              Xem chữ trong từ điển
            </Link>
          </Typography>
        )}
      </CardContent>
    </Card>
  )
}
