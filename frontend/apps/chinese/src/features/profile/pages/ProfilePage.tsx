import { Alert, Card, CardContent, Tab, Tabs, useMediaQuery, useTheme } from '@mui/material'
import { useAuth } from '@af/auth'
import { PageContainer, useTabParam } from '@af/ui'
import { PERMISSIONS } from '@/features/auth/permissions'
import { ProfileForm } from '../components/ProfileForm'
import { ChangePasswordForm } from '../components/ChangePasswordForm'
import { LearningSettingsTab } from '../components/LearningSettingsTab'

// F7: 'hoc-tap' (hạn mức từ mới/lượt ôn, độ nhớ mục tiêu, tốc độ đọc, tự đọc).
const TABS = ['thong-tin', 'mat-khau', 'hoc-tap'] as const
type TabKey = (typeof TABS)[number]
/** Người không có `study.use` không thấy tab Học tập (cài đặt SRS/TTS vô nghĩa với họ) — kèm Alert giải thích. */
const TABS_NO_STUDY = ['thong-tin', 'mat-khau'] as const satisfies readonly TabKey[]

/** `/ho-so?tab=thong-tin|mat-khau|hoc-tap` (F4 §5.3.F, F7 §5.3.2) — chỉ cần đăng nhập (`RequireAuth`), không cần quyền riêng. */
export function ProfilePage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const canStudy = useAuth().can(PERMISSIONS.STUDY_USE)
  // `?tab=hoc-tap` khi thiếu quyền ⇒ rơi về tab mặc định (useTabParam chỉ nhận giá trị trong danh sách cho phép).
  const [tab, setTab] = useTabParam<TabKey>(canStudy ? TABS : TABS_NO_STUDY, 'thong-tin')

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
        {canStudy && <Tab value="hoc-tap" label="Học tập" />}
      </Tabs>

      {!canStudy && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Tab "Học tập" (hạn mức từ mới, tốc độ đọc…) chỉ hiện khi tài khoản có quyền Học tập — liên hệ quản trị viên
          nếu bạn cần dùng phần học.
        </Alert>
      )}

      <Card>
        <CardContent>
          {tab === 'thong-tin' && <ProfileForm />}
          {tab === 'mat-khau' && <ChangePasswordForm />}
          {tab === 'hoc-tap' && <LearningSettingsTab />}
        </CardContent>
      </Card>
    </PageContainer>
  )
}
