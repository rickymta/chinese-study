import { Link as MuiLink, Typography } from '@mui/material'

/**
 * Dòng nhỏ cuối trang luyện viết (§5.3.2, R-W1): ghi công dữ liệu nét (Make Me a Hanzi qua hanzi-writer-data,
 * Arphic Public License — file `ARPHICPL.TXT` phải đi kèm mọi bản sao) và thư viện hanzi-writer (MIT).
 * Hai file giấy phép nằm trong `public/`, nginx phục vụ tĩnh (`location /hanzi-data/`, `/licenses/`).
 */
export function LicenseNote() {
  return (
    <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 4, lineHeight: 1.6 }}>
      Dữ liệu nét:{' '}
      <MuiLink href="https://github.com/skishore/makemeahanzi" target="_blank" rel="noopener noreferrer" color="inherit">
        Make Me a Hanzi
      </MuiLink>{' '}
      qua hanzi-writer-data 2.0.1 (
      <MuiLink href="/hanzi-data/ARPHICPL.TXT" target="_blank" rel="noopener noreferrer" color="inherit">
        Arphic Public License
      </MuiLink>
      , chỉ chọn tập con, không sửa nội dung) ·{' '}
      <MuiLink href="https://github.com/chanind/hanzi-writer" target="_blank" rel="noopener noreferrer" color="inherit">
        hanzi-writer
      </MuiLink>{' '}
      (
      <MuiLink href="/licenses/hanzi-writer.LICENSE.txt" target="_blank" rel="noopener noreferrer" color="inherit">
        MIT
      </MuiLink>
      ).
    </Typography>
  )
}
