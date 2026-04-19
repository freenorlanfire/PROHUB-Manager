import type { ReactNode } from 'react';
import { useAuth } from '../context/AuthContext';

type NavPage = 'companies' | 'settings';

interface LayoutProps {
  children: ReactNode;
  activePage: NavPage;
  onNavigate: (page: NavPage) => void;
  breadcrumbs?: Array<{ label: string; onClick?: () => void }>;
}

export function Layout({ children, activePage, onNavigate, breadcrumbs }: LayoutProps) {
  const { user, logout } = useAuth();

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-logo">
          <span className="sidebar-logo-mark">PH</span>
          <span className="sidebar-logo-text">PROHUB</span>
        </div>

        <nav className="sidebar-nav">
          <button
            className={`sidebar-item ${activePage === 'companies' ? 'active' : ''}`}
            onClick={() => onNavigate('companies')}
          >
            <IconBuilding />
            Companies
          </button>
          <button
            className={`sidebar-item ${activePage === 'settings' ? 'active' : ''}`}
            onClick={() => onNavigate('settings')}
          >
            <IconSettings />
            Settings
          </button>
        </nav>

        <div className="sidebar-footer">
          {user && (
            <div className="sidebar-user">
              <div className="sidebar-user-avatar">{user.name?.[0]?.toUpperCase() ?? 'U'}</div>
              <div className="sidebar-user-info">
                <span className="sidebar-user-name">{user.name}</span>
                <span className="sidebar-user-email">{user.email}</span>
              </div>
              <button
                className="sidebar-logout"
                onClick={logout}
                title="Sign out"
              >
                <IconLogout />
              </button>
            </div>
          )}
          <span className="sidebar-version">v1.0.0</span>
        </div>
      </aside>

      <div className="main-wrapper">
        <header className="topbar">
          {breadcrumbs && breadcrumbs.length > 0 && (
            <nav className="breadcrumbs" aria-label="Breadcrumb">
              {breadcrumbs.map((crumb, i) => (
                <span key={i} className="breadcrumb-item">
                  {i > 0 && <span className="breadcrumb-sep">/</span>}
                  {crumb.onClick ? (
                    <button className="breadcrumb-link" onClick={crumb.onClick}>
                      {crumb.label}
                    </button>
                  ) : (
                    <span className="breadcrumb-current">{crumb.label}</span>
                  )}
                </span>
              ))}
            </nav>
          )}
        </header>

        <main className="page-content">{children}</main>
      </div>
    </div>
  );
}

function IconBuilding() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="1"/>
      <path d="M9 3v18M15 3v18M3 9h18M3 15h18"/>
    </svg>
  );
}

function IconSettings() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="12" r="3"/>
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
    </svg>
  );
}

function IconLogout() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
      <polyline points="16 17 21 12 16 7"/>
      <line x1="21" y1="12" x2="9" y2="12"/>
    </svg>
  );
}
