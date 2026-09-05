import { describe, expect, it } from 'vitest'
import { buildApiUrl, problemMessage } from './api'

describe('buildApiUrl', () => {
  it('joins base url and path without double slashes', () => {
    expect(buildApiUrl('/api/dashboard/summary', 'http://localhost:8080')).toBe(
      'http://localhost:8080/api/dashboard/summary',
    )
  })

  it('strips trailing slash from base url', () => {
    expect(buildApiUrl('/api/stock', 'http://api.local/')).toBe('http://api.local/api/stock')
  })

  it('uses same-origin when base url empty (nginx proxy)', () => {
    expect(buildApiUrl('/api/dashboard/summary', '')).toBe('/api/dashboard/summary')
  })
})

describe('problemMessage', () => {
  it('prefers ProblemDetails detail', () => {
    expect(problemMessage(400, { detail: 'Kuantitas harus > 0.' })).toBe('Kuantitas harus > 0.')
  })

  it('falls back to status-based message when body is not ProblemDetails', () => {
    expect(problemMessage(502, null)).toBe('Permintaan gagal (HTTP 502).')
  })
})
