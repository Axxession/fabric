import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Save, Trash2 } from 'lucide-react';
import { useEffect, useState, type FormEvent } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';

type KioskProfile = components['schemas']['KioskProfileResponse'];
type Translation = components['schemas']['KioskTranslationResponse'];
type ThemeToken = components['schemas']['KioskThemeTokenResponse'];
type WelcomeSettings = components['schemas']['KioskWelcomeSettingsResponse'];
type KioskAsset = components['schemas']['KioskAssetResponse'];
type KioskAssetKind = components['schemas']['KioskAssetKind'];

type Tab = 'translations' | 'theme' | 'welcome' | 'assets';
type ProfileForm = { readonly name: string; readonly defaultLanguageCode: string };
type TranslationRow = { readonly languageCode: string; readonly key: string; readonly value: string };
type ThemeRow = { readonly key: string; readonly value: string };
type WelcomeForm = { readonly titleKey: string; readonly subtitleKey: string; readonly startButtonKey: string; readonly backgroundAssetName: string; readonly logoAssetName: string };
type AssetForm = { readonly name: string; readonly languageCode: string; readonly kind: KioskAssetKind; readonly altTextKey: string; readonly file: File | null };

const pageSize = 100;
const profilesQueryKey = ['automation', 'kiosk', 'profiles'] as const;
const assetKinds: readonly KioskAssetKind[] = ['Image', 'Background', 'Logo', 'Video'];

