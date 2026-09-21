// @af/chinese-kit — thành phần hiển thị tiếng Trung dùng chung giữa app học viên (`apps/chinese`) và module
// quản trị tiếng Trung trong admin (W13). Import thẳng TS source (workspace symlink, không build/dist).
//
// Ranh giới (quyết định W12): kit chỉ chứa thứ KHÔNG gọi API — tiện ích pinyin thuần, kiểu dữ liệu bài học/từ điển,
// thành phần hiển thị (Hanzi/Pinyin/khối nội dung/quiz/nút nghe) và ngữ cảnh đọc TTS. Hook gọi máy chủ
// (`useTtsRate`, `useLearningSettings`, query key...) ở lại app tiêu thụ và tiêm vào kit qua props.

// ─── Pinyin (số thanh ⇄ dấu thanh, biến điệu) ───
export {
  TONE_MARKS,
  normalizeNumbered,
  parseSyllable,
  toneOf,
  stripTone,
  syllableToMarked,
  splitPunctuation,
  stripPunctuation,
  numberedToMarked,
  markedToNumbered,
  displaySyllableKey,
  sandhiHints,
} from './pinyin/pinyin'
export type { Tone, ParsedSyllable, NumberedToMarkedOptions, SandhiHint } from './pinyin/pinyin'

// ─── Chữ Hán / pinyin hiển thị ───
export { Hanzi } from './components/Hanzi'
export type { HanziProps, HanziSize } from './components/Hanzi'
export { Pinyin } from './components/Pinyin'
export type { PinyinProps } from './components/Pinyin'

// ─── Đọc tiếng Trung (Web Speech API `zh-CN` qua `useSpeech` của @af/ui) ───
export { TTS_RATE_DEFAULT, TTS_RATE_MIN, TTS_RATE_MAX, clampTtsRate } from './speech/ttsRate'
export { ChineseSpeechProvider, useChineseSpeech } from './speech/ChineseSpeech'
export type { ChineseSpeechProviderProps, ChineseSpeechValue } from './speech/ChineseSpeech'
export { SpeakButton, NO_VOICE_TOOLTIP } from './speech/SpeakButton'
export type { SpeakButtonProps } from './speech/SpeakButton'

// ─── Bài học: kiểu dữ liệu + cây khối nội dung + quiz ───
export type {
  LessonReviewStatus,
  LessonProgressStatus,
  LessonProgress,
  LessonSummary,
  LessonListResponse,
  ZhLine,
  TextBlockPayload,
  DialogueBlockPayload,
  GrammarBlockPayload,
  TipVariant,
  TipBlockPayload,
  LessonBlock,
  LessonBlockType,
  GlossaryEntry,
  LessonWord,
  QuizQuestionType,
  QuizPromptLang,
  QuizOptionLang,
  QuizOption,
  QuizQuestion,
  LessonDetail,
  QuizAnswer,
  SubmitQuizRequest,
  QuizQuestionResult,
  QuizResult,
  QuizAttemptSummary,
  QuizAttemptsResponse,
} from './lesson/types'
export { PASS_THRESHOLD_PERCENT } from './lesson/types'
export { parseInlineZh, stripInlineZh } from './lesson/lib/inlineZh'
export type { InlineZhSegment } from './lesson/lib/inlineZh'
export { LessonDisplayProvider, useLessonDisplay } from './lesson/LessonDisplayContext'
export type { LessonDisplayValue } from './lesson/LessonDisplayContext'
export { InlineZh } from './lesson/InlineZh'
export type { InlineZhProps } from './lesson/InlineZh'
export { ZhLineRow } from './lesson/ZhLineRow'
export type { ZhLineRowProps } from './lesson/ZhLineRow'
export { GlossaryList } from './lesson/GlossaryList'
export { useSpeakQueue } from './lesson/useSpeakQueue'
export type { SpeakQueue } from './lesson/useSpeakQueue'
export { LessonContent } from './lesson/LessonContent'
export type { LessonContentProps } from './lesson/LessonContent'
export { TextBlock } from './lesson/blocks/TextBlock'
export { DialogueBlock } from './lesson/blocks/DialogueBlock'
export { GrammarBlock } from './lesson/blocks/GrammarBlock'
export { TipBlock } from './lesson/blocks/TipBlock'
export { OptionText, QuestionPrompt } from './lesson/quiz/QuestionParts'

// ─── Từ điển: kiểu dữ liệu + nguồn học liệu + chip trạng thái nghĩa ───
export type {
  MeaningViStatus,
  MeaningViSource,
  HanVietStatus,
  MatchKind,
  SearchParams,
  WordSummary,
  SearchResponse,
  WordCharacter,
  WordSrsInfo,
  WordDetail,
  CharacterDetail,
} from './dictionary/types'
export { SOURCES, sourceInfo, meaningViSourceLabel } from './dictionary/sources'
export type { SourceInfo } from './dictionary/sources'
export { MeaningStatusChip, MACHINE_MEANING_TOOLTIP } from './dictionary/MeaningStatusChip'
export type { MeaningStatusChipProps } from './dictionary/MeaningStatusChip'
