import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Pagination,
  Select,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { PageContainer, useTabParam } from '@af/ui'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { useLessons } from '@/features/lessons/hooks'
import { useHanziDataManifest, useWritingCharacters, useWritingSummary } from '../hooks'
import { WRITING_PAGE_SIZE } from '../api'
import { hasStrokeData } from '../lib/charData'
import { HOME_TABS, isLessonSlug, parsePageParam, tabToSet, type HomeTab } from '../lib/setParam'
import { CharacterGrid } from '../components/CharacterGrid'
import { LicenseNote } from '../components/LicenseNote'
import type { WritingSummary } from '../types'

const EMPTY_MESSAGES: Record<HomeTab, string> = {
  hsk1: 'Chưa có chữ nào trong bộ HSK 1 — học liệu chưa được nạp.',
  'bai-hoc': 'Bài này chưa có chữ nào để luyện.',
  'can-luyen': 'Chưa có chữ nào cần luyện thêm — tiếp tục giữ nhịp nhé.',
  'da-luyen': 'Bạn chưa luyện viết chữ nào. Bắt đầu với bộ HSK 1.',
}

function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card variant="outlined" sx={{ flex: 1, minWidth: 0 }}>
      <CardContent sx={{ py: 1.25, px: 1.5, '&:last-child': { pb: 1.25 } }}>
        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }} noWrap component="p">
          {label}
        </Typography>
        <Typography variant="h5" component="p" sx={{ fontWeight: 700, lineHeight: 1.2, fontVariantNumeric: 'tabular-nums' }}>
          {value}
        </Typography>
        {hint && (
          <Typography variant="caption" color="text.secondary" noWrap component="p">
            {hint}
          </Typography>
        )}
      </CardContent>
    </Card>
  )
}

function SummaryRow({ summary }: { summary: WritingSummary | undefined }) {
  if (!summary) {
    return (
      <Box sx={{ display: 'flex', gap: 1 }}>
        <Skeleton variant="rounded" height={72} sx={{ flex: 1 }} />
        <Skeleton variant="rounded" height={72} sx={{ flex: 1 }} />
        <Skeleton variant="rounded" height={72} sx={{ flex: 1 }} />
      </Box>
    )
  }
  return (
    <Box sx={{ display: 'flex', gap: 1 }}>
      <StatCard label="Đã luyện" value={`${summary.practicedChars}/${summary.totalChars}`} hint={summary.attemptsToday > 0 ? `${summary.attemptsToday} lượt hôm nay` : undefined} />
      <StatCard label="Đã thuộc" value={String(summary.masteredChars)} />
      <StatCard label="Cần luyện" value={String(summary.weakChars)} />
    </Box>
  )
}

/**
 * `/luyen-viet?tab=hsk1|bai-hoc|can-luyen|da-luyen&bai=<slug>&page=` (F8, cần `study.use`). URL là nguồn sự thật:
 * đổi tab đặt lại `page`; tab Bài học có ô chọn bài (mặc định bài đầu chưa hoàn thành). Ô chữ không có dữ liệu nét
 * hiện mờ + nhãn (R-W9). Phân trang 60 chữ/trang.
 */