export default function KioskProfileEditPage() {
  const { profileId } = useParams({ from: '/main/administration/automation/kiosk/profiles/$profileId/edit' });
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('translations');
  const [profileForm, setProfileForm] = useState<ProfileForm>({ name: '', defaultLanguageCode: 'en' });
  const [translations, setTranslations] = useState<TranslationRow[]>([]);
  const [theme, setTheme] = useState<ThemeRow[]>([]);
  const [welcome, setWelcome] = useState<WelcomeForm>({ titleKey: '', subtitleKey: '', startButtonKey: 'kiosk.start', backgroundAssetName: '', logoAssetName: '' });
  const [asset, setAsset] = useState<AssetForm>({ name: '', languageCode: '', kind: 'Image', altTextKey: '', file: null });

  const profilesQuery = useQuery({
    queryKey: profilesQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/kiosk-profiles', { params: { query: { Page: 0, PageSize: pageSize } } });
      if (error || !data) throw new Error('Could not load profiles.');
      return data.items ?? [];
    },
  });
  const profile = profilesQuery.data?.find((item) => item.id === profileId) ?? null;
  const translationsQuery = useQuery({ queryKey: profileConfigKey(profileId, 'translations'), queryFn: async () => { const { data, error } = await api.GET('/api/kiosk-profiles/{id}/translations', { params: { path: { id: profileId } } }); if (error || !data) throw new Error('Could not load translations.'); return data; } });
  const themeQuery = useQuery({ queryKey: profileConfigKey(profileId, 'theme'), queryFn: async () => { const { data, error } = await api.GET('/api/kiosk-profiles/{id}/theme', { params: { path: { id: profileId } } }); if (error || !data) throw new Error('Could not load theme.'); return data; } });
  const welcomeQuery = useQuery({ queryKey: profileConfigKey(profileId, 'welcome'), queryFn: async () => { const { data } = await api.GET('/api/kiosk-profiles/{id}/welcome', { params: { path: { id: profileId } } }); return data ?? null; } });
  const assetsQuery = useQuery({ queryKey: profileConfigKey(profileId, 'assets'), queryFn: async () => { const { data, error } = await api.GET('/api/kiosk-profiles/{id}/assets', { params: { path: { id: profileId } } }); if (error || !data) throw new Error('Could not load assets.'); return data; } });

  useEffect(() => { if (profile) setProfileForm({ name: profile.name, defaultLanguageCode: profile.defaultLanguageCode }); }, [profile]);
  useEffect(() => setTranslations((translationsQuery.data as Translation[] | undefined)?.map((item) => ({ languageCode: item.languageCode, key: item.key, value: item.value })) ?? []), [translationsQuery.data]);
  useEffect(() => setTheme((themeQuery.data as ThemeToken[] | undefined)?.map((item) => ({ key: item.key, value: item.value })) ?? []), [themeQuery.data]);
  useEffect(() => {
    const data = welcomeQuery.data as WelcomeSettings | null | undefined;
    if (data) {
      setWelcome({
        titleKey: data.titleKey,
        subtitleKey: data.subtitleKey ?? '',
        startButtonKey: data.startButtonKey,
        backgroundAssetName: data.backgroundAssetName ?? '',
        logoAssetName: data.logoAssetName ?? '',
      });
    }
  }, [welcomeQuery.data]);

  const updateProfile = useMutation({ mutationFn: async (request: ProfileForm) => { const { error } = await api.PUT('/api/kiosk-profiles/{id}', { params: { path: { id: profileId } }, body: request }); if (error) throw new Error('Could not save profile.'); }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: profilesQueryKey }); toast.success('Profile saved.'); }, onError: () => toast.error('Could not save profile.') });
  const saveTranslations = useSaveProfileConfig(profileId, 'translations', async () => api.PUT('/api/kiosk-profiles/{id}/translations', { params: { path: { id: profileId } }, body: { translations } }));
  const saveTheme = useSaveProfileConfig(profileId, 'theme', async () => api.PUT('/api/kiosk-profiles/{id}/theme', { params: { path: { id: profileId } }, body: { tokens: theme } }));
  const saveWelcome = useSaveProfileConfig(profileId, 'welcome', async () => api.PUT('/api/kiosk-profiles/{id}/welcome', { params: { path: { id: profileId } }, body: nullToUndefined(welcome) }));
  const uploadAsset = useMutation({ mutationFn: async () => { if (!asset.file) throw new Error('Select a file.'); const form = new FormData(); form.set('name', asset.name); form.set('languageCode', asset.languageCode); form.set('kind', asset.kind); form.set('altTextKey', asset.altTextKey); form.set('file', asset.file); const { error } = await api.POST('/api/kiosk-profiles/{id}/assets', { params: { path: { id: profileId } }, body: form as never }); if (error) throw new Error('Could not upload asset.'); }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: profileConfigKey(profileId, 'assets') }); setAsset({ name: '', languageCode: '', kind: 'Image', altTextKey: '', file: null }); toast.success('Asset uploaded.'); }, onError: () => toast.error('Could not upload asset.') });
  const deleteAsset = useMutation({ mutationFn: async (item: KioskAsset) => { const { error } = await api.DELETE('/api/kiosk-profiles/{id}/assets/{assetId}', { params: { path: { id: profileId, assetId: item.id } } }); if (error) throw new Error('Could not delete asset.'); }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: profileConfigKey(profileId, 'assets') }); toast.success('Asset deleted.'); }, onError: () => toast.error('Could not delete asset.') });

  function submitProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    updateProfile.mutate(profileForm);
  }

  return (
    <section className="grid gap-6">
      <Link to="/administration/automation/kiosk" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" />
        Back to kiosks
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{profile?.name ?? 'Kiosk profile'}</CardTitle>
          <CardDescription>Profile details and content settings.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 md:grid-cols-[1fr_12rem_auto]" onSubmit={submitProfile}>
            <Input value={profileForm.name} onChange={(event) => setProfileForm({ ...profileForm, name: event.target.value })} required />
            <Input value={profileForm.defaultLanguageCode} onChange={(event) => setProfileForm({ ...profileForm, defaultLanguageCode: event.target.value })} required />
            <Button type="submit"><Save className="size-4" />Save</Button>
          </form>
        </CardContent>
      </Card>

      <Tabs value={tab} onValueChange={(value) => setTab(toProfileTab(value))}>
        <TabsList aria-label="Kiosk profile sections" className="flex h-auto w-full flex-wrap justify-start sm:w-fit">
          <TabsTrigger value="translations">Translations</TabsTrigger>
          <TabsTrigger value="theme">Theme</TabsTrigger>
          <TabsTrigger value="welcome">Welcome screen</TabsTrigger>
          <TabsTrigger value="assets">Assets</TabsTrigger>
        </TabsList>

        <TabsContent value="translations">
          <EditableRows title="Translations" rows={translations} onRowsChange={setTranslations} onAdd={() => setTranslations([...translations, { languageCode: profile?.defaultLanguageCode ?? 'en', key: '', value: '' }])} onSave={() => saveTranslations.mutate()} fields={[["languageCode", "Code"], ["key", "Key"], ["value", "Value"]]} multiline="value" />
        </TabsContent>
        <TabsContent value="theme">
          <EditableRows title="Theme tokens" rows={theme} onRowsChange={setTheme} onAdd={() => setTheme([...theme, { key: '', value: '' }])} onSave={() => saveTheme.mutate()} fields={[["key", "Key"], ["value", "Value"]]} />
        </TabsContent>
        <TabsContent value="welcome">
          <WelcomeEditor welcome={welcome} setWelcome={setWelcome} onSave={() => saveWelcome.mutate()} />
        </TabsContent>
        <TabsContent value="assets">
          <AssetEditor asset={asset} setAsset={setAsset} assets={(assetsQuery.data as KioskAsset[] | undefined) ?? []} onUpload={() => uploadAsset.mutate()} onDelete={(item) => deleteAsset.mutate(item)} />
        </TabsContent>
      </Tabs>
    </section>
  );
}

