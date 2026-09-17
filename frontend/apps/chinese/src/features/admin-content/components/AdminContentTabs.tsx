import { Tab, Tabs } from '@mui/material'
import { Link as RouterLink, useLocation } from 'react-router-dom'

/**
 * Hai trang admin nội dung có hàng "Bài học | Từ vựng" — đây là LIÊN KẾT điều hướng (mỗi tab một route), không phải
 * tab trạng thái ⇒ không dùng `useTabParam` (lint `tabs-no-url` chỉ WARN; rà tay: hợp lệ theo hợp đồng §5.3.3).
 */
export function AdminContentTabs() {
  const { pathname } = useLocation()
  const value = pathname.startsWith('/quan-tri/tu-vung') ? 'tu-vung' : 'bai-hoc'
  return (
    <Tabs value={value} aria-label="Quản trị nội dung" sx={{ borderBottom: 1, borderColor: 'divider' }}>
      <Tab value="bai-hoc" label="Bài học" component={RouterLink} to="/quan-tri/bai-hoc" />
      <Tab value="tu-vung" label="Từ vựng" component={RouterLink} to="/quan-tri/tu-vung" />
    </Tabs>
  )
}
