export interface Company {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  logoUrl: string | null;
  websiteUrl: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Project {
  id: string;
  companyId: string;
  name: string;
  slug: string;
  description: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  archivedAtUtc: string | null;
}

export interface StatusEntry {
  id: string;
  projectId: string;
  status: string;
  note: string | null;
  createdBy: string | null;
  createdAtUtc: string;
}

export interface ContextDoc {
  id: string;
  projectId: string;
  content: string;
  updatedAtUtc: string;
  updatedBy: string | null;
}

export interface ContextDocVersion {
  id: string;
  contextDocId: string;
  projectId: string;
  content: string;
  version: number;
  createdAtUtc: string;
  createdBy: string | null;
}

export interface IntegrationLink {
  id: string;
  projectId: string;
  type: string;
  url: string;
  label: string | null;
  updatedAtUtc: string;
}

export interface Tag {
  id: string;
  projectId: string;
  tag: string;
}

export interface ProjectLink {
  id: string;
  projectId: string;
  label: string;
  url: string;
  createdAtUtc: string;
}

export interface HealthInfo {
  status: string;
  version: string;
  timestamp: string;
  environment: string;
}
