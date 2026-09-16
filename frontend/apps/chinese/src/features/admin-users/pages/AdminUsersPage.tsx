import { useEffect, useState } from 'react'
import { Alert, Box, Button, InputAdornment, Pagination, Stack, TextField, Typography, useMediaQuery, useTheme } from '@mui/material'
import { useSearchParams } from 'react-router-dom'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { useAuth } from '@af/auth'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { ADMIN_USERS_PAGE_SIZE, useAdminUsers } from '../hooks'
import type { AdminUser } from '../types'
import { UserList } from '../components/UserList'
import { UserRolesDialog } from '../components/UserRolesDialog'

const SEARCH_DEBOUNCE_MS = 300

/**
 * `/quan-tri/nguoi-dung?q=&page=` (F4 §5.3.F, cần `users.manage`): tìm (debounce 300 ms, `replace` lên URL),
 * phân trang, đổi vai trò qua `UserRolesDialog`. Không có nút nào ẩn theo quyền trong trang (R4-11) — chỉ mục menu.
 */
export function AdminUsersPage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const { account } = useAuth()
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const pageRaw = Number(params.get('page'))
  const page = Number.isInteger(pageRaw) && pageRaw >= 1 ? pageRaw : 1

  // Ô nhập cục bộ, đẩy lên URL sau 300 ms (replace — Back không đi qua từng ký tự).
  const [input, setInput] = useState(q)
  // URL luôn giữ `q` đã trim ⇒ chỉ đồng bộ ngược khi khác NGHĨA (Back/Forward, dán link), không được xoá dấu cách
  // người dùng đang gõ ("Nguyen " → "Nguyen" làm mất khoảng trắng giữa chừng).
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

  const users = useAdminUsers({ q, page })
  const [editing, setEditing] = useState<AdminUser | null>(null)

  const total = users.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / (users.data?.pageSize || ADMIN_USERS_PAGE_SIZE)))
  const err = users.error ? parseApiError(users.error) : null

  return (
    <PageContainer title="Người dùng">
      <Stack spacing={2}>
        <Typography variant="body2" color="text.secondary">
          Chỉ liệt kê người đã từng vào dịch vụ tiếng Trung. Vai trò ở đây là vai trò cục bộ của dịch vụ này.
        </Typography>

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

      <UserRolesDialog user={editing} onClose={() => setEditing(null)} />
    </PageContainer>
  )
}
