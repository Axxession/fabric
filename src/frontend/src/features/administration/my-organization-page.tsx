import { useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { Pencil, Plus } from 'lucide-react';
import { useState } from 'react';

import { HostsPanel } from '@/features/visitors-management/hosts-panel';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type Employee = components['schemas']['EmployeeResponse'];
type JobType = components['schemas']['JobTypeResponse'];
type OrganizationUnit = components['schemas']['OrganizationUnitResponse'];
type Persona = components['schemas']['PersonaResponse'];
type EmployeeStatus = components['schemas']['EmployeeStatus'];

type OrganizationTab = 'employees' | 'organizational-units' | 'personas' | 'hosts' | 'contractor-job-types';
type PaginationState = {
  readonly currentPage: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
};

const pageSize = 10;
const employeeStatuses: readonly EmployeeStatus[] = ['PreHire', 'Active', 'Leave', 'Suspended', 'Terminated', 'Archived'];

export default function MyOrganizationPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  const [employeePage, setEmployeePage] = useState(0);
  const [employeeQuery, setEmployeeQuery] = useState('');
  const [employeeStatus, setEmployeeStatus] = useState<'all' | EmployeeStatus>('all');

  const [organizationUnitPage, setOrganizationUnitPage] = useState(0);
  const [organizationUnitQuery, setOrganizationUnitQuery] = useState('');
  const [organizationUnitState, setOrganizationUnitState] = useState<'all' | 'active' | 'inactive'>('all');

  const [personaPage, setPersonaPage] = useState(0);
  const [personaQuery, setPersonaQuery] = useState('');
  const [personaState, setPersonaState] = useState<'all' | 'active' | 'inactive'>('all');

  const [jobTypePage, setJobTypePage] = useState(0);
  const [jobTypeQuery, setJobTypeQuery] = useState('');
  const [jobTypeState, setJobTypeState] = useState<'all' | 'active' | 'inactive'>('all');

  const employeesQuery = useQuery({
    queryKey: ['administration', 'my-organization', 'employees', employeePage, employeeQuery, employeeStatus],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: {
          query: {
            Query: employeeQuery || undefined,
            Status: employeeStatus === 'all' ? [] : [employeeStatus],
            OrganizationUnitId: undefined,
            IncludeDescendants: true,
            Page: employeePage,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load employees.');
      }

      return data;
    },
  });

  const organizationUnitsQuery = useQuery({
    queryKey: ['administration', 'my-organization', 'organization-units', organizationUnitPage, organizationUnitQuery, organizationUnitState],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', {
        params: {
          query: {
            Query: organizationUnitQuery || undefined,
            ParentId: undefined,
            IsActive: organizationUnitState === 'all' ? undefined : organizationUnitState === 'active',
            Page: organizationUnitPage,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load organizational units.');
      }

      return data;
    },
  });

  const personasQuery = useQuery({
    queryKey: ['administration', 'my-organization', 'personas', personaPage, personaQuery, personaState],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/personas', {
        params: {
          query: {
            Query: personaQuery || undefined,
            IsActive: personaState === 'all' ? undefined : personaState === 'active',
            Page: personaPage,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load personas.');
      }

      return data;
    },
  });

  const jobTypesQuery = useQuery({
    queryKey: ['administration', 'my-organization', 'contractor-job-types', jobTypePage, jobTypeQuery, jobTypeState],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/job-types', {
        params: {
          query: {
            Query: jobTypeQuery || undefined,
            IsActive: jobTypeState === 'all' ? undefined : jobTypeState === 'active',
            Page: jobTypePage,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load contractor job types.');
      }

      return data;
    },
  });

  function changeTab(nextTab: string) {
    if (!isOrganizationTab(nextTab)) {
      return;
    }

    void navigate({ to: '/administration/my-organization', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
        <Tabs value={activeTab} onValueChange={changeTab}>
          <TabsList>
            <TabsTrigger value="employees">Employees</TabsTrigger>
            <TabsTrigger value="organizational-units">Organizational Units</TabsTrigger>
            <TabsTrigger value="personas">Personas</TabsTrigger>
            <TabsTrigger value="contractor-job-types">Contractor Job Types</TabsTrigger>
            <TabsTrigger value="hosts">Hosts</TabsTrigger>
          </TabsList>

          <TabsContent value="employees">
            <EmployeesPanel
              query={employeeQuery}
              onQueryChange={(value) => {
                setEmployeeQuery(value);
                setEmployeePage(0);
              }}
              status={employeeStatus}
              onStatusChange={(value) => {
                setEmployeeStatus(value);
                setEmployeePage(0);
              }}
              response={employeesQuery.data}
              isLoading={employeesQuery.isLoading}
              isError={employeesQuery.isError}
              page={employeePage}
              setPage={setEmployeePage}
            />
          </TabsContent>

          <TabsContent value="organizational-units">
            <OrganizationUnitsPanel
              query={organizationUnitQuery}
              onQueryChange={(value) => {
                setOrganizationUnitQuery(value);
                setOrganizationUnitPage(0);
              }}
              state={organizationUnitState}
              onStateChange={(value) => {
                setOrganizationUnitState(value);
                setOrganizationUnitPage(0);
              }}
              response={organizationUnitsQuery.data}
              isLoading={organizationUnitsQuery.isLoading}
              isError={organizationUnitsQuery.isError}
              page={organizationUnitPage}
              setPage={setOrganizationUnitPage}
            />
          </TabsContent>

          <TabsContent value="personas">
            <PersonasPanel
              query={personaQuery}
              onQueryChange={(value) => {
                setPersonaQuery(value);
                setPersonaPage(0);
              }}
              state={personaState}
              onStateChange={(value) => {
                setPersonaState(value);
                setPersonaPage(0);
              }}
              response={personasQuery.data}
              isLoading={personasQuery.isLoading}
              isError={personasQuery.isError}
              page={personaPage}
              setPage={setPersonaPage}
            />
          </TabsContent>

          <TabsContent value="contractor-job-types">
            <ContractorJobTypesPanel
              query={jobTypeQuery}
              onQueryChange={(value) => {
                setJobTypeQuery(value);
                setJobTypePage(0);
              }}
              state={jobTypeState}
              onStateChange={(value) => {
                setJobTypeState(value);
                setJobTypePage(0);
              }}
              response={jobTypesQuery.data}
              isLoading={jobTypesQuery.isLoading}
              isError={jobTypesQuery.isError}
              page={jobTypePage}
              setPage={setJobTypePage}
            />
          </TabsContent>

          <TabsContent value="hosts">
            <HostsPanel />
          </TabsContent>
      </Tabs>
    </section>
  );
}

function EmployeesPanel({
  query,
  onQueryChange,
  status,
  onStatusChange,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly query: string;
  readonly onQueryChange: (value: string) => void;
  readonly status: 'all' | EmployeeStatus;
  readonly onStatusChange: (value: 'all' | EmployeeStatus) => void;
  readonly response: components['schemas']['PageOfEmployeeResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const employees = response?.items ?? [];
  const pagination = getPaginationState(response, employees.length, page, pageSize);

  return (
    <ListSection
      title="Employees"
      description="Paged list of employees and their current organizational placement."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load employees."
      emptyTitle="No employees found"
      emptyDescription="Try a different search or filter."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/my-organization/employees/new" className={buttonVariants() }><Plus className="size-4" aria-hidden="true" />Add employee</Link>}
      filters={
        <>
          <FilterInput label="Search employees" value={query} onChange={onQueryChange} placeholder="Search name, email, or employee number" />
          <FilterSelect label="Status" value={status} onChange={(value) => onStatusChange(value as 'all' | EmployeeStatus)} options={[{ value: 'all', label: 'All statuses' }, ...employeeStatuses.map((value) => ({ value, label: value }))]} />
        </>
      }
      table={
        <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Employee Number</th>
              <th className="px-4 py-3 font-semibold">Email</th>
              <th className="px-4 py-3 font-semibold">Organizational Unit</th>
              <th className="px-4 py-3 font-semibold">Job Title</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {employees.map((employee) => (
              <tr key={employee.id}>
                <td className="px-4 py-4 font-medium text-foreground">{employee.firstName} {employee.lastName}</td>
                <td className="px-4 py-4 text-muted-foreground">{employee.employeeNumber ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{employee.email ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{employee.organizationUnit?.name ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{employee.jobTitle ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{employee.status}</td>
                <td className="px-4 py-4">
                  <div className="flex justify-end">
                    <Link to="/administration/my-organization/employees/$employeeId/edit" params={{ employeeId: employee.id }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${employee.firstName} ${employee.lastName}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {employees.map((employee) => (
            <article key={employee.id} className="rounded-structural border border-border p-4">
              <h3 className="text-[15px] font-semibold text-foreground">{employee.firstName} {employee.lastName}</h3>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Employee Number</dt><dd>{employee.employeeNumber ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Email</dt><dd>{employee.email ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Organizational Unit</dt><dd>{employee.organizationUnit?.name ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Job Title</dt><dd>{employee.jobTitle ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Status</dt><dd>{employee.status}</dd></div>
                <div className="pt-2"><Link to="/administration/my-organization/employees/$employeeId/edit" params={{ employeeId: employee.id }} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${employee.firstName} ${employee.lastName}`}><Pencil className="size-4" aria-hidden="true" /></Link></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={employees.length > 0}
    />
  );
}

function OrganizationUnitsPanel({
  query,
  onQueryChange,
  state,
  onStateChange,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly query: string;
  readonly onQueryChange: (value: string) => void;
  readonly state: 'all' | 'active' | 'inactive';
  readonly onStateChange: (value: 'all' | 'active' | 'inactive') => void;
  readonly response: components['schemas']['PageOfOrganizationUnitResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const units = response?.items ?? [];
  const pagination = getPaginationState(response, units.length, page, pageSize);

  return (
    <ListSection
      title="Organizational Units"
      description="Paged list of organizational units and hierarchy metrics."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load organizational units."
      emptyTitle="No organizational units found"
      emptyDescription="Try a different search or filter."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/my-organization/organizational-units/new" className={buttonVariants() }><Plus className="size-4" aria-hidden="true" />Add organizational unit</Link>}
      filters={
        <>
          <FilterInput label="Search organizational units" value={query} onChange={onQueryChange} placeholder="Search name or code" />
          <FilterSelect label="State" value={state} onChange={(value) => onStateChange(value as 'all' | 'active' | 'inactive')} options={[{ value: 'all', label: 'All units' }, { value: 'active', label: 'Active only' }, { value: 'inactive', label: 'Inactive only' }]} />
        </>
      }
      table={
        <table className="w-full min-w-[60rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Code</th>
              <th className="px-4 py-3 font-semibold">Type</th>
              <th className="px-4 py-3 font-semibold">Parent</th>
              <th className="px-4 py-3 font-semibold">Depth</th>
              <th className="px-4 py-3 font-semibold">Employees</th>
              <th className="px-4 py-3 font-semibold">State</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {units.map((unit) => (
              <tr key={unit.id}>
                <td className="px-4 py-4 font-medium text-foreground">{unit.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{unit.code ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{unit.type}</td>
                <td className="px-4 py-4 text-muted-foreground">{unit.parentId ?? 'Root'}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatCount(unit.depth)}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatCount(unit.employeeCount)}</td>
                <td className="px-4 py-4 text-muted-foreground">{unit.isActive ? 'Active' : 'Inactive'}</td>
                <td className="px-4 py-4">
                  <div className="flex justify-end">
                    <Link to="/administration/my-organization/organizational-units/$organizationUnitId/edit" params={{ organizationUnitId: unit.id }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${unit.name}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {units.map((unit) => (
            <article key={unit.id} className="rounded-structural border border-border p-4">
              <h3 className="text-[15px] font-semibold text-foreground">{unit.name}</h3>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Code</dt><dd>{unit.code ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Type</dt><dd>{unit.type}</dd></div>
                <div><dt className="font-medium text-foreground">Parent</dt><dd>{unit.parentId ?? 'Root'}</dd></div>
                <div><dt className="font-medium text-foreground">Depth</dt><dd>{formatCount(unit.depth)}</dd></div>
                <div><dt className="font-medium text-foreground">Employees</dt><dd>{formatCount(unit.employeeCount)}</dd></div>
                <div><dt className="font-medium text-foreground">State</dt><dd>{unit.isActive ? 'Active' : 'Inactive'}</dd></div>
                <div className="pt-2"><Link to="/administration/my-organization/organizational-units/$organizationUnitId/edit" params={{ organizationUnitId: unit.id }} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${unit.name}`}><Pencil className="size-4" aria-hidden="true" /></Link></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={units.length > 0}
    />
  );
}

function PersonasPanel({
  query,
  onQueryChange,
  state,
  onStateChange,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly query: string;
  readonly onQueryChange: (value: string) => void;
  readonly state: 'all' | 'active' | 'inactive';
  readonly onStateChange: (value: 'all' | 'active' | 'inactive') => void;
  readonly response: components['schemas']['PageOfPersonaResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const personas = response?.items ?? [];
  const pagination = getPaginationState(response, personas.length, page, pageSize);

  return (
    <ListSection
      title="Personas"
      description="Paged list of personas assigned across the employee domain."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load personas."
      emptyTitle="No personas found"
      emptyDescription="Try a different search or filter."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/my-organization/personas/new" className={buttonVariants() }><Plus className="size-4" aria-hidden="true" />Add persona</Link>}
      filters={
        <>
          <FilterInput label="Search personas" value={query} onChange={onQueryChange} placeholder="Search persona name" />
          <FilterSelect label="State" value={state} onChange={(value) => onStateChange(value as 'all' | 'active' | 'inactive')} options={[{ value: 'all', label: 'All personas' }, { value: 'active', label: 'Active only' }, { value: 'inactive', label: 'Inactive only' }]} />
        </>
      }
      table={
        <table className="w-full min-w-[52rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">State</th>
              <th className="px-4 py-3 font-semibold">Created</th>
              <th className="px-4 py-3 font-semibold">Updated</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {personas.map((persona) => (
              <tr key={persona.id}>
                <td className="px-4 py-4 font-medium text-foreground">{persona.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{persona.isActive ? 'Active' : 'Inactive'}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTime(persona.createdAt)}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTime(persona.updatedAt)}</td>
                <td className="px-4 py-4">
                  <div className="flex justify-end">
                    <Link to="/administration/my-organization/personas/$personaId/edit" params={{ personaId: persona.id }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${persona.name}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {personas.map((persona) => (
            <article key={persona.id} className="rounded-structural border border-border p-4">
              <h3 className="text-[15px] font-semibold text-foreground">{persona.name}</h3>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">State</dt><dd>{persona.isActive ? 'Active' : 'Inactive'}</dd></div>
                <div><dt className="font-medium text-foreground">Created</dt><dd>{formatDateTime(persona.createdAt)}</dd></div>
                <div><dt className="font-medium text-foreground">Updated</dt><dd>{formatDateTime(persona.updatedAt)}</dd></div>
                <div className="pt-2"><Link to="/administration/my-organization/personas/$personaId/edit" params={{ personaId: persona.id }} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${persona.name}`}><Pencil className="size-4" aria-hidden="true" /></Link></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={personas.length > 0}
    />
  );
}

function ContractorJobTypesPanel({
  query,
  onQueryChange,
  state,
  onStateChange,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly query: string;
  readonly onQueryChange: (value: string) => void;
  readonly state: 'all' | 'active' | 'inactive';
  readonly onStateChange: (value: 'all' | 'active' | 'inactive') => void;
  readonly response: components['schemas']['PageOfJobTypeResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const jobTypes = response?.items ?? [];
  const pagination = getPaginationState(response, jobTypes.length, page, pageSize);

  return (
    <ListSection
      title="Contractor Job Types"
      description="Paged list of contractor job types used by contractor planning and requirement derivation."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load contractor job types."
      emptyTitle="No contractor job types found"
      emptyDescription="Try a different search or filter."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/my-organization/contractor-job-types/new" className={buttonVariants()}><Plus className="size-4" aria-hidden="true" />Add contractor job type</Link>}
      filters={
        <>
          <FilterInput label="Search contractor job types" value={query} onChange={onQueryChange} placeholder="Search code, name, or description" />
          <FilterSelect label="State" value={state} onChange={(value) => onStateChange(value as 'all' | 'active' | 'inactive')} options={[{ value: 'all', label: 'All job types' }, { value: 'active', label: 'Active only' }, { value: 'inactive', label: 'Inactive only' }]} />
        </>
      }
      table={
        <table className="w-full min-w-[60rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Code</th>
              <th className="px-4 py-3 font-semibold">Description</th>
              <th className="px-4 py-3 font-semibold">State</th>
              <th className="px-4 py-3 font-semibold">Created</th>
              <th className="px-4 py-3 font-semibold">Updated</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {jobTypes.map((jobType) => (
              <tr key={jobType.id}>
                <td className="px-4 py-4 font-medium text-foreground">{jobType.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{jobType.code}</td>
                <td className="px-4 py-4 text-muted-foreground">{jobType.description ?? '-'}</td>
                <td className="px-4 py-4 text-muted-foreground">{jobType.isActive ? 'Active' : 'Inactive'}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTime(jobType.createdAt)}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTime(jobType.updatedAt)}</td>
                <td className="px-4 py-4">
                  <div className="flex justify-end">
                    <Link to="/administration/my-organization/contractor-job-types/$jobTypeId/edit" params={{ jobTypeId: jobType.id }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${jobType.name}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {jobTypes.map((jobType) => (
            <article key={jobType.id} className="rounded-structural border border-border p-4">
              <h3 className="text-[15px] font-semibold text-foreground">{jobType.name}</h3>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Code</dt><dd>{jobType.code}</dd></div>
                <div><dt className="font-medium text-foreground">Description</dt><dd>{jobType.description ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">State</dt><dd>{jobType.isActive ? 'Active' : 'Inactive'}</dd></div>
                <div><dt className="font-medium text-foreground">Created</dt><dd>{formatDateTime(jobType.createdAt)}</dd></div>
                <div><dt className="font-medium text-foreground">Updated</dt><dd>{formatDateTime(jobType.updatedAt)}</dd></div>
                <div className="pt-2"><Link to="/administration/my-organization/contractor-job-types/$jobTypeId/edit" params={{ jobTypeId: jobType.id }} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${jobType.name}`}><Pencil className="size-4" aria-hidden="true" /></Link></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={jobTypes.length > 0}
    />
  );
}

function ListSection({
  title,
  description,
  isLoading,
  isError,
  errorMessage,
  emptyTitle,
  emptyDescription,
  totalItems,
  firstItem,
  lastItem,
  currentPage,
  totalPages,
  visiblePages,
  setPage,
  actions,
  filters,
  table,
  mobileList,
  hasItems,
}: {
  readonly title: string;
  readonly description: string;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly errorMessage: string;
  readonly emptyTitle: string;
  readonly emptyDescription: string;
  readonly totalItems: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly currentPage: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
  readonly setPage: (page: number) => void;
  readonly actions?: React.ReactNode;
  readonly filters: React.ReactNode;
  readonly table: React.ReactNode;
  readonly mobileList: React.ReactNode;
  readonly hasItems: boolean;
}) {
  return (
    <div className="grid gap-4 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        {actions ? <div>{actions}</div> : null}
      </div>

      <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">{filters}</div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{errorMessage}</p> : null}

      {!isLoading && !isError && !hasItems ? (
        <Empty>
          <EmptyHeader>
            <EmptyTitle>{emptyTitle}</EmptyTitle>
            <EmptyDescription>{emptyDescription}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="grid gap-4">
          <div className="md:hidden">
            {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading...</p> : null}
            {!isLoading ? mobileList : null}
          </div>

          <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
            {isLoading ? <p className="px-4 py-5 text-[14px] text-muted-foreground">Loading...</p> : table}
          </div>

          {!isLoading && !isError && totalItems > 0 ? (
            <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
              <p>Showing {firstItem}-{lastItem} of {totalItems}</p>
              <Pagination className="sm:mx-0 sm:w-auto">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious disabled={currentPage === 0} onClick={() => setPage(Math.max(0, currentPage - 1))} />
                  </PaginationItem>
                  {visiblePages.map((visiblePage, index) =>
                    visiblePage === 'ellipsis' ? (
                      <PaginationItem key={`${visiblePage}-${index}`}>
                        <PaginationEllipsis />
                      </PaginationItem>
                    ) : (
                      <PaginationItem key={visiblePage}>
                        <PaginationLink isActive={visiblePage === currentPage} onClick={() => setPage(visiblePage)}>
                          {visiblePage + 1}
                        </PaginationLink>
                      </PaginationItem>
                    ),
                  )}
                  <PaginationItem>
                    <PaginationNext disabled={currentPage >= totalPages - 1} onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))} />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}

function FilterInput({ label, value, onChange, placeholder }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly placeholder: string }) {
  return (
    <label className="grid gap-2 text-[14px] font-medium">
      <span>{label}</span>
      <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} />
    </label>
  );
}

function FilterSelect({ label, value, onChange, options }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly options: readonly { value: string; label: string }[] }) {
  return (
    <label className="grid gap-2 text-[14px] font-medium">
      <span>{label}</span>
      <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)}>
        {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    </label>
  );
}

function getActiveTab(searchStr: string): OrganizationTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isOrganizationTab(tab) ? tab : 'employees';
}

function isOrganizationTab(value: string | null | undefined): value is OrganizationTab {
  return value === 'employees' || value === 'organizational-units' || value === 'personas' || value === 'hosts' || value === 'contractor-job-types';
}

function getPaginationState(page: { currentPage?: number | string; totalPages?: null | number | string; totalItems?: null | number | string } | undefined, itemCount: number, requestedPage: number, resolvedPageSize: number): PaginationState {
  const totalItems = Number(page?.totalItems ?? itemCount);
  const totalPages = Math.max(Number(page?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(page?.currentPage ?? requestedPage), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * resolvedPageSize + 1;
  const lastItem = Math.min((currentPage + 1) * resolvedPageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);
  return { currentPage, firstItem, lastItem, totalItems, totalPages, visiblePages };
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = Array.from(pages).filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages).sort((left, right) => left - right);
  const visiblePages: Array<number | 'ellipsis'> = [];

  sortedPages.forEach((pageNumber, index) => {
    if (index > 0 && pageNumber - sortedPages[index - 1] > 1) {
      visiblePages.push('ellipsis');
    }

    visiblePages.push(pageNumber);
  });

  return visiblePages;
}

function formatCount(value: number | string | null | undefined) {
  return value === null || value === undefined ? '-' : String(value);
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}
