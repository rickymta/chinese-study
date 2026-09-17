import type { ReactElement } from 'react'
import { Alert, AlertTitle, Box, Button, Chip, Stack, Tooltip, Typography } from '@mui/material'
import PublishOutlinedIcon from '@mui/icons-material/PublishOutlined'
import UnpublishedOutlinedIcon from '@mui/icons-material/UnpublishedOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import RestoreOutlinedIcon from '@mui/icons-material/RestoreOutlined'
import { formatRelativeTime } from '@af/utils'
import type { LessonAction } from '../hooks'
import type { AdminLesson, LessonStatus } from '../types'

export const LESSON_STATUS_LABELS: Record<LessonStatus, string> = { draft: 'Nháp', published: 'Đã xuất bản', archived: 'Lưu trữ' }
export const LESSON_STATUS_COLORS: Record<LessonStatus, 'default' | 'success' | 'warning'> = { draft: 'default', published: 'success', archived: 'warning' }

export interface LessonStatusBarProps {
  lesson: AdminLesson
  /** Có tab nào chưa lưu ⇒ khoá các thao tác trạng thái (chúng chạy trên bản đã lưu) và giải thích. */
  dirtyAny: boolean
  pending: LessonAction | 'delete' | null
  onAction: (action: LessonAction) => void
  onDelete: () => void
  /** Lỗi 422 `LESSON_NOT_PUBLISHABLE` (`details.problems`) — liệt kê tới khi thao tác kế tiếp. */
  publishProblems: string[]
  /** Cảnh báo sau xuất bản thành công (`warnings`). */
  publishWarnings: string[]
  /** Kiểm cục bộ trên BẢN NHÁP (R-CA4) — báo sớm trước khi gọi server. */
  localProblems: string[]
}

/**
 * Đầu trang soạn bài: chip trạng thái/duyệt/nguồn, lần sửa gần nhất; nút Xuất bản / Gỡ xuất bản / Duyệt nội dung /
 * Xoá (lưu trữ) / Khôi phục theo trạng thái; Alert lỗi xuất bản + cảnh báo. Nút khoá luôn kèm lời giải thích.
 */
