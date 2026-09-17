import { useEffect, useState } from 'react'
import { Alert, Box, Button, InputAdornment, Pagination, Stack, TextField, Typography, useMediaQuery, useTheme } from '@mui/material'
import { useSearchParams } from 'react-router-dom'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { useAuth } from '@af/auth'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { CMS_USERS_PAGE_SIZE, useCmsUsers } from '../hooks'
import type { CmsUser } from '../types'
import { UserList } from '../components/UserList'
import { EditRolesDialog } from '../components/EditRolesDialog'

const SEARCH_DEBOUNCE_MS = 300

/**
 * `/he-thong/nguoi-dung-cms?q=&page=` (hợp đồng W2 §5.3.1, W3a dời dưới `/he-thong`; đường cũ `/nguoi-dung-cms`
 * chuyển hướng — cần `cms:users.manage`): tìm (debounce 300 ms, `replace` lên URL),
 * phân trang lên URL, sửa vai trò qua `EditRolesDialog`. Không có nút nào ẩn theo quyền trong trang — chỉ mục menu.
 */
export function CmsUsersPage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const { account } = useAuth()
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const pageRaw = Number(params.get('page'))
  const page = Number.isInteger(pageRaw) && pageRaw >= 1 ? pageRaw : 1

  // Ô nhập cục bộ, đẩy lên URL sau 300 ms (replace — Back không đi qua từng ký tự).
  const [input, setInput] = useState(q)
  // URL luôn giữ `q` đã trim ⇒ chỉ đồng bộ ngược khi khác NGHĨA (Back/Forward, dán link), không xoá dấu cách đang gõ.
  useEffect(() => {
    setInput((prev) => (prev.trim() === q ? prev : q))
  }, [q])
  useEffect(() => {
    if (input.trim() === q) return
    const t = setTimeout(() => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          const trimmed = input.trim()
          if (trimmed) out.set('q', trimmed)
          else out.delete('q')
          out.delete('page') // đổi từ khoá ⇒ về trang 1
          return out
        },
        { replace: true },
      )
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(t)
  }, [input, q, setParams])

  const setPage = (next: number) => {
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        if (next <= 1) out.delete('page')
        else out.set('page', String(next))
        return out
      },
      { replace: true },
    )
  }

  const users = useCmsUsers({ q, page })
  const [editing, setEditing] = useState<CmsUser | null>(null)

  const total = users.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / (users.data?.pageSize || CMS_USERS_PAGE_SIZE)))
  const err = users.error ? parseApiError(users.error) : null

  return (
    <PageContainer title="Người dùng CMS">
      <Stack spacing={2}>
        <Alert severity="info">
          Người dùng xuất hiện ở đây sau lần đầu mở trang Admin. Tài khoản mới không có quyền nào cho tới khi được gán
          vai trò. Vai trò ở đây là vai trò của CMS (website, hộp thư, tài khoản nền tảng, hệ thống) — vai trò ở từng
          ngôn ngữ (vd Tiếng Trung) quản lý riêng.
        </Alert>

        <TextField
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Tìm theo email hoặc tên hiển thị"
          fullWidth
          size="small"
          autoComplete="off"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchOutlinedIcon fontSize="small" />
                </InputAdornment>
              ),
            },
            htmlInput: { 'aria-label': 'Tìm người dùng', maxLength: 100, autoCapitalize: 'none', spellCheck: false },
          }}
        />

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void users.refetch()}>
                Thử lại
              </Button>
            }
          >
            Không tải được danh sách: {err.message}
          </Alert>
        )}

        <UserList users={users.data?.items ?? []} currentUserId={account?.id ?? null} loading={users.isPending || users.isPlaceholderData} onEditRoles={setEditing} />

        {total > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.secondary">
              {total} người dùng
            </Typography>
            {pageCount > 1 && (
              <Pagination
                count={pageCount}
                page={Math.min(page, pageCount)}
                onChange={(_e, p) => setPage(p)}
                size={isXs ? 'small' : 'medium'}
                siblingCount={isXs ? 0 : 1}
              />
            )}
          </Box>
        )}
      </Stack>

      <EditRolesDialog user={editing} onClose={() => setEditing(null)} />
    </PageContainer>
  )
}
