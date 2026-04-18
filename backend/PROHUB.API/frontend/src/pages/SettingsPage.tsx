import { useState, useEffect } from 'react';
import { api } from '../api/client';
import type { HealthInfo } from '../api/types';

export function SettingsPage() {
  const [health, setHealth] = useState<HealthInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<HealthInfo>('/health').then(res => {
      if (res.ok && res.data) setHealth(res.data);
      else setError(res.error ?? 'Health check failed');
      setLoading(false);
    });
  }, []);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Settings</h1>
          <p className="page-subtitle">System information</p>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <h2 className="panel-title">Health</h2>
          {loading && <div className="skeleton-row" />}
          {error && <div className="alert-error">{error}</div>}
          {health && (
            <table className="info-table">
              <tbody>
                <tr>
                  <td className="info-key">Status</td>
                  <td>
                    <span className={`status-badge ${health.status === 'healthy' ? 'status-active' : 'status-blocked'}`}>
                      {health.status}
                    </span>
                  </td>
                </tr>
                <tr>
                  <td className="info-key">Environment</td>
                  <td>{health.environment}</td>
                </tr>
                <tr>
                  <td className="info-key">Timestamp</td>
                  <td className="mono">{new Date(health.timestamp).toLocaleString()}</td>
                </tr>
              </tbody>
            </table>
          )}
        </div>

        <div className="panel">
          <h2 className="panel-title">App</h2>
          <table className="info-table">
            <tbody>
              <tr>
                <td className="info-key">Version</td>
                <td className="mono">{health?.version ?? '—'}</td>
              </tr>
              <tr>
                <td className="info-key">API base</td>
                <td className="mono">/api</td>
              </tr>
              <tr>
                <td className="info-key">Swagger</td>
                <td>
                  <a href="/swagger" target="_blank" rel="noopener noreferrer" className="link-url">
                    /swagger
                  </a>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