function EditableRows<TRow extends Record<string, string>>({ title, rows, onRowsChange, onAdd, onSave, fields, multiline }: { readonly title: string; readonly rows: TRow[]; readonly onRowsChange: (rows: TRow[]) => void; readonly onAdd: () => void; readonly onSave: () => void; readonly fields: readonly (readonly [keyof TRow & string, string])[]; readonly multiline?: keyof TRow & string }) {
  return <Card><CardHeader><div className="flex items-center justify-between gap-3"><div><CardTitle>{title}</CardTitle><CardDescription>Each save replaces this section.</CardDescription></div><Button type="button" variant="outline" onClick={onAdd}><Plus className="size-4" />Add</Button></div></CardHeader><CardContent className="grid gap-3">{rows.map((row, index) => <div key={index} className="grid gap-3 rounded-interactive border border-border p-3"><div className="grid gap-3 md:grid-cols-3">{fields.map(([key, label]) => <label key={key} className="grid gap-1 text-[13px] font-medium">{label}{multiline === key ? <Textarea value={String(row[key] ?? '')} onChange={(event) => onRowsChange(replaceAt(rows, index, { ...row, [key]: event.target.value }))} /> : <Input value={String(row[key] ?? '')} onChange={(event) => onRowsChange(replaceAt(rows, index, { ...row, [key]: event.target.value }))} />}</label>)}</div><div className="flex justify-end"><Button type="button" variant="ghost" size="sm" onClick={() => onRowsChange(rows.filter((_, rowIndex) => rowIndex !== index))}><Trash2 className="size-4" />Remove</Button></div></div>)}<div className="flex justify-end"><Button type="button" onClick={onSave}><Save className="size-4" />Save {title.toLowerCase()}</Button></div></CardContent></Card>;
}

