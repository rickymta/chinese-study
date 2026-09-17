#!/usr/bin/env node
// content/chinese/scripts/build-hanzi-data.mjs
//
// Đóng gói TẬP CON dữ liệu nét chữ `hanzi-writer-data@2.0.1` — chỉ những chữ có trong
// `content/chinese/data/characters/characters.json` — vào `frontend/apps/chinese/public/hanzi-data/`.
// Chép NGUYÊN TỪNG BYTE (không minify, không sửa nội dung), chỉ đổi tên file theo mã Unicode hex
// thường (vd 爱 → 7231.json), kèm `ARPHICPL.TXT` nguyên văn + `NOTICE.md` giải thích thay đổi.
// Xem R-W1 + §5.4.4 của docs/agent-workflow/2026-09-17-antfarm-f8-f11-chi-tiet.md.
//
// Chạy: `yarn --cwd content build:hanzi-data:chinese` (tương đương
// `node chinese/scripts/build-hanzi-data.mjs` chạy từ thư mục content/).

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CONTENT_ROOT = path.resolve(__dirname, '..', '..'); // content/
const CHINESE_ROOT = path.join(CONTENT_ROOT, 'chinese'); // content/chinese/
const REPO_ROOT = path.resolve(CONTENT_ROOT, '..'); // gốc repo

const CHARACTERS_PATH = path.join(CHINESE_ROOT, 'data', 'characters', 'characters.json');
const SRC_PKG_DIR = path.join(CONTENT_ROOT, 'node_modules', 'hanzi-writer-data');
const SRC_PKG_JSON = path.join(SRC_PKG_DIR, 'package.json');
const SRC_ARPHICPL = path.join(SRC_PKG_DIR, 'ARPHICPL.TXT');

const DEST_DIR = path.join(REPO_ROOT, 'frontend', 'apps', 'chinese', 'public', 'hanzi-data');
const DEST_ARPHICPL_LICENSES = path.join(CHINESE_ROOT, 'LICENSES', 'ARPHICPL.TXT');

const EXPECTED_VERSION = '2.0.1';

function die(message) {
  console.error(`✗ ${message}`);
  process.exit(1);
}

function codePointHex(char) {
  return char.codePointAt(0).toString(16);
}

// --- Bước 1: kiểm phiên bản hanzi-writer-data ---
if (!fs.existsSync(SRC_PKG_JSON)) {
  die(
    `Không tìm thấy ${path.relative(REPO_ROOT, SRC_PKG_DIR)} — chạy \`yarn --cwd content install\` trước.`
  );
}
const srcPkg = JSON.parse(fs.readFileSync(SRC_PKG_JSON, 'utf8'));
if (srcPkg.version !== EXPECTED_VERSION) {
  die(
    `hanzi-writer-data phiên bản "${srcPkg.version}" khác kỳ vọng "${EXPECTED_VERSION}" — ` +
      `cập nhật EXPECTED_VERSION và đối chiếu lại giấy phép/README trước khi build.`
  );
}

if (!fs.existsSync(CHARACTERS_PATH)) {
  die(`Không tìm thấy ${path.relative(REPO_ROOT, CHARACTERS_PATH)}.`);
}

// --- Bước 2: đọc danh sách chữ (duy nhất, sắp theo code point) ---
const charactersDoc = JSON.parse(fs.readFileSync(CHARACTERS_PATH, 'utf8'));
const chars = [...new Set(charactersDoc.characters.map((c) => c.hanzi))].sort((a, b) =>
  a.codePointAt(0) - b.codePointAt(0)
);

// --- Bước 3: dọn thư mục đích (giữ nguyên thư mục, xoá mọi *.json cũ) ---
fs.mkdirSync(DEST_DIR, { recursive: true });
for (const entry of fs.readdirSync(DEST_DIR)) {
  if (entry.endsWith('.json')) fs.unlinkSync(path.join(DEST_DIR, entry));
}

// --- Bước 4: chép từng chữ, byte-for-byte, đổi tên theo mã Unicode hex thường ---
const copied = [];
const missing = [];
for (const ch of chars) {
  const srcPath = path.join(SRC_PKG_DIR, `${ch}.json`);
  if (!fs.existsSync(srcPath)) {
    missing.push(ch);
    continue;
  }
  const hex = codePointHex(ch);
  fs.copyFileSync(srcPath, path.join(DEST_DIR, `${hex}.json`));
  copied.push(ch);
}

