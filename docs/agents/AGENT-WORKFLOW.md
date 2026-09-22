# Quy trình giao tiếp đa Agent — AntFarm (repo chinese-study)

> Định nghĩa cách một yêu cầu từ người dùng được xử lý qua chuỗi agent chuyên trách.
> Các agent khai báo dưới dạng **Claude Code subagent** tại `.claude/agents/*.md`.
> Chuyển thể từ quy trình của dự án MedDental (`mdt-re-construct`) ngày 2026-09-16.

## Sơ đồ luồng

```
Người dùng
   │
   ▼
┌─────────────────────────────────────────────────────────────┐
│ ORCHESTRATOR (Opus) — phân loại & điều phối                  │
│  • Câu hỏi/tra cứu (kể cả hỏi tiếng Trung) → tự trả lời       │
│  • Nâng cấp / Tạo mới   → khởi động luồng coding bên dưới     │
└─────────────────────────────────────────────────────────────┘
   │
   ▼
INVESTIGATION (Haiku) ── khảo sát source, HỎI NGAY nếu mơ hồ
   ▼
BUSINESS ANALYSIS (Opus) ── docs/agent-workflow/YYYY-MM-DD-<slug>-hop-dong-thuc-thi.md
   │   + PHÂN RÃ FEATURE (đơn vị commit & test độc lập)
   ▼
╔══════════ VÒNG LẶP: LẦN LƯỢT TỪNG FEATURE ═════════════════╗
║  ┌──────── thực thi song song (phạm vi RIÊNG feature) ────┐ ║
║  │ BACKEND  DATABASE  CONTENT (Sonnet) · FRONTEND (Fable) │ ║
║  │ build+test  tsc -b   migration   học liệu kiểm schema  │ ║
║  └────────────────────────────────────────────────────────┘ ║
║     ▼                                                       ║
║  REVIEW (Opus) ── chưa ổn → trả lại agent thực thi → lặp    ║
║     ▼                                                       ║
║  INTEGRATION (Opus) ── ráp + acceptance + COMMIT LOCAL      ║
║     ▼                                                       ║
║  ⏸ DỪNG → báo người dùng review/test (+ "học thử ngay")    ║
║     └──► feature kế tiếp                                    ║
╚═════════════════════════════════════════════════════════════╝
```

## Bảng agent

| Agent | File | Model | Vai trò |
|---|---|---|---|
| Orchestrator | `.claude/agents/orchestrator.md` | Opus | Phân loại + điều phối + báo cáo |
| Investigation | `.claude/agents/investigation.md` | Haiku | Khảo sát read-only, hỏi sớm |
| Business Analysis | `.claude/agents/business-analysis.md` | Opus | Hợp đồng thực thi + phân rã feature (cả nghiệp vụ sư phạm) |
| Backend Implement | `.claude/agents/backend-implement.md` | Sonnet | .NET 10 DDD 4 lớp + build/test sạch |
| Frontend Implement | `.claude/agents/frontend-implement.md` | **Fable** | React 19 + MUI v9 + `tsc -b` sạch — gọi qua `Agent` luôn truyền `model: "fable"`. **Cũng làm toàn bộ `mobile/` (Flutter/Dart)** — lời giao việc ghi rõ "Flutter trong `mobile/`", cổng kiểm `mobile/tool/ci.sh` |
| Database Implement | `.claude/agents/database-implement.md` | Sonnet | Schema/migration/seed PostgreSQL |
| **Content Implement** | `.claude/agents/content-implement.md` | Sonnet | **Mở rộng**: học liệu HSK/pinyin/bài học, giấy phép nguồn |
| Review | `.claude/agents/review.md` | Opus | Review BE/FE/DB/học liệu vs hợp đồng + convention |
| Integration | `.claude/agents/integration.md` | Opus | End-to-end + acceptance + commit local |

Model khai bằng **alias** (`opus`/`sonnet`/`haiku`) → luôn map sang bản mới nhất.

## Nguyên tắc vận hành

1. **Phiên chính đóng vai Orchestrator**, gọi worker qua công cụ `Agent`.
2. **Chỉ là câu hỏi → trả lời ngay.**
3. **Hỏi sớm hơn là đoán sai.**
4. **Hợp đồng thực thi là điểm tựa chống mất bộ nhớ** — mọi giai đoạn đọc từ `docs/agent-workflow/`.
5. **Song song khi độc lập** — nhưng chỉ trong phạm vi một feature.
6. **Dứt điểm từng feature:** xong → kiểm tra → commit local riêng → dừng cho người dùng review.
7. **Vòng lặp chất lượng:** Review/Integration được trả ngược về agent thực thi.
8. **Build + test sạch, commit local riêng mỗi feature, không push.**
9. **Thêm sub-agent khi cần** (devops/infra, security-review, test/QA...).

## Sản phẩm bàn giao giữa các chặng

- Investigation → tóm tắt hiện trạng + câu hỏi làm rõ.
- Business Analysis → file hợp đồng + thứ tự thực thi.
- BE/FE/DB/Content → danh sách file + kết quả build/test/kiểm dữ liệu + điểm phối hợp.
- Review → PASS / danh sách điểm phải sửa (file:dòng).
- Integration → báo cáo end-to-end + verify + hash commit.