export function WritingHomePage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const [tab] = useTabParam<HomeTab>(HOME_TABS, 'hsk1')
  const [params, setParams] = useSearchParams()
  const page = parsePageParam(params.get('page'))
  const baiParam = params.get('bai')

  // Đổi tab và chọn bài đều đặt lại `page` (bộ khác có số trang khác) — một lần `setParams` để không ghi đè nhau.
  const changeTab = useCallback(
    (next: HomeTab) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          out.delete('page')
          if (next === 'hsk1') out.delete('tab')
          else out.set('tab', next)
          return out
        },
        { replace: true },
      )
    },
    [setParams],
  )
  const changeBai = useCallback(
    (slug: string) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          out.delete('page')
          out.set('bai', slug)
          return out
        },
        { replace: true },
      )
    },
    [setParams],
  )
  const changePage = useCallback(
    (next: number) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          if (next <= 1) out.delete('page')
          else out.set('page', String(next))
          return out
        },
        { replace: true },
      )
      window.scrollTo({ top: 0 })
    },
    [setParams],
  )

  const lessons = useLessons(tab === 'bai-hoc')
  const lessonItems = lessons.data?.items ?? []
  // Bài đang chọn: `?bai=` hợp lệ và có trong danh sách; không thì bài tiếp theo nên học (hoặc bài đầu).
  const effectiveSlug = useMemo(() => {
    if (tab !== 'bai-hoc') return null
    if (isLessonSlug(baiParam) && lessonItems.some((l) => l.slug === baiParam)) return baiParam
    return lessons.data?.nextLessonSlug ?? lessonItems[0]?.slug ?? null
  }, [tab, baiParam, lessonItems, lessons.data?.nextLessonSlug])

  const set = tabToSet(tab, effectiveSlug)
  const manifest = useHanziDataManifest()
  const summary = useWritingSummary()
  const list = useWritingCharacters(set, page, WRITING_PAGE_SIZE)
  const data = list.data
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasData = useCallback((h: string) => hasStrokeData(h, manifest.data), [manifest.data])

  return (
    <PageContainer title="Luyện viết chữ Hán" maxWidth={900}>
      <Stack sx={{ gap: 2 }}>
        {summary.isError ? <QueryErrorAlert error={summary.error} onRetry={() => void summary.refetch()} /> : <SummaryRow summary={summary.data} />}

        {manifest.isError && (
          <Alert severity="warning">Không tải được danh mục dữ liệu nét — tạm thời chưa mở được bảng viết. Tải lại trang để thử lại.</Alert>
        )}

        <Tabs
          value={tab}
          onChange={(_e, v: HomeTab) => changeTab(v)}
          variant={isXs ? 'scrollable' : 'standard'}
          scrollButtons={false}
          allowScrollButtonsMobile
          sx={{ borderBottom: 1, borderColor: 'divider' }}
          aria-label="Bộ chữ"
        >
          <Tab value="hsk1" label="HSK 1" />
          <Tab value="bai-hoc" label="Bài học" />
          <Tab value="can-luyen" label="Cần luyện" />
          <Tab value="da-luyen" label="Đã luyện" />
        </Tabs>

        {tab === 'bai-hoc' && (
          <FormControl size="small" fullWidth sx={{ maxWidth: 420 }}>
            <InputLabel id="writing-lesson-label">Bài học</InputLabel>
            <Select
              labelId="writing-lesson-label"
              label="Bài học"
              value={effectiveSlug ?? ''}
              onChange={(e) => changeBai(String(e.target.value))}
              disabled={lessons.isLoading || lessonItems.length === 0}
              displayEmpty
            >
              {lessonItems.length === 0 && (
                <MenuItem value="" disabled>
                  {lessons.isLoading ? 'Đang tải bài học…' : 'Chưa có bài học nào được xuất bản'}
                </MenuItem>
              )}
              {lessonItems.map((l) => (
                <MenuItem key={l.slug} value={l.slug}>
                  Bài {l.orderIndex}: {l.title}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        {tab === 'bai-hoc' && lessons.isError && <QueryErrorAlert error={lessons.error} onRetry={() => void lessons.refetch()} />}

        {set === null ? (
          tab === 'bai-hoc' && !lessons.isLoading && !lessons.isError ? (
            <Typography color="text.secondary">Chưa có bài học nào được xuất bản để chọn chữ.</Typography>
          ) : null
        ) : list.isError ? (
          <QueryErrorAlert error={list.error} onRetry={() => void list.refetch()} />
        ) : !data ? (
          <Skeleton variant="rounded" height={320} />
        ) : data.items.length === 0 ? (
          <Alert severity="info">{EMPTY_MESSAGES[tab]}</Alert>
        ) : (
          <Stack sx={{ gap: 1.5, opacity: list.isPlaceholderData ? 0.6 : 1 }}>
            <Typography variant="body2" color="text.secondary">
              {data.totalCount} chữ · bấm một chữ để luyện. Chấm màu: xám chưa viết, cam đang luyện, xanh đã thuộc.
            </Typography>
            <CharacterGrid items={data.items} set={set} hasData={hasData} />
            {totalPages > 1 && (
              <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                <Pagination count={totalPages} page={Math.min(page, totalPages)} onChange={(_e, p) => changePage(p)} color="primary" siblingCount={isXs ? 0 : 1} />
              </Box>
            )}
          </Stack>
        )}

        <LicenseNote />
      </Stack>
    </PageContainer>
  )
}
