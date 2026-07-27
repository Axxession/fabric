import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { ApprovalGroupForm, type ApprovalGroupFormValues } from './approval-group-form';

type ApprovalGroupMemberResponse = components['schemas']['ApprovalGroupMemberResponse'];
type ApprovalGroupResponse = components['schemas']['ApprovalGroupResponse'];
type CreateApprovalGroupMemberRequest = components['schemas']['CreateApprovalGroupMemberRequest'];
type IdentityResponse = components['schemas']['IdentityResponse'];
type UpdateApprovalGroupRequest = components['schemas']['UpdateApprovalGroupRequest'];

const approvalGroupsQueryKey = ['administration', 'access-model', 'approval-groups'] as const;

export default function ApprovalGroupEditPage() {
  const { approvalGroupId } = useParams({ from: '/main/administration/access-model/approval-groups/$approvalGroupId/edit' });
  const queryClient = useQueryClient();
  const [selectedIdentityId, setSelectedIdentityId] = useState('');
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);
  const [isAddOpen, setIsAddOpen] = useState(false);

  const approvalGroupQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, approvalGroupId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-groups/{approvalGroupId}', { params: { path: { approvalGroupId } } });
      if (error || !data) throw new Error('Could not load approval group.');
      return data;
    },
  });

  const membersQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'members'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-groups/{approvalGroupId}/members', { params: { path: { approvalGroupId }, query: { Page: 0, PageSize: 200 } } });
      if (error) throw new Error('Could not load approval group members.');
      return data;
    },
  });

  const identitiesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'approval-groups', 'identities'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/identities', { params: { query: { query: undefined, status: 'Active', affiliationType: undefined, page: 0, pageSize: 200 } } });
      if (error) throw new Error('Could not load identities.');
      return data?.items ?? [];
    },
  });

  const memberLocationDetailsQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'member-location-details', membersQuery.data?.items?.map((item) => item.responsibleLocationId).join(',') ?? ''],
    enabled: Boolean(membersQuery.data?.items?.length),
    queryFn: async () => {
      const locations = await Promise.all((membersQuery.data?.items ?? []).map(async (member) => {
        const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: member.responsibleLocationId } } });
        if (error || !data) throw new Error('Could not load member location details.');
        return data;
      }));
      return new Map(locations.map((location) => [location.id, location]));
    },
  });

  const updateApprovalGroup = useMutation({
    mutationFn: async (request: UpdateApprovalGroupRequest) => {
      const { error } = await api.PUT('/api/access-catalog/approval-groups/{approvalGroupId}', { params: { path: { approvalGroupId } }, body: request });
      if (error) throw new Error('Could not save approval group.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: approvalGroupsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, approvalGroupId] }),
      ]);
      toast.success('Approval group saved.');
    },
    onError: () => toast.error('Could not save approval group.'),
  });

  const addMember = useMutation({
    mutationFn: async (request: CreateApprovalGroupMemberRequest) => {
      const { error } = await api.POST('/api/access-catalog/approval-groups/{approvalGroupId}/members', { params: { path: { approvalGroupId } }, body: request });
      if (error) throw new Error('Could not add approval group member.');
    },
    onSuccess: async () => {
      setSelectedIdentityId('');
      setSelectedLocationId(null);
      setIsAddOpen(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'members'] }),
        queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'member-location-details'] }),
      ]);
      toast.success('Approval group member added.');
    },
    onError: () => toast.error('Could not add approval group member.'),
  });

  const removeMember = useMutation({
    mutationFn: async (memberId: string) => {
      const { error } = await api.DELETE('/api/access-catalog/approval-groups/{approvalGroupId}/members/{memberId}', { params: { path: { approvalGroupId, memberId } } });
      if (error) throw new Error('Could not remove approval group member.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'members'] }),
        queryClient.invalidateQueries({ queryKey: [...approvalGroupsQueryKey, approvalGroupId, 'member-location-details'] }),
      ]);
      toast.success('Approval group member removed.');
    },
    onError: () => toast.error('Could not remove approval group member.'),
  });

  const currentApprovalGroup = approvalGroupQuery.data;
  const members = membersQuery.data?.items ?? [];
  const identitiesById = new Map((identitiesQuery.data ?? []).map((item) => [item.id, item]));
  const memberIdentityIds = new Set(members.map((item) => item.identityId));
  const availableIdentities = (identitiesQuery.data ?? []).filter((item) => !memberIdentityIds.has(item.id));
  const memberLocationDetails = memberLocationDetailsQuery.data ?? new Map<string, LocationResponse>();

  function handleSubmit(values: ApprovalGroupFormValues) {
    updateApprovalGroup.mutate({ name: values.name, status: values.status });
  }

  function handleAddMember() {
    if (!selectedIdentityId || !selectedLocationId) {
      return;
    }
    addMember.mutate({ identityId: selectedIdentityId, responsibleLocationId: selectedLocationId });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit approval group</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update approval group details and manage members.</p>
        </div>
      </header>

      <Card className="p-6">
        {approvalGroupQuery.isError || updateApprovalGroup.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{approvalGroupQuery.isError ? 'Could not load approval group.' : 'Could not save approval group.'}</p> : null}
        {approvalGroupQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading approval group...</p> : null}
        {!approvalGroupQuery.isLoading && currentApprovalGroup && !approvalGroupQuery.isError ? <ApprovalGroupForm initialValues={toFormValues(currentApprovalGroup)} isSubmitting={updateApprovalGroup.isPending} submitLabel="Save" includeStatus onSubmit={handleSubmit} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Members</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Add approvers to the group and remove them when no longer needed.</p>
          </div>
          <Button type="button" variant="outline" disabled={addMember.isPending || availableIdentities.length === 0} onClick={() => setIsAddOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAddOpen ? 'Cancel' : 'Add'}
          </Button>
        </div>

        {isAddOpen ? (
          <div className="grid gap-4 rounded-structural border border-border p-4">
            <label className="grid gap-2 text-[14px] font-medium sm:max-w-96">
              <span>Identity</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedIdentityId} onChange={(event) => setSelectedIdentityId(event.target.value)} disabled={addMember.isPending || availableIdentities.length === 0}>
                <option value="">Select identity</option>
                {availableIdentities.map((identity: IdentityResponse) => <option key={identity.id} value={identity.id}>{identity.displayName}</option>)}
              </select>
            </label>
            <LocationSelector value={selectedLocationId} onChange={setSelectedLocationId} level="Site" disabled={addMember.isPending} />
            <div className="flex justify-end">
              <Button type="button" disabled={!selectedIdentityId || !selectedLocationId || addMember.isPending} onClick={handleAddMember}>
                <Plus className="size-4" aria-hidden="true" />
                Add member
              </Button>
            </div>
          </div>
        ) : null}

        {membersQuery.isError || identitiesQuery.isError || memberLocationDetailsQuery.isError || addMember.isError || removeMember.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{membersQuery.isError ? 'Could not load approval group members.' : identitiesQuery.isError ? 'Could not load identities.' : memberLocationDetailsQuery.isError ? 'Could not load member locations.' : addMember.isError ? 'Could not add approval group member.' : 'Could not remove approval group member.'}</p> : null}
        {membersQuery.isLoading || identitiesQuery.isLoading || memberLocationDetailsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading members...</p> : null}
        {!membersQuery.isLoading && members.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No members added yet.</p> : null}

        {members.length > 0 ? (
          <div className="grid gap-3">
            {members.map((member: ApprovalGroupMemberResponse) => {
              const identity = identitiesById.get(member.identityId);
              return (
                <div key={member.id} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                  <div className="min-w-0">
                    <p className="font-medium text-foreground">{identity?.displayName ?? member.identityId}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{getLocationLabel(memberLocationDetails.get(member.responsibleLocationId))}</p>
                  </div>
                  <Button type="button" variant="outline" size="sm" disabled={removeMember.isPending} onClick={() => removeMember.mutate(member.id)}>
                    <Trash2 className="size-4" aria-hidden="true" />
                    Remove
                  </Button>
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(approvalGroup: ApprovalGroupResponse): ApprovalGroupFormValues {
  return {
    name: approvalGroup.name,
    status: approvalGroup.status,
  };
}
