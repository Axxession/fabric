import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, RefreshCcw, Save } from 'lucide-react';
import { useEffect, useMemo, useRef, useState, type MutableRefObject } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api, apiBaseUrl, getAccessToken } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

import { CardEditor, type CardEditorHandle } from './card-editor/card-editor';
import type { CardSize } from './card-editor/card-editor.types';
import { detectTemplateFields } from './card-editor/card-editor.utils';
import { printDesignsQueryKey, standardPrintMediaQueryKey, type CreatePrintDesignRequest, type PreviewPrintTemplateRequest, type PrintDesign, type RenderMedia, type RenderProfile, type RenderProfileRequest, type RenderTarget, type UpdatePrintDesignRequest } from './card-management-types';

type Mode = 'create' | 'edit';
type RenderProfileFormState = {
  target: RenderTarget;
  dpi: string;
  background: string;
  quality: string;
};

const DEFAULT_RENDER_PROFILE: RenderProfileFormState = {
  target: 'BmpImage',
  dpi: '300',
  background: '#FFFFFF',
  quality: '',
};

export function PrintDesignCreatePage() {
  return <PrintDesignFormPage mode="create" />;
}

export default function PrintDesignEditPage() {
  const { printDesignId } = useParams({ from: '/desfire-studio/card-editor/$printDesignId/edit' });
  return <PrintDesignFormPage mode="edit" printDesignId={printDesignId} />;
}

