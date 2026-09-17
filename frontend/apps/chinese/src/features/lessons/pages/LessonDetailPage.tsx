import { useEffect, useRef } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  FormControlLabel,
  IconButton,
  Link,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Skeleton,
  Stack,
  Switch,
  Tab,
  Tabs,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import CheckOutlinedIcon from '@mui/icons-material/CheckOutlined'
import DrawOutlinedIcon from '@mui/icons-material/DrawOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { useAuth } from '@af/auth'
import { PageContainer, useBackTo, useTabParam } from '@af/ui'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useChineseSpeech, LessonDisplayProvider, LessonContent, GlossaryList } from '@af/chinese-kit'
import type { LessonDetail } from '@af/chinese-kit'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeechProvider'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { VoiceMissingAlert } from '@/features/pinyin/components/VoiceMissingAlert'
import { useToneStats } from '@/features/pinyin/hooks'
import { useLesson, useStartLesson } from '../hooks'
import { useDisplayPrefs } from '../lib/displayPrefs'
import { LessonWordList } from '../components/LessonWordList'
import { LessonStatusChip, UNREVIEWED_LESSON_LABEL } from '../components/LessonCard'
import { QuizRunner } from '../components/quiz/QuizRunner'

const TABS = ['noi-dung', 'tu-vung', 'quiz'] as const
type TabKey = (typeof TABS)[number]

/** Dưới ngưỡng này (số câu luyện thanh đã trả lời ở F5) ⇒ gợi ý học Pinyin trước — không chặn. */
const TONE_DRILL_HINT_THRESHOLD = 40

function DetailSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Skeleton variant="text" width={240} height={40} />
      <Skeleton variant="rounded" height={80} />
      <Skeleton variant="rounded" height={48} />
      <Skeleton variant="rounded" height={220} />
    </Stack>
  )
}

function ToneStatsHint() {
  // Lỗi (503 học liệu, mạng...) ⇒ không hiện gì; không retry để khỏi làm chậm trang bài.
  const query = useToneStats()
  if (!query.data || query.data.totalAnswered >= TONE_DRILL_HINT_THRESHOLD) return null
  return (
    <Alert severity="info">
      Nên học xong phần{' '}
      <Link component={RouterLink} to="/pinyin">
        Pinyin &amp; thanh điệu
      </Link>{' '}
      trước để nghe đúng thanh trong hội thoại và câu hỏi nghe.
    </Alert>
  )
}

function LessonHeader({ lesson, onBack }: { lesson: LessonDetail; onBack: () => void }) {
  const objectives = lesson.objectives ?? []
  // F10: người có `content.manage` thấy nút "Sửa bài" ⇒ trang soạn (quyền đọc từ /api/me, không suy từ vai trò).
  const { can } = useAuth()
  const canManage = can(PERMISSIONS.CONTENT_MANAGE)
  return (
    <Stack sx={{ gap: 1.5 }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
        <Tooltip title="Danh sách bài học">
          <IconButton aria-label="Quay lại danh sách bài học" onClick={onBack} edge="start" sx={{ mt: -0.5 }}>
            <ArrowBackIcon />
          </IconButton>
        </Tooltip>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="caption" color="text.secondary">
            Bài {lesson.orderIndex}
          </Typography>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 700, lineHeight: 1.3 }}>
            {lesson.title}
          </Typography>
          <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 0.75 }}>
            <LessonStatusChip lesson={lesson} />
            <Chip size="small" variant="outlined" label={`~${lesson.estimatedMinutes} phút`} />
            {lesson.reviewStatus === 'machine' && (
              <Tooltip title="Bài do hệ thống soạn, chưa có người duyệt — có thể còn sai sót." enterTouchDelay={0}>
                <Chip size="small" variant="outlined" color="warning" label={UNREVIEWED_LESSON_LABEL} />
              </Tooltip>
            )}
          </Box>
        </Box>
        {canManage && (
          <Tooltip title="Mở trình soạn bài (quản trị nội dung)">
            <Button
              component={RouterLink}
              to={`/quan-tri/bai-hoc/${lesson.id}`}
              size="small"
              variant="outlined"
              startIcon={<EditOutlinedIcon />}
              sx={{ flexShrink: 0, minHeight: 36 }}
            >
              Sửa bài
            </Button>
          </Tooltip>
        )}
      </Box>
      {lesson.summary && <Typography color="text.secondary">{lesson.summary}</Typography>}
      {objectives.length > 0 && (
        <Box>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            Sau bài này bạn sẽ
          </Typography>
          <List dense disablePadding>
            {objectives.map((o, i) => (
              <ListItem key={i} disableGutters sx={{ py: 0.25 }}>
                <ListItemIcon sx={{ minWidth: 28 }}>
                  <CheckOutlinedIcon fontSize="small" color="success" />
                </ListItemIcon>
                <ListItemText primary={o} slotProps={{ primary: { variant: 'body2' } }} />
              </ListItem>
            ))}
          </List>
        </Box>
      )}
    </Stack>
  )
}

