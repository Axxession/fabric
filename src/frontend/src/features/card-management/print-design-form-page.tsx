import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, Save } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { CardEditor, type CardEditorHandle } from './card-editor/card-editor';
import type { CardSize } from './card-editor/card-editor.types';
import { printDesignsQueryKey, standardPrintMediaQueryKey, type CreatePrintDesignRequest, type PrintDesign, type PrintSurfaceKind, type RenderMedia, type UpdatePrintDesignRequest } from './card-management-types';

type Mode = 'create' | 'edit';

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
  }, [printDesign]);

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

    if (mode === 'create') {
      savePrintDesign.mutate({
        name: name.trim(),
        version: version.trim() ? Number(version) : null,
        description: description.trim() || null,
        surfaceKind: 'Card',
        designJson,
      });
      return;
    }

    savePrintDesign.mutate({
      name: name.trim(),
      version: Number(version || 1),
      description: description.trim() || null,
      surfaceKind: 'Card',
      designJson,
    });
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

          {standardMediaQuery.isLoading || (mode === 'edit' && printDesignQuery.isLoading) ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.printDesignForm.loading')}</p> : null}
          {!standardMediaQuery.isLoading && (mode === 'create' || printDesign) ? (
            <CardEditor
              ref={editorRef}
              cardSizes={cardSizes}
              profile="id-card"
              initialTemplate={printDesign?.designJson ?? null}
              onSave={() => {
                toast.success(t('cardManagement.printDesignForm.editorSaved'));
              }}
            />
          ) : null}

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
