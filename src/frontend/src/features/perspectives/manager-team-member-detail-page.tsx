import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';

type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type EmployeeResponse = components['schemas']['EmployeeResponse'];
type PackageResponse = components['schemas']['PackageResponse'];

export default function ManagerTeamMemberDetailPage() {
  const { employeeId } = useParams({ from: '/main/manager/my-team/$employeeId' });
  const actorQuery = useCurrentActor();
  const managerEmployeeId = actorQuery.data?.employeeId ?? null;

  const allowedTeamQuery = useQuery({
    queryKey: ['manager', 'my-team', 'allowed', managerEmployeeId],
    enabled: actorQuery.data?.isManager === true && Boolean(managerEmployeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: {
          query: {
            Query: undefined,
            Status: [],
            OrganizationUnitId: undefined,
            ManagerEmployeeId: managerEmployeeId ?? undefined,
            IncludeIndirectReports: true,
            IncludeDescendants: true,
            Page: 0,
            PageSize: 1000,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not validate team membership.');
      }

      return new Set((data?.items ?? []).map((item) => item.id));
    },
  });

  const isAllowed = allowedTeamQuery.data?.has(employeeId) ?? false;

  const employeeQuery = useQuery({
    queryKey: ['manager', 'my-team', employeeId, 'employee'],
    enabled: allowedTeamQuery.isSuccess && isAllowed,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees/{id}', { params: { path: { id: employeeId } } });

      if (error || !data) {
        throw new Error('Could not load employee.');
      }

      return data;
    },
  });

  const identityId = employeeQuery.data?.identityId ?? null;

  const grantsQuery = useQuery({
    queryKey: ['manager', 'my-team', employeeId, 'grants', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/access-grants', {
        params: { query: { IdentityId: identityId ?? undefined, PackageId: undefined, Status: 'Active', Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load assigned packages.');
      }

      return data?.items ?? [];
    },
  });

  const packageIds = Array.from(new Set((grantsQuery.data ?? []).map((grant) => grant.packageId)));

  const packagesQuery = useQuery({
    queryKey: ['manager', 'my-team', employeeId, 'packages', packageIds.join(',')],
    enabled: packageIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages', {
        params: { query: { Name: undefined, Page: 0, PageSize: 200, ids: packageIds } as never },
      });

      if (error) {
        throw new Error('Could not load package details.');
      }

      return new Map((data?.items ?? []).map((item: PackageResponse) => [item.id, item]));
    },
  });

  const credentialsQuery = useQuery({
    queryKey: ['manager', 'my-team', employeeId, 'credentials', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credentials', {
        params: { query: { CredentialTypeId: undefined, IdentityId: identityId ?? undefined, Status: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load credentials.');
      }

      return (data?.items ?? []).filter((credential: CredentialResponse) => !['Expired', 'Revoked', 'Archived'].includes(credential.status));
    },
  });

  const credentialTypeIds = Array.from(new Set((credentialsQuery.data ?? []).map((credential) => credential.credentialTypeId)));

  const credentialTypesQuery = useQuery({
    queryKey: ['manager', 'my-team', employeeId, 'credential-types', credentialTypeIds.join(',')],
    enabled: credentialTypeIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credential-types', {
        params: { query: { Query: undefined, Technology: undefined, Status: undefined, Page: 0, PageSize: 200, ids: credentialTypeIds } as never },
      });

      if (error) {
        throw new Error('Could not load credential types.');
      }

      return new Map((data?.items ?? []).map((item: CredentialTypeResponse) => [item.id, item]));
    },
  });

  const employee = employeeQuery.data;
  const activeGrants = (grantsQuery.data ?? []).filter((grant: AccessGrantResponse) => grant.status === 'Active');
  const assignedPackages = Array.from(new Map(activeGrants.map((grant: AccessGrantResponse) => [grant.packageId, grant])).values())
    .sort((left, right) => {
      const leftName = packagesQuery.data?.get(left.packageId)?.name ?? left.packageId;
      const rightName = packagesQuery.data?.get(right.packageId)?.name ?? right.packageId;
      return leftName.localeCompare(rightName);
    });
  const credentials = credentialsQuery.data ?? [];

  return (
    <section className="grid gap-6">
      <Link to="/manager/my-team" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back to My Team
      </Link>

      {actorQuery.data && !actorQuery.data.isManager ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Manager access is required to view this page.</p> : null}
      {allowedTeamQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not validate team membership.</p> : null}
      {!allowedTeamQuery.isLoading && allowedTeamQuery.isSuccess && !isAllowed ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">This employee is not in your reporting line.</p> : null}
      {employeeQuery.isLoading ? <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading employee...</p> : null}
      {employeeQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load employee.</p> : null}

      {employee ? (
        <>
          <EmployeeSummary employee={employee} managerEmployeeId={managerEmployeeId} />

          <div className="grid gap-6 xl:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Assigned Packages</CardTitle>
                <CardDescription>Current active package assignments for this employee.</CardDescription>
              </CardHeader>
              <CardContent>
                {grantsQuery.isError || packagesQuery.isError ? <ErrorText message="Could not load assigned packages." /> : null}
                {grantsQuery.isLoading || packagesQuery.isLoading ? <MutedText message="Loading assigned packages..." /> : null}
                {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length === 0 ? <EmptyText message="No active packages assigned." /> : null}
                {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length > 0 ? (
                  <div className="grid gap-3">
                    {assignedPackages.map((grant) => {
                      const pkg = packagesQuery.data?.get(grant.packageId);

                      return (
                        <div key={grant.id} className="rounded-structural border border-border p-4">
                          <div className="flex flex-wrap items-start justify-between gap-3">
                            <div>
                              <p className="font-medium text-foreground">{pkg?.name ?? grant.packageId}</p>
                              <p className="mt-1 text-[13px] text-muted-foreground">{pkg?.description ?? 'Current package assignment.'}</p>
                            </div>
                            <Badge variant="success">Active</Badge>
                          </div>
                          <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground sm:grid-cols-2">
                            <div className="flex items-center justify-between gap-3 sm:block">
                              <dt>Valid from</dt>
                              <dd className="text-right text-foreground sm:mt-1 sm:text-left">{formatDateTimeLabel(grant.validFrom)}</dd>
                            </div>
                            <div className="flex items-center justify-between gap-3 sm:block">
                              <dt>Valid until</dt>
                              <dd className="text-right text-foreground sm:mt-1 sm:text-left">{grant.validUntil ? formatDateTimeLabel(grant.validUntil) : 'No end date'}</dd>
                            </div>
                          </dl>
                        </div>
                      );
                    })}
                  </div>
                ) : null}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Credentials</CardTitle>
                <CardDescription>Current credentials linked to this employee identity.</CardDescription>
              </CardHeader>
              <CardContent>
                {credentialsQuery.isError || credentialTypesQuery.isError ? <ErrorText message="Could not load credentials." /> : null}
                {credentialsQuery.isLoading || credentialTypesQuery.isLoading ? <MutedText message="Loading credentials..." /> : null}
                {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length === 0 ? <EmptyText message="No credentials assigned yet." /> : null}
                {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length > 0 ? (
                  <div className="grid gap-3">
                    {credentials.map((credential) => {
                      const credentialType = credentialTypesQuery.data?.get(credential.credentialTypeId);

                      return (
                        <div key={credential.id} className="rounded-structural border border-border p-4">
                          <div className="flex flex-wrap items-start justify-between gap-3">
                            <div>
                              <p className="font-medium text-foreground">{credentialType?.name ?? credential.formattedIdentifier}</p>
                              <p className="mt-1 text-[13px] text-muted-foreground">{credential.formattedIdentifier}</p>
                            </div>
                            <Badge variant={getCredentialStatusVariant(credential.status)}>{credential.status}</Badge>
                          </div>
                          <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground sm:grid-cols-2">
                            <div className="flex items-center justify-between gap-3 sm:block">
                              <dt>Purpose</dt>
                              <dd className="text-right text-foreground sm:mt-1 sm:text-left">{credential.purpose}</dd>
                            </div>
                            <div className="flex items-center justify-between gap-3 sm:block">
                              <dt>Valid until</dt>
                              <dd className="text-right text-foreground sm:mt-1 sm:text-left">{credential.validUntil ? formatDateTimeLabel(credential.validUntil) : 'No end date'}</dd>
                            </div>
                          </dl>
                        </div>
                      );
                    })}
                  </div>
                ) : null}
              </CardContent>
            </Card>
          </div>
        </>
      ) : null}
    </section>
  );
}

