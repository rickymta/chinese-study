import { Card, CardContent, Tab, Tabs, useMediaQuery, useTheme } from '@mui/material'
import { PageContainer, useTabParam } from '@af/ui'
import { ProfileForm } from '../components/ProfileForm'
import { ChangePasswordForm } from '../components/ChangePasswordForm'

// F7 thêm 'hoc-tap' (mục tiêu ngày, tốc độ đọc...).
const TABS = ['thong-tin', 'mat-khau'] as const
type TabKey = (typeof TABS)[number]

/** `/ho-so?tab=thong-tin|mat-khau` (F4 §5.3.F) — chỉ cần đăng nhập (`RequireAuth`), không cần quyền riêng. */
export function ProfilePage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const [tab, setTab] = useTabParam<TabKey>(TABS, 'thong-tin')

  return (
    <PageContainer title="Hồ sơ" maxWidth={720}>
      <Tabs
        value={tab}
        onChange={(_e, v: TabKey) => setTab(v)}
        variant={isXs ? 'fullWidth' : 'standard'}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        aria-label="Mục hồ sơ"
      >
        <Tab value="thong-tin" label="Thông tin" />
        <Tab value="mat-khau" label="Mật khẩu" />
      </Tabs>

      <Card>
        <CardContent>
          {tab === 'thong-tin' && <ProfileForm />}
          {tab === 'mat-khau' && <ChangePasswordForm />}
        </CardContent>
      </Card>
    </PageContainer>
  )
}