export function LessonStatusBar({ lesson, dirtyAny, pending, onAction, onDelete, publishProblems, publishWarnings, localProblems }: LessonStatusBarProps) {
  const archived = lesson.status === 'archived'
  const busy = pending !== null
  const lockReason = dirtyAny ? 'Lưu các thay đổi ở tab đang sửa trước — thao tác này chạy trên bản đã lưu.' : null
  const actionsDisabled = busy || !!lockReason

  const wrap = (node: ReactElement, title: string | null) =>
    title ? (
      <Tooltip title={title} enterTouchDelay={0}>
        <span style={{ display: 'inline-flex' }}>{node}</span>
      </Tooltip>
    ) : (
      node
    )

  return (
    <Stack sx={{ gap: 1.5 }}>
      <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', alignItems: 'center' }}>
        <Chip size="small" color={LESSON_STATUS_COLORS[lesson.status]} label={LESSON_STATUS_LABELS[lesson.status]} />
        {lesson.reviewStatus === 'machine' ? (
          <Chip size="small" variant="outlined" color="warning" label="Nội dung chưa được duyệt" />
        ) : (
          <Chip size="small" variant="outlined" color="success" icon={<VerifiedOutlinedIcon />} label="Đã duyệt" />
        )}
        <Chip size="small" variant="outlined" label={lesson.source === 'seed' ? 'Bài có sẵn (seed)' : 'Tự soạn'} />
        {lesson.hasAttempts && <Chip size="small" variant="outlined" label="Đã có người làm quiz" />}
        {lesson.editedAt && (
          <Typography variant="caption" color="text.secondary">
            Sửa {formatRelativeTime(lesson.editedAt)}
            {lesson.editedByName ? ` bởi ${lesson.editedByName}` : ''}
          </Typography>
        )}
      </Box>

      {archived ? (
        <Alert severity="info">
          <AlertTitle>Bài đã lưu trữ — chỉ xem</AlertTitle>
          Học viên không thấy bài này và mọi ô đều khoá. Khôi phục để sửa (bài sẽ về trạng thái Nháp).
        </Alert>
      ) : (
        lesson.status === 'draft' && (
          <Alert severity="info" variant="outlined">
            Bài đang là <strong>Nháp</strong> — học viên chưa thấy. Đủ điều kiện (≥ 1 khối, ≥ 1 từ, ≥ 3 câu quiz) thì bấm Xuất bản.
          </Alert>
        )
      )}

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        {archived ? (
          <Button variant="contained" startIcon={<RestoreOutlinedIcon />} onClick={() => onAction('restore')} loading={pending === 'restore'} disabled={busy}>
            Khôi phục
          </Button>
        ) : (
          <>
            {lesson.status === 'published'
              ? wrap(
                  <Button variant="outlined" color="warning" startIcon={<UnpublishedOutlinedIcon />} onClick={() => onAction('unpublish')} loading={pending === 'unpublish'} disabled={actionsDisabled}>
                    Gỡ xuất bản
                  </Button>,
                  lockReason,
                )
              : wrap(
                  <Button variant="contained" startIcon={<PublishOutlinedIcon />} onClick={() => onAction('publish')} loading={pending === 'publish'} disabled={actionsDisabled}>
                    Xuất bản
                  </Button>,
                  lockReason,
                )}
            {lesson.reviewStatus === 'machine' &&
              wrap(
                <Button variant="outlined" color="success" startIcon={<VerifiedOutlinedIcon />} onClick={() => onAction('review')} loading={pending === 'review'} disabled={actionsDisabled}>
                  Duyệt nội dung
                </Button>,
                lockReason,
              )}
            {wrap(
              <Button variant="text" color="error" startIcon={<DeleteOutlineIcon />} onClick={onDelete} loading={pending === 'delete'} disabled={actionsDisabled}>
                {lesson.source === 'admin' && !lesson.hasAttempts ? 'Xoá' : 'Lưu trữ'}
              </Button>,
              lockReason,
            )}
          </>
        )}
      </Box>
      {lockReason && (
        <Typography variant="caption" color="text.secondary">
          {lockReason}
        </Typography>
      )}

      {publishProblems.length > 0 && (
        <Alert severity="error">
          <AlertTitle>Chưa xuất bản được</AlertTitle>
          <Box component="ul" sx={{ pl: 2.5, my: 0 }}>
            {publishProblems.map((p) => (
              <li key={p}>{p}</li>
            ))}
          </Box>
        </Alert>
      )}
      {publishProblems.length === 0 && localProblems.length > 0 && lesson.status !== 'published' && !archived && (
        <Alert severity="warning" variant="outlined">
          <AlertTitle>Bản nháp hiện chưa đủ điều kiện xuất bản</AlertTitle>
          <Box component="ul" sx={{ pl: 2.5, my: 0 }}>
            {localProblems.slice(0, 8).map((p) => (
              <li key={p}>{p}</li>
            ))}
            {localProblems.length > 8 && <li>… và {localProblems.length - 8} lỗi khác</li>}
          </Box>
        </Alert>
      )}
      {/* Server tính `warnings` ở MỌI phản hồi (không chỉ lúc xuất bản) — hiện gợi ý sớm; sau xuất bản đổi tiêu đề. */}
      {(publishWarnings.length > 0 || (!archived && (lesson.warnings ?? []).length > 0)) && (
        <Alert severity="warning" variant={publishWarnings.length > 0 ? 'standard' : 'outlined'}>
          <AlertTitle>{publishWarnings.length > 0 ? 'Đã xuất bản — lưu ý' : 'Gợi ý (không chặn xuất bản)'}</AlertTitle>
          <Box component="ul" sx={{ pl: 2.5, my: 0 }}>
            {(publishWarnings.length > 0 ? publishWarnings : (lesson.warnings ?? [])).map((w) => (
              <li key={w}>{w}</li>
            ))}
          </Box>
        </Alert>
      )}
    </Stack>
  )
}