function EmployeeSummary({ employee, managerEmployeeId }: { readonly employee: EmployeeResponse; readonly managerEmployeeId: string | null }) {
  return (
    <Card className="p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-[28px] font-semibold tracking-tight">{employee.firstName} {employee.lastName}</h1>
            <Badge variant={employee.status === 'Active' ? 'success' : 'secondary'}>{employee.status}</Badge>
            <Badge variant="outline">{employee.managerEmployeeId === managerEmployeeId ? 'Direct report' : 'Indirect report'}</Badge>
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">{employee.email ?? 'No email'}</p>
        </div>
      </div>
      <div className="mt-6 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <Info label="Employee Number" value={employee.employeeNumber ?? '-'} />
        <Info label="Organizational Unit" value={employee.organizationUnit.name} />
        <Info label="Job Title" value={employee.jobTitle ?? '-'} />
        <Info label="Reporting Level" value={employee.managerEmployeeId === managerEmployeeId ? 'Direct' : 'Indirect'} />
      </div>
    </Card>
  );
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 break-all text-[14px] font-medium text-foreground">{value}</div></div>;
}

function ErrorText({ message }: { readonly message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { readonly message: string }) {
  return <p className="text-[14px] text-muted-foreground">{message}</p>;
}

function EmptyText({ message }: { readonly message: string }) {
  return <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function getCredentialStatusVariant(status: CredentialResponse['status']) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Suspended':
      return 'warning';
    case 'Issued':
      return 'secondary';
    default:
      return 'outline';
  }
}
