import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { Alert, Box, Button, IconButton, Skeleton, Stack, Tab, Tabs, Tooltip, Typography, useMediaQuery, useTheme } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import { PageContainer, useBackTo, useConfirm, useTabParam, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeechProvider'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { useAdminLesson, useDeleteLesson, useLessonAction, useReplaceLessonBlocks, useReplaceLessonQuiz, useReplaceLessonWords, useUpdateLessonMeta, type LessonAction } from '../hooks'
import {
  blocksDirty,
  blocksFromLesson,
  blocksToRequest,
  checkPublishable,
  emptyParagraphErrors,
  metaDirty,
  metaFromLesson,
  metaToRequest,
  quizDirty,
  quizFromLesson,
  quizToRequest,
  wordsDirty,
  type BlockDraft,
  type MetaDraft,
  type QuizQuestionDraft,
} from '../lib/lessonDraft'
import { validateMeta } from '../lib/metaValidation'
import { useUnsavedChangesGuard } from '../lib/useUnsavedChangesGuard'
import { flattenValidationDetails, type PathErrors } from '../lib/validationErrors'
import type { AdminLesson, AdminLessonWord } from '../types'
import { BlockListEditor } from '../components/BlockListEditor'
import { ConflictAlert } from '../components/ConflictAlert'
import { LessonMetaForm } from '../components/LessonMetaForm'
import { LessonPreview } from '../components/LessonPreview'
import { LessonStatusBar } from '../components/LessonStatusBar'
import { LessonWordsEditor } from '../components/LessonWordsEditor'
import { QuizEditor } from '../components/QuizEditor'
import { SaveBar } from '../components/SaveBar'

const TABS = ['thong-tin', 'noi-dung', 'tu-vung', 'quiz', 'xem-truoc'] as const
type TabKey = (typeof TABS)[number]
type EditTab = Exclude<TabKey, 'xem-truoc'>
const TAB_LABELS: Record<TabKey, string> = { 'thong-tin': 'Thông tin', 'noi-dung': 'Nội dung', 'tu-vung': 'Từ vựng', quiz: 'Quiz', 'xem-truoc': 'Xem trước' }

interface Base {
  version: number
  meta: MetaDraft
  blocks: BlockDraft[]
  words: AdminLessonWord[]
  quiz: QuizQuestionDraft[]
}

interface EditorState {
  base: Base
  meta: MetaDraft
  blocks: BlockDraft[]
  words: AdminLessonWord[]
  quiz: QuizQuestionDraft[]
}

const baseFromLesson = (lesson: AdminLesson): Base => ({
  version: lesson.version,
  meta: metaFromLesson(lesson),
  blocks: blocksFromLesson(lesson.blocks),
  words: lesson.words,
  quiz: quizFromLesson(lesson.quiz),
})

const freshState = (lesson: AdminLesson): EditorState => {
  const base = baseFromLesson(lesson)
  return { base, meta: base.meta, blocks: base.blocks, words: base.words, quiz: base.quiz }
}

/**
 * Gộp bài mới từ server vào state: tab nào KHÔNG có thay đổi (so với base cũ) thì lấy bản mới; tab đang sửa dở giữ
 * bản nháp (409 "tải lại, giữ bản nháp"). `force` = tab vừa lưu xong ⇒ luôn lấy bản server (đã chuẩn hoá).
 */
function mergeLesson(prev: EditorState | null, lesson: AdminLesson, force?: EditTab): EditorState {
  if (!prev) return freshState(lesson)
  if (prev.base.version === lesson.version && !force) return prev
  const nb = baseFromLesson(lesson)
  return {
    base: nb,
    meta: force === 'thong-tin' || !metaDirty(prev.meta, prev.base.meta) ? nb.meta : prev.meta,
    blocks: force === 'noi-dung' || !blocksDirty(prev.blocks, prev.base.blocks) ? nb.blocks : prev.blocks,
    words: force === 'tu-vung' || !wordsDirty(prev.words, prev.base.words) ? nb.words : prev.words,
    quiz: force === 'quiz' || !quizDirty(prev.quiz, prev.base.quiz) ? nb.quiz : prev.quiz,
  }
}

type TabErrors = Partial<Record<EditTab, { fields: PathErrors; message: string | null }>>

function EditSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Skeleton variant="text" width={240} height={40} />
      <Skeleton variant="rounded" height={64} />
      <Skeleton variant="rounded" height={48} />
      <Skeleton variant="rounded" height={260} />
    </Stack>
  )
}

