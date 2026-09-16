#!/usr/bin/env node
// content/chinese/scripts/fetch-sources.mjs
//
// Tải các nguồn ngoài ghim commit/sha256 trong sources/sources.lock.json về content/chinese/.raw/
// (gitignore). Bỏ qua tải lại nếu file đã có VÀ hash khớp. Hash lệch ⇒ dừng, báo lỗi rõ ràng
// (không tự ghi đè lock — dùng --update-lock khi CHỦ ĐỘNG nâng phiên bản nguồn).
//
// Chạy: `yarn --cwd content fetch:chinese` (tương đương `node chinese/scripts/fetch-sources.mjs`).

import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { unzipSync } from 'fflate';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CHINESE_ROOT = path.resolve(__dirname, '..');
const RAW_DIR = path.join(CHINESE_ROOT, '.raw');
const LOCK_PATH = path.join(CHINESE_ROOT, 'sources', 'sources.lock.json');

const UPDATE_LOCK = process.argv.includes('--update-lock');

function sha256Hex(buf) {
  return createHash('sha256').update(buf).digest('hex');
}

async function download(url) {
  const res = await fetch(url);
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} khi tải ${url}`);
  }
  return Buffer.from(await res.arrayBuffer());
}

async function main() {
  const lock = JSON.parse(fs.readFileSync(LOCK_PATH, 'utf8'));
  fs.mkdirSync(RAW_DIR, { recursive: true });

  let changed = false;

  for (const [key, meta] of Object.entries(lock.files)) {
    const destPath = path.join(RAW_DIR, meta.raw);
    let buf = null;
    if (fs.existsSync(destPath)) {
      buf = fs.readFileSync(destPath);
      const hash = sha256Hex(buf);
      if (hash === meta.sha256) {
        console.log(`= ${key}: đã có, hash khớp (${meta.raw})`);
        continue;
      }
      if (!UPDATE_LOCK) {
        console.error(
          `✗ ${key}: file đã tải (${meta.raw}) có sha256 ${hash}, KHÁC lock ${meta.sha256}. ` +
            `Xoá ${path.relative(CHINESE_ROOT, destPath)} để tải lại, hoặc chạy với --update-lock nếu chủ động nâng phiên bản nguồn.`,
        );
        process.exitCode = 1;
        return;
      }
    }
    console.log(`↓ ${key}: tải ${meta.url}`);
    buf = await download(meta.url);
    const hash = sha256Hex(buf);
    if (!UPDATE_LOCK && hash !== meta.sha256) {
      console.error(`✗ ${key}: sha256 tải về (${hash}) KHÁC lock (${meta.sha256}) — dừng, không ghi file.`);
      process.exitCode = 1;
      return;
    }
    fs.writeFileSync(destPath, buf);
    console.log(`  ✓ đã lưu ${path.relative(CHINESE_ROOT, destPath)} (${buf.length} byte, sha256 ${hash})`);
    if (UPDATE_LOCK && hash !== meta.sha256) {
      meta.sha256 = hash;
      meta.bytes = buf.length;
      changed = true;
    }
  }

  // Giải nén 3 file Unihan cần dùng + CJKRadicals đã tải riêng (không cần giải nén).
  const unihanZipPath = path.join(RAW_DIR, lock.files.unihan.raw);
  const unihanOutDir = path.join(RAW_DIR, 'unihan');
  const NEEDED = ['Unihan_Readings.txt', 'Unihan_Variants.txt', 'Unihan_IRGSources.txt'];
  fs.mkdirSync(unihanOutDir, { recursive: true });
  const missing = NEEDED.filter((f) => !fs.existsSync(path.join(unihanOutDir, f)));
  if (missing.length > 0) {
    console.log(`↓ Giải nén Unihan.zip → .raw/unihan/ (${NEEDED.join(', ')})`);
    const zipBuf = fs.readFileSync(unihanZipPath);
    const entries = unzipSync(new Uint8Array(zipBuf), {
      filter: (file) => NEEDED.includes(path.basename(file.name)),
    });
    for (const [name, data] of Object.entries(entries)) {
      const outPath = path.join(unihanOutDir, path.basename(name));
      fs.writeFileSync(outPath, Buffer.from(data));
      console.log(`  ✓ ${path.basename(name)}`);
    }
  } else {
    console.log('= Unihan: đã giải nén sẵn ở .raw/unihan/');
  }

  if (UPDATE_LOCK && changed) {
    fs.writeFileSync(LOCK_PATH, `${JSON.stringify(lock, null, 2)}\n`);
    console.log('✓ Đã cập nhật sources.lock.json (--update-lock)');
  }

  console.log('\nHoàn tất tải nguồn.');
}

main().catch((err) => {
  console.error(`✗ Lỗi: ${err.message}`);
  process.exitCode = 1;
});
