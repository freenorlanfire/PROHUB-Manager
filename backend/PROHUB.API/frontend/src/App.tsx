import { useState } from 'react';
import { useAuth } from './context/AuthContext';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { CompaniesPage } from './pages/CompaniesPage';
import { ProjectsPage } from './pages/ProjectsPage';
import { ProjectDetailPage } from './pages/ProjectDetailPage';
import { SettingsPage } from './pages/SettingsPage';
import { MejorasPage } from './pages/MejorasPage';
import './App.css';

type Route =
  | { page: 'companies' }
  | { page: 'projects'; companyId: string; companyName: string }
  | { page: 'project-detail'; projectId: string; projectName: string; companyId: string; companyName: string }
  | { page: 'settings' }
  | { page: 'mejoras'; projectId?: string; projectName?: string };

function AppRoutes() {
  const [route, setRoute] = useState<Route>({ page: 'companies' });

  function navCompanies() { setRoute({ page: 'companies' }); }
  function navProjects(companyId: string, companyName: string) {
    setRoute({ page: 'projects', companyId, companyName });
  }
  function navProjectDetail(projectId: string, projectName: string, companyId: string, companyName: string) {
    setRoute({ page: 'project-detail', projectId, projectName, companyId, companyName });
  }
  function navSettings() { setRoute({ page: 'settings' }); }
  function navMejoras(projectId?: string, projectName?: string) {
    setRoute({ page: 'mejoras', projectId, projectName });
  }

  const activePage =
    route.page === 'settings' ? 'settings' :
    route.page === 'mejoras'  ? 'mejoras'  :
    'companies';

  const breadcrumbs = buildBreadcrumbs(route, navCompanies, navProjects);

  return (
    <Layout
      activePage={activePage}
      onNavigate={p =>
        p === 'settings' ? navSettings() :
        p === 'mejoras'  ? navMejoras()  :
        navCompanies()
      }
      breadcrumbs={breadcrumbs}
    >
      {route.page === 'companies' && (
        <CompaniesPage onSelectCompany={(id, name) => navProjects(id, name)} />
      )}
      {route.page === 'projects' && (
        <ProjectsPage
          companyId={route.companyId}
          companyName={route.companyName}
          onSelectProject={(id, name) =>
            navProjectDetail(id, name, route.companyId, route.companyName)
          }
        />
      )}
      {route.page === 'project-detail' && (
        <ProjectDetailPage
          projectId={route.projectId}
          companyId={route.companyId}
          onBack={() => navProjects(route.companyId, route.companyName)}
          onMejoras={() => navMejoras(route.projectId, route.projectName)}
        />
      )}
      {route.page === 'settings' && <SettingsPage />}
      {route.page === 'mejoras' && (
        <MejorasPage projectId={route.projectId} projectName={route.projectName} />
      )}
    </Layout>
  );
}

function App() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="boot-screen">
        <span className="sidebar-logo-mark" style={{ width: 40, height: 40, fontSize: 16 }}>PH</span>
      </div>
    );
  }

  if (!isAuthenticated) return <LoginPage />;

  return <AppRoutes />;
}

function buildBreadcrumbs(
  route: Route,
  navCompanies: () => void,
  navProjects: (id: string, name: string) => void
) {
  switch (route.page) {
    case 'companies':
      return [{ label: 'Companies' }];
    case 'projects':
      return [
        { label: 'Companies', onClick: navCompanies },
        { label: route.companyName },
      ];
    case 'project-detail':
      return [
        { label: 'Companies', onClick: navCompanies },
        { label: route.companyName, onClick: () => navProjects(route.companyId, route.companyName) },
        { label: route.projectName },
      ];
    case 'settings':
      return [{ label: 'Settings' }];
    case 'mejoras':
      return [{ label: '⚡ Mejoras' }];
    default:
      return [];
  }
}

export default App;
