import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { useState, type FormEvent, type ReactNode } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge, type BadgeVariant } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type KioskProfile = components['schemas']['KioskProfileResponse'];
type Kiosk = components['schemas']['KioskResponse'] & {
  readonly activeSessionId?: string | null;
  readonly activeSessionStatus?: string | null;
};

type Tab = 'kiosks' | 'profiles';
type ProfileForm = { readonly name: string; readonly defaultLanguageCode: string };
type KioskForm = { readonly name: string; readonly profileId: string; readonly showDetailedErrors: boolean };

const pageSize = 100;
const profilesQueryKey = ['automation', 'kiosk', 'profiles'] as const;
const kiosksQueryKey = ['automation', 'kiosk', 'kiosks'] as const;

export function WorkflowKiosksPanel() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('kiosks');
  const [isProfileFormOpen, setIsProfileFormOpen] = useState(false);
  const [isKioskFormOpen, setIsKioskFormOpen] = useState(false);
  const [profileForm, setProfileForm] = useState<ProfileForm>({ name: '', defaultLanguageCode: 'en' });
  const [kioskForm, setKioskForm] = useState<KioskForm>({ name: '', profileId: '', showDetailedErrors: false });

  const profilesQuery = useQuery({
    queryKey: profilesQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/kiosk-profiles', { params: { query: { Page: 0, PageSize: pageSize } } });
      if (error || !data) throw new Error('Could not load profiles.');
      return data.items ?? [];
    },
  });

  const kiosksQuery = useQuery({
    queryKey: kiosksQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/kiosks', { params: { query: { Page: 0, PageSize: pageSize } } });
      if (error || !data) throw new Error('Could not load kiosks.');
      return data.items ?? [];
    },
  });

  const profiles = profilesQuery.data ?? [];
  const kiosks = kiosksQuery.data ?? [];

  const createProfile = useMutation({
    mutationFn: async (request: ProfileForm) => {
      const { error } = await api.POST('/api/kiosk-profiles', { body: request });
      if (error) throw new Error('Could not create profile.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: profilesQueryKey });
      setProfileForm({ name: '', defaultLanguageCode: 'en' });
      setIsProfileFormOpen(false);
      toast.success('Kiosk profile created.');
    },
    onError: () => toast.error('Could not create kiosk profile.'),
  });

  const deleteProfile = useMutation({
    mutationFn: async (profile: KioskProfile) => {
      const { error } = await api.DELETE('/api/kiosk-profiles/{id}', { params: { path: { id: profile.id } } });
      if (error) throw new Error('Could not delete profile.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: profilesQueryKey });
      toast.success('Kiosk profile deleted.');
    },
    onError: () => toast.error('Could not delete kiosk profile. Check if kiosks use it.'),
  });

  const createKiosk = useMutation({
    mutationFn: async (request: KioskForm) => {
      const { error } = await api.POST('/api/kiosks', { body: request });
      if (error) throw new Error('Could not create kiosk.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      setKioskForm({ name: '', profileId: profiles[0]?.id ?? '', showDetailedErrors: false });
      setIsKioskFormOpen(false);
      toast.success('Kiosk created.');
    },
    onError: () => toast.error('Could not create kiosk.'),
  });

  const deleteKiosk = useMutation({
    mutationFn: async (kiosk: Kiosk) => {
      const { error } = await api.DELETE('/api/kiosks/{id}' as never, { params: { path: { id: kiosk.id } } } as never);
      if (error) throw new Error('Could not delete kiosk.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      toast.success('Kiosk deleted.');
    },
    onError: () => toast.error('Could not delete kiosk.'),
  });

  function submitProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createProfile.mutate(profileForm);
  }

  function submitKiosk(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createKiosk.mutate({ ...kioskForm, profileId: kioskForm.profileId || profiles[0]?.id || '' });
  }

  function confirmDeleteKiosk(kiosk: Kiosk) {
    if (window.confirm(`Delete kiosk "${kiosk.name}"? This also removes its sessions and device mappings.`)) {
      deleteKiosk.mutate(kiosk);
    }
  }

  function confirmDeleteProfile(profile: KioskProfile) {
    if (window.confirm(`Delete kiosk profile "${profile.name}"? Kiosks using this profile may block deletion.`)) {
      deleteProfile.mutate(profile);
    }
  }

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-[20px] font-semibold tracking-tight">Workflow Kiosks</h2>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage workflow-driven kiosks and reusable kiosk profiles.</p>
          </div>
          <Button type="button" onClick={() => tab === 'kiosks' ? setIsKioskFormOpen((current) => !current) : setIsProfileFormOpen((current) => !current)}>
            <Plus className="size-4" />
            Add {tab === 'kiosks' ? 'kiosk' : 'profile'}
          </Button>
        </div>
      </div>

      <div className="p-4 sm:p-6">
        <Tabs value={tab} onValueChange={(value) => setTab(value === 'profiles' ? 'profiles' : 'kiosks')}>
          <TabsList aria-label="Workflow kiosk sections">
            <TabsTrigger value="kiosks">Kiosks</TabsTrigger>
            <TabsTrigger value="profiles">Kiosk Profiles</TabsTrigger>
          </TabsList>

          <TabsContent value="kiosks">
            <KiosksTab
              kiosks={kiosks}
              profiles={profiles}
              loading={kiosksQuery.isLoading}
              error={kiosksQuery.isError}
              isFormOpen={isKioskFormOpen}
              form={kioskForm}
              setForm={setKioskForm}
              onSubmit={submitKiosk}
              onDelete={confirmDeleteKiosk}
              busy={createKiosk.isPending || deleteKiosk.isPending}
            />
          </TabsContent>

          <TabsContent value="profiles">
            <ProfilesTab
              profiles={profiles}
              loading={profilesQuery.isLoading}
              error={profilesQuery.isError}
              isFormOpen={isProfileFormOpen}
              form={profileForm}
              setForm={setProfileForm}
              onSubmit={submitProfile}
              onDelete={confirmDeleteProfile}
              busy={createProfile.isPending || deleteProfile.isPending}
            />
          </TabsContent>
        </Tabs>
      </div>
    </section>
  );
}

function KiosksTab({ kiosks, profiles, loading, error, isFormOpen, form, setForm, onSubmit, onDelete, busy }: { readonly kiosks: Kiosk[]; readonly profiles: KioskProfile[]; readonly loading: boolean; readonly error: boolean; readonly isFormOpen: boolean; readonly form: KioskForm; readonly setForm: (form: KioskForm) => void; readonly onSubmit: (event: FormEvent<HTMLFormElement>) => void; readonly onDelete: (kiosk: Kiosk) => void; readonly busy: boolean }) {
  return (
    <div className="grid gap-4">
      {isFormOpen ? (
        <form className="grid gap-4 rounded-structural border border-border p-4" onSubmit={onSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Lobby kiosk" required />
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={form.profileId || profiles[0]?.id || ''} onChange={(event) => setForm({ ...form, profileId: event.target.value })} required>
              {profiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name}</option>)}
            </select>
          </div>
          <div className="flex justify-end">
            <Button type="submit" disabled={busy || profiles.length === 0}>Create kiosk</Button>
          </div>
        </form>
      ) : null}

      <TableShell empty={kiosks.length === 0 && !loading && !error} emptyTitle="No kiosks" emptyDescription="Create a kiosk before assigning workflow and device mappings.">
        <table className="w-full min-w-[60rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Profile</th>
              <th className="px-4 py-3 font-semibold">Mode</th>
              <th className="px-4 py-3 font-semibold">Session</th>
              <th className="px-4 py-3 font-semibold">Workflow</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {loading ? <LoadingRow colSpan={6} label="Loading kiosks..." /> : null}
            {error ? <ErrorRow colSpan={6} label="Could not load kiosks." /> : null}
            {kiosks.map((kiosk) => (
              <tr key={kiosk.id}>
                <td className="px-4 py-4 font-medium text-foreground">{kiosk.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{profiles.find((profile) => profile.id === kiosk.profileId)?.name ?? kiosk.profileId}</td>
                <td className="px-4 py-4"><ModeBadge mode={kiosk.mode} /></td>
                <td className="px-4 py-4 text-muted-foreground">{kiosk.activeSessionId ? kiosk.activeSessionStatus ?? 'Active' : 'Idle'}</td>
                <td className="px-4 py-4 text-muted-foreground">{kiosk.workflowDefinitionId ?? '-'}</td>
                <td className="px-4 py-4">
                  <RowActions editTo="/administration/automation/kiosk/$kioskId/edit" params={{ kioskId: kiosk.id }} deleteLabel={`Delete ${kiosk.name}`} onDelete={() => onDelete(kiosk)} busy={busy} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </TableShell>
    </div>
  );
}

function ProfilesTab({ profiles, loading, error, isFormOpen, form, setForm, onSubmit, onDelete, busy }: { readonly profiles: KioskProfile[]; readonly loading: boolean; readonly error: boolean; readonly isFormOpen: boolean; readonly form: ProfileForm; readonly setForm: (form: ProfileForm) => void; readonly onSubmit: (event: FormEvent<HTMLFormElement>) => void; readonly onDelete: (profile: KioskProfile) => void; readonly busy: boolean }) {
  return (
    <div className="grid gap-4">
      {isFormOpen ? (
        <form className="grid gap-4 rounded-structural border border-border p-4" onSubmit={onSubmit}>
          <div className="grid gap-4 md:grid-cols-[1fr_12rem]">
            <Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Visitor kiosk profile" required />
            <Input value={form.defaultLanguageCode} onChange={(event) => setForm({ ...form, defaultLanguageCode: event.target.value })} placeholder="en" required />
          </div>
          <div className="flex justify-end">
            <Button type="submit" disabled={busy}>Create profile</Button>
          </div>
        </form>
      ) : null}

      <TableShell empty={profiles.length === 0 && !loading && !error} emptyTitle="No kiosk profiles" emptyDescription="Create a profile before adding content, theme, assets, or kiosks.">
        <table className="w-full min-w-[42rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Default language</th>
              <th className="px-4 py-3 font-semibold">Updated</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {loading ? <LoadingRow colSpan={4} label="Loading profiles..." /> : null}
            {error ? <ErrorRow colSpan={4} label="Could not load profiles." /> : null}
            {profiles.map((profile) => (
              <tr key={profile.id}>
                <td className="px-4 py-4 font-medium text-foreground">{profile.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{profile.defaultLanguageCode}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDate(profile.updatedAt)}</td>
                <td className="px-4 py-4">
                  <RowActions editTo="/administration/automation/kiosk/profiles/$profileId/edit" params={{ profileId: profile.id }} deleteLabel={`Delete ${profile.name}`} onDelete={() => onDelete(profile)} busy={busy} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </TableShell>
    </div>
  );
}

function RowActions({ editTo, params, deleteLabel, onDelete, busy }: { readonly editTo: string; readonly params: Record<string, string>; readonly deleteLabel: string; readonly onDelete: () => void; readonly busy: boolean }) {
  return (
    <div className="flex justify-end gap-2">
      <Link to={editTo} params={params} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label="Edit">
        <Pencil className="size-4" />
      </Link>
      <Button type="button" variant="ghost" size="sm" aria-label={deleteLabel} disabled={busy} onClick={onDelete}>
        <Trash2 className="size-4" />
      </Button>
    </div>
  );
}

function TableShell({ empty, emptyTitle, emptyDescription, children }: { readonly empty: boolean; readonly emptyTitle: string; readonly emptyDescription: string; readonly children: ReactNode }) {
  if (empty) {
    return (
      <Empty>
        <EmptyHeader>
          <EmptyTitle>{emptyTitle}</EmptyTitle>
          <EmptyDescription>{emptyDescription}</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return <div className="overflow-x-auto rounded-structural border border-border">{children}</div>;
}

function LoadingRow({ colSpan, label }: { readonly colSpan: number; readonly label: string }) {
  return <tr><td className="px-4 py-5 text-muted-foreground" colSpan={colSpan}>{label}</td></tr>;
}

function ErrorRow({ colSpan, label }: { readonly colSpan: number; readonly label: string }) {
  return <tr><td className="px-4 py-5 text-error" colSpan={colSpan}>{label}</td></tr>;
}

function ModeBadge({ mode }: { readonly mode: components['schemas']['KioskMode'] }) {
  const variant: BadgeVariant = mode === 'Active' ? 'success' : mode === 'Maintenance' ? 'warning' : 'secondary';
  return <Badge variant={variant}>{mode}</Badge>;
}

function formatDate(value: string | null) {
  return value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : 'never';
}
