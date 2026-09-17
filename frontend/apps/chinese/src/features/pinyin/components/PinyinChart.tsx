import { Fragment, useMemo, useState } from 'react'
import { Alert, Box, Button, ButtonBase, Chip, Skeleton, Stack, Typography, useMediaQuery, useTheme } from '@mui/material'
import { useTabParam } from '@af/ui'
import { parseApiError } from '@af/utils'
import { displaySyllableKey } from '@/lib/pinyin'
import { usePinyinChart } from '../hooks'
import type { FinalGroup, InitialGroup, PinyinSyllable } from '../types'
import { SyllableDrawer } from './SyllableDrawer'

const INITIAL_GROUPS = ['all', 'moi', 'dau-luoi', 'cuong-luoi', 'mat-luoi', 'dau-luoi-truoc', 'uon-luoi'] as const
const FINAL_GROUPS = ['all', 'don', 'kep', 'mui', 'i', 'u', 'v', 'dac-biet'] as const
type InitialFilter = (typeof INITIAL_GROUPS)[number]
type FinalFilter = (typeof FINAL_GROUPS)[number]

export const INITIAL_GROUP_LABELS: Record<InitialGroup | 'all', string> = {
  all: 'Tất cả',
  khong: 'Không thanh mẫu',
  moi: 'Môi (b p m f)',
  'dau-luoi': 'Đầu lưỡi (d t n l)',
  'cuong-luoi': 'Cuống lưỡi (g k h)',
  'mat-luoi': 'Mặt lưỡi (j q x)',
  'dau-luoi-truoc': 'Đầu lưỡi trước (z c s)',
  'uon-luoi': 'Uốn lưỡi (zh ch sh r)',
}
export const FINAL_GROUP_LABELS: Record<FinalGroup | 'all', string> = {
  all: 'Tất cả',
  don: 'Đơn',
  kep: 'Kép',
  mui: 'Mũi (-n/-ng)',
  i: 'Nhóm i',
  u: 'Nhóm u',
  v: 'Nhóm ü',
  'dac-biet': 'Đặc biệt',
}

const CELL_H = 40
const CELL_W = 44
const FIRST_COL_W = 56

/**
 * Tab Bảng: hàng = vận mẫu (theo nhóm), cột = Ø + thanh mẫu; lọc nhóm lên URL (`?tm=&vm=`, replace).
 * Chỉ VÙNG BẢNG cuộn ngang (trang không cuộn ngang ở 375px); hàng tiêu đề + cột đầu sticky; ô ≥ 44×40 px.
 */
