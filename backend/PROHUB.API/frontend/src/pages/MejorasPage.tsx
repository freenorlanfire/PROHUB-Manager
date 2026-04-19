import { useState, useEffect, useCallback } from 'react';
import { trendsApi, type TrendItem } from '../api/trends';

const SOURCE_LABELS: Record<string, string> = {
  hn: 'Hacker News',
  devto: 'dev.to',
  github: 'GitHub',
};

const SOURCE_COLORS: Record<string, string> = {
  hn: '#ff6600',
  devto: '#3b49df',
  github: '#f0883e',
};

const PRESET_TAGS = [
  ['react', 'typescript', 'frontend'],
  ['dotnet', 'csharp', 'aspnetcore'],
  ['ai', 'llm', 'claude'],
  ['devops', 'docker', 'kubernetes'],
  ['rust', 'wasm', 'performance'],
  ['postgres', 'database', 'sql'],
];

interface Props {
  projectId?: string;
  projectName?: string;
}

export function MejorasPage({ projectId, projectName }: Props) {
  const [items, setItems] = useState<TrendItem[]>([]);
  const [queryTags, setQueryTags] = useState<string[]>([]);
  const [fetchedAt, setFetchedAt] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tagInput, setTagInput] = useState('');
  const [activeSource, setActiveSource] = useState<string>('all');

  const fetchTrends = useCallback(async (tags: string[]) => {
    setLoading(true);
    setError(null);
    const res = projectId && tags.length === 0
      ? await trendsApi.getForProject(projectId)
      : await trendsApi.get(tags.length > 0 ? tags : ['programming', 'software', 'devops']);
    setLoading(false);

    if (res.ok && res.data) {
      setItems(res.data.items);
      setQueryTags(res.data.queryTags);
      setFetchedAt(res.data.fetchedAtUtc);
    } else {
      setError(res.error ?? 'Failed to load trends.');
    }
  }, [projectId]);

  useEffect(() => {
    fetchTrends([]);
  }, [fetchTrends]);

  function handleTagSearch(e: React.FormEvent) {
    e.preventDefault();
    const tags = tagInput.split(',').map(t => t.trim().toLowerCase()).filter(Boolean);
    if (tags.length > 0) fetchTrends(tags);
  }

  function handlePreset(tags: string[]) {
    setTagInput(tags.join(', '));
    fetchTrends(tags);
  }

  const sources = ['all', 'hn', 'devto', 'github'];
  const filtered = activeSource === 'all' ? items : items.filter(i => i.source === activeSource);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">
            <span style={{ color: 'var(--accent)', marginRight: '0.5rem' }}>⚡</span>
            Mejoras
          </h1>
          <p className="page-subtitle">
            {projectName
              ? `Tech trends relevant to ${projectName}`
              : 'Curated tech trends from Hacker News, dev.to &amp; GitHub'}
          </p>
        </div>
        <button className="btn-secondary" onClick={() => fetchTrends(queryTags)} disabled={loading}>
          {loading ? (
            <span className="btn-spinner">↻</span>
          ) : '↻ Refresh'}
        </button>
      </div>

      {/* Search bar */}
      <form onSubmit={handleTagSearch} className="trends-search-row">
        <input
          className="input"
          value={tagInput}
          onChange={e => setTagInput(e.target.value)}
          placeholder="Search by tags: react, typescript, ai, docker..."
          style={{ flex: 1 }}
        />
        <button type="submit" className="btn-primary" disabled={loading}>
          Search
        </button>
      </form>

      {/* Preset chips */}
      <div className="trends-presets">
        {PRESET_TAGS.map(tags => (
          <button
            key={tags[0]}
            className="trend-preset-chip"
            onClick={() => handlePreset(tags)}
          >
            {tags[0]}
          </button>
        ))}
      </div>

      {/* Active query */}
      {queryTags.length > 0 && (
        <div className="trends-meta">
          <div className="trend-active-tags">
            {queryTags.map(tag => (
              <span key={tag} className="tag-chip" style={{ fontSize: '11px' }}>{tag}</span>
            ))}
          </div>
          {fetchedAt && (
            <span className="text-dim" style={{ fontSize: '11px' }}>
              Fetched {new Date(fetchedAt).toLocaleTimeString()}
            </span>
          )}
        </div>
      )}

      {/* Source filter tabs */}
      <div className="tabs" style={{ marginBottom: '1.25rem' }}>
        {sources.map(s => (
          <button
            key={s}
            className={`tab ${activeSource === s ? 'tab-active' : ''}`}
            onClick={() => setActiveSource(s)}
          >
            {s === 'all' ? `All (${items.length})` : SOURCE_LABELS[s]}
            {s !== 'all' && (
              <span className="source-count">{items.filter(i => i.source === s).length}</span>
            )}
          </button>
        ))}
      </div>

      {/* Content */}
      {error && <div className="alert-error">{error}</div>}

      {loading ? (
        <div className="trends-grid">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="skeleton-card trends-skeleton" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="empty-state">
          <p className="empty-title">No results found</p>
          <p className="empty-sub">Try different tags or refresh.</p>
        </div>
      ) : (
        <div className="trends-grid">
          {filtered.map(item => (
            <TrendCard key={item.id} item={item} />
          ))}
        </div>
      )}
    </div>
  );
}

function TrendCard({ item }: { item: TrendItem }) {
  const color = SOURCE_COLORS[item.source] ?? '#888';
  const label = SOURCE_LABELS[item.source] ?? item.source;

  function fmtDate(iso: string) {
    try {
      return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    } catch {
      return iso;
    }
  }

  function fmtPoints(n: number) {
    if (n >= 1000) return `${(n / 1000).toFixed(1)}k`;
    return n.toString();
  }

  return (
    <a
      href={item.url}
      target="_blank"
      rel="noopener noreferrer"
      className="trend-card"
    >
      <div className="trend-card-top">
        <span className="trend-source-badge" style={{ background: color + '22', color }}>
          {label}
        </span>
        {item.points > 0 && (
          <span className="trend-points">
            ★ {fmtPoints(item.points)}
          </span>
        )}
      </div>

      <h3 className="trend-title">{item.title}</h3>

      {item.description && (
        <p className="trend-desc">{item.description.slice(0, 120)}{item.description.length > 120 ? '…' : ''}</p>
      )}

      <div className="trend-footer">
        {item.author && <span className="trend-author">@{item.author}</span>}
        <span className="trend-date">{fmtDate(item.publishedAt)}</span>
        {item.tags.length > 0 && (
          <span className="trend-lang">{item.tags[0]}</span>
        )}
      </div>
    </a>
  );
}
