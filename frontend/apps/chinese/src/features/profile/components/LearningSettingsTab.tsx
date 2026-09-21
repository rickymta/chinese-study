import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, FormControlLabel, FormHelperText, Skeleton, Slider, Stack, Switch, TextField, Typography } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeechProvider'
import { SpeakButton, TTS_RATE_MAX, TTS_RATE_MIN } from '@af/chinese-kit'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { useLearningSettings, useUpdateLearningSettings } from '@/features/srs/hooks'
import type { LearningSettingsResponse } from '@/features/srs/types'

// Luật §6.2: dailyNewCards 0..50; dailyReviewLimit 10..1000; desiredRetention 0.80..0.97; ttsRate 0.5..1.2.
const schema = z.object({
  dailyNewCards: z.number({ error: 'Nhập một số' }).int('Phải là số nguyên').min(0, 'Ít nhất 0').max(50, 'Nhiều nhất 50'),
  dailyReviewLimit: z.number({ error: 'Nhập một số' }).int('Phải là số nguyên').min(10, 'Ít nhất 10').max(1000, 'Nhiều nhất 1000'),
  /** Hiển thị theo %, gửi lên dạng 0,80–0,97. */
  retentionPercent: z.number({ error: 'Nhập một số' }).int().min(80, 'Ít nhất 80%').max(97, 'Nhiều nhất 97%'),
  ttsRate: z.number({ error: 'Nhập một số' }).min(TTS_RATE_MIN, 'Chậm nhất 0,5').max(TTS_RATE_MAX, 'Nhanh nhất 1,2'),
  autoPlayAudio: z.boolean(),
})
type FormValues = z.infer<typeof schema>

const FIELD_LABELS: Record<keyof FormValues, string> = {
  dailyNewCards: 'Từ mới mỗi ngày',
  dailyReviewLimit: 'Giới hạn lượt ôn/ngày',
  retentionPercent: 'Độ nhớ mục tiêu',
  ttsRate: 'Tốc độ đọc',
  autoPlayAudio: 'Tự đọc khi hiện thẻ',
}

/** Tên trường server (`desiredRetention`) ⇒ tên trường form (`retentionPercent`). */
const SERVER_TO_FORM: Record<string, keyof FormValues> = {
  dailyNewCards: 'dailyNewCards',
  dailyReviewLimit: 'dailyReviewLimit',
  desiredRetention: 'retentionPercent',
  ttsRate: 'ttsRate',
  autoPlayAudio: 'autoPlayAudio',
}

const toForm = (s: LearningSettingsResponse): FormValues => ({
  dailyNewCards: s.dailyNewCards,
  dailyReviewLimit: s.dailyReviewLimit,
  retentionPercent: Math.round(s.desiredRetention * 100),
  ttsRate: Math.round(s.ttsRate * 100) / 100,
  autoPlayAudio: s.autoPlayAudio,
})

const RATE_MARKS = [
  { value: 0.5, label: 'Chậm' },
  { value: 0.8, label: '0,8' },
  { value: 1, label: 'Thường' },
  { value: 1.2, label: 'Nhanh' },
]
const NEW_CARD_MARKS = [0, 10, 20, 30, 40, 50].map((v) => ({ value: v, label: String(v) }))
const RETENTION_MARKS = [80, 85, 90, 95, 97].map((v) => ({ value: v, label: `${v}%` }))

const vi = (n: number, digits = 1) => n.toFixed(digits).replace('.', ',')

