import { type FormEvent, useState } from 'react';
import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Pencil, Plus, Trash2, X } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { SiteForm, type SiteFormValues } from './site-form';

type Building = components['schemas']['BuildingResponse'];
type ApprovalGroupMemberResponse = components['schemas']['ApprovalGroupMemberResponse'];
type ApprovalGroupResponse = components['schemas']['ApprovalGroupResponse'];
type IdentityResponse = components['schemas']['IdentityResponse'];

const locationsQueryKey = ['facility', 'locations'] as const;
const approvalGroupsQueryKey = ['administration', 'access-model', 'approval-groups'] as const;
const emptySite: SiteFormValues = { name: '', address: '' };

export default function SiteEditPage() {
  const { siteId } = useParams({ from: '/main/administration/sites/$siteId/edit' });
  const queryClient = useQueryClient();
  const [isAddingBuilding, setIsAddingBuilding] = useState(false);
  const [buildingName, setBuildingName] = useState('');
  const [buildingAddress, setBuildingAddress] = useState('');
  const [openApprovalGroupId, setOpenApprovalGroupId] = useState<string | null>(null);
  const [selectedIdentityId, setSelectedIdentityId] = useState('');

  const siteQuery = useQuery({
    queryKey: [...locationsQueryKey, siteId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: siteId } },
      });

      if (error || !data || data.type !== 'Site') {
        throw new Error('Could not load site.');
      }

      return data.site;
    },
  });

  const updateSite = useMutation({
    mutationFn: async (values: SiteFormValues) => {
      const { error } = await api.PUT('/api/locations/sites/{siteId}', {
        params: { path: { siteId } },
        body: { name: values.name },
      });

      if (error) {
        throw new Error('Could not save site.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: locationsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId] }),
      ]);
      toast.success('Site saved.');
    },
  });

  const buildingsQuery = useQuery({
    queryKey: [...locationsQueryKey, siteId, 'buildings'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites/{siteId}/buildings', {
        params: { path: { siteId } },
      });

      if (error || !data) {
        throw new Error('Could not load buildings.');
      }

      return data;
    },
  });

  const approvalGroupsQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, 'site-details-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-groups', {
        params: { query: { Name: undefined, ids: [], Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load approval groups.');
      }

      return data?.items ?? [];
    },
  });

  const identitiesQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, 'identities'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/identities', {
        params: { query: { query: undefined, status: 'Active', affiliationType: undefined, page: 0, pageSize: 200 } },
      });

      if (error) {
        throw new Error('Could not load identities.');
      }

      return data?.items ?? [];
    },
  });

  const approvalGroupMembersQueries = useQueries({
    queries: (approvalGroupsQuery.data ?? []).map((approvalGroup) => ({
      queryKey: [...approvalGroupsQueryKey, approvalGroup.id, 'members', 'site-details'],
      queryFn: async () => {
        const { data, error } = await api.GET('/api/access-catalog/approval-groups/{approvalGroupId}/members', {
          params: { path: { approvalGroupId: approvalGroup.id }, query: { Page: 0, PageSize: 200 } },
        });

        if (error) {
          throw new Error('Could not load approval group members.');
        }

        return data?.items ?? [];
      },
    })),
  });

  const addApprovalGroupMember = useMutation({
    mutationFn: async ({ approvalGroupId, identityId }: { approvalGroupId: string; identityId: string }) => {
      const { error } = await api.POST('/api/access-catalog/approval-groups/{approvalGroupId}/members', {
        params: { path: { approvalGroupId } },
        body: { identityId, responsibleLocationId: siteId },
      });

      if (error) {
        throw new Error('Could not add approval group member.');
      }
    },
    onSuccess: async (_data, variables) => {
      setOpenApprovalGroupId(null);
      setSelectedIdentityId('');
      await queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, variables.approvalGroupId, 'members', 'site-details'] });
      toast.success('Approval group member added.');
    },
    onError: () => toast.error('Could not add approval group member.'),
  });

  const removeApprovalGroupMember = useMutation({
    mutationFn: async ({ approvalGroupId, memberId }: { approvalGroupId: string; memberId: string }) => {
      const { error } = await api.DELETE('/api/access-catalog/approval-groups/{approvalGroupId}/members/{memberId}', {
        params: { path: { approvalGroupId, memberId } },
      });

      if (error) {
        throw new Error('Could not remove approval group member.');
      }
    },
    onSuccess: async (_data, variables) => {
      await queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, variables.approvalGroupId, 'members', 'site-details'] });
      toast.success('Approval group member removed.');
    },
    onError: () => toast.error('Could not remove approval group member.'),
  });

  const addBuilding = useMutation({
    mutationFn: async (values: { name: string; address: string }) => {
      const { error } = await api.POST('/api/locations/sites/{siteId}/buildings', {
        params: { path: { siteId } },
        body: {
          name: values.name,
          address: values.address || null,
        },
      });

      if (error) {
        throw new Error('Could not add building.');
      }
    },
    onSuccess: async () => {
      setBuildingName('');
      setBuildingAddress('');
      setIsAddingBuilding(false);
      await queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings'] });
      toast.success('Building added.');
    },
  });

  const deleteBuilding = useMutation({
    mutationFn: async (buildingId: string) => {
      const { error } = await api.DELETE('/api/locations/sites/{siteId}/buildings/{buildingId}', {
        params: { path: { siteId, buildingId } },
      });

      if (error) {
        throw new Error('Could not delete building.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings'] });
      toast.success('Building deleted.');
    },
  });

  function handleSubmit(values: SiteFormValues) {
    updateSite.mutate(values);
  }

  function handleAddBuilding(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    addBuilding.mutate({ name: buildingName, address: buildingAddress });
  }

  function handleCancelAddBuilding() {
    setBuildingName('');
    setBuildingAddress('');
    setIsAddingBuilding(false);
  }

  function handleDeleteBuilding(building: Building) {
    const confirmed = window.confirm(`Delete building ${building.name}?`);

    if (confirmed) {
      deleteBuilding.mutate(building.id);
    }
  }

  function handleToggleApprovalGroup(approvalGroupId: string) {
    setSelectedIdentityId('');
    setOpenApprovalGroupId((current) => (current === approvalGroupId ? null : approvalGroupId));
  }

  function handleAddApprovalGroupMember(approvalGroupId: string) {
    if (!selectedIdentityId) {
      return;
    }

    addApprovalGroupMember.mutate({ approvalGroupId, identityId: selectedIdentityId });
  }

  const initialValues: SiteFormValues = siteQuery.data
    ? {
        name: siteQuery.data.name ?? '',
        address: siteQuery.data.address ?? '',
      }
    : emptySite;
  const approvalGroups = approvalGroupsQuery.data ?? [];
  const identitiesById = new Map((identitiesQuery.data ?? []).map((identity: IdentityResponse) => [identity.id, identity]));
  const approvalGroupMembersById = new Map(
    approvalGroups.map((approvalGroup, index) => [approvalGroup.id, approvalGroupMembersQueries[index]?.data ?? []] as const),
  );
  const isApprovalGroupsLoading = approvalGroupsQuery.isLoading || identitiesQuery.isLoading || approvalGroupMembersQueries.some((query) => query.isLoading);
  const isApprovalGroupsError = approvalGroupsQuery.isError || identitiesQuery.isError || approvalGroupMembersQueries.some((query) => query.isError) || addApprovalGroupMember.isError || removeApprovalGroupMember.isError;

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit site</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update site details used across facility workflows.</p>
        </div>
      </header>

      <Card className="p-6">
        {siteQuery.isError || updateSite.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {siteQuery.isError ? 'Could not load site.' : 'Could not save site.'}
          </p>
        ) : null}

        {siteQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading site...</p> : null}

        {!siteQuery.isLoading && !siteQuery.isError ? (
          <SiteForm initialValues={initialValues} isSubmitting={updateSite.isPending} submitLabel="Save" onSubmit={handleSubmit} />
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Buildings</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Manage buildings within this site.</p>
          </div>
          <Button type="button" onClick={() => setIsAddingBuilding((current) => !current)}>
            {isAddingBuilding ? <X className="size-4" aria-hidden="true" /> : <Plus className="size-4" aria-hidden="true" />}
            {isAddingBuilding ? 'Cancel' : 'Add building'}
          </Button>
        </div>

        {buildingsQuery.isError || addBuilding.isError || deleteBuilding.isError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {buildingsQuery.isError ? 'Could not load buildings.' : addBuilding.isError ? 'Could not add building.' : 'Could not delete building.'}
          </p>
        ) : null}

        {isAddingBuilding ? (
          <form className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-[1fr_1fr_auto_auto] md:items-end" onSubmit={handleAddBuilding}>
            <label className="grid gap-2 text-[14px] font-medium">
              Building name
              <input
                className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
                value={buildingName}
                onChange={(event) => setBuildingName(event.target.value)}
                required
              />
            </label>

            <label className="grid gap-2 text-[14px] font-medium">
              Building address
              <input
                className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
                value={buildingAddress}
                onChange={(event) => setBuildingAddress(event.target.value)}
              />
            </label>

            <Button type="submit" disabled={addBuilding.isPending}>
              <Plus className="size-4" aria-hidden="true" />
              {addBuilding.isPending ? 'Adding...' : 'Create'}
            </Button>
            <Button type="button" variant="outline" onClick={handleCancelAddBuilding} disabled={addBuilding.isPending}>
              Cancel
            </Button>
          </form>
        ) : null}

        <BuildingsList buildings={buildingsQuery.data ?? []} isDeleting={deleteBuilding.isPending} isLoading={buildingsQuery.isLoading} siteId={siteId} onDelete={handleDeleteBuilding} />
      </Card>

      <Card className="p-6">
        <div>
          <h3 className="text-[18px] font-semibold tracking-tight">Approval groups</h3>
          <p className="mt-2 text-[14px] text-muted-foreground">Review assigned members for this site in each approval group.</p>
        </div>

        {isApprovalGroupsError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {approvalGroupsQuery.isError
              ? 'Could not load approval groups.'
              : identitiesQuery.isError
                ? 'Could not load identities.'
                : addApprovalGroupMember.isError
                  ? 'Could not add approval group member.'
                  : removeApprovalGroupMember.isError
                    ? 'Could not remove approval group member.'
                    : 'Could not load approval group members.'}
          </p>
        ) : null}

        {isApprovalGroupsLoading ? <p className="text-[14px] text-muted-foreground">Loading approval groups...</p> : null}

        {!isApprovalGroupsLoading && approvalGroups.length === 0 ? (
          <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No approval groups yet.</p>
        ) : null}

        {!isApprovalGroupsLoading && approvalGroups.length > 0 ? (
          <ApprovalGroupsList
            approvalGroups={approvalGroups}
            approvalGroupMembersById={approvalGroupMembersById}
            identitiesById={identitiesById}
            isAddOpenForApprovalGroup={openApprovalGroupId}
            isAddingMember={addApprovalGroupMember.isPending}
            isRemovingMember={removeApprovalGroupMember.isPending}
            selectedIdentityId={selectedIdentityId}
            siteId={siteId}
            onAddMember={handleAddApprovalGroupMember}
            onRemoveMember={(approvalGroupId, memberId) => removeApprovalGroupMember.mutate({ approvalGroupId, memberId })}
            onSelectedIdentityIdChange={setSelectedIdentityId}
            onToggleAddMember={handleToggleApprovalGroup}
          />
        ) : null}
      </Card>
    </div>
  );
}

