#!/usr/bin/env node
// content/chinese/scripts/fetch-wiktionary-hanviet.mjs
//
// Tải tư liệu ĐỐI CHIẾU (không chép thẳng vào dữ liệu) âm Hán Việt từ English Wiktionary cho mọi chữ đang
// dùng trong content/chinese/data/characters/characters.json (+ phồn thể tương ứng), ghi vào
// content/chinese/.raw/wiktionary-hanviet.json (gitignore) để người/agent biên soạn sources/han-viet.json
// đối chiếu thêm bên cạnh Unihan kVietnamese (xem SOURCES.md — cả hai nguồn đều chỉ là tư liệu tham khảo,
// có thể lẫn âm Nôm/dữ liệu nhiễu).
//
// KHÔNG bắt buộc chạy trước build:chinese — build-hsk.mjs không đọc file này, chỉ dùng cho việc biên soạn
// thủ công han-viet.json. Chạy: `yarn --cwd content hanviet:chinese`.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CHINESE_ROOT = path.resolve(__dirname, '..');
const RAW_DIR = path.join(CHINESE_ROOT, '.raw');
const SOURCES_DIR = path.join(CHINESE_ROOT, 'sources');
const CHARACTERS_PATH = path.join(CHINESE_ROOT, 'data', 'characters', 'characters.json');
const LOCK_PATH = path.join(SOURCES_DIR, 'sources.lock.json');

const BATCH_SIZE = 50;
const DELAY_MS = 1500;
// Header HTTP chỉ nhận ByteString (Latin-1) — không dùng tiếng Việt có dấu ở đây.
const USER_AGENT = 'AntFarm-content/0.1 (contact via github.com repo; AntFarm Chinese learning project)';

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function collectTitles() {
  if (!fs.existsSync(CHARACTERS_PATH)) {
    throw new Error(`Không thấy ${path.relative(CHINESE_ROOT, CHARACTERS_PATH)} — chạy build:chinese trước.`);
  }
  const doc = JSON.parse(fs.readFileSync(CHARACTERS_PATH, 'utf8'));
  const titles = new Set();
  for (const c of doc.characters) {
    titles.add(c.hanzi);
    for (const t of c.traditionalVariants ?? []) titles.add(t);
  }
  return [...titles];
}

function chunk(arr, size) {
  const out = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

/** Trích đoạn ==Vietnamese== từ wikitext (nếu có) — dừng ở heading cấp 2 (==...==) tiếp theo. */
function extractVietnameseSection(wikitext) {
  const idx = wikitext.indexOf('==Vietnamese==');
  if (idx === -1) return null;
  const rest = wikitext.slice(idx + '==Vietnamese=='.length);
  const nextHeading = rest.search(/\n==[^=]/);
  return nextHeading === -1 ? rest : rest.slice(0, nextHeading);
}

/** Tách tham số 'key=' trong template {{...}} MediaWiki, trả về mảng giá trị đã tách bởi ','. */
function extractTemplateParam(section, key) {
  const re = new RegExp(`\\|\\s*${key}\\s*=\\s*([^|}]+)`, 'g');
  const values = [];
  let m;
  while ((m = re.exec(section))) {
    const raw = m[1].trim();
    for (const part of raw.split(',')) {
      const cleaned = part
        .replace(/\[\[|\]\]/g, '')
        .replace(/-[a-z]+$/i, '') // bỏ hậu tố nguồn sau '-' (vd 'hảo-lea')
        .trim();
      if (cleaned) values.push(cleaned);
    }
  }
  return values;
}

async function fetchBatch(titles) {
  const url = new URL('https://en.wiktionary.org/w/api.php');
  url.searchParams.set('action', 'query');
  url.searchParams.set('prop', 'revisions');
  url.searchParams.set('rvprop', 'content');
  url.searchParams.set('rvslots', 'main');
  url.searchParams.set('format', 'json');
  url.searchParams.set('formatversion', '2');
  url.searchParams.set('titles', titles.join('|'));

  const res = await fetch(url, { headers: { 'User-Agent': USER_AGENT } });
  if (!res.ok) throw new Error(`HTTP ${res.status} khi gọi Wiktionary API`);
  const data = await res.json();
  const pages = data?.query?.pages ?? [];
  const out = {};
  for (const page of pages) {
    if (page.missing || !page.revisions?.length) continue;
    const wikitext = page.revisions[0].slots.main.content;
    const section = extractVietnameseSection(wikitext);
    if (!section) continue;
    const hanviet = extractTemplateParam(section, 'hanviet');
    const reading = extractTemplateParam(section, 'reading');
    if (hanviet.length === 0 && reading.length === 0) continue;
    out[page.title] = { hanviet, reading };
  }
  return out;
}

async function main() {
  const titles = collectTitles();
  console.log(`Tổng ${titles.length} tiêu đề (chữ giản thể + phồn thể) cần tra Wiktionary.`);
  const batches = chunk(titles, BATCH_SIZE);
  const result = {};
  for (const [i, batch] of batches.entries()) {
    console.log(`Lô ${i + 1}/${batches.length} (${batch.length} tiêu đề)...`);
    try {
      const partial = await fetchBatch(batch);
      Object.assign(result, partial);
    } catch (err) {
      console.error(`✗ Lỗi lô ${i + 1}: ${err.message} — bỏ qua lô này, tiếp tục.`);
    }
    if (i < batches.length - 1) await sleep(DELAY_MS);
  }

  fs.mkdirSync(RAW_DIR, { recursive: true });
  const outPath = path.join(RAW_DIR, 'wiktionary-hanviet.json');
  fs.writeFileSync(outPath, `${JSON.stringify(result, null, 2)}\n`);
  console.log(`✓ Đã ghi ${Object.keys(result).length}/${titles.length} mục vào ${path.relative(CHINESE_ROOT, outPath)}`);

  if (fs.existsSync(LOCK_PATH)) {
    const lock = JSON.parse(fs.readFileSync(LOCK_PATH, 'utf8'));
    lock.wiktionary = { fetchedAt: new Date().toISOString(), titles: titles.length };
    fs.writeFileSync(LOCK_PATH, `${JSON.stringify(lock, null, 2)}\n`);
    console.log('✓ Đã cập nhật sources.lock.json (wiktionary.fetchedAt/titles)');
  }

  console.log('\nLưu ý: đây CHỈ là tư liệu đối chiếu (như Unihan kVietnamese) — có thể lẫn dữ liệu không');
  console.log('chuẩn/đóng góp cộng đồng chưa kiểm chứng. Đối chiếu thủ công trước khi sửa sources/han-viet.json.');
}

main().catch((err) => {
  console.error(`✗ Lỗi: ${err.message}`);
  process.exitCode = 1;
});