function SettingsForm({ settings }: { settings: LearningSettingsResponse }) {
  const toast = useToast()
  const update = useUpdateLearningSettings()
  const [formError, setFormError] = useState<string | null>(null)
  const defaults = useMemo(() => toForm(settings), [settings])

  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    watch,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: defaults })

  useEffect(() => {
    reset(defaults)
  }, [defaults, reset])

  const ttsRate = watch('ttsRate')
  const retention = watch('retentionPercent')

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      const saved = await update.mutateAsync({
        dailyNewCards: values.dailyNewCards,
        dailyReviewLimit: values.dailyReviewLimit,
        desiredRetention: Math.round(values.retentionPercent) / 100,
        ttsRate: Math.round(values.ttsRate * 100) / 100,
        autoPlayAudio: values.autoPlayAudio,
      })
      reset(toForm(saved))
      toast.success('Đã lưu cài đặt học tập')
    } catch (err) {
      const parsed = parseApiError(err)
      let attached = false
      for (const [field, messages] of Object.entries(parsed.fieldErrors ?? {})) {
        const formField = SERVER_TO_FORM[field]
        if (formField && messages[0]) {
          setError(formField, { type: 'server', message: messages[0] })
          attached = true
        }
      }
      if (!attached) setFormError(parsed.message)
    }
  })

  return (
    <Box component="form" onSubmit={onSubmit} noValidate>
      <Stack spacing={3} sx={{ maxWidth: 560 }}>
        {formError && <Alert severity="error">{formError}</Alert>}
        {settings.isDefault && !isDirty && (
          <Alert severity="info" sx={{ py: 0.5 }}>
            Đang dùng cài đặt mặc định. Người mới bắt đầu nên giữ 10 từ mới/ngày vài tuần đầu.
          </Alert>
        )}

        {/* ── Từ mới mỗi ngày ── */}
        <Controller
          name="dailyNewCards"
          control={control}
          render={({ field }) => (
            <Box>
              <Typography id="daily-new-cards-label" gutterBottom>
                {FIELD_LABELS.dailyNewCards}: <strong>{field.value}</strong>
              </Typography>
              <Slider
                aria-labelledby="daily-new-cards-label"
                value={field.value}
                onChange={(_e, v) => field.onChange(Array.isArray(v) ? v[0] : v)}
                onBlur={field.onBlur}
                min={0}
                max={50}
                step={1}
                marks={NEW_CARD_MARKS}
                valueLabelDisplay="auto"
              />
              <FormHelperText error={!!errors.dailyNewCards}>
                {errors.dailyNewCards?.message ?? 'Thẻ mới được đưa vào ôn mỗi ngày (0 = tạm ngừng học từ mới, chỉ ôn thẻ cũ).'}
              </FormHelperText>
            </Box>
          )}
        />

        {/* ── Giới hạn lượt ôn/ngày ── */}
        <TextField
          {...register('dailyReviewLimit', { valueAsNumber: true })}
          label={FIELD_LABELS.dailyReviewLimit}
          type="number"
          fullWidth
          error={!!errors.dailyReviewLimit}
          helperText={errors.dailyReviewLimit?.message ?? 'Từ 10 đến 1000. Thẻ đến hạn vượt giới hạn sẽ chờ sang ngày sau.'}
          slotProps={{ htmlInput: { min: 10, max: 1000, step: 10, inputMode: 'numeric' }, inputLabel: { shrink: true } }}
        />

        {/* ── Độ nhớ mục tiêu ── */}
        <Controller
          name="retentionPercent"
          control={control}
          render={({ field }) => (
            <Box>
              <Typography id="retention-label" gutterBottom>
                {FIELD_LABELS.retentionPercent}: <strong>{retention}%</strong>
              </Typography>
              <Slider
                aria-labelledby="retention-label"
                value={field.value}
                onChange={(_e, v) => field.onChange(Array.isArray(v) ? v[0] : v)}
                onBlur={field.onBlur}
                min={80}
                max={97}
                step={1}
                marks={RETENTION_MARKS}
                valueLabelDisplay="auto"
                valueLabelFormat={(v) => `${v}%`}
              />
              <FormHelperText error={!!errors.retentionPercent}>
                {errors.retentionPercent?.message ?? 'Cao hơn ⇒ ôn dày hơn, nhớ chắc hơn. Chỉ ảnh hưởng các lượt chấm sau.'}
              </FormHelperText>
            </Box>
          )}
        />

        {/* ── Tốc độ đọc + nghe thử theo giá trị ĐANG kéo ── */}
        <Controller
          name="ttsRate"
          control={control}
          render={({ field }) => (
            <Box>
              <Typography id="tts-rate-label" gutterBottom>
                {FIELD_LABELS.ttsRate}: <strong>{vi(field.value, 2)}</strong>
              </Typography>
              <Slider
                aria-labelledby="tts-rate-label"
                value={field.value}
                onChange={(_e, v) => field.onChange(Array.isArray(v) ? v[0] : v)}
                onBlur={field.onBlur}
                min={TTS_RATE_MIN}
                max={TTS_RATE_MAX}
                step={0.05}
                marks={RATE_MARKS}
                valueLabelDisplay="auto"
                valueLabelFormat={(v) => vi(v, 2)}
              />
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mt: 1 }}>
                <SpeakButton text="你好" variant="button" label="Nghe thử" rate={ttsRate} />
                <Typography variant="body2" color="text.secondary">
                  nǐ hǎo
                </Typography>
              </Box>
              <FormHelperText error={!!errors.ttsRate}>
                {errors.ttsRate?.message ?? 'Mới học nên để 0,7–0,8 để nghe rõ đường nét thanh; quen rồi tăng dần lên 1,0.'}
              </FormHelperText>
            </Box>
          )}
        />

        {/* ── Tự đọc khi hiện thẻ ── */}
        <Controller
          name="autoPlayAudio"
          control={control}
          render={({ field }) => (
            <Box>
              <FormControlLabel
                control={<Switch checked={field.value} onChange={(_e, checked) => field.onChange(checked)} onBlur={field.onBlur} />}
                label={FIELD_LABELS.autoPlayAudio}
              />
              <FormHelperText error={!!errors.autoPlayAudio}>
                {errors.autoPlayAudio?.message ??
                  'Trong phiên ôn, thẻ vừa hiện sẽ được đọc ngay. Trên iPhone chỉ đọc được sau khi bạn bấm chấm thẻ trước.'}
              </FormHelperText>
            </Box>
          )}
        />

        <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end', flexWrap: 'wrap' }}>
          <Button color="inherit" onClick={() => reset(defaults)} disabled={!isDirty || isSubmitting}>
            Hoàn tác
          </Button>
          <Button type="submit" variant="contained" loading={isSubmitting} disabled={!isDirty}>
            Lưu
          </Button>
        </Box>
      </Stack>
    </Box>
  )
}

function LearningSettingsInner() {
  const query = useLearningSettings()
  if (query.isError) return <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
  if (query.isLoading || !query.data) {
    return (
      <Stack spacing={3} sx={{ maxWidth: 560 }}>
        <Skeleton variant="rounded" height={64} />
        <Skeleton variant="rounded" height={56} />
        <Skeleton variant="rounded" height={64} />
        <Skeleton variant="rounded" height={96} />
      </Stack>
    )
  }
  return <SettingsForm settings={query.data} />
}

/**
 * Tab "Học tập" của `/ho-so?tab=hoc-tap` (F7.2 §5.3.2): hạn mức từ mới/lượt ôn, độ nhớ mục tiêu, tốc độ đọc
 * (nghe thử theo giá trị đang kéo), tự đọc khi hiện thẻ. Lưu ⇒ `PUT /api/me/learning-settings` ⇒ invalidate
 * `summary`; 400 hiện dưới ô theo `details`.
 */
export function LearningSettingsTab() {
  return (
    <ChineseSpeechProvider>
      <LearningSettingsInner />
    </ChineseSpeechProvider>
  )
}
