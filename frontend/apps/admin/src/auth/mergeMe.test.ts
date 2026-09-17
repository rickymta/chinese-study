import { describe, expect, it } from 'vitest'
import { ApiError } from '@af/api'
import { hasUnavailableService, mergeServiceResults, normalizeServiceMe, type ServiceMeResult } from './mergeMe'

const ok = (value: unknown): PromiseSettledResult<unknown> => ({ status: 'fulfilled', value })
const fail = (reason: unknown): PromiseSettledResult<unknown> => ({ status: 'rejected', reason })

const cmsMe = (permissions: string[]) => ({
  id: '0192',
  email: 'quan@vidu.com',
  displayName: 'Quân',
  roles: ['admin'],
  permissions,
})

const CMS = { code: 'cms', label: 'CMS' }
const CHINESE = { code: 'chinese', label: 'Tiếng Trung', adminPermissions: ['users.manage', 'content.manage'] }

describe('mergeServiceResults', () => {
  it('gắn tiền tố service cho mọi quyền và giữ thứ tự', () => {
    const results: ServiceMeResult[] = [
      { ...CMS, outcome: ok(cmsMe(['site.manage', 'users.manage'])) },
      { ...CHINESE, outcome: ok({ permissions: ['content.manage', 'users.manage'] }) },
    ]
    const me = mergeServiceResults(results)
    expect(me.permissions).toEqual(['cms:site.manage', 'cms:users.manage', 'chinese:content.manage', 'chinese:users.manage'])
    expect(me.services.cms?.status).toBe('ok')
    expect(me.services.chinese?.status).toBe('ok')
    expect(hasUnavailableService(me)).toBe(false)
  })

  it('bỏ quyền không thuộc adminPermissions của service ngôn ngữ (study.use không phải quyền admin)', () => {
    const me = mergeServiceResults([
      { ...CMS, outcome: ok(cmsMe([])) },
      { ...CHINESE, outcome: ok({ permissions: ['study.use'] }) },
    ])
    expect(me.permissions).toEqual([])
    // Cả hai service đều trả lời ⇒ 0 quyền là thật ⇒ RequireAuth đưa /403.
    expect(hasUnavailableService(me)).toBe(false)
  })

  it('service không phản hồi (5xx/mạng) ⇒ unavailable, quyền các service khác vẫn còn, không ném', () => {
    const me = mergeServiceResults([
      { ...CMS, outcome: ok(cmsMe(['users.manage'])) },
      { ...CHINESE, outcome: fail(new ApiError('Dịch vụ chưa sẵn sàng', { status: 502 })) },
    ])
    expect(me.permissions).toEqual(['cms:users.manage'])
    expect(me.services.chinese?.status).toBe('unavailable')
    expect(hasUnavailableService(me)).toBe(true)
  })

  it('403 là service đã trả lời và từ chối ⇒ ok với 0 quyền, KHÔNG làm hasUnavailableService thành true', () => {
    const me = mergeServiceResults([
      { ...CMS, outcome: fail(new ApiError('Không có quyền', { status: 403 })) },
      { ...CHINESE, outcome: ok({ permissions: ['study.use'] }) },
    ])
    expect(me.services.cms?.status).toBe('ok')
    expect(me.permissions).toEqual([])
    expect(hasUnavailableService(me)).toBe(false)
  })

  it('404 (service chưa có route) cũng chỉ là unavailable', () => {
    const me = mergeServiceResults([
      { ...CMS, outcome: fail(new ApiError('Không tìm thấy', { status: 404 })) },
      { ...CHINESE, outcome: ok({ permissions: ['users.manage'] }) },
    ])
    expect(me.services.cms?.status).toBe('unavailable')
    expect(me.permissions).toEqual(['chinese:users.manage'])
  })

  it('401 ở bất kỳ service nào ⇒ ném lại nguyên lỗi', () => {
    const err = new ApiError('Cần đăng nhập', { status: 401 })
    expect(() =>
      mergeServiceResults([
        { ...CMS, outcome: ok(cmsMe(['users.manage'])) },
        { ...CHINESE, outcome: fail(err) },
      ]),
    ).toThrow(err)
  })

  it('tất cả service đều unavailable ⇒ ném lỗi có tên từng service', () => {
    expect(() =>
      mergeServiceResults([
        { ...CMS, outcome: fail(new ApiError('Không kết nối được', { status: undefined })) },
        { ...CHINESE, outcome: fail(new Error('timeout')) },
      ]),
    ).toThrow(/CMS: Không kết nối được.*Tiếng Trung: timeout/)
  })

  it('không nhân đôi quyền trùng và chịu được thân phản hồi thiếu trường', () => {
    const me = mergeServiceResults([
      { ...CMS, outcome: ok({ permissions: ['users.manage', 'users.manage', 42, null] }) },
      { ...CHINESE, outcome: ok(undefined) },
    ])
    expect(me.permissions).toEqual(['cms:users.manage'])
    expect(me.services.chinese).toEqual({ status: 'ok', me: normalizeServiceMe(undefined) })
  })
})

describe('normalizeServiceMe', () => {
  it('bù giá trị an toàn khi thiếu/sai kiểu', () => {
    expect(normalizeServiceMe({ id: 1, email: 'a@b.c', roles: 'admin', permissions: ['x', 2] })).toEqual({
      id: '',
      email: 'a@b.c',
      displayName: '',
      roles: [],
      permissions: ['x'],
    })
  })
})