function PrintDesignFormPage({ mode, printDesignId }: { readonly mode: Mode; readonly printDesignId?: string }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const editorRef = useRef<CardEditorHandle | null>(null);
  const [name, setName] = useState('');
  const [version, setVersion] = useState('');
  const [description, setDescription] = useState('');
  const [renderProfile, setRenderProfile] = useState<RenderProfileFormState>(DEFAULT_RENDER_PROFILE);
  const [previewFields, setPreviewFields] = useState<string[]>([]);
  const [previewData, setPreviewData] = useState<Record<string, string>>({});
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewContentType, setPreviewContentType] = useState<string | null>(null);
  const [previewFileName, setPreviewFileName] = useState<string | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const previewObjectUrlRef = useRef<string | null>(null);

  const standardMediaQuery = useQuery({
    queryKey: standardPrintMediaQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/printing/media/standard');
      if (error || !data) {
        throw new Error('Could not load standard media.');
      }
      return data;
    },
  });

  const printDesignQuery = useQuery({
    queryKey: [...printDesignsQueryKey, printDesignId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/printing/designs/{id}', { params: { path: { id: printDesignId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load print design.');
      }
      return data;
    },
    enabled: mode === 'edit' && !!printDesignId,
  });

  const printDesign = printDesignQuery.data;
  const cardMedia = useMemo(() => mergeCurrentMedia(filterCardMedia(standardMediaQuery.data ?? []), printDesign), [standardMediaQuery.data, printDesign]);
  const cardSizes = useMemo<CardSize[]>(() => cardMedia.map(toCardSize), [cardMedia]);

  useEffect(() => {
    if (!printDesign) {
      return;
    }

    setName(printDesign.name);
    setVersion(String(printDesign.version));
    setDescription(printDesign.description ?? '');
    setRenderProfile(toRenderProfileFormState(printDesign.defaultRenderProfile));
    syncPreviewFields(printDesign.designJson);
  }, [printDesign]);

  useEffect(() => {
    return () => {
      revokePreviewUrl(previewObjectUrlRef);
    };
  }, []);

  const savePrintDesign = useMutation({
    mutationFn: async (request: CreatePrintDesignRequest | UpdatePrintDesignRequest) => {
      if (mode === 'create') {
        const { data, error } = await api.POST('/api/printing/designs', { body: request as CreatePrintDesignRequest });
        if (error || !data) {
          throw new Error('Could not create print design.');
        }
        return data;
      }

      const { data, error } = await api.PUT('/api/printing/designs/{id}', {
        params: { path: { id: printDesignId ?? '' } },
        body: request as UpdatePrintDesignRequest,
      });
      if (error || !data) {
        throw new Error('Could not update print design.');
      }
      return data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: printDesignsQueryKey });
      if (printDesignId) {
        await queryClient.invalidateQueries({ queryKey: [...printDesignsQueryKey, printDesignId] });
      }
      toast.success(mode === 'create' ? t('cardManagement.printDesignForm.created') : t('cardManagement.printDesignForm.updated'));
      await navigate({ to: '/desfire-studio/card-editor' });
    },
    onError: () => toast.error(mode === 'create' ? t('cardManagement.printDesignForm.createFailed') : t('cardManagement.printDesignForm.updateFailed')),
  });

  const previewMutation = useMutation({
    mutationFn: async (request: PreviewPrintTemplateRequest) => {
      const accessToken = getAccessToken();
      const response = await fetch(`${apiBaseUrl || ''}/api/printing/preview`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        throw new Error('Could not render print preview.');
      }

      const blob = await response.blob();
      return {
        blob,
        contentType: response.headers.get('content-type') ?? blob.type,
        fileName: parseFileName(response.headers.get('content-disposition')),
      };
    },
    onSuccess: ({ blob, contentType, fileName }) => {
      revokePreviewUrl(previewObjectUrlRef);
      const nextPreviewUrl = URL.createObjectURL(blob);
      previewObjectUrlRef.current = nextPreviewUrl;
      setPreviewUrl(nextPreviewUrl);
      setPreviewContentType(contentType || null);
      setPreviewFileName(fileName);
      setPreviewError(null);
    },
    onError: () => {
      setPreviewError(t('cardManagement.printDesignForm.previewFailed'));
      revokePreviewUrl(previewObjectUrlRef);
      setPreviewUrl(null);
      setPreviewContentType(null);
      setPreviewFileName(null);
    },
  });

  function submit() {
    const designJson = editorRef.current?.serialize() ?? '';
    if (!designJson) {
      toast.error(t('cardManagement.printDesignForm.emptyDesign'));
      return;
    }

    if (!name.trim()) {
      toast.error(t('cardManagement.printDesignForm.nameRequired'));
      return;
    }

    const nextRenderProfile = toRenderProfileRequest(renderProfile);
    if (!nextRenderProfile) {
      toast.error(t('cardManagement.printDesignForm.renderProfileInvalid'));
      return;
    }

    if (mode === 'create') {
      savePrintDesign.mutate({
        name: name.trim(),
        version: version.trim() ? Number(version) : null,
        description: description.trim() || null,
        surfaceKind: 'Card',
        designJson,
        defaultRenderProfile: nextRenderProfile,
      });
      return;
    }

    savePrintDesign.mutate({
      name: name.trim(),
      version: Number(version || 1),
      description: description.trim() || null,
      surfaceKind: 'Card',
      designJson,
      defaultRenderProfile: nextRenderProfile,
    });
  }

  function refreshPreviewFields() {
    const designJson = editorRef.current?.serialize() ?? printDesign?.designJson ?? '';
    syncPreviewFields(designJson);
  }

  function preview() {
    const designJson = editorRef.current?.serialize() ?? '';
    if (!designJson) {
      toast.error(t('cardManagement.printDesignForm.emptyDesign'));
      return;
    }

    syncPreviewFields(designJson);

    const nextRenderProfile = toRenderProfileRequest(renderProfile);
    if (!nextRenderProfile) {
      toast.error(t('cardManagement.printDesignForm.renderProfileInvalid'));
      return;
    }

    setPreviewError(null);
    previewMutation.mutate({
      designJson,
      data: previewData,
      renderProfile: nextRenderProfile,
    });
  }

  function syncPreviewFields(designJson: string) {
    const detectedFields = detectTemplateFields(designJson);
    setPreviewFields(detectedFields);
    setPreviewData((current) => {
      const next: Record<string, string> = {};
      detectedFields.forEach((field) => {
        next[field] = current[field] ?? '';
      });
      return next;
    });
    return detectedFields;
  }

  return (
    <section className="grid gap-6">
      <Link to="/desfire-studio/card-editor" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />{t('cardManagement.printDesignForm.back')}</Link>

      {printDesignQuery.isError ? <PanelError>{t('cardManagement.printDesignForm.couldNotLoad')}</PanelError> : null}
      {standardMediaQuery.isError ? <PanelError>{t('cardManagement.printDesignForm.couldNotLoadMedia')}</PanelError> : null}

      <Card>
        <CardHeader>
          <CardTitle>{mode === 'create' ? t('cardManagement.printDesignForm.createTitle') : t('cardManagement.printDesignForm.editTitle')}</CardTitle>
          <CardDescription>{t('cardManagement.printDesignForm.description')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('cardManagement.printDesignForm.name')}</span>
              <Input value={name} onChange={(event) => setName(event.target.value)} required />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('cardManagement.printDesignForm.version')}</span>
              <Input value={version} type="number" min={1} onChange={(event) => setVersion(event.target.value)} placeholder={mode === 'create' ? t('cardManagement.printDesignForm.versionPlaceholder') : undefined} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
              <span>{t('cardManagement.printDesignForm.descriptionLabel')}</span>
              <Input value={description} onChange={(event) => setDescription(event.target.value)} />
            </label>
          </div>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>{t('cardManagement.printDesignForm.renderProfileTitle')}</CardTitle>
              <CardDescription>{t('cardManagement.printDesignForm.renderProfileDescription')}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('cardManagement.printDesignForm.renderTarget')}</span>
                <select
                  className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary"
                  value={renderProfile.target}
                  onChange={(event) => setRenderProfile((current) => ({ ...current, target: event.target.value as RenderTarget }))}
                >
                  <option value="BmpImage">BMP</option>
                  <option value="PngImage">PNG</option>
                  <option value="JpegImage">JPEG</option>
                </select>
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('cardManagement.printDesignForm.renderDpi')}</span>
                <Input type="number" min={1} value={renderProfile.dpi} onChange={(event) => setRenderProfile((current) => ({ ...current, dpi: event.target.value }))} />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('cardManagement.printDesignForm.renderBackground')}</span>
                <input type="color" value={renderProfile.background} onChange={(event) => setRenderProfile((current) => ({ ...current, background: event.target.value }))} className="h-10 w-full rounded-interactive border border-border bg-content p-1" />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('cardManagement.printDesignForm.renderQuality')}</span>
                <Input type="number" min={0} max={100} value={renderProfile.quality} onChange={(event) => setRenderProfile((current) => ({ ...current, quality: event.target.value }))} placeholder="90" />
              </label>
            </CardContent>
          </Card>

          <Tabs defaultValue="edit">
            <TabsList className="w-fit">
              <TabsTrigger value="edit">{t('cardManagement.printDesignForm.editTab')}</TabsTrigger>
              <TabsTrigger value="preview">{t('cardManagement.printDesignForm.previewTab')}</TabsTrigger>
            </TabsList>

            <TabsContent value="edit" forceMount className="grid gap-6 data-[state=inactive]:hidden">
              {standardMediaQuery.isLoading || (mode === 'edit' && printDesignQuery.isLoading) ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.printDesignForm.loading')}</p> : null}
              {!standardMediaQuery.isLoading && (mode === 'create' || printDesign) ? (
                <CardEditor
                  ref={editorRef}
                  cardSizes={cardSizes}
                  profile="id-card"
                  initialTemplate={printDesign?.designJson ?? null}
                  onSave={(designJson) => {
                    syncPreviewFields(designJson);
                    toast.success(t('cardManagement.printDesignForm.editorSaved'));
                  }}
                />
              ) : null}
            </TabsContent>

            <TabsContent value="preview" forceMount className="grid gap-6 data-[state=inactive]:hidden xl:grid-cols-[22rem_minmax(0,1fr)]">
              <Card className="border-border/70">
                <CardHeader>
                  <CardTitle>{t('cardManagement.printDesignForm.previewFieldsTitle')}</CardTitle>
                  <CardDescription>{t('cardManagement.printDesignForm.previewFieldsDescription')}</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4">
                  <div className="flex items-center justify-between gap-2">
                    <p className="text-[13px] text-muted-foreground">{t('cardManagement.printDesignForm.previewDescription')}</p>
                    <Button type="button" variant="outline" size="sm" onClick={refreshPreviewFields}>
                      <RefreshCcw className="size-4" aria-hidden="true" />
                      {t('cardManagement.printDesignForm.refreshFields')}
                    </Button>
                  </div>

                  {previewFields.length === 0 ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.printDesignForm.noPreviewFields')}</p> : null}

                  {previewFields.map((field) => (
                    <label key={field} className="grid gap-2 text-[14px] font-medium">
                      <span>{field}</span>
                      <Input value={previewData[field] ?? ''} onChange={(event) => setPreviewData((current) => ({ ...current, [field]: event.target.value }))} />
                    </label>
                  ))}

                  <Button type="button" variant="outline" onClick={preview} disabled={previewMutation.isPending || standardMediaQuery.isLoading || (mode === 'edit' && printDesignQuery.isLoading)}>
                    {previewMutation.isPending ? t('cardManagement.printDesignForm.previewLoading') : t('cardManagement.printDesignForm.previewAction')}
                  </Button>
                </CardContent>
              </Card>

              <Card className="border-border/70">
                <CardHeader>
                  <CardTitle>{t('cardManagement.printDesignForm.previewTitle')}</CardTitle>
                  <CardDescription>{t('cardManagement.printDesignForm.previewDescription')}</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4">
                  {previewError ? <PanelError>{previewError}</PanelError> : null}

                  <div className="flex min-h-[20rem] items-center justify-center rounded-structural border border-border bg-background p-4">
                    {previewUrl ? <img src={previewUrl} alt={t('cardManagement.printDesignForm.previewImageAlt')} className="max-h-[28rem] max-w-full rounded border border-border bg-white shadow-sm" /> : <p className="text-center text-[14px] text-muted-foreground">{previewMutation.isPending ? t('cardManagement.printDesignForm.previewLoading') : t('cardManagement.printDesignForm.previewEmpty')}</p>}
                  </div>

                  {previewUrl ? (
                    <div className="grid gap-1 text-[13px] text-muted-foreground">
                      <p>{t('cardManagement.printDesignForm.previewTargetValue', { target: renderProfile.target })}</p>
                      <p>{t('cardManagement.printDesignForm.previewDpiValue', { dpi: renderProfile.dpi || '300' })}</p>
                      {previewContentType ? <p>{t('cardManagement.printDesignForm.previewContentTypeValue', { contentType: previewContentType })}</p> : null}
                      {previewFileName ? <p>{t('cardManagement.printDesignForm.previewFileNameValue', { fileName: previewFileName })}</p> : null}
                    </div>
                  ) : null}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>

          <div className="flex justify-end gap-2">
            <Button type="button" onClick={submit} disabled={savePrintDesign.isPending || standardMediaQuery.isLoading || (mode === 'edit' && printDesignQuery.isLoading)}>
              <Save className="size-4" aria-hidden="true" />
              {savePrintDesign.isPending ? t('cardManagement.printDesignForm.saving') : t('cardManagement.printDesignForm.save')}
            </Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function filterCardMedia(media: RenderMedia[]) {
  return media.filter((item) => item.label.startsWith('CR79') || item.label.startsWith('CR80'));
}

function mergeCurrentMedia(media: RenderMedia[], printDesign: PrintDesign | undefined) {
  if (!printDesign) {
    return media;
  }

  const exists = media.some((item) => item.label === printDesign.media.label && Number(item.width) === Number(printDesign.media.width) && Number(item.height) === Number(printDesign.media.height) && item.orientation === printDesign.media.orientation);
  return exists ? media : [printDesign.media, ...media];
}

function toCardSize(media: RenderMedia): CardSize {
  return {
    label: media.label,
    width: Number(media.width),
    height: Number(media.height),
    orientation: media.orientation,
  };
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}

function toRenderProfileFormState(renderProfile: RenderProfile | null | undefined): RenderProfileFormState {
  if (!renderProfile) {
    return DEFAULT_RENDER_PROFILE;
  }

  return {
    target: renderProfile.target,
    dpi: String(renderProfile.dpi),
    background: renderProfile.background ?? '#FFFFFF',
    quality: renderProfile.quality == null ? '' : String(renderProfile.quality),
  };
}

function toRenderProfileRequest(renderProfile: RenderProfileFormState): RenderProfileRequest | null {
  const dpi = Number(renderProfile.dpi);
  if (!Number.isFinite(dpi) || dpi <= 0) {
    return null;
  }

  const qualityValue = renderProfile.quality.trim();
  const parsedQuality = qualityValue ? Number(qualityValue) : null;
  if (qualityValue && (parsedQuality === null || !Number.isFinite(parsedQuality) || parsedQuality < 0 || parsedQuality > 100)) {
    return null;
  }

  return {
    target: renderProfile.target,
    dpi,
    background: renderProfile.background.trim() || null,
    quality: parsedQuality,
  };
}

function revokePreviewUrl(previewObjectUrlRef: MutableRefObject<string | null>) {
  if (previewObjectUrlRef.current) {
    URL.revokeObjectURL(previewObjectUrlRef.current);
    previewObjectUrlRef.current = null;
  }
}

function parseFileName(contentDisposition: string | null) {
  if (!contentDisposition) {
    return null;
  }

  const match = /filename="?([^\"]+)"?/i.exec(contentDisposition);
  return match?.[1] ?? null;
}
