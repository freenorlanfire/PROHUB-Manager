import { useState, useEffect } from 'react';
import { statusApi } from '../api/status';
import { contextDocApi } from '../api/contextDoc';
import { integrationsApi } from '../api/integrations';
import { tagsApi } from '../api/tags';
import { linksApi } from '../api/links';
import { projectsApi } from '../api/projects';
import { useToast } from '../context/ToastContext';
import type {
  Project, StatusEntry, ContextDoc, IntegrationLink, Tag, ProjectLink
} from '../api/types';
import { StatusBadge } from './ProjectsPage';

const TABS = ['Timeline', 'Context Doc', 'Integrations', 'Tags', 'Links'] as const;
type Tab = (typeof TABS)[number];

const INTEGRATION_TYPES = ['repo', 'ci', 'staging', 'prod', 'docs'] as const;

const STATUSES = ['active', 'in-progress', 'planning', 'blocked', 'paused', 'completed', 'archived'];

interface Props {
  projectId: string;
  companyId: string;
  onBack?: () => void;
}

export function ProjectDetailPage({ projectId, companyId: _companyId, onBack }: Props) {
  const [project, setProject] = useState<Project | null>(null);
  const [activeTab, setActiveTab] = useState<Tab>('Timeline');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(false);
  const { success, error: toastError } = useToast();

  useEffect(() => {
    setLoading(true);
    projectsApi.get(projectId).then(res => {
      if (res.ok && res.data) setProject(res.data);
      else setError(res.error ?? 'Failed to load project');
      setLoading(false);
    });
  }, [projectId]);

  async function handleDelete() {
    const res = await projectsApi.delete(projectId);
    if (res.ok) {
      success('Project deleted.');
      onBack?.();
    } else {
      toastError(res.error ?? 'Failed to delete project.');
    }
  }

  if (loading) return <div className="page"><div className="skeleton-card" /></div>;
  if (error || !project) return <div className="page"><div className="alert-error">{error}</div></div>;

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">{project.name}</h1>
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '0.25rem' }}>
            <StatusBadge status={project.status} />
            <span className="card-slug">{project.slug}</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button className="btn-secondary" onClick={() => setEditOpen(true)}>Edit</button>
          {!deleteConfirm ? (
            <button className="btn-ghost" onClick={() => setDeleteConfirm(true)}>Delete</button>
          ) : (
            <>
              <button className="btn-danger" onClick={handleDelete}>Confirm Delete</button>
              <button className="btn-ghost" onClick={() => setDeleteConfirm(false)}>Cancel</button>
            </>
          )}
        </div>
      </div>

      <div className="tabs">
        {TABS.map(tab => (
          <button
            key={tab}
            className={`tab ${activeTab === tab ? 'tab-active' : ''}`}
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="tab-content">
        {activeTab === 'Timeline' && <TimelineTab projectId={projectId} />}
        {activeTab === 'Context Doc' && <ContextDocTab projectId={projectId} />}
        {activeTab === 'Integrations' && <IntegrationsTab projectId={projectId} />}
        {activeTab === 'Tags' && <TagsTab projectId={projectId} />}
        {activeTab === 'Links' && <LinksTab projectId={projectId} />}
      </div>

      {editOpen && (
        <EditProjectModal
          project={project}
          onClose={() => setEditOpen(false)}
          onSaved={updated => { setProject(updated); setEditOpen(false); success('Project updated.'); }}
        />
      )}
    </div>
  );
}

/* ── Edit Project Modal ───────────────────────────────────────────────────────── */

function EditProjectModal({
  project,
  onClose,
  onSaved,
}: {
  project: Project;
  onClose: () => void;
  onSaved: (updated: Project) => void;
}) {
  const [name, setName] = useState(project.name);
  const [description, setDescription] = useState(project.description ?? '');
  const [status, setStatus] = useState(project.status);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { setError('Name is required.'); return; }
    setSaving(true);
    setError(null);
    const res = await projectsApi.update(project.id, {
      name: name.trim(),
      description: description.trim() || undefined,
      status,
    });
    if (res.ok && res.data) onSaved(res.data);
    else setError(res.error ?? 'Failed to save.');
    setSaving(false);
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">Edit Project</h2>
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
          <label className="field">
            <span className="field-label">Status</span>
            <select className="input" value={status} onChange={e => setStatus(e.target.value)}>
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          <div className="modal-footer">
            <button type="button" className="btn-ghost" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={saving}>
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

/* ── Timeline tab ─────────────────────────────────────────────────────────────── */

function TimelineTab({ projectId }: { projectId: string }) {
  const [entries, setEntries] = useState<StatusEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState('active');
  const [note, setNote] = useState('');
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => { load(); }, [projectId]);

  async function load() {
    setLoading(true);
    const res = await statusApi.history(projectId);
    if (res.ok && res.data) setEntries(res.data);
    setLoading(false);
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErr(null);
    const res = await statusApi.add(projectId, { status, note: note.trim() || undefined });
    if (res.ok && res.data) {
      setEntries(prev => [res.data!, ...prev]);
      setNote('');
    } else setErr(res.error ?? 'Failed to add status');
    setSaving(false);
  }

  return (
    <div className="tab-section">
      <form onSubmit={handleAdd} className="panel" style={{ marginBottom: '1.5rem' }}>
        <h3 className="panel-title">Add Status Update</h3>
        {err && <div className="alert-error">{err}</div>}
        <div className="row-fields">
          <label className="field">
            <span className="field-label">Status</span>
            <select className="input" value={status} onChange={e => setStatus(e.target.value)}>
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          <label className="field" style={{ flex: 1 }}>
            <span className="field-label">Note</span>
            <input
              className="input"
              value={note}
              onChange={e => setNote(e.target.value)}
              placeholder="What changed?"
            />
          </label>
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <button type="submit" className="btn-primary" disabled={saving}>
            {saving ? 'Adding...' : 'Add Status'}
          </button>
        </div>
      </form>

      {loading ? (
        <div className="skeleton-card" />
      ) : entries.length === 0 ? (
        <div className="empty-state">
          <p className="empty-title">No status entries yet</p>
        </div>
      ) : (
        <div className="timeline">
          {entries.map((e, i) => (
            <div key={e.id} className="timeline-entry">
              <div className="timeline-dot" />
              {i < entries.length - 1 && <div className="timeline-line" />}
              <div className="timeline-body">
                <div className="timeline-row">
                  <StatusBadge status={e.status} />
                  <span className="timeline-time">{fmtDateTime(e.createdAtUtc)}</span>
                  {e.createdBy && <span className="timeline-by">{e.createdBy}</span>}
                </div>
                {e.note && <p className="timeline-note">{e.note}</p>}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

/* ── Context Doc tab ──────────────────────────────────────────────────────────── */

function ContextDocTab({ projectId }: { projectId: string }) {
  const [doc, setDoc] = useState<ContextDoc | null>(null);
  const [content, setContent] = useState('');
  const [preview, setPreview] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    contextDocApi.get(projectId).then(res => {
      if (res.ok && res.data) { setDoc(res.data); setContent(res.data.content); }
      setLoading(false);
    });
  }, [projectId]);

  async function handleSave() {
    setSaving(true);
    setErr(null);
    const res = await contextDocApi.save(projectId, { content });
    if (res.ok && res.data) { setDoc(res.data); setSaved(true); setTimeout(() => setSaved(false), 2500); }
    else setErr(res.error ?? 'Save failed');
    setSaving(false);
  }

  if (loading) return <div className="skeleton-card" />;

  return (
    <div className="tab-section">
      <div className="doc-toolbar">
        <div className="tabs" style={{ marginBottom: 0 }}>
          <button className={`tab ${!preview ? 'tab-active' : ''}`} onClick={() => setPreview(false)}>Edit</button>
          <button className={`tab ${preview ? 'tab-active' : ''}`} onClick={() => setPreview(true)}>Preview</button>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          {doc?.updatedAtUtc && (
            <span className="field-label">Last saved: {fmtDateTime(doc.updatedAtUtc)}</span>
          )}
          {saved && <span className="save-confirm">Saved</span>}
          <button className="btn-primary" onClick={handleSave} disabled={saving}>
            {saving ? 'Saving...' : 'Save Version'}
          </button>
        </div>
      </div>
      {err && <div className="alert-error">{err}</div>}
      {preview ? (
        <div className="doc-preview panel" dangerouslySetInnerHTML={{ __html: renderMd(content) }} />
      ) : (
        <textarea
          className="input doc-editor"
          value={content}
          onChange={e => setContent(e.target.value)}
          placeholder="# Project Context&#10;&#10;Write markdown here..."
          spellCheck={false}
        />
      )}
    </div>
  );
}

/* ── Integrations tab ─────────────────────────────────────────────────────────── */

function IntegrationsTab({ projectId }: { projectId: string }) {
  const [links, setLinks] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    integrationsApi.get(projectId).then(res => {
      if (res.ok && res.data) {
        const map: Record<string, string> = {};
        res.data.forEach((l: IntegrationLink) => { map[l.type] = l.url; });
        setLinks(map);
      }
      setLoading(false);
    });
  }, [projectId]);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErr(null);
    const payload = INTEGRATION_TYPES
      .filter(t => links[t]?.trim())
      .map(t => ({ type: t, url: links[t].trim() }));

    const res = await integrationsApi.upsert(projectId, payload);
    if (res.ok) { setSaved(true); setTimeout(() => setSaved(false), 2500); }
    else setErr(res.error ?? 'Failed to save integrations');
    setSaving(false);
  }

  if (loading) return <div className="skeleton-card" />;

  return (
    <div className="tab-section">
      <form onSubmit={handleSave} className="panel">
        <h3 className="panel-title">Integration Links</h3>
        {err && <div className="alert-error">{err}</div>}
        {INTEGRATION_TYPES.map(type => (
          <label key={type} className="field">
            <span className="field-label" style={{ textTransform: 'uppercase', letterSpacing: '0.08em' }}>{type}</span>
            <input
              className="input"
              type="url"
              value={links[type] ?? ''}
              onChange={e => setLinks(prev => ({ ...prev, [type]: e.target.value }))}
              placeholder={`https://...`}
            />
          </label>
        ))}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', alignItems: 'center', marginTop: '0.5rem' }}>
          {saved && <span className="save-confirm">Saved</span>}
          <button type="submit" className="btn-primary" disabled={saving}>
            {saving ? 'Saving...' : 'Save Integrations'}
          </button>
        </div>
      </form>
    </div>
  );
}

/* ── Tags tab ─────────────────────────────────────────────────────────────────── */

function TagsTab({ projectId }: { projectId: string }) {
  const [tags, setTags] = useState<Tag[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    tagsApi.list(projectId).then(res => {
      if (res.ok && res.data) setTags(res.data);
      setLoading(false);
    });
  }, [projectId]);

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    if (!input.trim()) return;
    setErr(null);
    const res = await tagsApi.add(projectId, input.trim());
    if (res.ok && res.data) { setTags(prev => [...prev, res.data!]); setInput(''); }
    else setErr(res.error ?? 'Failed to add tag');
  }

  async function handleDelete(tag: string) {
    const res = await tagsApi.delete(projectId, tag);
    if (res.ok) setTags(prev => prev.filter(t => t.tag !== tag));
  }

  if (loading) return <div className="skeleton-card" />;

  return (
    <div className="tab-section">
      <div className="panel">
        <h3 className="panel-title">Tags</h3>
        {err && <div className="alert-error">{err}</div>}
        <form onSubmit={handleAdd} style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
          <input
            className="input"
            value={input}
            onChange={e => setInput(e.target.value)}
            placeholder="Add a tag..."
            style={{ flex: 1 }}
          />
          <button type="submit" className="btn-primary">Add</button>
        </form>
        <div className="tag-list">
          {tags.length === 0 && <span className="text-dim">No tags yet.</span>}
          {tags.map(t => (
            <span key={t.id} className="tag-chip">
              {t.tag}
              <button className="tag-remove" onClick={() => handleDelete(t.tag)}>x</button>
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

/* ── Links tab ────────────────────────────────────────────────────────────────── */

function LinksTab({ projectId }: { projectId: string }) {
  const [links, setLinks] = useState<ProjectLink[]>([]);
  const [label, setLabel] = useState('');
  const [url, setUrl] = useState('');
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    linksApi.list(projectId).then(res => {
      if (res.ok && res.data) setLinks(res.data);
      setLoading(false);
    });
  }, [projectId]);

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    if (!label.trim() || !url.trim()) { setErr('Label and URL are required.'); return; }
    setErr(null);
    const res = await linksApi.add(projectId, { label: label.trim(), url: url.trim() });
    if (res.ok && res.data) { setLinks(prev => [...prev, res.data!]); setLabel(''); setUrl(''); }
    else setErr(res.error ?? 'Failed to add link');
  }

  async function handleDelete(id: string) {
    const res = await linksApi.delete(projectId, id);
    if (res.ok) setLinks(prev => prev.filter(l => l.id !== id));
  }

  if (loading) return <div className="skeleton-card" />;

  return (
    <div className="tab-section">
      <form onSubmit={handleAdd} className="panel" style={{ marginBottom: '1.5rem' }}>
        <h3 className="panel-title">Add Link</h3>
        {err && <div className="alert-error">{err}</div>}
        <div className="row-fields">
          <label className="field">
            <span className="field-label">Label</span>
            <input className="input" value={label} onChange={e => setLabel(e.target.value)} placeholder="API Docs" />
          </label>
          <label className="field" style={{ flex: 1 }}>
            <span className="field-label">URL</span>
            <input className="input" value={url} onChange={e => setUrl(e.target.value)} placeholder="https://..." type="url" />
          </label>
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <button type="submit" className="btn-primary">Add Link</button>
        </div>
      </form>

      <div className="link-list">
        {links.length === 0 && <div className="empty-state"><p className="empty-title">No links yet</p></div>}
        {links.map(l => (
          <div key={l.id} className="link-row">
            <div className="link-info">
              <span className="link-label">{l.label}</span>
              <a href={l.url} target="_blank" rel="noopener noreferrer" className="link-url">{l.url}</a>
            </div>
            <button className="btn-ghost btn-sm" onClick={() => handleDelete(l.id)}>Delete</button>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ── Helpers ──────────────────────────────────────────────────────────────────── */

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
}

function renderMd(text: string): string {
  return text
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/^### (.+)$/gm, '<h3>$1</h3>')
    .replace(/^## (.+)$/gm, '<h2>$1</h2>')
    .replace(/^# (.+)$/gm, '<h1>$1</h1>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    .replace(/`(.+?)`/g, '<code>$1</code>')
    .replace(/^- (.+)$/gm, '<li>$1</li>')
    .replace(/(<li>[\s\S]+?<\/li>)(?=\n|$)/g, '<ul>$1</ul>')
    .replace(/\n\n/g, '<br/><br/>')
    .replace(/\n/g, '<br/>');
}
