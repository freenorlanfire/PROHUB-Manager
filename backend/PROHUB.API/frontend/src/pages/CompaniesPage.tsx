import { useState, useEffect } from 'react';
import { companiesApi } from '../api/companies';
import type { Company } from '../api/types';

interface Props {
  onSelectCompany: (id: string, name: string) => void;
}

type ModalState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; company: Company };

export function CompaniesPage({ onSelectCompany }: Props) {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<ModalState>({ mode: 'closed' });
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const res = await companiesApi.list();
      if (res.ok && res.data) setCompanies(res.data);
      else setError(res.error ?? 'Failed to load companies');
    } catch (e) {
      setError(`Network error: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(id: string) {
    const res = await companiesApi.delete(id);
    if (res.ok) {
      setCompanies(prev => prev.filter(c => c.id !== id));
      setDeleteConfirm(null);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Companies</h1>
          <p className="page-subtitle">{companies.length} registered</p>
        </div>
        <button className="btn-primary" onClick={() => setModal({ mode: 'create' })}>
          + New Company
        </button>
      </div>

      {error && <div className="alert-error">{error}</div>}

      {loading ? (
        <div className="grid-3">
          {[1, 2, 3].map(i => <div key={i} className="skeleton-card" />)}
        </div>
      ) : companies.length === 0 ? (
        <div className="empty-state">
          <p className="empty-title">No companies yet</p>
          <p className="empty-sub">Create your first company to get started.</p>
          <button className="btn-primary" onClick={() => setModal({ mode: 'create' })}>
            + New Company
          </button>
        </div>
      ) : (
        <div className="grid-3">
          {companies.map(c => (
            <div key={c.id} className="card">
              <div className="card-header">
                <div>
                  <h2 className="card-title">{c.name}</h2>
                  <span className="card-slug">{c.slug}</span>
                </div>
              </div>
              {c.description && (
                <p className="card-desc">{c.description}</p>
              )}
              <div className="card-meta">
                Created {fmtDate(c.createdAtUtc)}
              </div>
              <div className="card-actions">
                <button
                  className="btn-primary btn-sm"
                  onClick={() => onSelectCompany(c.id, c.name)}
                >
                  Projects
                </button>
                <button
                  className="btn-secondary btn-sm"
                  onClick={() => setModal({ mode: 'edit', company: c })}
                >
                  Edit
                </button>
                {deleteConfirm === c.id ? (
                  <>
                    <button
                      className="btn-danger btn-sm"
                      onClick={() => handleDelete(c.id)}
                    >
                      Confirm
                    </button>
                    <button
                      className="btn-ghost btn-sm"
                      onClick={() => setDeleteConfirm(null)}
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <button
                    className="btn-ghost btn-sm"
                    onClick={() => setDeleteConfirm(c.id)}
                  >
                    Delete
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {modal.mode !== 'closed' && (
        <CompanyModal
          initial={modal.mode === 'edit' ? modal.company : undefined}
          onClose={() => setModal({ mode: 'closed' })}
          onSaved={() => { setModal({ mode: 'closed' }); load(); }}
        />
      )}
    </div>
  );
}

interface CompanyModalProps {
  initial?: Company;
  onClose: () => void;
  onSaved: () => void;
}

function CompanyModal({ initial, onClose, onSaved }: CompanyModalProps) {
  const [name, setName] = useState(initial?.name ?? '');
  const [description, setDescription] = useState(initial?.description ?? '');
  const [websiteUrl, setWebsiteUrl] = useState(initial?.websiteUrl ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { setError('Name is required.'); return; }
    setSaving(true);
    setError(null);

    const body = {
      name: name.trim(),
      description: description.trim() || undefined,
      websiteUrl: websiteUrl.trim() || undefined,
    };

    const res = initial
      ? await companiesApi.update(initial.id, body)
      : await companiesApi.create(body);

    if (res.ok) onSaved();
    else setError(res.error ?? 'Failed to save.');
    setSaving(false);
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">{initial ? 'Edit Company' : 'New Company'}</h2>
          <button className="modal-close" onClick={onClose}>X</button>
        </div>
        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="alert-error">{error}</div>}
          <label className="field">
            <span className="field-label">Name *</span>
            <input
              className="input"
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="Acme Corp"
              autoFocus
            />
          </label>
          <label className="field">
            <span className="field-label">Description</span>
            <textarea
              className="input textarea"
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="What does this company do?"
              rows={3}
            />
          </label>
          <label className="field">
            <span className="field-label">Website</span>
            <input
              className="input"
              value={websiteUrl}
              onChange={e => setWebsiteUrl(e.target.value)}
              placeholder="https://acme.com"
              type="url"
            />
          </label>
          <div className="modal-footer">
            <button type="button" className="btn-ghost" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={saving}>
              {saving ? 'Saving...' : initial ? 'Save Changes' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}
