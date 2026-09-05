import type { Status } from '../lib/data'

const MAP: Record<string, { label: string; css: string }> = {
  Aman: { label: 'Aman', css: 'sm-pill sm-pill-success' },
  Warning: { label: 'Warning', css: 'sm-pill sm-pill-warning' },
  Kritis: { label: 'Kritis', css: 'sm-pill sm-pill-danger' },
}

/// Mirror StatusPill() Blazor: pill status atau "-" netral bila tidak dihitung.
export function StatusPill({ status }: { status: Status | null | undefined }) {
  const entry = status ? MAP[status] : undefined
  if (!entry) {
    return <span className="sm-pill sm-pill-neutral">-</span>
  }
  return <span className={entry.css}>{entry.label}</span>
}

export function statusText(status: Status | null | undefined): string {
  return status ?? '-'
}
