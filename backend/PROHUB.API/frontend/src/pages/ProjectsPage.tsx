import { useState, useEffect } from 'react';
import { projectsApi } from '../api/projects';
import type { Project } from '../api/types';

interface Props {
  companyId: string;
  companyName: string;
  onSelectProject: (id: string, name: string) => void;
}

type ModalState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; project: Project };

export function ProjectsPage({ companyId, companyName, onSelectProject }: Props) {
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<ModalState>({ mode: 'closed' });
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  useEffect(() => { load(); }, [companyId]);

  async function load() {
    setLoading(true);
    setError(null);
    const res = await projectsApi.list(companyId);
    if (res.ok && res.data) setProjects(res.data);
    else setError(res.error ?? 'Failed to load projects');
    setLoading(false);
  }

  async function handleDelete(id: string) {
    const res = await projectsApi.delete(id);
    if (res.ok) { setProjects(prev => prev.filter(p => p.id !== id)); setDeleteConfirm(null); }
  }

  async function handleArchive(id: string) {
    const res = await projectsApi.archive(id);
    if (res.ok && res.data) setProjects(prev => prev.map(p => p.id === id ? res.data! : p));
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Projects</h1>
          <p className="page-subtitle">{companyName} &mdash; {projects.length} total</p>
        </div>
        <button className="btn-primary" onClick={() => setModal({ mode: 'create' })}>
          + New Project
        </button>
      </div>

      {error && <div className="alert-error">{error}</div>}

      {loading ? (
        <div className="grid-3">
          {[1, 2, 3].map(i => <div key={i} className="skeleton-card" />)}
        </div>
      ) : projects.length === 0 ? (
        <div className="empty-state">
          <p className="empty-title">No projects yet</p>
          <p className="empty-sub">Add a project to start tracking status and docs.</p>
          <button className="btn-primary" onClick={() => setModal({ mode: 'create' })}>
            + New Project
          </button>
        </div>
      ) : (
        <div className="grid-3">
          {projects.map(p => (
            <div key={p.id} className="card">
              <div className="card-header">
                <div>
                  <h2 className="card-title">{p.name}</h2>
                  <span className="card-slug">{p.slug}</span>
                </div>
                <StatusBadge status={p.status} />
              </div>
              {p.description && <p className="card-desc">{p.description}</p>}
              <div className="card-meta">
                Updated {fmtDate(p.updatedAtUtc)}
                {p.archivedAtUtc && <span className="tag-chip tag-muted">Archived</span>}
              </div>
              <div className="card-actions">
                <button
                  className="btn-primary btn-sm"
                  onClick={() => onSelectProject(p.id, p.name)}
                >
                  Open
                </button>
                <button
                  className="btn-secondary btn-sm"
                  onClick={() => setModal({ mode: 'edit', project: p })}
                >
                  Edit
                </button>
                {!p.archivedAtUtc && (
                  <button
                    className="btn-ghost btn-sm"
                    onClick={() => handleArchive(p.id)}
                  >
                    Archive
                  </button>
                )}
                {deleteConfirm === p.id ? (
                  <>
                    <button className="btn-danger btn-sm" onClick={() => handleDelete(p.id)}>Confirm</button>
                    <button className="btn-ghost btn-sm" onClick={() => setDeleteConfirm(null)}>Cancel</button>
                  </>
                ) : (
                  <button className="btn-ghost btn-sm" onClick={() => setDeleteConfirm(p.id)}>Delete</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {modal.mode !== 'closed' && (
        <ProjectModal
          companyId={companyId}
          initial={modal.mode === 'edit' ? modal.project : undefined}
          onClose={() => setModal({ mode: 'closed' })}
          onSaved={() => { setModal({ mode: 'closed' }); load(); }}
        />
      )}
    </div>
  );
}

function ProjectModal({
  companyId,
  initial,
  onClose,
  onSaved,
}: {
  companyId: string;
  initial?: Project;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState(initial?.name ?? '');
  const [description, setDescription] = useState(initial?.description ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { setError('Name is required.'); return; }
    setSaving(true);
    setError(null);

    const res = initial
      ? await projectsApi.update(initial.id, { name: name.trim(), description: description.trim() || undefined })
      : await projectsApi.create({ companyId, name: name.trim(), description: description.trim() || undefined });

    if (res.ok) onSaved();
    else setError(res.error ?? 'Failed to save.');
    setSaving(false);
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">{initial ? 'Edit Project' : 'New Project'}</h2>
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
              placeholder="My App"
              autoFocus
            />
          </label>
          <label className="field">
            <span className="field-label">Description</span>
            <textarea
              className="input textarea"
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="What is this project?"
              rows={3}
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

export function StatusBadge({ status }: { status: string }) {
  const cls = STATUS_CLASSES[status] ?? 'status-default';
  return <span className={`status-badge ${cls}`}>{status}</span>;
}

const STATUS_CLASSES: Record<string, string> = {
  active: 'status-active',
  'in-progress': 'status-progress',
  planning: 'status-planning',
  blocked: 'status-blocked',
  paused: 'status-paused',
  completed: 'status-completed',
  archived: 'status-archived',
};

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}