function WelcomeEditor({ welcome, setWelcome, onSave }: { readonly welcome: WelcomeForm; readonly setWelcome: (welcome: WelcomeForm) => void; readonly onSave: () => void }) {
  return <Card><CardHeader><CardTitle>Welcome screen</CardTitle><CardDescription>Configure welcome screen translation keys and optional assets.</CardDescription></CardHeader><CardContent className="grid gap-4"><div className="grid gap-3 md:grid-cols-2"><Input value={welcome.titleKey} onChange={(event) => setWelcome({ ...welcome, titleKey: event.target.value })} placeholder="Title key" /><Input value={welcome.subtitleKey} onChange={(event) => setWelcome({ ...welcome, subtitleKey: event.target.value })} placeholder="Subtitle key" /><Input value={welcome.startButtonKey} onChange={(event) => setWelcome({ ...welcome, startButtonKey: event.target.value })} placeholder="Start button key" /><Input value={welcome.backgroundAssetName} onChange={(event) => setWelcome({ ...welcome, backgroundAssetName: event.target.value })} placeholder="Background asset" /><Input value={welcome.logoAssetName} onChange={(event) => setWelcome({ ...welcome, logoAssetName: event.target.value })} placeholder="Logo asset" /></div><div className="flex justify-end"><Button type="button" onClick={onSave}><Save className="size-4" />Save welcome</Button></div></CardContent></Card>;
}

function AssetEditor({ asset, setAsset, assets, onUpload, onDelete }: { readonly asset: AssetForm; readonly setAsset: (asset: AssetForm) => void; readonly assets: KioskAsset[]; readonly onUpload: () => void; readonly onDelete: (asset: KioskAsset) => void }) {
  return <Card><CardHeader><CardTitle>Assets</CardTitle><CardDescription>Upload and remove profile assets.</CardDescription></CardHeader><CardContent className="grid gap-4"><div className="grid gap-3 md:grid-cols-2"><Input value={asset.name} onChange={(event) => setAsset({ ...asset, name: event.target.value })} placeholder="Asset name" /><Input value={asset.languageCode} onChange={(event) => setAsset({ ...asset, languageCode: event.target.value })} placeholder="Language optional" /><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]" value={asset.kind} onChange={(event) => setAsset({ ...asset, kind: event.target.value as KioskAssetKind })}>{assetKinds.map((kind) => <option key={kind} value={kind}>{kind}</option>)}</select><Input value={asset.altTextKey} onChange={(event) => setAsset({ ...asset, altTextKey: event.target.value })} placeholder="Alt text key" /><input type="file" className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]" onChange={(event) => setAsset({ ...asset, file: event.target.files?.[0] ?? null })} /></div><div className="flex justify-end"><Button type="button" onClick={onUpload}>Upload asset</Button></div><div className="overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[42rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Kind</th><th className="px-4 py-3 font-semibold">Language</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr></thead><tbody className="divide-y divide-border">{assets.map((item) => <tr key={item.id}><td className="px-4 py-4 font-medium">{item.name}</td><td className="px-4 py-4 text-muted-foreground">{item.kind}</td><td className="px-4 py-4 text-muted-foreground">{item.languageCode || 'all'}</td><td className="px-4 py-4 text-right"><Button type="button" variant="ghost" size="sm" onClick={() => onDelete(item)}><Trash2 className="size-4" />Delete</Button></td></tr>)}</tbody></table></div></CardContent></Card>;
}

function useSaveProfileConfig(profileId: string, section: string, mutationFn: () => Promise<{ error?: unknown }>) { const queryClient = useQueryClient(); return useMutation({ mutationFn: async () => { const result = await mutationFn(); if (result.error) throw new Error(`Could not save ${section}.`); }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: profileConfigKey(profileId, section) }); toast.success(`${section} saved.`); }, onError: () => toast.error(`Could not save ${section}.`) }); }
function profileConfigKey(profileId: string, section: string) { return ['automation', 'kiosk', 'profiles', profileId, section] as const; }
function replaceAt<T>(items: T[], index: number, item: T) { return items.map((current, currentIndex) => currentIndex === index ? item : current); }
function nullToUndefined(welcome: WelcomeForm) { return { titleKey: welcome.titleKey, subtitleKey: welcome.subtitleKey || null, startButtonKey: welcome.startButtonKey, backgroundAssetName: welcome.backgroundAssetName || null, logoAssetName: welcome.logoAssetName || null }; }
function toProfileTab(value: string): Tab { return value === 'theme' || value === 'welcome' || value === 'assets' ? value : 'translations'; }