function AdminLessonEditInner() {
  const { id = '' } = useParams<{ id: string }>()
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const navigate = useNavigate()
  const goBack = useBackTo('/quan-tri/bai-hoc')
  const confirm = useConfirm()
  const toast = useToast()
  const query = useAdminLesson(id)
  const lesson = query.data
  const [tab, setTab] = useTabParam<TabKey>(TABS, 'thong-tin')

  const [state, setState] = useState<EditorState | null>(null)
  const [errors, setErrors] = useState<TabErrors>({})
  const [conflict, setConflict] = useState(false)
  const [publishProblems, setPublishProblems] = useState<string[]>([])
  const [publishWarnings, setPublishWarnings] = useState<string[]>([])
  const [pendingAction, setPendingAction] = useState<LessonAction | 'delete' | null>(null)

  // Bài (mới/tải lại) ⇒ gộp vào state theo quy tắc `mergeLesson`.
  useEffect(() => {
    if (lesson) setState((prev) => mergeLesson(prev, lesson))
  }, [lesson])

  const saveMeta = useUpdateLessonMeta(id)
  const saveBlocks = useReplaceLessonBlocks(id)
  const saveWords = useReplaceLessonWords(id)
  const saveQuiz = useReplaceLessonQuiz(id)
  const action = useLessonAction(id)
  const remove = useDeleteLesson(id)

  const dirty = useMemo(
    () => ({
      'thong-tin': !!state && metaDirty(state.meta, state.base.meta),
      'noi-dung': !!state && blocksDirty(state.blocks, state.base.blocks),
      'tu-vung': !!state && wordsDirty(state.words, state.base.words),
      quiz: !!state && quizDirty(state.quiz, state.base.quiz),
    }),
    [state],
  )
  const dirtyAny = dirty['thong-tin'] || dirty['noi-dung'] || dirty['tu-vung'] || dirty.quiz
  useUnsavedChangesGuard(dirtyAny)

  const localCheck = useMemo(
    () => (state ? checkPublishable({ title: state.meta.title, blocks: state.blocks, words: state.words, quiz: state.quiz }) : { problems: [], warnings: [] }),
    [state],
  )

  const archived = lesson?.status === 'archived'
  const disabled = archived || !state
  const disabledReason = archived ? 'Bài đã lưu trữ — chỉ xem. Khôi phục để sửa.' : undefined

  const setTabError = (t: EditTab, fields: PathErrors, message: string | null) => setErrors((prev) => ({ ...prev, [t]: { fields, message } }))
  const clearTabError = (t: EditTab) => setErrors((prev) => ({ ...prev, [t]: undefined }))

  /** Xử lý lỗi ghi chung: 409 ⇒ ConflictAlert; 400 ⇒ lỗi theo đường dẫn; mã nghiệp vụ ⇒ thông điệp tại tab. */
  const handleWriteError = (t: EditTab, err: unknown) => {
    const p = parseApiError(err)
    if (p.status === 409 && p.code === 'CONCURRENCY_CONFLICT') {
      setConflict(true)
      return
    }
    if (p.code === 'SLUG_TAKEN') return setTabError('thong-tin', { slug: ['Slug này đã có bài khác dùng — chọn slug khác.'] }, null)
    if (p.code === 'SLUG_LOCKED') return setTabError('thong-tin', { slug: [p.message] }, null)
    if (p.code === 'UNKNOWN_WORD') {
      // Backend trả `details.wordIds`; đọc thêm `ids` để dự phòng.
      const ids = ((p.details?.wordIds ?? p.details?.ids) as string[] | undefined) ?? []
      return setTabError('tu-vung', {}, `Có từ không tồn tại trong từ điển${ids.length ? ` (${ids.length} id)` : ''} — bỏ từ đó rồi lưu lại.`)
    }
    if (p.code === 'UNKNOWN_QUESTION') return setTabError('quiz', {}, 'Có câu hỏi không còn thuộc bài này (đã bị xoá ở nơi khác) — tải lại bài.')
    if (p.status === 400) {
      const fields = flattenValidationDetails(p.details)
      return setTabError(t, fields, Object.keys(fields).length ? null : p.message)
    }
    setTabError(t, {}, p.message)
  }

  const afterSave = (t: EditTab, saved: AdminLesson) => {
    setState((prev) => mergeLesson(prev, saved, t))
    clearTabError(t)
    setConflict(false)
    // Danh sách lỗi xuất bản lần trước nói về bản CŨ — đã lưu thay đổi thì bỏ (khối "chưa đủ điều kiện" cục bộ vẫn báo nếu còn).
    setPublishProblems([])
    toast.success('Đã lưu')
  }

  const onSaveMeta = async () => {
    if (!state || !lesson) return
    const local = validateMeta(state.meta, !!lesson.publishedAt, lesson.slug)
    if (Object.keys(local).length) return setTabError('thong-tin', local, null)
    try {
      afterSave('thong-tin', await saveMeta.mutateAsync(metaToRequest(state.meta, lesson.version)))
    } catch (err) {
      handleWriteError('thong-tin', err)
    }
  }
  const onSaveBlocks = async () => {
    if (!state || !lesson) return
    // Đoạn văn rỗng: chặn ở client (request giữ nguyên vị trí đoạn nên lỗi gắn đúng ô).
    const empties = emptyParagraphErrors(state.blocks)
    if (Object.keys(empties).length) return setTabError('noi-dung', empties, null)
    try {
      afterSave('noi-dung', await saveBlocks.mutateAsync({ version: lesson.version, blocks: blocksToRequest(state.blocks) }))
    } catch (err) {
      handleWriteError('noi-dung', err)
    }
  }
  const onSaveWords = async () => {
    if (!state || !lesson) return
    try {
      afterSave('tu-vung', await saveWords.mutateAsync({ version: lesson.version, wordIds: state.words.map((w) => w.id) }))
    } catch (err) {
      handleWriteError('tu-vung', err)
    }
  }
  const onSaveQuiz = async () => {
    if (!state || !lesson) return
    try {
      afterSave('quiz', await saveQuiz.mutateAsync({ version: lesson.version, questions: quizToRequest(state.quiz) }))
    } catch (err) {
      handleWriteError('quiz', err)
    }
  }

  const revert = (t: EditTab) => {
    setState((prev) => {
      if (!prev) return prev
      switch (t) {
        case 'thong-tin':
          return { ...prev, meta: prev.base.meta }
        case 'noi-dung':
          return { ...prev, blocks: prev.base.blocks }
        case 'tu-vung':
          return { ...prev, words: prev.base.words }
        case 'quiz':
          return { ...prev, quiz: prev.base.quiz }
      }
    })
    clearTabError(t)
  }

  // 409: tải bản mới, GIỮ bản nháp (mergeLesson giữ tab dirty) — người dùng bấm Lưu lại với version mới.
  const reloadKeep = async () => {
    await query.refetch()
    setConflict(false)
  }
  const discardAll = async () => {
    const ok = await confirm({ title: 'Bỏ mọi thay đổi?', message: 'Mọi tab sẽ về bản mới nhất trên máy chủ.', confirmText: 'Bỏ thay đổi', tone: 'danger' })
    if (!ok) return
    const r = await query.refetch()
    if (r.data) setState(freshState(r.data))
    setErrors({})
    setConflict(false)
  }

  const runAction = async (a: LessonAction) => {
    if (!lesson) return
    setPublishProblems([])
    setPublishWarnings([])
    if (a === 'publish' && localCheck.problems.length) {
      setPublishProblems(localCheck.problems)
      return
    }
    setPendingAction(a)
    try {
      const saved = await action.mutateAsync({ action: a, version: lesson.version })
      setState((prev) => mergeLesson(prev, saved))
      setConflict(false)
      if (a === 'publish') {
        setPublishWarnings(saved.warnings ?? [])
        toast.success('Đã xuất bản — học viên thấy bài ngay')
      } else if (a === 'unpublish') toast.success('Đã gỡ xuất bản — bài về Nháp')
      else if (a === 'review') toast.success('Đã đánh dấu nội dung đã duyệt')
      else toast.success('Đã khôi phục — bài về Nháp')
    } catch (err) {
      const p = parseApiError(err)
      if (p.status === 409 && p.code === 'CONCURRENCY_CONFLICT') setConflict(true)
      else if (p.code === 'LESSON_NOT_PUBLISHABLE') {
        const problems = (p.details?.problems as string[] | undefined) ?? []
        setPublishProblems(problems.length ? problems : [p.message])
      } else toast.error(p.message)
    } finally {
      setPendingAction(null)
    }
  }

  const onDelete = async () => {
    if (!lesson) return
    const hard = lesson.source === 'admin' && !lesson.hasAttempts
    const ok = await confirm({
      title: hard ? 'Xoá vĩnh viễn bài này?' : 'Lưu trữ bài này?',
      message: hard
        ? `"${lesson.title}" do bạn tự soạn và chưa ai làm quiz — xoá là mất hẳn, không khôi phục được.`
        : `"${lesson.title}" sẽ được lưu trữ (có người đã học hoặc là bài có sẵn): học viên không thấy nữa, bạn khôi phục được sau.`,
      confirmText: hard ? 'Xoá vĩnh viễn' : 'Lưu trữ',
      tone: 'danger',
    })
    if (!ok) return
    setPendingAction('delete')
    try {
      const result = await remove.mutateAsync(lesson.version)
      if (result.result === 'deleted') {
        toast.success('Đã xoá bài')
        navigate('/quan-tri/bai-hoc', { replace: true })
      } else {
        setState((prev) => mergeLesson(prev, result.lesson))
        toast.success('Đã lưu trữ bài')
      }
    } catch (err) {
      const p = parseApiError(err)
      if (p.status === 409 && p.code === 'CONCURRENCY_CONFLICT') setConflict(true)
      else toast.error(p.message)
    } finally {
      setPendingAction(null)
    }
  }

  // Chuyển tab khi tab hiện tại có thay đổi chưa lưu ⇒ nhắc (bản nháp VẪN giữ — chỉ nhắc quay lại lưu); sang "Xem
  // trước" không nhắc (xem chính bản nháp).
  const changeTab = useCallback(
    async (next: TabKey) => {
      if (next === tab) return
      if (tab !== 'xem-truoc' && next !== 'xem-truoc' && dirty[tab]) {
        const ok = await confirm({
          title: 'Tab này có thay đổi chưa lưu',
          message: `Tab "${TAB_LABELS[tab]}" đang có thay đổi chưa lưu. Bạn vẫn chuyển tab được — bản nháp được giữ tới khi rời trang — nhưng nhớ quay lại bấm Lưu.`,
          confirmText: 'Chuyển tab',
          cancelText: 'Ở lại lưu',
        })
        if (!ok) return
      }
      setTab(next)
    },
    [tab, dirty, confirm, setTab],
  )

  if (query.isError) {
    return (
      <PageContainer maxWidth={960}>
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      </PageContainer>
    )
  }
  if (query.isLoading || !lesson || !state) {
    return (
      <PageContainer maxWidth={960}>
        <EditSkeleton />
      </PageContainer>
    )
  }

  const saving = { 'thong-tin': saveMeta.isPending, 'noi-dung': saveBlocks.isPending, 'tu-vung': saveWords.isPending, quiz: saveQuiz.isPending }
  const tabLabel = (t: TabKey) => (t !== 'xem-truoc' && dirty[t] ? `${TAB_LABELS[t]} •` : TAB_LABELS[t])
  const tabMessage = (t: EditTab) => errors[t]?.message ?? null

  return (
    <PageContainer maxWidth={960}>
      <Stack sx={{ gap: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
          <Tooltip title="Danh sách bài">
            <IconButton aria-label="Quay lại danh sách bài" onClick={goBack} edge="start" sx={{ mt: -0.5 }}>
              <ArrowBackIcon />
            </IconButton>
          </Tooltip>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="caption" color="text.secondary">
              Bài {lesson.orderIndex} · /bai-hoc/{lesson.slug}
            </Typography>
            <Typography component="h1" variant="h5" sx={{ fontWeight: 700, lineHeight: 1.3 }}>
              {lesson.title}
            </Typography>
          </Box>
          {lesson.status === 'published' && (
            <Button component={RouterLink} to={`/bai-hoc/${lesson.slug}`} size="small" endIcon={<OpenInNewIcon />} target="_blank" rel="noopener" sx={{ flexShrink: 0 }}>
              Mở bài
            </Button>
          )}
        </Box>

        <LessonStatusBar
          lesson={lesson}
          dirtyAny={dirtyAny}
          pending={pendingAction}
          onAction={(a) => void runAction(a)}
          onDelete={() => void onDelete()}
          publishProblems={publishProblems}
          publishWarnings={publishWarnings}
          localProblems={localCheck.problems}
        />

        <ConflictAlert open={conflict} onReloadKeep={() => void reloadKeep()} onDiscard={() => void discardAll()} busy={query.isFetching} />

        <Tabs
          value={tab}
          onChange={(_e, v: TabKey) => void changeTab(v)}
          variant={isXs ? 'scrollable' : 'standard'}
          scrollButtons="auto"
          allowScrollButtonsMobile
          sx={{ borderBottom: 1, borderColor: 'divider' }}
          aria-label="Mục soạn bài"
        >
          {TABS.map((t) => (
            <Tab key={t} value={t} label={tabLabel(t)} />
          ))}
        </Tabs>

        {tab !== 'xem-truoc' && tabMessage(tab) && <Alert severity="error">{tabMessage(tab)}</Alert>}

        {tab === 'thong-tin' && (
          <>
            <LessonMetaForm value={state.meta} onChange={(meta) => setState((p) => (p ? { ...p, meta } : p))} slugLocked={!!lesson.publishedAt} errors={errors['thong-tin']?.fields} disabled={disabled} />
            <SaveBar dirty={dirty['thong-tin']} saving={saving['thong-tin']} disabled={disabled} disabledReason={disabledReason} onSave={() => void onSaveMeta()} onRevert={() => revert('thong-tin')} />
          </>
        )}
        {tab === 'noi-dung' && (
          <>
            <BlockListEditor blocks={state.blocks} onChange={(blocks) => setState((p) => (p ? { ...p, blocks } : p))} errors={errors['noi-dung']?.fields} disabled={disabled} />
            <SaveBar dirty={dirty['noi-dung']} saving={saving['noi-dung']} disabled={disabled} disabledReason={disabledReason} onSave={() => void onSaveBlocks()} onRevert={() => revert('noi-dung')} />
          </>
        )}
        {tab === 'tu-vung' && (
          <>
            <LessonWordsEditor words={state.words} onChange={(words) => setState((p) => (p ? { ...p, words } : p))} disabled={disabled} />
            <SaveBar dirty={dirty['tu-vung']} saving={saving['tu-vung']} disabled={disabled} disabledReason={disabledReason} onSave={() => void onSaveWords()} onRevert={() => revert('tu-vung')} />
          </>
        )}
        {tab === 'quiz' && (
          <>
            {lesson.status === 'published' && (
              <Alert severity="info" variant="outlined">
                Bài đang xuất bản — sửa quiz vẫn được; học viên đang làm dở sẽ phải làm lại từ đầu (R-CA5).
              </Alert>
            )}
            <QuizEditor questions={state.quiz} onChange={(quiz) => setState((p) => (p ? { ...p, quiz } : p))} errors={errors.quiz?.fields} disabled={disabled} />
            <SaveBar dirty={dirty.quiz} saving={saving.quiz} disabled={disabled} disabledReason={disabledReason} onSave={() => void onSaveQuiz()} onRevert={() => revert('quiz')} />
          </>
        )}
        {tab === 'xem-truoc' && <LessonPreview meta={state.meta} blocks={state.blocks} words={state.words} quiz={state.quiz} dirtyAny={dirtyAny} />}
      </Stack>
    </PageContainer>
  )
}

/**
 * `/quan-tri/bai-hoc/:id?tab=thong-tin|noi-dung|tu-vung|quiz|xem-truoc` (F10 §5.3.3, cần `content.manage`).
 * Mỗi tab có nút Lưu riêng gọi đúng endpoint với `version` hiện tại; 409 ⇒ `ConflictAlert` (tải lại giữ bản nháp /
 * bỏ thay đổi); 400 ⇒ lỗi theo đường dẫn tại ô; 422 `LESSON_NOT_PUBLISHABLE` ⇒ liệt kê `problems`; bài lưu trữ ⇒
 * chỉ xem. Bọc `ChineseSpeechProvider` cho nút nghe ở tab Xem trước.
 */
export function AdminLessonEditPage() {
  return (
    <ChineseSpeechProvider>
      <AdminLessonEditInner />
    </ChineseSpeechProvider>
  )
}