export function PinyinChart() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const chart = usePinyinChart()
  // D33: mobile mặc định nhóm "môi" để bảng hẹp; desktop "Tất cả".
  const [tm, setTm] = useTabParam<InitialFilter>(INITIAL_GROUPS, isMobile ? 'moi' : 'all', 'tm')
  const [vm, setVm] = useTabParam<FinalFilter>(FINAL_GROUPS, 'all', 'vm')
  const [selected, setSelected] = useState<PinyinSyllable | null>(null)

  const lookup = useMemo(() => {
    const map = new Map<string, PinyinSyllable>()
    for (const s of chart.data?.syllables ?? []) map.set(`${s.initial}|${s.final}`, s)
    return map
  }, [chart.data])

  if (chart.isPending) return <Skeleton variant="rounded" height={320} />
  if (chart.isError) {
    const err = parseApiError(chart.error)
    return (
      <Alert
        severity={err.status === 503 ? 'warning' : 'error'}
        action={
          <Button color="inherit" size="small" onClick={() => void chart.refetch()}>
            Thử lại
          </Button>
        }
      >
        {err.status === 503 ? 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.' : err.message}
      </Alert>
    )
  }

  const { initials, finals } = chart.data
  // Cột Ø luôn hiện; cột thanh mẫu lọc theo nhóm.
  const columns = initials.filter((i) => i.code === '' || tm === 'all' || i.group === tm)
  const rows = finals.filter((f) => vm === 'all' || f.group === vm)
  const initialsByCode = new Map(initials.map((i) => [i.code, i]))
  const finalsByCode = new Map(finals.map((f) => [f.code, f]))

  // Nhóm hàng theo `group` (giữ thứ tự file) để chèn tiêu đề nhóm.
  const grouped: { group: FinalGroup; rows: typeof rows }[] = []
  for (const f of rows) {
    const last = grouped[grouped.length - 1]
    if (last && last.group === f.group) last.rows.push(f)
    else grouped.push({ group: f.group, rows: [f] })
  }

  const headerBg = theme.palette.background.paper

  return (
    <Stack spacing={1.5}>
      <Box>
        <Typography variant="caption" color="text.secondary">
          Thanh mẫu
        </Typography>
        <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 0.5 }}>
          {INITIAL_GROUPS.map((g) => (
            <Chip key={g} label={INITIAL_GROUP_LABELS[g]} size="small" color={tm === g ? 'primary' : 'default'} onClick={() => setTm(g)} />
          ))}
        </Box>
      </Box>
      <Box>
        <Typography variant="caption" color="text.secondary">
          Vận mẫu
        </Typography>
        <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 0.5 }}>
          {FINAL_GROUPS.map((g) => (
            <Chip key={g} label={FINAL_GROUP_LABELS[g]} size="small" color={vm === g ? 'primary' : 'default'} onClick={() => setVm(g)} />
          ))}
        </Box>
      </Box>

      <Typography variant="body2" color="text.secondary">
        Bấm một ô để nghe 4 thanh. Ô chữ mờ: âm tiết có thật nhưng chưa có chữ minh hoạ đọc đúng.
      </Typography>

      <Box
        sx={{
          overflow: 'auto',
          maxHeight: 'calc(100dvh - 220px)',
          border: 1,
          borderColor: 'divider',
          borderRadius: 2,
          WebkitOverflowScrolling: 'touch',
        }}
      >
        <Box
          component="table"
          sx={{ borderCollapse: 'separate', borderSpacing: 0, minWidth: FIRST_COL_W + columns.length * CELL_W }}
        >
          <Box component="thead">
            <Box component="tr">
              <Box
                component="th"
                sx={{ position: 'sticky', top: 0, left: 0, zIndex: 3, bgcolor: headerBg, width: FIRST_COL_W, minWidth: FIRST_COL_W, height: CELL_H, borderBottom: 1, borderRight: 1, borderColor: 'divider', fontSize: 12, color: 'text.secondary' }}
              >
                vận \ thanh
              </Box>
              {columns.map((c) => (
                <Box
                  component="th"
                  key={c.code || 'none'}
                  sx={{ position: 'sticky', top: 0, zIndex: 2, bgcolor: headerBg, minWidth: CELL_W, height: CELL_H, borderBottom: 1, borderColor: 'divider', fontWeight: 700 }}
                >
                  {c.code === '' ? 'Ø' : c.display}
                </Box>
              ))}
            </Box>
          </Box>
          <Box component="tbody">
            {grouped.map((g) => (
              <Fragment key={g.group}>
                <Box component="tr">
                  <Box
                    component="td"
                    colSpan={columns.length + 1}
                    sx={{ position: 'sticky', left: 0, bgcolor: 'action.hover', px: 1, py: 0.5, fontSize: 12, fontWeight: 600, color: 'text.secondary' }}
                  >
                    {FINAL_GROUP_LABELS[g.group]}
                  </Box>
                </Box>
                {g.rows.map((f) => (
                  <Box component="tr" key={f.code}>
                    <Box
                      component="th"
                      scope="row"
                      sx={{ position: 'sticky', left: 0, zIndex: 1, bgcolor: headerBg, minWidth: FIRST_COL_W, height: CELL_H, borderRight: 1, borderColor: 'divider', fontWeight: 700, textAlign: 'center' }}
                    >
                      {f.display}
                    </Box>
                    {columns.map((c) => {
                      const s = lookup.get(`${c.code}|${f.code}`)
                      const hasTones = !!s && Object.keys(s.tones).length > 0
                      return (
                        <Box component="td" key={c.code || 'none'} sx={{ p: 0, textAlign: 'center' }}>
                          {s && (
                            <ButtonBase
                              onClick={() => setSelected(s)}
                              aria-label={`Âm tiết ${displaySyllableKey(s.syllable)}`}
                              sx={{
                                width: '100%',
                                minWidth: CELL_W,
                                height: CELL_H,
                                fontSize: 14,
                                color: hasTones ? 'text.primary' : 'text.disabled',
                                '&:hover': { bgcolor: 'action.hover' },
                              }}
                            >
                              {displaySyllableKey(s.syllable)}
                            </ButtonBase>
                          )}
                        </Box>
                      )
                    })}
                  </Box>
                ))}
              </Fragment>
            ))}
          </Box>
        </Box>
      </Box>

      <SyllableDrawer
        syllable={selected}
        initial={selected ? initialsByCode.get(selected.initial) : undefined}
        final={selected ? finalsByCode.get(selected.final) : undefined}
        onClose={() => setSelected(null)}
      />
    </Stack>
  )
}
