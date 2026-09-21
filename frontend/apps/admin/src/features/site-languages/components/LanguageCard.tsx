import { Box, Card, CardContent, IconButton, Link, Tooltip, Typography } from '@mui/material'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import { LangText } from '@af/ui'
import { ReorderButtons } from '@/components/ReorderButtons'
import { StatusChip, type StatusChipOption } from '@/components/StatusChip'
import { LANGUAGE_CODE_TO_BCP47, type LanguageDto } from '../types'

export const LANGUAGE_STATUS_OPTIONS: Record<string, StatusChipOption> = {
  open: { label: 'Đang mở', color: 'success' },
  coming_soon: { label: 'Sắp ra mắt', color: 'warning' },
  hidden: { label: 'Ẩn', color: 'default' },
}

export interface LanguageCardProps {
  language: LanguageDto
  index: number
  count: number
  disabled?: boolean
  onMove: (from: number, to: number) => void
  onEdit: (language: LanguageDto) => void
  onDelete: (language: LanguageDto) => void
}

/** Thẻ một ngôn ngữ: tên + tên bản địa (đặt `lang` đúng — chữ Hán cần phông CJK), trạng thái, appUrl, nút sắp/sửa/xoá. */
export function LanguageCard({ language, index, count, disabled, onMove, onEdit, onDelete }: LanguageCardProps) {
  const bcp47 = LANGUAGE_CODE_TO_BCP47[language.code]
  return (
    <Card variant="outlined" sx={{ borderLeft: 4, borderLeftColor: language.accentColor ?? 'divider' }}>
      <CardContent sx={{ display: 'flex', gap: 1, alignItems: 'flex-start', '&:last-child': { pb: 2 } }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {language.name}
            </Typography>
            {language.nativeName && (
              <Typography variant="subtitle1" color="text.secondary" component="span">
                {bcp47 ? <LangText lang={bcp47}>{language.nativeName}</LangText> : language.nativeName}
              </Typography>
            )}
            <StatusChip value={language.status} options={LANGUAGE_STATUS_OPTIONS} />
          </Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            Mã: <code>{language.code}</code>
          </Typography>
          {language.tagline && (
            <Typography variant="body2" sx={{ mt: 0.5 }}>
              {language.tagline}
            </Typography>
          )}
          {language.appUrl ? (
            <Link href={language.appUrl} target="_blank" rel="noopener noreferrer" variant="body2" sx={{ display: 'block', mt: 0.5, wordBreak: 'break-all' }}>
              {language.appUrl}
            </Link>
          ) : (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              Chưa có đường dẫn ứng dụng
            </Typography>
          )}
        </Box>
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, alignItems: 'center', flexShrink: 0 }}>
          <ReorderButtons index={index} count={count} onMove={onMove} disabled={disabled} itemLabel={`ngôn ngữ ${language.name}`} />
          <Tooltip title="Sửa">
            <span>
              <IconButton size="small" aria-label={`Sửa ngôn ngữ ${language.name}`} disabled={disabled} onClick={() => onEdit(language)}>
                <EditOutlinedIcon fontSize="inherit" />
              </IconButton>
            </span>
          </Tooltip>
          <Tooltip title="Xoá">
            <span>
              <IconButton size="small" color="error" aria-label={`Xoá ngôn ngữ ${language.name}`} disabled={disabled} onClick={() => onDelete(language)}>
                <DeleteOutlineIcon fontSize="inherit" />
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      </CardContent>
    </Card>
  )
}
