import { useLayoutEffect, type ReactNode } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Card,
  CardActionArea,
  Chip,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { LangText, PageContainer, linkState, useBackTo } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeech'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import { useWord } from '../hooks'
import { posLabels } from '../lib/pos'
import { meaningViSourceLabel } from '../lib/sources'
import { readingAt } from '../lib/characterReading'
import { MeaningStatusChip } from '../components/MeaningStatusChip'
import { QueryErrorAlert } from '../components/QueryErrorAlert'
import { SourceAttribution } from '../components/SourceAttribution'
import { AddToSrsButton } from '@/features/srs/components/AddToSrsButton'
import type { WordDetail } from '../types'

/** Tiêu đề khối (h2) thống nhất cho trang chi tiết. */
function SectionTitle({ children }: { children: ReactNode }) {
  return (
    <Typography component="h2" variant="subtitle1" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
      {children}
    </Typography>
  )
}

function WordSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Skeleton variant="rounded" width={160} height={80} />
      <Skeleton variant="text" width={120} />
      <Skeleton variant="rounded" height={24} width={240} />
      <Skeleton variant="rounded" height={96} />
      <Skeleton variant="rounded" height={48} />
    </Stack>
  )
}

export interface WordDetailBodyProps {
  word: WordDetail
  /**
   * `true` ⇒ liên kết sang chữ (`/tu-dien/chu/:hanzi`) mở tab mới — dùng khi thân trang nằm trong ngăn kéo của
   * phiên ôn (F7.2): bấm vào chữ không được kéo người học rời phiên.
   */
  openLinksInNewTab?: boolean
}

