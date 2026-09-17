import { useEffect, useRef, useState } from 'react'
import { useLocation, useSearchParams } from 'react-router-dom'
import {
  Box,
  Chip,
  InputAdornment,
  List,
  Pagination,
  Skeleton,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import { PageContainer, useScrollRestore } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useWordSearch } from '../hooks'
import { SEARCH_PAGE_SIZE } from '../api'
import { WordListItem } from '../components/WordListItem'
import { QueryErrorAlert } from '../components/QueryErrorAlert'
import { SourceAttribution } from '../components/SourceAttribution'

const SEARCH_PLACEHOLDER = 'Chữ Hán, pinyin (ni3hao3 / nǐhǎo / nihao), nghĩa hoặc Hán Việt'
/** Kho hiện chỉ có HSK 3.0 cấp 1 — chip lọc giữ sẵn cho các cấp sau (§5.3.1). */
const HSK_FILTER_LEVEL = 1
const SKELETON_ROWS = 6

/** Đọc `page` từ URL: số nguyên ≥ 1, sai ⇒ 1. */
function parsePage(raw: string | null): number {
  const n = Number(raw)
  return Number.isInteger(n) && n >= 1 ? n : 1
}

/** Đọc `hsk` từ URL: 1..7 theo §6.1, sai ⇒ không lọc. Chip chỉ bật/tắt cấp 1 nhưng URL chia sẻ có thể mang cấp khác. */
function parseHsk(raw: string | null): number | undefined {
  const n = Number(raw)
  return Number.isInteger(n) && n >= 1 && n <= 7 ? n : undefined
}

/**
 * `/tu-dien?q=&hsk=&page=` (F6.3, cần `study.use`). URL là nguồn sự thật: ô gõ ⇒ debounce 300 ms ⇒ ghi `q` lên URL
 * (`replace`, đặt lại `page`); tải lại trang / Back từ chi tiết vẫn giữ kết quả + vị trí cuộn (`useScrollRestore`).
 */
