import { useLayoutEffect, type ReactNode } from 'react'
import { useParams } from 'react-router-dom'
import { Box, Button, List, Skeleton, Stack, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { LangText, PageContainer, useBackTo } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeech'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import { useCharacter } from '../hooks'
import { WordListItem } from '../components/WordListItem'
import { QueryErrorAlert } from '../components/QueryErrorAlert'
import { SourceAttribution } from '../components/SourceAttribution'
import type { CharacterDetail } from '../types'

/** Một dòng "Nhãn: giá trị" trong bảng thông tin chữ. */
function InfoRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box sx={{ display: 'flex', gap: 1, alignItems: 'baseline', flexWrap: 'wrap' }}>
      <Typography component="dt" variant="body2" color="text.secondary" sx={{ minWidth: 88, flexShrink: 0 }}>
        {label}
      </Typography>
      <Typography component="dd" sx={{ m: 0, minWidth: 0 }}>
        {children}
      </Typography>
    </Box>
  )
}

function CharacterBody({ ch }: { ch: CharacterDetail }) {
  const readings = (ch.pinyinReadings ?? []).filter(Boolean)
  const hanViet = (ch.hanViet ?? []).filter(Boolean)
  const traditional = (ch.traditionalVariants ?? []).filter((t) => t && t !== ch.hanzi)
  const words = ch.words ?? []

  return (
    <Stack sx={{ gap: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
        <Hanzi component="h1" size="xxl" sx={{ fontSize: 96, lineHeight: 1.1, m: 0, fontWeight: 400 }}>
          {ch.hanzi}
        </Hanzi>
        <SpeakButton text={ch.hanzi} size="large" ariaLabel="Nghe" />
      </Box>

      <Box component="dl" sx={{ m: 0, display: 'flex', flexDirection: 'column', gap: 0.75 }}>
        <InfoRow label="Cách đọc">
          {readings.length === 0 ? (
            <Typography component="span" color="text.secondary">
              —
            </Typography>
          ) : (
            readings.map((r, i) => (
              <span key={r} title={r}>
                {i > 0 && ' · '}
                {numberedToMarked(r)}
              </span>
            ))
          )}
        </InfoRow>
        <InfoRow label="Hán Việt">
          {hanViet.length === 0 ? (
            <Typography component="span" color="text.secondary">
              —
            </Typography>
          ) : (
            hanViet.map((h, i) => (
              <Typography key={h} component="span" sx={{ fontWeight: i === 0 ? 700 : 400, textTransform: 'uppercase' }}>
                {i > 0 && <span style={{ fontWeight: 400 }}> · </span>}
                {h}
              </Typography>
            ))
          )}
        </InfoRow>
        {ch.strokeCount != null && <InfoRow label="Số nét">{ch.strokeCount}</InfoRow>}
        {ch.radical && (
          <InfoRow label="Bộ thủ">
            <LangText lang="zh-CN" sx={{ fontSize: 20 }}>
              {ch.radical}
            </LangText>
            {ch.radicalNumber != null && ` (bộ số ${ch.radicalNumber})`}
          </InfoRow>
        )}
        {traditional.length > 0 && (
          <InfoRow label="Phồn thể">
            <LangText lang="zh-TW" sx={{ fontSize: 20 }}>
              {traditional.join('、')}
            </LangText>
          </InfoRow>
        )}
      </Box>

      <Box>
        <Typography component="h2" variant="subtitle1" sx={{ fontWeight: 700, mb: 0.5 }}>
          Từ có chữ này
        </Typography>
        {words.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Chưa có từ nào trong kho chứa chữ này.
          </Typography>
        ) : (
          <List disablePadding sx={{ mx: { xs: -1, sm: 0 } }}>
            {words.map((w) => (
              <WordListItem key={w.id} word={w} />
            ))}
          </List>
        )}
        {/* F8 sẽ thêm nút "Luyện viết" ở đây. */}
      </Box>
    </Stack>
  )
}

function CharacterDetailInner() {
  const { hanzi } = useParams<{ hanzi: string }>()
  const goBack = useBackTo('/tu-dien')
  const query = useCharacter(hanzi)
  // Mở chi tiết chữ luôn từ đầu trang (xem ghi chú ở WordDetailPage).
  useLayoutEffect(() => {
    window.scrollTo(0, 0)
  }, [hanzi])

  return (
    <PageContainer maxWidth={800}>
      <Button startIcon={<ArrowBackIcon />} onClick={goBack} sx={{ mb: 1, ml: -1 }}>
        Quay lại
      </Button>
      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !query.data ? (
        <Stack sx={{ gap: 2 }}>
          <Skeleton variant="rounded" width={120} height={110} />
          <Skeleton variant="text" width={200} />
          <Skeleton variant="text" width={160} />
          <Skeleton variant="rounded" height={128} />
        </Stack>
      ) : (
        <CharacterBody ch={query.data} />
      )}
      <SourceAttribution />
    </PageContainer>
  )
}

/** `/tu-dien/chu/:hanzi` (F6.3, cần `study.use`) — `hanzi` đã được router giải mã URL. 404/400 ⇒ trang lỗi chung. */
export function CharacterDetailPage() {
  return (
    <ChineseSpeechProvider>
      <CharacterDetailInner />
    </ChineseSpeechProvider>
  )
}