/** Thân trang chi tiết từ — tách ra để F7.2 dùng lại trong `AppDrawer` "Xem chi tiết" của phiên ôn. */
export function WordDetailBody({ word, openLinksInNewTab = false }: WordDetailBodyProps) {
  const location = useLocation()
  const marked = numberedToMarked(word.pinyin)
  const pos = posLabels(word.pos)
  const meaningsVi = word.meaningsVi ?? []
  const meaningsEn = word.meaningsEn ?? []
  const characters = word.characters ?? []
  const variants = (word.variants ?? []).filter(Boolean)
  const showTraditional = !!word.traditional && word.traditional !== word.simplified
  const sourceLabel = meaningViSourceLabel(word.meaningViSource)

  return (
    <Stack sx={{ gap: 3 }}>
      {/* ── Khối đầu: chữ + loa, pinyin, phồn thể/dạng khác/ví dụ, Hán Việt ── */}
      <Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
          <Hanzi component="h1" size="xl" sx={{ fontSize: { xs: 56, sm: 72 }, m: 0, fontWeight: 400 }}>
            {word.simplified}
          </Hanzi>
          {/* Tốc độ đọc lấy từ cài đặt người học (`useTtsRate`, mặc định 0,8) qua `ChineseSpeechProvider`. */}
          <SpeakButton text={word.simplified} size="large" ariaLabel="Nghe" />
        </Box>
        <Typography variant="h6" component="p" sx={{ fontSize: 20, fontWeight: 500 }} title={word.pinyin}>
          {marked}
        </Typography>
        {word.hanViet && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap', mt: 0.5 }}>
            <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
              Hán Việt: {word.hanViet}
            </Typography>
            {word.hanVietStatus === 'derived' && <Chip size="small" variant="outlined" label="Hán Việt suy ra" />}
          </Box>
        )}
        <Stack sx={{ gap: 0.25, mt: 1 }}>
          {showTraditional && (
            <Typography variant="body2" color="text.secondary">
              Phồn thể: <LangText lang="zh-TW" sx={{ fontSize: 18 }}>{word.traditional}</LangText>
            </Typography>
          )}
          {variants.length > 0 && (
            <Typography variant="body2" color="text.secondary">
              Dạng khác: <LangText lang="zh-CN" sx={{ fontSize: 18 }}>{variants.join('、')}</LangText>
            </Typography>
          )}
          {word.usageNote && (
            <Typography variant="body2" color="text.secondary">
              Ví dụ dùng: <LangText lang="zh-CN" sx={{ fontSize: 18 }}>{word.usageNote}</LangText>
            </Typography>
          )}
        </Stack>
      </Box>

      {/* ── Chip cấp HSK + từ loại ── */}
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        {word.hsk3Level != null && <Chip size="small" color="primary" label={`HSK 3.0 · cấp ${word.hsk3Level}`} />}
        {word.hsk2Level != null && <Chip size="small" variant="outlined" label={`HSK 2.0 · cấp ${word.hsk2Level}`} />}
        {word.hskExam2026Level != null && (
          <Chip size="small" variant="outlined" label={`Đề thi 2026 · cấp ${word.hskExam2026Level}`} />
        )}
        {pos.map((p) => (
          <Chip key={p} size="small" variant="outlined" color="secondary" label={p} />
        ))}
      </Box>

      {/* F7.2: thêm vào ôn tập / trạng thái thẻ (`word.srs`). */}
      <AddToSrsButton word={word} />

      {/* ── Nghĩa tiếng Việt ── */}
      <Box>
        <SectionTitle>
          Nghĩa tiếng Việt
          <MeaningStatusChip status={word.meaningViStatus} />
          {sourceLabel && (
            <Typography component="span" variant="caption" color="text.secondary" sx={{ fontWeight: 400 }}>
              {sourceLabel}
            </Typography>
          )}
        </SectionTitle>
        {meaningsVi.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Chưa có nghĩa tiếng Việt — xem nghĩa tiếng Anh bên dưới.
          </Typography>
        ) : (
          <Box component="ol" sx={{ pl: 3, my: 0.5, '& li': { mb: 0.5 } }}>
            {meaningsVi.map((m, i) => (
              <Typography component="li" key={i}>
                {m}
              </Typography>
            ))}
          </Box>
        )}
      </Box>

      {/* ── Nghĩa tiếng Anh (mặc định đóng) ── */}
      <Accordion disableGutters variant="outlined" sx={{ '&::before': { display: 'none' } }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />} aria-controls="meanings-en" id="meanings-en-header">
          <Typography sx={{ fontWeight: 600 }}>Nghĩa tiếng Anh (CC-CEDICT)</Typography>
        </AccordionSummary>
        <AccordionDetails id="meanings-en">
          {meaningsEn.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Không có.
            </Typography>
          ) : (
            <Box component="ol" lang="en" sx={{ pl: 3, my: 0, '& li': { mb: 0.5 } }}>
              {meaningsEn.map((m, i) => (
                <Typography component="li" key={i}>
                  {m}
                </Typography>
              ))}
            </Box>
          )}
        </AccordionDetails>
      </Accordion>

      {/* ── Chữ trong từ ── */}
      {characters.length > 0 && (
        <Box>
          <SectionTitle>Chữ trong từ</SectionTitle>
          <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1 }}>
            {characters.map((c, i) => {
              const reading = readingAt(word.pinyin, i, c.pinyinReadings)
              const hanViet = c.hanViet?.[0]
              return (
                <Card key={`${c.hanzi}-${i}`} variant="outlined" sx={{ width: 72, height: 88, flexShrink: 0 }}>
                  <CardActionArea
                    component={Link}
                    to={`/tu-dien/chu/${encodeURIComponent(c.hanzi)}`}
                    state={linkState(location)}
                    target={openLinksInNewTab ? '_blank' : undefined}
                    rel={openLinksInNewTab ? 'noopener' : undefined}
                    aria-label={`Chữ ${c.hanzi}`}
                    sx={{ height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 0.25, px: 0.5 }}
                  >
                    <Hanzi size="lg" sx={{ fontSize: 32 }}>
                      {c.hanzi}
                    </Hanzi>
                    {hanViet && (
                      <Typography variant="caption" color="text.secondary" noWrap sx={{ textTransform: 'uppercase', maxWidth: '100%' }}>
                        {hanViet}
                      </Typography>
                    )}
                    {reading && (
                      <Typography variant="caption" noWrap sx={{ maxWidth: '100%' }}>
                        {numberedToMarked(reading)}
                      </Typography>
                    )}
                  </CardActionArea>
                </Card>
              )
            })}
          </Box>
        </Box>
      )}
    </Stack>
  )
}

function WordDetailInner() {
  const { id } = useParams<{ id: string }>()
  const goBack = useBackTo('/tu-dien')
  const query = useWord(id)
  // Mở chi tiết (kể cả từ → chữ → từ khác) luôn từ đầu trang — trình duyệt giữ scrollY của trang danh sách trước đó.
  useLayoutEffect(() => {
    window.scrollTo(0, 0)
  }, [id])

  return (
    <PageContainer maxWidth={800}>
      <Button startIcon={<ArrowBackIcon />} onClick={goBack} sx={{ mb: 1, ml: -1 }}>
        Từ điển
      </Button>
      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !query.data ? (
        <WordSkeleton />
      ) : (
        <WordDetailBody word={query.data} />
      )}
      <SourceAttribution />
    </PageContainer>
  )
}

/** `/tu-dien/:id` (F6.3, cần `study.use`). 404 ⇒ `createApiClient` tự điều hướng `/404`. */
export function WordDetailPage() {
  return (
    <ChineseSpeechProvider>
      <WordDetailInner />
    </ChineseSpeechProvider>
  )
}
