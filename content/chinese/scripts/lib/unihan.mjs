// content/chinese/scripts/lib/unihan.mjs
//
// Đọc dữ liệu Unicode Unihan 18.0.0 (định dạng tab-separated "U+XXXX\tkThuocTinh\tGiaTri") và CJKRadicals.txt
// (định dạng "SO; MA_BO_THU_HEX; MA_CHU_CJK_HEX"). Dùng bởi build-hsk.mjs (F6.1) — không phân phối lại nội
// dung Unihan, chỉ đọc cục bộ từ content/chinese/.raw/unihan/ (gitignore) lúc build.

import fs from 'node:fs';

/**
 * Đọc một hoặc nhiều file Unihan_*.txt, gộp thành Map<chữ, { [thuocTinh]: giaTriTho }>.
 * @param {string[]} filePaths
 * @returns {Map<string, Record<string, string>>}
 */
export function loadUnihanProperties(filePaths) {
  const map = new Map();
  for (const filePath of filePaths) {
    if (!fs.existsSync(filePath)) continue;
    const content = fs.readFileSync(filePath, 'utf8');
    for (const line of content.split('\n')) {
      if (!line || line.startsWith('#')) continue;
      const tabIdx1 = line.indexOf('\t');
      if (tabIdx1 === -1) continue;
      const tabIdx2 = line.indexOf('\t', tabIdx1 + 1);
      if (tabIdx2 === -1) continue;
      const cpField = line.slice(0, tabIdx1);
      const prop = line.slice(tabIdx1 + 1, tabIdx2);
      const value = line.slice(tabIdx2 + 1);
      if (!cpField.startsWith('U+')) continue;
      const cp = Number.parseInt(cpField.slice(2), 16);
      const ch = String.fromCodePoint(cp);
      let entry = map.get(ch);
      if (!entry) {
        entry = {};
        map.set(ch, entry);
      }
      entry[prop] = value;
    }
  }
  return map;
}

/**
 * Đọc CJKRadicals.txt → Map<"soBoThu" (vd "38", "120'"), { radicalChar, cjkChar }>.
 * `cjkChar` là chữ thuộc khối CJK Unified Ideographs dùng để hiển thị (cột 3).
 * @param {string} filePath
 * @returns {Map<string, { radicalChar: string, cjkChar: string }>}
 */
export function parseCjkRadicals(filePath) {
  const map = new Map();
  if (!fs.existsSync(filePath)) return map;
  const content = fs.readFileSync(filePath, 'utf8');
  for (const rawLine of content.split('\n')) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const parts = line.split(';').map((s) => s.trim());
    if (parts.length !== 3) continue;
    const [num, radicalHex, cjkHex] = parts;
    // Một số radical giản thể (vd 182'', 208'', 212''') không có mã bộ thủ Khang Hy riêng — cột 2 rỗng.
    const radicalCp = radicalHex ? Number.parseInt(radicalHex, 16) : NaN;
    const radicalChar = Number.isFinite(radicalCp) ? String.fromCodePoint(radicalCp) : null;
    const cjkCp = Number.parseInt(cjkHex, 16);
    if (!Number.isFinite(cjkCp)) continue;
    const cjkChar = String.fromCodePoint(cjkCp);
    map.set(num, { radicalChar, cjkChar });
  }
  return map;
}

/**
 * Tách `kRSUnicode` (vd "38.3", "120'.5 12.2") → { radicalKey: "38", strokesAfterRadical: 3 } của mục ĐẦU TIÊN.
 * `radicalKey` giữ nguyên hậu tố `'` (giản thể) để tra CJKRadicals.
 * @param {string} kRSUnicode
 */
export function parseFirstRsUnicode(kRSUnicode) {
  const first = kRSUnicode.trim().split(/\s+/)[0];
  const m = first.match(/^(\d+'{0,3})\.(-?\d+)$/);
  if (!m) return null;
  return { radicalKey: m[1], strokesAfterRadical: Number.parseInt(m[2], 10) };
}
