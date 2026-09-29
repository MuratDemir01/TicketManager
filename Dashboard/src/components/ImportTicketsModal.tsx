import { useRef, useState } from 'react'
import { api, ApiError } from '../api'
import { parseImportFile, type ImportTicketRow } from '../importParse'
import { priorityLabel } from '../types'

type Step = 'pick' | 'preview' | 'confirm' | 'done'

interface Props {
  open: boolean
  onClose: () => void
  onImported: () => void
}

export function ImportTicketsModal({ open, onClose, onImported }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [step, setStep] = useState<Step>('pick')
  const [fileName, setFileName] = useState('')
  const [rows, setRows] = useState<ImportTicketRow[]>([])
  const [parseErrors, setParseErrors] = useState<string[]>([])
  const [importErrors, setImportErrors] = useState<string[]>([])
  const [createdCount, setCreatedCount] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!open) return null

  function reset() {
    setStep('pick')
    setFileName('')
    setRows([])
    setParseErrors([])
    setImportErrors([])
    setCreatedCount(0)
    setBusy(false)
    setError(null)
    if (inputRef.current) inputRef.current.value = ''
  }

  function handleClose() {
    if (busy) return
    reset()
    onClose()
  }

  async function onFileChange(file: File | null) {
    if (!file) return
    setError(null)
    setBusy(true)
    try {
      const result = await parseImportFile(file)
      setFileName(file.name)
      setRows(result.rows)
      setParseErrors(result.errors)
      setStep('preview')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Dosya okunamadı')
    } finally {
      setBusy(false)
    }
  }

  async function confirmImport() {
    setBusy(true)
    setError(null)
    try {
      const result = await api.importTickets(
        rows.map((r) => ({
          title: r.title,
          description: r.description,
          customerName: r.customerName,
          customerEmail: r.customerEmail,
          priority: r.priority,
          assignedUserId: r.assignedUserId || null,
        })),
      )
      setCreatedCount(result.createdCount)
      setImportErrors(result.errors)
      setStep('done')
      if (result.createdCount > 0) onImported()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İçe aktarma başarısız')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={handleClose}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="import-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="modal-header">
          <h2 id="import-modal-title">CSV / Excel içe aktarma</h2>
          <button
            type="button"
            className="btn btn-ghost"
            onClick={handleClose}
            disabled={busy}
            aria-label="Kapat"
          >
            ×
          </button>
        </header>

        <div className="modal-body">
          {step === 'pick' && (
            <>
              <p className="muted">
                Kolonlar: Title, Description, CustomerName, CustomerEmail,
                Priority, isteğe bağlı AssignedUserId. En fazla 200 satır.
                Biçimler: .csv, .xlsx
              </p>
              <input
                ref={inputRef}
                type="file"
                accept=".csv,.txt,.xlsx,.xls"
                disabled={busy}
                onChange={(e) => void onFileChange(e.target.files?.[0] ?? null)}
              />
            </>
          )}

          {step === 'preview' && (
            <>
              <p>
                <strong>{fileName}</strong> — {rows.length} satır hazır
                {parseErrors.length > 0 && (
                  <span className="warn-inline">
                    {' '}
                    ({parseErrors.length} uyarı/hata)
                  </span>
                )}
              </p>
              {parseErrors.length > 0 && (
                <ul className="import-errors">
                  {parseErrors.slice(0, 8).map((e) => (
                    <li key={e}>{e}</li>
                  ))}
                  {parseErrors.length > 8 && (
                    <li>…ve {parseErrors.length - 8} tane daha</li>
                  )}
                </ul>
              )}
              {rows.length > 0 && (
                <div className="table-wrap import-preview">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Başlık</th>
                        <th>Müşteri</th>
                        <th>Öncelik</th>
                        <th>Atanan</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.slice(0, 10).map((r, i) => (
                        <tr key={`${r.title}-${i}`}>
                          <td>{i + 1}</td>
                          <td>{r.title}</td>
                          <td>
                            {r.customerName}
                            <div className="muted small">{r.customerEmail}</div>
                          </td>
                          <td>{priorityLabel(r.priority)}</td>
                          <td>{r.assignedUserId || '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  {rows.length > 10 && (
                    <p className="muted small">
                      Önizlemede ilk 10 satır gösteriliyor.
                    </p>
                  )}
                </div>
              )}
            </>
          )}

          {step === 'confirm' && (
            <p>
              <strong>{rows.length}</strong> talep oluşturulacak. Bu işlem geri
              alınamaz. Emin misiniz?
            </p>
          )}

          {step === 'done' && (
            <>
              <p>
                <strong>{createdCount}</strong> talep oluşturuldu
                {importErrors.length > 0 && (
                  <span className="warn-inline">
                    {' '}
                    ({importErrors.length} satır atlandı)
                  </span>
                )}
                .
              </p>
              {importErrors.length > 0 && (
                <ul className="import-errors">
                  {importErrors.slice(0, 10).map((e) => (
                    <li key={e}>{e}</li>
                  ))}
                </ul>
              )}
            </>
          )}

          {error && <p className="error">{error}</p>}
        </div>

        <footer className="modal-footer">
          {step === 'pick' && (
            <button type="button" className="btn" onClick={handleClose}>
              İptal
            </button>
          )}
          {step === 'preview' && (
            <>
              <button
                type="button"
                className="btn"
                disabled={busy}
                onClick={() => {
                  reset()
                }}
              >
                Başka dosya
              </button>
              <button
                type="button"
                className="btn primary"
                disabled={busy || rows.length === 0}
                onClick={() => setStep('confirm')}
              >
                Devam
              </button>
            </>
          )}
          {step === 'confirm' && (
            <>
              <button
                type="button"
                className="btn"
                disabled={busy}
                onClick={() => setStep('preview')}
              >
                Geri
              </button>
              <button
                type="button"
                className="btn primary"
                disabled={busy}
                onClick={() => void confirmImport()}
              >
                {busy ? 'Aktarılıyor…' : 'Evet, aktar'}
              </button>
            </>
          )}
          {step === 'done' && (
            <button type="button" className="btn primary" onClick={handleClose}>
              Kapat
            </button>
          )}
        </footer>
      </div>
    </div>
  )
}
