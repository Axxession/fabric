import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type CourseLanguageResponse = components['schemas']['CourseLanguageResponse'];
type CourseVersionResponse = components['schemas']['CourseVersionResponse'];

export default function LmsCourseLanguageEditPage() {
  const { courseId, languageId } = useParams({ from: '/main/administration/lms/courses/$courseId/languages/$languageId' });
  const queryClient = useQueryClient();
  const [languageCode, setLanguageCode] = useState('');
  const [displayLabel, setDisplayLabel] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [versionTitle, setVersionTitle] = useState('');
  const [versionFile, setVersionFile] = useState<File | null>(null);

  const languageQuery = useQuery({
    queryKey: ['administration', 'lms', 'course', courseId, 'language', languageId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses/{id}/languages/{languageId}', { params: { path: { id: courseId, languageId } } } as never);
      const language = data as CourseLanguageResponse | undefined;
      if (error || !language) throw new Error('Could not load language.');
      setLanguageCode(language.languageCode);
      setDisplayLabel(language.displayLabel);
      setIsActive(language.isActive);
      return language;
    },
  });

  const versionsQuery = useQuery({
    queryKey: ['administration', 'lms', 'course', courseId, 'language', languageId, 'versions'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses/{id}/languages/{languageId}/versions', { params: { path: { id: courseId, languageId } } } as never);
      if (error || !data) throw new Error('Could not load versions.');
      return data as CourseVersionResponse[];
    },
  });

  const updateLanguage = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.PUT('/api/learning/courses/{id}/languages/{languageId}', { params: { path: { id: courseId, languageId } }, body: { languageCode, displayLabel, isActive } } as never);
      if (error || !data) throw new Error('Could not save language.');
      return data as CourseLanguageResponse;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'language', languageId] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'languages'] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'courses'] });
      toast.success('Language saved.');
    },
    onError: () => toast.error('Could not save language.'),
  });

  const uploadVersion = useMutation({
    mutationFn: async () => {
      const formData = new FormData();
      if (versionTitle.trim()) formData.set('title', versionTitle.trim());
      if (versionFile) formData.set('file', versionFile);
      const { data, error } = await api.POST('/api/learning/courses/{id}/languages/{languageId}/versions', { params: { path: { id: courseId, languageId } }, body: formData as never } as never);
      if (error || !data) throw new Error('Could not upload version.');
      return data as CourseVersionResponse;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'language', languageId, 'versions'] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'language', languageId] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'languages'] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'courses'] });
      toast.success('Version uploaded.');
    },
    onError: () => toast.error('Could not upload version.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}><ArrowLeft className="size-4" aria-hidden="true" /></Button>
        <div><h2 className="text-[20px] font-semibold tracking-tight">Language detail</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Edit language metadata and upload SCORM package versions for this language.</p></div>
      </header>

      <Card className="p-6">
        {languageQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading language...</p> : null}
        {languageQuery.data ? <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); updateLanguage.mutate(); }}>
          <label className="grid gap-2 text-[14px] font-medium"><span>Language code</span><Input value={languageCode} onChange={(event) => setLanguageCode(event.target.value)} required /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Display label</span><Input value={displayLabel} onChange={(event) => setDisplayLabel(event.target.value)} required /></label>
          <label className="flex items-center gap-3 rounded-structural border border-border p-4 text-[14px] font-medium"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Active language</label>
          <div className="flex justify-end"><Button type="submit" disabled={updateLanguage.isPending}>Save language</Button></div>
        </form> : null}
      </Card>

      <Card className="p-6">
        <div><h3 className="text-[18px] font-semibold tracking-tight">Versions</h3><p className="mt-2 text-[14px] text-muted-foreground">Upload and review versions for this language.</p></div>
        <form className="mt-4 grid gap-4 md:grid-cols-[1fr_1fr_auto]" onSubmit={(event) => { event.preventDefault(); if (!versionFile) { toast.error('Select a SCORM package.'); return; } uploadVersion.mutate(); }}>
          <Input value={versionTitle} onChange={(event) => setVersionTitle(event.target.value)} placeholder="Optional version title" />
          <Input type="file" accept=".zip" onChange={(event) => setVersionFile(event.target.files?.[0] ?? null)} />
          <Button type="submit" disabled={uploadVersion.isPending}>Upload version</Button>
        </form>
        <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[52rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Version</th><th className="px-4 py-3 font-semibold">SCORM</th><th className="px-4 py-3 font-semibold">Scored</th><th className="px-4 py-3 font-semibold">SCOs</th><th className="px-4 py-3 font-semibold">Published</th></tr></thead><tbody className="divide-y divide-border">{(versionsQuery.data ?? []).map((item) => <tr key={item.id}><td className="px-4 py-4"><div><p className="font-medium text-foreground">Version {item.versionNumber}</p><p className="mt-1 text-[13px] text-muted-foreground">{item.title}</p></div></td><td className="px-4 py-4 text-muted-foreground">{item.scormVersion}</td><td className="px-4 py-4 text-muted-foreground">{item.emitsScore ? 'Yes' : 'No'}</td><td className="px-4 py-4 text-muted-foreground">{item.scos.length}</td><td className="px-4 py-4 text-muted-foreground">{new Date(item.publishedAt).toLocaleString()}</td></tr>)}</tbody></table></div>
      </Card>
    </div>
  );
}
