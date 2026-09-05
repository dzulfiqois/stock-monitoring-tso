import { useEffect } from 'react'
import type { ReactNode } from 'react'

/// Wrapper modal gaya Blazor: backdrop blur + fixed wrapper + sm-modal (header/body/footer).
/// ESC menutup; klik backdrop menutup.
export function Modal({
  title,
  onClose,
  children,
  footer,
  maxWidth,
}: {
  title: string
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  maxWidth?: number
}) {
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <>
      <div className="sm-modal-backdrop" onClick={onClose} />
      <div
        style={{
          position: 'fixed',
          inset: 0,
          zIndex: 110,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          overflowY: 'auto',
        }}
      >
        <div className="sm-modal" style={maxWidth ? { maxWidth } : undefined}>
          <div className="sm-modal-header">
            <h3 className="sm-title-sm" style={{ margin: 0 }}>
              {title}
            </h3>
            <button className="sm-btn sm-btn-outline sm-btn-sm" onClick={onClose} type="button">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>
                close
              </span>
            </button>
          </div>
          <div className="sm-modal-body">{children}</div>
          {footer ? <div className="sm-modal-footer">{footer}</div> : null}
        </div>
      </div>
    </>
  )
}
