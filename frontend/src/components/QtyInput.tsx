import { useState } from 'react'

/// Input angka dengan buffer teks: field bisa dikosongkan dan diketik ulang bebas
/// (fokus tidak pernah hilang, tidak ada remount). Kosong = 0 bagi form.
/// Placeholder "0" tampil sebagai hint bila nilai nol — bukan nilai literal.
export function QtyInput({
  value,
  onChange,
  min = 0,
  step = 1,
  style,
  className,
  disabled,
  placeholder,
}: {
  value: number
  onChange: (v: number) => void
  min?: number
  step?: number
  style?: React.CSSProperties
  className?: string
  disabled?: boolean
  placeholder?: string
}) {
  // null = tampilkan nilai dari prop (normalisasi saat blur / perubahan dari luar)
  const [text, setText] = useState<string | null>(null)
  const shown = text ?? (value === 0 ? '' : String(value))

  return (
    <input
      type="number"
      min={min}
      step={step}
      disabled={disabled}
      placeholder={placeholder ?? (value === 0 ? '0' : undefined)}
      className={className}
      style={style}
      value={shown}
      onChange={(e) => {
        setText(e.target.value)
        onChange(e.target.value === '' ? 0 : Number(e.target.value))
      }}
      onBlur={() => setText(null)}
    />
  )
}