// --- Bước 5: chép ARPHICPL.TXT nguyên văn vào thư mục đích VÀ content/chinese/LICENSES/ ---
if (!fs.existsSync(SRC_ARPHICPL)) {
  die(`Không tìm thấy ${path.relative(REPO_ROOT, SRC_ARPHICPL)}.`);
}
fs.copyFileSync(SRC_ARPHICPL, path.join(DEST_DIR, 'ARPHICPL.TXT'));
fs.copyFileSync(SRC_ARPHICPL, DEST_ARPHICPL_LICENSES);

// --- Bước 6: ghi index.json (khoá theo thứ tự cố định, không mốc thời gian sinh) ---
const index = {
  dataset: 'hanzi-writer-data',
  version: EXPECTED_VERSION,
  license: 'Arphic Public License (ARPHICPL.TXT)',
  naming: 'codepoint-hex-lower',
  count: copied.length,
  characters: copied,
  missing,
};
fs.writeFileSync(path.join(DEST_DIR, 'index.json'), `${JSON.stringify(index, null, 2)}\n`, 'utf8');

// --- Bước 7: ghi NOTICE.md (nội dung cố định, tiếng Việt + tiếng Anh) ---
const notice = `# NOTICE — hanzi-writer-data (tập con)

## Tiếng Việt

Nguồn: \`hanzi-writer-data\` phiên bản 2.0.1 (npm, https://github.com/chanind/hanzi-writer-data), dữ liệu
lấy từ dự án Make Me a Hanzi (https://github.com/skishore/makemeahanzi), trích xuất từ phông chữ
Arphic PL KaitiM GB / UKai — Copyright 1999 Arphic Technology Co., Ltd.; Copyright 2016 Shaunak Kishore.
Phân phối lại theo Arphic Public License, toàn văn ở file \`ARPHICPL.TXT\` cạnh thư mục này.

**Thay đổi của AntFarm:** thư mục này CHỈ chứa tập con các chữ có trong
\`content/chinese/data/characters/characters.json\` (từ vựng HSK 3.0 cấp 1) và các file đã được ĐỔI TÊN từ
tên gốc là ký tự Hán (vd \`爱.json\`) sang mã điểm Unicode dạng thập lục phân chữ thường (vd \`7231.json\`).
Nội dung TỪNG FILE giữ nguyên từng byte, không sửa, không minify. Ngày sinh của thư mục này ghi tay khi
đổi phiên bản nguồn (không tự động sinh, để lần build lại cho ra kết quả giống hệt — không tạo diff).

## English

Source: \`hanzi-writer-data\` version 2.0.1 (npm, https://github.com/chanind/hanzi-writer-data), data
derived from the Make Me a Hanzi project (https://github.com/skishore/makemeahanzi), extracted from the
Arphic PL KaitiM GB / UKai font — Copyright 1999 Arphic Technology Co., Ltd.; Copyright 2016 Shaunak
Kishore. Redistributed under the Arphic Public License, full text in \`ARPHICPL.TXT\` next to this folder.

**AntFarm's changes:** this folder ONLY contains the subset of characters present in
\`content/chinese/data/characters/characters.json\` (HSK 3.0 level 1 vocabulary), and files have been
RENAMED from their original name (the Hanzi character itself, e.g. \`爱.json\`) to the lowercase hexadecimal
Unicode code point (e.g. \`7231.json\`). The content of each file is byte-for-byte identical to the
original, not modified or minified. The generation date of this folder is recorded by hand whenever the
source version changes (not auto-generated, so re-running the build produces an identical result — no
diff).
`;
fs.writeFileSync(path.join(DEST_DIR, 'NOTICE.md'), notice, 'utf8');

// --- Bước 8: in báo cáo ---
let totalBytes = 0;
for (const entry of fs.readdirSync(DEST_DIR)) {
  totalBytes += fs.statSync(path.join(DEST_DIR, entry)).size;
}
console.log(`✓ Đã chép ${copied.length}/${chars.length} chữ vào ${path.relative(REPO_ROOT, DEST_DIR)}`);
if (missing.length > 0) {
  console.log(`! Thiếu dữ liệu nét cho ${missing.length} chữ: ${missing.join(', ')}`);
}
console.log(`  Tổng dung lượng thư mục đích (chưa nén): ${(totalBytes / 1024).toFixed(1)} KB`);
