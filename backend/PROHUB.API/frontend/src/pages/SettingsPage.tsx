import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { authApi } from '../api/auth';
import { api } from '../api/client';

export function SettingsPage() {
  const { user } = useAuth();
  const { success, error: toastError } = useToast();

  // Profile section
  const [name, setName] = useState(user?.name ?? '');
  const [savingProfile, setSavingProfile] = useState(false);

  // Change password section
  const [currentPwd, setCurrentPwd] = useState('');
  const [newPwd, setNewPwd] = useState('');
  const [confirmPwd, setConfirmPwd] = useState('');
  const [savingPwd, setSavingPwd] = useState(false);
  const [showPwds, setShowPwds] = useState(false);

  // System health
  const [health, setHealth] = useState<any>(null);

  useEffect(() => {
    api.get<any>('/health').then(res => {
      if (res.ok && res.data) setHealth(res.data);
    });
  }, []);

  async function handleProfileSave(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    setSavingProfile(true);
    const res = await authApi.updateProfile({ name: name.trim() });
    if (res.ok) success('Profile updated.');
    else toastError(res.error ?? 'Failed to update profile.');
    setSavingProfile(false);
  }

  async function handlePasswordChange(e: React.FormEvent) {
    e.preventDefault();
    if (newPwd !== confirmPwd) { toastError('New passwords do not match.'); return; }
    if (newPwd.length < 8) { toastError('Password must be at least 8 characters.'); return; }
    setSavingPwd(true);
    const res = await authApi.changePassword({ currentPassword: currentPwd, newPassword: newPwd });
    if (res.ok) {
      success('Password changed successfully.');
      setCurrentPwd('');
      setNewPwd('');
      setConfirmPwd('');
    } else {
      toastError(res.error ?? 'Failed to change password.');
    }
    setSavingPwd(false);
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Settings</h1>
          <p className="page-subtitle">Account &amp; system configuration</p>
        </div>
      </div>

      <div className="settings-grid">
        {/* Profile */}
        <div className="panel">
          <h2 className="panel-title">Profile</h2>
          <div className="settings-avatar-row">
            <div className="settings-avatar">{user?.name?.[0]?.toUpperCase() ?? 'U'}</div>
            <div>
              <p className="settings-user-name">{user?.name}</p>
              <p className="settings-user-email">{user?.email}</p>
              <span className={`status-badge ${user?.role === 'owner' ? 'status-active' : 'status-planning'}`}>
                {user?.role}
              </span>
            </div>
          </div>
          <form onSubmit={handleProfileSave} style={{ marginTop: '1.25rem' }}>
            <label className="field">
              <span className="field-label">Display Name</span>
              <input
                className="input"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="Your name"
              />
            </label>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.75rem' }}>
              <button type="submit" className="btn-primary" disabled={savingProfile}>
                {savingProfile ? 'Saving...' : 'Save Profile'}
              </button>
            </div>
          </form>
        </div>

        {/* System */}
        <div className="panel">
          <h2 className="panel-title">System</h2>
          <table className="info-table">
            <tbody>
              <tr>
                <td className="info-key">DB</td>
                <td>
                  <span className={`status-badge ${health?.db === 'connected' ? 'status-active' : 'status-blocked'}`}>
                    {health?.db ?? '—'}
                  </span>
                </td>
              </tr>
              <tr>
                <td className="info-key">Environment</td>
                <td className="mono">{health?.environment ?? '—'}</td>
              </tr>
              <tr>
                <td className="info-key">Version</td>
                <td className="mono">{health?.version ?? '—'}</td>
              </tr>
              <tr>
                <td className="info-key">Swagger</td>
                <td><a href="/swagger" target="_blank" rel="noopener noreferrer" className="link-url">/swagger</a></td>
              </tr>
              <tr>
                <td className="info-key">Last check</td>
                <td className="mono">{health ? new Date(health.ts ?? health.timestamp).toLocaleTimeString() : '—'}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* Change password — full width */}
        <div className="panel settings-full">
          <h2 className="panel-title">Change Password</h2>
          <form onSubmit={handlePasswordChange}>
            <div className="row-fields">
              <label className="field">
                <span className="field-label">Current Password</span>
                <input
                  className="input"
                  type={showPwds ? 'text' : 'password'}
                  value={currentPwd}
                  onChange={e => setCurrentPwd(e.target.value)}
                  placeholder="••••••••"
                />
              </label>
              <label className="field">
                <span className="field-label">New Password</span>
                <input
                  className="input"
                  type={showPwds ? 'text' : 'password'}
                  value={newPwd}
                  onChange={e => setNewPwd(e.target.value)}
                  placeholder="min 8 chars"
                />
              </label>
              <label className="field">
                <span className="field-label">Confirm New Password</span>
                <input
                  className="input"
                  type={showPwds ? 'text' : 'password'}
                  value={confirmPwd}
                  onChange={e => setConfirmPwd(e.target.value)}
                  placeholder="••••••••"
                />
              </label>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '0.75rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '13px', color: 'var(--text-dim)' }}>
                <input
                  type="checkbox"
                  checked={showPwds}
                  onChange={e => setShowPwds(e.target.checked)}
                />
                Show passwords
              </label>
              <button type="submit" className="btn-primary" disabled={savingPwd}>
                {savingPwd ? 'Changing...' : 'Change Password'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