function ApprovalGroupsList({
  approvalGroups,
  approvalGroupMembersById,
  identitiesById,
  isAddOpenForApprovalGroup,
  isAddingMember,
  isRemovingMember,
  selectedIdentityId,
  siteId,
  onAddMember,
  onRemoveMember,
  onSelectedIdentityIdChange,
  onToggleAddMember,
}: {
  readonly approvalGroups: ApprovalGroupResponse[];
  readonly approvalGroupMembersById: Map<string, ApprovalGroupMemberResponse[]>;
  readonly identitiesById: Map<string, IdentityResponse>;
  readonly isAddOpenForApprovalGroup: string | null;
  readonly isAddingMember: boolean;
  readonly isRemovingMember: boolean;
  readonly selectedIdentityId: string;
  readonly siteId: string;
  readonly onAddMember: (approvalGroupId: string) => void;
  readonly onRemoveMember: (approvalGroupId: string, memberId: string) => void;
  readonly onSelectedIdentityIdChange: (identityId: string) => void;
  readonly onToggleAddMember: (approvalGroupId: string) => void;
}) {
  return (
    <div className="grid gap-3">
      {approvalGroups.map((approvalGroup) => {
        const siteMembers = (approvalGroupMembersById.get(approvalGroup.id) ?? []).filter((member) => member.responsibleLocationId === siteId);
        const siteMemberIdentityIds = new Set(siteMembers.map((member) => member.identityId));
        const availableIdentities = [...identitiesById.values()].filter((identity) => !siteMemberIdentityIds.has(identity.id));
        const isAddOpen = isAddOpenForApprovalGroup === approvalGroup.id;

        return (
          <article key={approvalGroup.id} className="rounded-structural border border-border p-4">
            <div className="grid gap-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                <h4 className="text-[15px] font-semibold text-foreground">{approvalGroup.name}</h4>
                </div>
                <Button type="button" variant="outline" size="sm" disabled={isAddingMember || availableIdentities.length === 0} onClick={() => onToggleAddMember(approvalGroup.id)}>
                  <Plus className="size-4" aria-hidden="true" />
                  {isAddOpen ? 'Cancel' : 'Add member'}
                </Button>
              </div>

              {isAddOpen ? (
                <div className="grid gap-4 rounded-structural border border-border p-4">
                  <label className="grid gap-2 text-[14px] font-medium sm:max-w-96">
                    <span>Identity</span>
                    <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedIdentityId} onChange={(event) => onSelectedIdentityIdChange(event.target.value)} disabled={isAddingMember || availableIdentities.length === 0}>
                      <option value="">Select identity</option>
                      {availableIdentities.map((identity) => <option key={identity.id} value={identity.id}>{identity.displayName}</option>)}
                    </select>
                  </label>
                  <div className="flex justify-end">
                    <Button type="button" disabled={!selectedIdentityId || isAddingMember} onClick={() => onAddMember(approvalGroup.id)}>
                      <Plus className="size-4" aria-hidden="true" />
                      Add member
                    </Button>
                  </div>
                </div>
              ) : null}

              {siteMembers.length === 0 ? (
                <p className="text-[14px] text-muted-foreground">No members assigned yet.</p>
              ) : (
                <ul className="grid gap-2">
                  {siteMembers.map((member) => (
                    <li key={member.id} className="flex items-center justify-between gap-3 rounded-interactive border border-border bg-content px-3 py-2 text-[14px] text-foreground">
                      <span className="min-w-0 truncate">{identitiesById.get(member.identityId)?.displayName ?? member.identityId}</span>
                      <Button type="button" variant="outline" size="sm" disabled={isRemovingMember} onClick={() => onRemoveMember(approvalGroup.id, member.id)}>
                        <Trash2 className="size-4" aria-hidden="true" />
                        Remove
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </article>
        );
      })}
    </div>
  );
}

function BuildingsList({
  buildings,
  isDeleting,
  isLoading,
  siteId,
  onDelete,
}: {
  readonly buildings: Building[];
  readonly isDeleting: boolean;
  readonly isLoading: boolean;
  readonly siteId: string;
  readonly onDelete: (building: Building) => void;
}) {
  if (isLoading) {
    return <p className="text-[14px] text-muted-foreground">Loading buildings...</p>;
  }

  if (buildings.length === 0) {
    return <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No buildings yet.</p>;
  }

  return (
    <div className="grid gap-3">
      <div className="grid gap-3 md:hidden">
        {buildings.map((building) => (
          <article key={building.id} className="rounded-structural border border-border p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h4 className="truncate text-[15px] font-semibold text-foreground">{building.name}</h4>
                <p className="mt-1 text-[14px] text-muted-foreground">{building.address || 'No address'}</p>
              </div>
              <div className="flex shrink-0 gap-2">
                <Link
                  to="/administration/sites/$siteId/buildings/$buildingId/edit"
                  params={{ siteId, buildingId: building.id }}
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                  aria-label={`Edit ${building.name}`}
                >
                  <Pencil className="size-4" aria-hidden="true" />
                </Link>
                <button
                  type="button"
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-error text-error transition hover:bg-error-background disabled:cursor-not-allowed disabled:opacity-60"
                  aria-label={`Delete ${building.name}`}
                  disabled={isDeleting}
                  onClick={() => onDelete(building)}
                >
                  <Trash2 className="size-4" aria-hidden="true" />
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>
      <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
      <table className="w-full min-w-[40rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-semibold">Building</th>
            <th className="px-4 py-3 font-semibold">Address</th>
            <th className="px-4 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {buildings.map((building) => (
            <tr key={building.id}>
              <td className="px-4 py-4 font-medium text-foreground">{building.name}</td>
              <td className="px-4 py-4 text-muted-foreground">{building.address || 'No address'}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link
                    to="/administration/sites/$siteId/buildings/$buildingId/edit"
                    params={{ siteId, buildingId: building.id }}
                    className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                    aria-label={`Edit ${building.name}`}
                  >
                    <Pencil className="size-4" aria-hidden="true" />
                  </Link>
                  <button
                    type="button"
                    className="inline-flex size-9 items-center justify-center rounded-interactive border border-error text-error transition hover:bg-error-background disabled:cursor-not-allowed disabled:opacity-60"
                    aria-label={`Delete ${building.name}`}
                    disabled={isDeleting}
                    onClick={() => onDelete(building)}
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>
    </div>
  );
}
