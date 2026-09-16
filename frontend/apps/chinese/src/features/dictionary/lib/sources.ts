/**
 * Khoá nguồn học liệu (`sources[]` trong API, §5.4.1 hợp đồng F6/F7) → nhãn hiển thị + giấy phép + liên kết.
 * Dùng cho dòng ghi công cuối trang (`SourceAttribution`) — nghĩa vụ CC BY-SA 4.0 / MIT / Unicode License.
 */
export interface SourceInfo {
  label: string
  license: string
  url: string
  /** Trang giấy phép (mở khi bấm vào tên giấy phép). */
  licenseUrl: string
}

const CC_BY_SA = { license: 'CC BY-SA 4.0', licenseUrl: 'https://creativecommons.org/licenses/by-sa/4.0/' }
const MIT = { license: 'MIT', licenseUrl: 'https://opensource.org/license/mit' }

export const SOURCES: Readonly<Record<string, SourceInfo>> = {
  'hsk30-official': { label: 'HSK 3.0 (danh sách chính thức)', url: 'https://github.com/elkmovie/hsk30', ...MIT },
  'complete-hsk-vocabulary': {
    label: 'complete-hsk-vocabulary',
    url: 'https://github.com/drkameleon/complete-hsk-vocabulary',
    ...MIT,
  },
  'cc-cedict': { label: 'CC-CEDICT (MDBG)', url: 'https://www.mdbg.net/chinese/dictionary?page=cc-cedict', ...CC_BY_SA },
  cvdict: { label: 'CVDICT – Phong Phan', url: 'https://github.com/ph0ngp/CVDICT', ...CC_BY_SA },
  unihan: {
    label: 'Unicode Unihan',
    url: 'https://www.unicode.org/charts/unihan.html',
    license: 'Unicode License v3',
    licenseUrl: 'https://www.unicode.org/license.txt',
  },
  wiktionary: { label: 'Wiktionary contributors', url: 'https://en.wiktionary.org/', ...CC_BY_SA },
  'han-viet-curated': { label: 'Hán Việt do AntFarm biên soạn', url: 'https://github.com/', ...CC_BY_SA },
  machine: { label: 'Dịch máy (AntFarm)', url: 'https://github.com/', ...CC_BY_SA },
  'hsk1-overrides': { label: 'Chọn cách đọc (AntFarm)', url: 'https://github.com/', ...CC_BY_SA },
}

/** Thông tin nguồn theo khoá; khoá lạ (nguồn thêm sau) ⇒ `null` để nơi gọi bỏ qua. */
export function sourceInfo(key: string): SourceInfo | null {
  return SOURCES[key] ?? null
}

/** Nhãn nguồn nghĩa Việt (`meaningViSource`) cho chú thích ở chi tiết từ. */
export function meaningViSourceLabel(source: string | null | undefined): string | null {
  switch (source) {
    case 'cvdict':
      return 'Dịch từ CVDICT'
    case 'machine':
      return 'Dịch máy'
    case 'manual':
      return 'Biên soạn tay'
    default:
      return null
  }
}