export function DictionarySearchPage() {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const location = useLocation()
  const [params, setParams] = useSearchParams()

  const urlQ = params.get('q') ?? ''
  const hsk = parseHsk(params.get('hsk'))
  const page = parsePage(params.get('page'))

  // Ô gõ giữ giá trị riêng để phản hồi tức thì; `lastAppliedQ` là `q` gần nhất đã đồng bộ giữa ô gõ và URL — dùng
  // để phân biệt "người dùng gõ" (ghi URL) với "URL đổi từ ngoài" (Back/Forward ⇒ đổ lại vào ô gõ), tránh vòng lặp.
  const [input, setInput] = useState(urlQ)
  const debounced = useDebouncedValue(input, 300)
  const lastAppliedQ = useRef(urlQ)

  useEffect(() => {
    if (urlQ === lastAppliedQ.current) return
    lastAppliedQ.current = urlQ
    setInput(urlQ)
  }, [urlQ])

  useEffect(() => {
    // Chuỗi toàn khoảng trắng ⇒ URL không có `q` ⇒ giá trị "đã áp" là rỗng (khớp `urlQ` sau khi ghi).
    const nextQ = debounced.trim() ? debounced : ''
    if (nextQ === lastAppliedQ.current) return
    lastAppliedQ.current = nextQ
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        if (nextQ) out.set('q', nextQ)
        else out.delete('q')
        out.delete('page')
        return out
      },
      { replace: true },
    )
  }, [debounced, setParams])

  const toggleHsk = () => {
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        if (hsk === HSK_FILTER_LEVEL) out.delete('hsk')
        else out.set('hsk', String(HSK_FILTER_LEVEL))
        out.delete('page')
        return out
      },
      { replace: true },
    )
  }

  const goToPage = (next: number) => {
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
  }

  const query = useWordSearch({ q: urlQ, hsk, page, pageSize: SEARCH_PAGE_SIZE })
  const data = query.data
  const isBrowsing = urlQ.trim() === ''
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1

  useScrollRestore('tu-dien:' + location.search, !query.isLoading && !!data)

  // `isPlaceholderData`: đang giữ kết quả của từ khoá/trang CŨ (keepPreviousData) ⇒ không ghép số cũ với `q` mới.
  const counted = data && !query.isPlaceholderData ? data : null
  let title: string
  if (isBrowsing) title = counted ? `Lộ trình HSK 1 (${counted.totalCount} từ)` : 'Lộ trình HSK 1'
  else title = counted ? `${counted.totalCount} kết quả cho “${urlQ}”` : `Đang tìm “${urlQ}”…`

  return (
    <PageContainer title="Từ điển" maxWidth={800}>
      {/* Ô tìm dính đầu trang: dưới AppBar trên ở điện thoại (`--af-top-bar-offset`), sát mép ở md+. Kéo ra bằng
          padding của AppLayout ở xs để nền phủ hết bề ngang khi nội dung cuộn qua. */}
      <Box
        sx={{
          position: 'sticky',
          top: 'var(--af-top-bar-offset, 0px)',
          zIndex: 2,
          bgcolor: 'background.default',
          mx: { xs: -2, md: 0 },
          px: { xs: 2, md: 0 },
          pt: 0.5,
          pb: 1,
        }}
      >
        <TextField
          fullWidth
          type="search"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={SEARCH_PLACEHOLDER}
          autoFocus={isDesktop}
          inputMode="search"
          slotProps={{
            htmlInput: { maxLength: 64, 'aria-label': 'Tìm từ', autoComplete: 'off', spellCheck: false },
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon color="action" />
                </InputAdornment>
              ),
            },
          }}
        />
        <Stack direction="row" sx={{ gap: 1, mt: 1, flexWrap: 'wrap', alignItems: 'center' }}>
          <Chip
            label={`HSK ${HSK_FILTER_LEVEL}`}
            size="small"
            color={hsk === HSK_FILTER_LEVEL ? 'primary' : 'default'}
            variant={hsk === HSK_FILTER_LEVEL ? 'filled' : 'outlined'}
            onClick={toggleHsk}
            aria-pressed={hsk === HSK_FILTER_LEVEL}
          />
        </Stack>
      </Box>

      <Typography component="h2" variant="subtitle1" sx={{ fontWeight: 600, mt: 1.5, mb: 1 }}>
        {title}
      </Typography>

      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !data ? (
        <Stack sx={{ gap: 0.5 }}>
          {Array.from({ length: SKELETON_ROWS }, (_, i) => (
            <Skeleton key={i} variant="rounded" height={64} />
          ))}
        </Stack>
      ) : data.items.length === 0 ? (
        <Box sx={{ py: 4, textAlign: 'center' }}>
          <Typography sx={{ fontWeight: 600, mb: 1 }}>Không tìm thấy “{urlQ}”.</Typography>
          <Typography variant="body2" color="text.secondary">
            Thử gõ theo cách khác: chữ Hán (<Hanzi size="sm">爱</Hanzi>), pinyin có thanh (<i>ai4</i> hoặc <i>ài</i>)
            hay không thanh (<i>ai</i>), nghĩa tiếng Việt (<i>yêu</i>) hoặc âm Hán Việt (<i>ái</i>).
          </Typography>
        </Box>
      ) : (
        <>
          {/* Đang tải trang/từ khoá mới nhưng còn dữ liệu cũ (`keepPreviousData`) ⇒ làm mờ thay vì nháy skeleton. */}
          <List
            disablePadding
            sx={{ opacity: query.isFetching ? 0.6 : 1, transition: 'opacity 150ms', mx: { xs: -1, sm: 0 } }}
            aria-busy={query.isFetching}
          >
            {data.items.map((w) => (
              <WordListItem key={w.id} word={w} />
            ))}
          </List>
          {totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
              <Pagination
                size="small"
                siblingCount={0}
                count={totalPages}
                page={Math.min(page, totalPages)}
                onChange={(_e, p) => goToPage(p)}
              />
            </Box>
          )}
        </>
      )}

      <SourceAttribution />
    </PageContainer>
  )
}