function LessonDetailInner() {
  const { slug = '' } = useParams<{ slug: string }>()
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const query = useLesson(slug)
  const [tab, setTab] = useTabParam<TabKey>(TABS, 'noi-dung')
  const goBack = useBackTo('/bai-hoc')
  const prefs = useDisplayPrefs()
  const { status } = useChineseSpeech()
  const startLesson = useStartLesson(slug)

  // "Bắt đầu bài" MỘT LẦN cho mỗi bài khi mở bài chưa có tiến độ (R-LS12) — ref giữ id bài đã gọi: chặn StrictMode
  // gọi đôi, và đổi `slug` (bài khác, cùng component) thì gọi lại cho bài mới. API vốn idempotent.
  const startedForRef = useRef<string | null>(null)
  const lesson = query.data
  const { mutate: startMutate } = startLesson
  useEffect(() => {
    if (!lesson || lesson.progress || startedForRef.current === lesson.id) return
    startedForRef.current = lesson.id
    startMutate(lesson.id)
  }, [lesson, startMutate])

  if (query.isError) {
    return (
      <PageContainer maxWidth={800}>
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      </PageContainer>
    )
  }
  if (query.isLoading || !lesson) {
    return (
      <PageContainer maxWidth={800}>
        <DetailSkeleton />
      </PageContainer>
    )
  }

  return (
    <PageContainer maxWidth={800}>
      <LessonDisplayProvider value={{ showPinyin: prefs.showPinyin, showVi: prefs.showVi }}>
        <Stack sx={{ gap: 2 }}>
          <LessonHeader lesson={lesson} onBack={goBack} />
          <VoiceMissingAlert status={status} />
          <ToneStatsHint />

          <Tabs
            value={tab}
            onChange={(_e, v: TabKey) => setTab(v)}
            variant={isXs ? 'fullWidth' : 'standard'}
            sx={{ borderBottom: 1, borderColor: 'divider' }}
            aria-label="Mục của bài học"
          >
            <Tab value="noi-dung" label="Nội dung" />
            <Tab value="tu-vung" label={`Từ vựng (${lesson.words.length})`} />
            <Tab value="quiz" label={`Quiz (${lesson.quiz.length})`} />
          </Tabs>

          {/* Ba tab GIỮ MOUNTED, chỉ ẩn bằng `hidden`: đổi tab giữa chừng quiz không mất đáp án/clientAttemptId
              (review F9); tab Nội dung giữ trạng thái "Nghe cả đoạn". */}
          <Box role="tabpanel" hidden={tab !== 'noi-dung'}>
            <Stack sx={{ gap: 2 }}>
              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                <FormControlLabel
                  control={<Switch checked={prefs.showPinyin} onChange={(_e, v) => prefs.setShowPinyin(v)} size="small" />}
                  label="Pinyin"
                />
                <FormControlLabel
                  control={<Switch checked={prefs.showVi} onChange={(_e, v) => prefs.setShowVi(v)} size="small" />}
                  label="Nghĩa tiếng Việt"
                />
              </Box>
              <LessonContent blocks={lesson.blocks} glossary={lesson.glossary} />
            </Stack>
          </Box>

          <Box role="tabpanel" hidden={tab !== 'tu-vung'}>
            <Stack sx={{ gap: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Hoàn thành quiz ({'≥'} 80%) thì các từ dưới đây tự vào ôn tập. Bấm một từ để xem chi tiết trong từ điển.
              </Typography>
              <LessonWordList words={lesson.words} />
              {/* F8: sang trang luyện viết với bộ chữ của bài này (`set=lesson:<slug>`). */}
              {lesson.words.length > 0 && (
                <Box>
                  <Button
                    component={RouterLink}
                    to={`/luyen-viet?tab=bai-hoc&bai=${encodeURIComponent(lesson.slug)}`}
                    variant="outlined"
                    startIcon={<DrawOutlinedIcon />}
                    sx={{ minHeight: 44 }}
                  >
                    Luyện viết chữ của bài
                  </Button>
                </Box>
              )}
              {lesson.glossary && lesson.glossary.length > 0 && <GlossaryList entries={lesson.glossary} />}
            </Stack>
          </Box>

          <Box role="tabpanel" hidden={tab !== 'quiz'}>
            <QuizRunner key={lesson.id} lesson={lesson} slug={slug} onLessonChanged={() => void query.refetch()} active={tab === 'quiz'} />
          </Box>
        </Stack>
      </LessonDisplayProvider>
    </PageContainer>
  )
}

/**
 * `/bai-hoc/:slug?tab=noi-dung|tu-vung|quiz` (F9, cần `study.use`). Một `useSpeech('zh')` dùng chung cho cả trang
 * (nút nghe từng dòng, "Nghe cả đoạn", câu nghe của quiz). 404 (bài không published) do `createApiClient` điều hướng.
 */
export function LessonDetailPage() {
  return (
    <ChineseSpeechProvider>
      <LessonDetailInner />
    </ChineseSpeechProvider>
  )
}
