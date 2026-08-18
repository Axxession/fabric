import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { format, parseISO } from 'date-fns';
import { ArrowRight, CalendarIcon, CheckCircle2, ChevronLeft, ChevronRight, Plus, Trash2 } from 'lucide-react';
import { useEffect, useId, useState } from 'react';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { getGrantComplianceLabel, getGrantComplianceVariant } from '@/shared/access-grants/grant-status';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Calendar } from '@/shared/components/ui/calendar';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';
import { cn } from '@/shared/utils/cn';

type AccessDurationKind = components['schemas']['AccessDurationKind'];
type ApprovalRequirementsPreviewAccessItemResponse = components['schemas']['ApprovalRequirementsPreviewAccessItemResponse'];
type ApprovalRequirementPreviewResponse = components['schemas']['ApprovalRequirementPreviewResponse'];
type CatalogPackageResponse = components['schemas']['CatalogPackageResponse'];
type CatalogResponse = components['schemas']['CatalogResponse'];
type CreatePackageRequestRequest = components['schemas']['CreatePackageRequestRequest'];
type EmployeeWorkLocationResponse = components['schemas']['EmployeeWorkLocationResponse'];
type PackageRequestResponse = components['schemas']['PackageRequestResponse'];
type PackageResponse = components['schemas']['PackageResponse'];
type PackageRequestPreviewResponse = components['schemas']['PackageRequestPreviewResponse'];
type PreviewPackageRequestApprovalsRequest = components['schemas']['PreviewPackageRequestApprovalsRequest'];

type EmployeeRequestTab = 'new-request' | 'my-requests';
type MyRequestFilter = 'all' | 'in-progress' | 'approved' | 'partially-approved' | 'rejected' | 'expired';
type RequestStep = 0 | 1 | 2 | 3;
type RequestablePackage = PackageResponse & { catalogIds: string[] };

const requestSteps = [
  { title: 'Select Package', description: 'Choose package to request.' },
  { title: 'Time Period', description: 'Set duration, locations, and justification.' },
  { title: 'Approval Chain', description: 'Review required approvals.' },
  { title: 'Submit', description: 'Confirm and send request.' },
] as const;

const myRequestsQueryKey = ['employee', 'request-access', 'my-requests'] as const;

export default function EmployeeRequestAccessPage() {
  const actorQuery = useCurrentActor();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<EmployeeRequestTab>('new-request');
  const [myRequestFilter, setMyRequestFilter] = useState<MyRequestFilter>('all');
  const [step, setStep] = useState<RequestStep>(0);
  const [selectedPackageId, setSelectedPackageId] = useState('');
  const [durationKind, setDurationKind] = useState<AccessDurationKind>('Permanent');
  const [validFrom, setValidFrom] = useState(getDefaultDateTime());
  const [validUntil, setValidUntil] = useState('');
  const [businessJustification, setBusinessJustification] = useState('');
  const [selectedLocationIds, setSelectedLocationIds] = useState<string[]>([]);
  const [pickerLocationId, setPickerLocationId] = useState<string | null>(null);
  const [isAddLocationOpen, setIsAddLocationOpen] = useState(false);
  const [stepError, setStepError] = useState<string | null>(null);
  const [submittedRequest, setSubmittedRequest] = useState<PackageRequestResponse | null>(null);
  const [hasInitializedWorkLocations, setHasInitializedWorkLocations] = useState(false);

  const actor = actorQuery.data;
  const identityId = actor?.identityId ?? null;
  const employeeId = actor?.employeeId ?? null;

  const requestablePackagesQuery = useQuery({
    queryKey: ['employee', 'request-access', 'requestable-packages'],
    queryFn: async () => {
      const [{ data: packagesData, error: packagesError }, { data: catalogsData, error: catalogsError }] = await Promise.all([
        api.GET('/api/access-catalog/packages', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } }),
        api.GET('/api/access-catalog/catalogs', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } }),
      ]);

      if (packagesError || catalogsError) {
        throw new Error('Could not load requestable packages.');
      }

      const activeCatalogs = (catalogsData?.items ?? []).filter((catalog: CatalogResponse) => catalog.status === 'Active');
      const catalogPackagePages = await Promise.all(
        activeCatalogs.map(async (catalog) => {
          const { data, error } = await api.GET('/api/access-catalog/catalogs/{catalogId}/packages', {
            params: { path: { catalogId: catalog.id }, query: { Page: 0, PageSize: 200 } },
          });

          if (error) {
            throw new Error('Could not load requestable packages.');
          }

          return { catalogId: catalog.id, items: data?.items ?? [] };
        }),
      );

      const packageCatalogs = new Map<string, Set<string>>();

      catalogPackagePages.forEach((page) => {
        page.items.forEach((item: CatalogPackageResponse) => {
          if (!item.isRequestable) {
            return;
          }

          const current = packageCatalogs.get(item.packageId) ?? new Set<string>();
          current.add(page.catalogId);
          packageCatalogs.set(item.packageId, current);
        });
      });

      return (packagesData?.items ?? [])
        .filter((item: PackageResponse) => item.status === 'Active' && packageCatalogs.has(item.id))
        .map((item: PackageResponse) => ({ ...item, catalogIds: Array.from(packageCatalogs.get(item.id) ?? []) }))
        .sort((left, right) => left.name.localeCompare(right.name));
    },
  });

  const employeeWorkLocationsQuery = useQuery({
    queryKey: ['employee', 'request-access', 'work-locations', employeeId],
    enabled: Boolean(employeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees/{id}/work-locations', { params: { path: { id: employeeId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load employee work locations.');
      }
      return data;
    },
  });

  useEffect(() => {
    if (hasInitializedWorkLocations || !employeeWorkLocationsQuery.data) {
      return;
    }

    setSelectedLocationIds(employeeWorkLocationsQuery.data.map((item: EmployeeWorkLocationResponse) => item.locationId));
    setHasInitializedWorkLocations(true);
  }, [employeeWorkLocationsQuery.data, hasInitializedWorkLocations]);

  const selectedLocationDetailsQuery = useQuery({
    queryKey: ['employee', 'request-access', 'location-details', selectedLocationIds.join(',')],
    enabled: selectedLocationIds.length > 0,
    queryFn: async () => {
      const locations = await Promise.all(
        selectedLocationIds.map(async (locationId) => {
          const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: locationId } } });
          if (error || !data) {
            throw new Error('Could not load selected locations.');
          }
          return data;
        }),
      );

      return new Map(locations.map((location) => [location.id, location]));
    },
  });

  const approvalPreviewEnabled = activeTab === 'new-request' && step >= 2 && Boolean(identityId) && selectedPackageId !== '' && selectedLocationIds.length > 0;
  const approvalPreviewQuery = useQuery({
    queryKey: ['employee', 'request-access', 'approval-preview', selectedPackageId, identityId, selectedLocationIds.join(',')],
    enabled: approvalPreviewEnabled,
    queryFn: async () => {
      const request: PreviewPackageRequestApprovalsRequest = {
        packageId: selectedPackageId,
        beneficiaryIdentityId: identityId ?? '',
        locationIds: selectedLocationIds,
        durationKind,
        validFrom: new Date(validFrom).toISOString(),
        validUntil: durationKind === 'Permanent' ? null : new Date(validUntil).toISOString(),
      };

      const { data, error } = await api.POST('/api/access-catalog/package-requests/approval-preview', { body: request });
      if (error || !data) {
        throw new Error('Could not load approval preview.');
      }
      return data;
    },
  });

  const myRequestsQuery = useQuery({
    queryKey: [...myRequestsQueryKey, identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/package-requests', {
        params: { query: { RequesterIdentityId: identityId ?? undefined, BeneficiaryIdentityId: undefined, Status: undefined, ids: [] } as never },
      });
      if (error) {
        throw new Error('Could not load requests.');
      }
      return (data?.items ?? []).sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime());
    },
  });

  const requestPackageDetailsQuery = useQuery({
    queryKey: ['employee', 'request-access', 'request-package-details', (myRequestsQuery.data ?? []).map((item) => item.packageId).join(',')],
    enabled: (myRequestsQuery.data?.length ?? 0) > 0,
    queryFn: async () => {
      const packageIds = Array.from(new Set((myRequestsQuery.data ?? []).map((item) => item.packageId)));
      const packages = await Promise.all(
        packageIds.map(async (packageId) => {
          const { data, error } = await api.GET('/api/access-catalog/packages/{packageId}', { params: { path: { packageId } } });
          if (error || !data) {
            return null;
          }
          return data;
        }),
      );

      return new Map(packages.filter((item): item is PackageResponse => item !== null).map((item) => [item.id, item]));
    },
  });

  const submitRequest = useMutation({
    mutationFn: async (request: CreatePackageRequestRequest) => {
      const { data, error } = await api.POST('/api/access-catalog/package-requests', { body: request });
      if (error || !data) {
        throw new Error('Could not submit request.');
      }
      return data;
    },
    onSuccess: async (request) => {
      setSubmittedRequest(request);
      await queryClient.invalidateQueries({ queryKey: myRequestsQueryKey });
      toast.success('Request submitted.');
    },
    onError: () => {
      toast.error('Could not submit request.');
    },
  });

  const requestablePackages = requestablePackagesQuery.data ?? [];
  const selectedPackage = requestablePackages.find((item) => item.id === selectedPackageId) ?? null;
  const workLocations = employeeWorkLocationsQuery.data ?? [];
  const defaultLocationIds = new Set(workLocations.map((item: EmployeeWorkLocationResponse) => item.locationId));
  const primaryLocationIds = new Set(workLocations.filter((item: EmployeeWorkLocationResponse) => item.isPrimary).map((item: EmployeeWorkLocationResponse) => item.locationId));
  const selectedLocationDetails = selectedLocationDetailsQuery.data ?? new Map<string, LocationResponse>();
  const approvalPreview = approvalPreviewQuery.data?.approvals ?? [];
  const compliancePreview = approvalPreviewQuery.data?.compliance ?? [];
  const myRequests = myRequestsQuery.data ?? [];
  const filteredMyRequests = myRequests.filter((request) => matchesMyRequestFilter(request, myRequestFilter));
  const myRequestPackages = requestPackageDetailsQuery.data ?? new Map<string, PackageResponse>();
  const hasActorContext = Boolean(identityId && employeeId);

  function resetForm() {
    setStep(0);
    setSelectedPackageId('');
    setDurationKind('Permanent');
    setValidFrom(getDefaultDateTime());
    setValidUntil('');
    setBusinessJustification('');
    setSelectedLocationIds(workLocations.map((item: EmployeeWorkLocationResponse) => item.locationId));
    setPickerLocationId(null);
    setIsAddLocationOpen(false);
    setStepError(null);
    setSubmittedRequest(null);
  }

  function openRequest(requestId: string) {
    void navigate({ to: '/employee/request-access/$requestId', params: { requestId } });
  }

  function validateStep(currentStep: RequestStep) {
    if (!hasActorContext) {
      return 'Current employee identity is incomplete.';
    }

    if (currentStep === 0 && !selectedPackageId) {
      return 'Select a package to continue.';
    }

    if (currentStep === 1) {
      if (!validFrom) {
        return 'Select a start date.';
      }
      if (selectedLocationIds.length === 0) {
        return 'Add at least one location.';
      }
      if (businessJustification.trim() === '') {
        return 'Add business justification.';
      }
      if (durationKind === 'Temporary') {
        if (!validUntil) {
          return 'Select an end date.';
        }
        if (new Date(validUntil).getTime() <= new Date(validFrom).getTime()) {
          return 'End date must be after start date.';
        }
      }
    }

    if (currentStep === 2 && approvalPreviewQuery.isError) {
      return 'Approval preview must load before you continue.';
    }

    return null;
  }

  function handleNext() {
    const error = validateStep(step);
    setStepError(error);
    if (error) {
      return;
    }

    if (step < 3) {
      setStep((current) => (current + 1) as RequestStep);
    }
  }

  function handleBack() {
    setStepError(null);
    if (step > 0) {
      setStep((current) => (current - 1) as RequestStep);
    }
  }

  function handleAddLocation() {
    if (!pickerLocationId || selectedLocationIds.includes(pickerLocationId)) {
      return;
    }

    setSelectedLocationIds((current) => [...current, pickerLocationId]);
    setPickerLocationId(null);
    setStepError(null);
  }

  function handleRemoveLocation(locationId: string) {
    setSelectedLocationIds((current) => current.filter((item) => item !== locationId));
    setStepError(null);
  }

  function handleSubmit() {
    const error = validateStep(3);
    setStepError(error);
    if (error || !identityId) {
      return;
    }

    submitRequest.mutate({
      packageId: selectedPackageId,
      requesterIdentityId: identityId,
      beneficiaryIdentityId: identityId,
      locationIds: selectedLocationIds,
      requestReason: businessJustification.trim(),
      durationKind,
      validFrom: new Date(validFrom).toISOString(),
      validUntil: durationKind === 'Permanent' ? null : new Date(validUntil).toISOString(),
    });
  }

  return (
    <div className="grid gap-6">
      {!actorQuery.isLoading && !hasActorContext ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load current employee identity. Access requests need both employee and identity records.</p>
      ) : null}

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as EmployeeRequestTab)}>
        <TabsList>
          <TabsTrigger value="new-request">New Request</TabsTrigger>
          <TabsTrigger value="my-requests">My Requests</TabsTrigger>
        </TabsList>

        <TabsContent value="new-request" className="grid gap-6">
          <div className="rounded-structural border border-border bg-content p-6 md:p-7">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-primary">Request Flow</p>
              <div className="flex flex-wrap items-center gap-4 text-[12px] text-muted-foreground">
                <span className="inline-flex items-center gap-2"><span className="size-2 rounded-full bg-primary" aria-hidden="true" />In progress</span>
                <span className="inline-flex items-center gap-2"><span className="size-2 rounded-full bg-success" aria-hidden="true" />Completed</span>
                <span className="inline-flex items-center gap-2"><span className="size-2 rounded-full bg-border" aria-hidden="true" />Upcoming</span>
              </div>
            </div>

            <div className="mt-6 overflow-x-auto">
              <div className="relative min-w-[52rem]">
                <span className="absolute left-[0.5rem] right-[0.5rem] top-2 h-px bg-border" aria-hidden="true" />
                <ol className="grid grid-cols-4 gap-8">
                {requestSteps.map((item, index) => {
                  const isActive = step === index;
                  const isComplete = step > index || submittedRequest !== null;
                  const stepToneClass = isComplete ? 'text-success' : isActive ? 'text-primary' : 'text-muted-foreground';
                  const nodeClass = isComplete ? 'border-success bg-success-background' : isActive ? 'border-primary bg-active-blue' : 'border-border bg-content';

                  return (
                    <li key={item.title} className="grid grid-rows-[24px_minmax(92px,auto)] gap-5">
                      <div className="flex items-center">
                        <span className={cn('relative z-10 flex size-4 shrink-0 items-center justify-center rounded-full border-2', nodeClass)}>
                          {isComplete ? <span className="size-1.5 rounded-full bg-success" aria-hidden="true" /> : isActive ? <span className="size-1.5 rounded-full bg-primary" aria-hidden="true" /> : null}
                        </span>
                      </div>
                      <div>
                        <div className="flex items-end gap-3">
                          <span className={cn('text-[36px] leading-none font-semibold tracking-tight', stepToneClass)}>{String(index + 1).padStart(2, '0')}</span>
                          <span className={cn('pb-1 text-[13px] font-semibold uppercase tracking-[0.18em]', stepToneClass)}>{item.title}</span>
                        </div>
                        <p className="mt-3 max-w-[16rem] text-[13px] leading-5 text-muted-foreground">{item.description}</p>
                      </div>
                    </li>
                  );
                })}
                </ol>
              </div>
            </div>
          </div>

          {submittedRequest ? (
            <div className="mt-4 rounded-structural border border-border bg-content p-6 sm:p-8">
              <div className="flex max-w-2xl flex-col gap-4">
                <div className="flex items-center gap-3 text-success">
                  <CheckCircle2 className="size-6" aria-hidden="true" />
                  <h2 className="text-[24px] font-semibold tracking-tight text-foreground">Request sent</h2>
                </div>
                <p className="text-[14px] leading-6 text-muted-foreground">Your request for <span className="font-medium text-foreground">{selectedPackage?.name ?? 'selected package'}</span> was submitted with status <span className="font-medium text-foreground">{submittedRequest.status}</span>.</p>
                <div className="flex flex-wrap gap-3 pt-2">
                  <Button type="button" onClick={resetForm}>New request</Button>
                  <Button type="button" variant="outline" onClick={() => setActiveTab('my-requests')}>Open my requests</Button>
                </div>
              </div>
            </div>
          ) : (
            <div className="mt-4 grid gap-6">
              <div className="grid gap-6">
                <div className="rounded-structural border border-border bg-content p-6">
                  {step === 0 ? (
                    <div className="grid gap-5">
                      <div>
                        <h2 className="text-[20px] font-semibold tracking-tight">Select package</h2>
                        <p className="mt-2 text-[14px] text-muted-foreground">Choose a requestable package from active catalogues.</p>
                      </div>

                      {requestablePackagesQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load requestable packages.</p> : null}
                      {requestablePackagesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading requestable packages...</p> : null}

                      {!requestablePackagesQuery.isLoading ? (
                        <div className="grid gap-3">
                          {requestablePackages.map((item) => {
                            const isSelected = item.id === selectedPackageId;

                            return (
                              <button
                                key={item.id}
                                type="button"
                                className={cn('rounded-[18px] border bg-content p-5 text-left shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]', isSelected ? 'border-primary/30 bg-active-blue/60' : 'border-border')}
                                onClick={() => { setSelectedPackageId(item.id); setStepError(null); }}
                                disabled={!hasActorContext}
                              >
                                <div className="flex items-start justify-between gap-3">
                                  <div>
                                    <p className="text-[15px] font-semibold text-foreground">{item.name}</p>
                                    <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{item.description ?? 'No package description.'}</p>
                                  </div>
                                  {isSelected ? <Badge variant="default">Selected</Badge> : null}
                                </div>
                                <div className="mt-4 flex flex-wrap gap-2">
                                  <RequestMetaChip>{item.catalogIds.length === 1 ? '1 catalogue' : `${item.catalogIds.length} catalogues`}</RequestMetaChip>
                                  <RequestMetaChip>requestable</RequestMetaChip>
                                </div>
                              </button>
                            );
                          })}
                        </div>
                      ) : null}
                    </div>
                  ) : null}

                  {step === 1 ? (
                    <div className="grid gap-6">
                      <div>
                        <h2 className="text-[20px] font-semibold tracking-tight">Set time period</h2>
                        <p className="mt-2 text-[14px] text-muted-foreground">Choose duration, confirm locations, and explain the business need.</p>
                      </div>

                      <section className="grid gap-4">
                        <div className="grid gap-4 md:grid-cols-2">
                          <button type="button" className={durationKind === 'Permanent' ? 'rounded-[18px] border border-primary/20 bg-active-blue p-5 text-left shadow-[0_10px_28px_rgba(17,24,39,0.08)]' : 'rounded-[18px] border border-border bg-content p-5 text-left shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]'} onClick={() => { setDurationKind('Permanent'); setStepError(null); }}>
                            <span className="block text-[15px] font-semibold text-foreground">Permanent</span>
                            <span className="mt-1 block text-[14px] text-muted-foreground">Keep access without end date.</span>
                          </button>
                          <button type="button" className={durationKind === 'Temporary' ? 'rounded-[18px] border border-primary/20 bg-active-blue p-5 text-left shadow-[0_10px_28px_rgba(17,24,39,0.08)]' : 'rounded-[18px] border border-border bg-content p-5 text-left shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]'} onClick={() => { setDurationKind('Temporary'); setStepError(null); }}>
                            <span className="block text-[15px] font-semibold text-foreground">Temporary</span>
                            <span className="mt-1 block text-[14px] text-muted-foreground">Grant access for a fixed date range.</span>
                          </button>
                        </div>

                        <div className="grid gap-5 md:grid-cols-2">
                          <DateTimeField label="Valid from" value={validFrom} onChange={(value) => { setValidFrom(value); setStepError(null); }} />
                          {durationKind === 'Temporary' ? <DateTimeField label="Valid until" value={validUntil} onChange={(value) => { setValidUntil(value); setStepError(null); }} placeholder="Pick end date" /> : <SummaryBlock label="Valid until" value="No end date" />}
                        </div>
                      </section>

                      <section className="grid gap-4">
                        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                          <div>
                            <h3 className="text-[18px] font-semibold tracking-tight">Request locations</h3>
                            <p className="mt-2 text-[14px] text-muted-foreground">Your work locations are prefilled. Add more locations if needed.</p>
                          </div>
                          <Button type="button" variant="outline" onClick={() => setIsAddLocationOpen((current) => !current)} disabled={!hasActorContext}>
                            <Plus className="size-4" aria-hidden="true" />
                            {isAddLocationOpen ? 'Cancel' : 'Add location'}
                          </Button>
                        </div>

                        {isAddLocationOpen ? (
                          <div className="grid gap-4 rounded-[18px] border border-border bg-background p-4">
                            <LocationSelector value={pickerLocationId} onChange={setPickerLocationId} level="Room" />
                            <div className="flex justify-end">
                              <Button type="button" disabled={!pickerLocationId || selectedLocationIds.includes(pickerLocationId)} onClick={handleAddLocation}>
                                <Plus className="size-4" aria-hidden="true" />
                                Add location
                              </Button>
                            </div>
                          </div>
                        ) : null}

                        {employeeWorkLocationsQuery.isError || selectedLocationDetailsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{employeeWorkLocationsQuery.isError ? 'Could not load default work locations.' : 'Could not load selected locations.'}</p> : null}
                        {employeeWorkLocationsQuery.isLoading || selectedLocationDetailsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading locations...</p> : null}

                        {selectedLocationIds.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No locations selected yet.</p> : null}

                        {selectedLocationIds.length > 0 ? (
                          <div className="grid gap-3">
                            {selectedLocationIds.map((locationId) => (
                              <div key={locationId} className="flex items-center justify-between gap-4 rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                                <div className="min-w-0">
                                  <p className="text-[15px] font-semibold text-foreground">{getLocationLabel(selectedLocationDetails.get(locationId))}</p>
                                  <p className="mt-1 text-[14px] text-muted-foreground">{defaultLocationIds.has(locationId) ? primaryLocationIds.has(locationId) ? 'Primary work location' : 'Default work location' : 'Added for this request'}</p>
                                </div>
                                <Button type="button" variant="outline" size="sm" onClick={() => handleRemoveLocation(locationId)}>
                                  <Trash2 className="size-4" aria-hidden="true" />
                                  Remove
                                </Button>
                              </div>
                            ))}
                          </div>
                        ) : null}
                      </section>

                      <label className="grid gap-2 text-[14px] font-medium">
                        <span>Business justification</span>
                        <Textarea value={businessJustification} onChange={(event) => { setBusinessJustification(event.target.value); setStepError(null); }} rows={6} placeholder="Explain why you need this access." />
                      </label>
                    </div>
                  ) : null}

                  {step === 2 ? (
                    <div className="grid gap-6">
                      <div>
                        <h2 className="text-[20px] font-semibold tracking-tight">Review approval and compliance</h2>
                        <p className="mt-2 text-[14px] text-muted-foreground">Inspect the approval path and current compliance posture before submitting.</p>
                      </div>

                      {approvalPreviewQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load approval preview.</p> : null}
                      {approvalPreviewQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading approval preview...</p> : null}

                      {!approvalPreviewQuery.isLoading ? (
                        <div className="grid gap-6">
                          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                            <SummaryBlock label="Access items" value={String(approvalPreview.length)} />
                            <SummaryBlock label="Approval requirements" value={String(approvalPreview.reduce((total, item) => total + item.requirements.length, 0))} />
                            <SummaryBlock label="Autoapproved items" value={String(approvalPreview.filter((item) => item.requirements.length === 0).length)} />
                            <SummaryBlock label="Locations with issues" value={String(compliancePreview.filter(hasComplianceIssue).length)} />
                          </div>

                          <section className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                            <h3 className="text-[18px] font-semibold tracking-tight text-foreground">Approval path</h3>
                            <p className="mt-2 text-[14px] text-muted-foreground">Each access item shows whether it autoapproves or requires additional review. Open details only when you need the exact requirement breakdown.</p>
                            <div className="mt-5 grid gap-4">
                              {approvalPreview.length === 0 && !approvalPreviewQuery.isError ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No approval requirements returned. This request will autoapprove.</p> : approvalPreview.map((item) => <ApprovalPreviewRow key={item.accessItemId} item={item} selectedLocationDetails={selectedLocationDetails} />)}
                            </div>
                          </section>

                          <section className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                            <h3 className="text-[18px] font-semibold tracking-tight text-foreground">Compliance status</h3>
                            <p className="mt-2 text-[14px] text-muted-foreground">Provisioning waits until the required compliance checks are satisfied. Problematic locations stay visible first; compliant locations collapse below.</p>
                            <div className="mt-4 rounded-interactive border border-border bg-background px-4 py-3 text-[13px] text-muted-foreground">Approval can still proceed while compliance is pending. Provisioning resumes once blocking compliance items are fulfilled.</div>
                            {compliancePreview.length === 0 && !approvalPreviewQuery.isError ? <p className="mt-5 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No compliance preview returned.</p> : (
                              <div className="mt-5 grid gap-4">
                                {compliancePreview.filter(hasComplianceIssue).map((item) => <CompliancePreviewRow key={item.locationId} item={item} />)}
                                {compliancePreview.filter((item) => !hasComplianceIssue(item)).length > 0 ? (
                                  <details className="rounded-[18px] border border-border bg-background p-4">
                                    <summary className="cursor-pointer list-none text-[14px] font-semibold text-foreground">Compliant locations ({compliancePreview.filter((item) => !hasComplianceIssue(item)).length})</summary>
                                    <div className="mt-4 grid gap-3">
                                      {compliancePreview.filter((item) => !hasComplianceIssue(item)).map((item) => <CompliancePreviewRow key={item.locationId} item={item} compact />)}
                                    </div>
                                  </details>
                                ) : null}
                              </div>
                            )}
                          </section>
                        </div>
                      ) : null}
                    </div>
                  ) : null}

                  {step === 3 ? (
                    <div className="grid gap-6">
                      <div>
                        <h2 className="text-[20px] font-semibold tracking-tight">Submit request</h2>
                        <p className="mt-2 text-[14px] text-muted-foreground">Review the request one last time before sending it for approval.</p>
                      </div>

                      <div className="grid gap-4 md:grid-cols-2">
                        <SummaryBlock label="Package" value={selectedPackage?.name ?? '-'} />
                        <SummaryBlock label="Time period" value={durationKind === 'Permanent' ? 'Permanent' : 'Range'} />
                        <SummaryBlock label="Valid from" value={formatDateTimeLabel(validFrom)} />
                        <SummaryBlock label="Valid until" value={durationKind === 'Permanent' ? 'No end date' : formatDateTimeLabel(validUntil)} />
                      </div>

                      <div className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Locations</p>
                        <div className="mt-4 flex flex-wrap gap-2">
                          {selectedLocationIds.map((locationId) => <RequestMetaChip key={locationId}>{getLocationLabel(selectedLocationDetails.get(locationId))}</RequestMetaChip>)}
                        </div>
                      </div>

                      <div className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Business justification</p>
                        <p className="mt-3 whitespace-pre-wrap text-[14px] leading-6 text-foreground">{businessJustification.trim()}</p>
                      </div>

                      <div className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Approval summary</p>
                        <div className="mt-4 grid gap-2 text-[14px] text-foreground">
                          {approvalPreview.length === 0 ? <p>Autoapproved</p> : approvalPreview.map((item) => <p key={item.accessItemId}>{item.name}: {item.requirements.length === 0 ? 'Autoapproved' : `${item.requirements.length} approval${item.requirements.length === 1 ? '' : 's'} required`}{item.isComplianceRequired ? '' : ' • compliance not required'}</p>)}
                        </div>
                      </div>

                      <div className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Compliance summary</p>
                        <div className="mt-4 grid gap-2 text-[14px] text-foreground">
                          {compliancePreview.length === 0 ? <p>No compliance preview.</p> : compliancePreview.map((item) => <p key={item.locationId}>{item.locationLabel}: {getGrantComplianceLabel(item.status)}{item.compliantUntil ? ` until ${formatDateTimeLabel(item.compliantUntil)}` : ''}</p>)}
                        </div>
                      </div>
                    </div>
                  ) : null}

                  {stepError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{stepError}</p> : null}
                  {submitRequest.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not submit request.</p> : null}
                </div>

                <div className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)]">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <Button type="button" variant="outline" onClick={handleBack} disabled={step === 0 || submitRequest.isPending}>
                      <ChevronLeft className="size-4" aria-hidden="true" />
                      Back
                    </Button>

                    {step < 3 ? (
                      <Button type="button" onClick={handleNext} disabled={submitRequest.isPending}>
                        Next
                        <ArrowRight className="size-4" aria-hidden="true" />
                      </Button>
                    ) : (
                      <Button type="button" onClick={handleSubmit} disabled={submitRequest.isPending || approvalPreviewQuery.isLoading}>
                        {submitRequest.isPending ? 'Submitting...' : 'Submit request'}
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}
        </TabsContent>

        <TabsContent value="my-requests">
          <Card className="p-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h2 className="text-[20px] font-semibold tracking-tight">My Requests</h2>
                <p className="mt-2 text-[14px] text-muted-foreground">Track package requests you have submitted.</p>
              </div>
              <Link to="/employee/request-access" className={buttonVariants({ variant: 'outline' })}>New request</Link>
            </div>

            <div className="mt-6 flex flex-wrap gap-2 border-b border-border pb-5">
              {myRequestFilters.map((filter) => {
                const count = myRequests.filter((request) => matchesMyRequestFilter(request, filter.value)).length;
                const isActive = myRequestFilter === filter.value;

                return (
                  <button
                    key={filter.value}
                    type="button"
                    className={cn(
                      'inline-flex items-center gap-2 rounded-interactive border px-3 py-2 text-[13px] font-semibold transition',
                      isActive
                        ? 'border-primary/20 bg-active-blue text-primary'
                        : 'border-border bg-content text-muted-foreground hover:bg-hover-blue hover:text-foreground',
                    )}
                    onClick={() => setMyRequestFilter(filter.value)}
                  >
                    <span>{filter.label}</span>
                    <span className="text-[12px] font-medium opacity-80">{count}</span>
                  </button>
                );
              })}
            </div>

            {myRequestsQuery.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load requests.</p> : null}
            {myRequestsQuery.isLoading ? <p className="mt-6 text-[14px] text-muted-foreground">Loading requests...</p> : null}

            {!myRequestsQuery.isLoading && myRequests.length === 0 ? <p className="mt-6 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No requests yet.</p> : null}
            {!myRequestsQuery.isLoading && myRequests.length > 0 && filteredMyRequests.length === 0 ? <p className="mt-6 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No requests match this status.</p> : null}

            {filteredMyRequests.length > 0 ? (
              <div className="mt-6 grid gap-4">
                <div className="hidden overflow-x-auto rounded-[18px] border border-border bg-content shadow-[0_10px_28px_rgba(17,24,39,0.08)] md:block">
                  <table className="w-full min-w-[60rem] border-collapse text-left text-[14px]">
                    <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                      <tr>
                        <th className="px-5 py-4 font-semibold">Package</th>
                        <th className="px-5 py-4 font-semibold">Status</th>
                        <th className="px-5 py-4 font-semibold">Created</th>
                        <th className="px-5 py-4 font-semibold">Valid from</th>
                        <th className="px-5 py-4 font-semibold">Valid until</th>
                        <th className="px-5 py-4 text-right font-semibold">Open</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredMyRequests.map((request, index) => (
                        <tr key={request.id} className={cn('cursor-pointer transition hover:bg-hover-blue/45', index !== 0 && 'border-t border-border')} role="link" tabIndex={0} onClick={() => openRequest(request.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(request.id); } }}>
                          <td className="px-5 py-5 align-top">
                            <div>
                              <p className="text-[15px] font-semibold text-foreground">{myRequestPackages.get(request.packageId)?.name ?? request.packageId}</p>
                              <p className="mt-1 text-[13px] leading-5 text-muted-foreground">Track approval progress and effective dates for this access request.</p>
                            </div>
                          </td>
                          <td className="px-5 py-5 align-top"><Badge variant={getRequestStatusVariant(request)}>{formatRequestStatus(request)}</Badge></td>
                          <td className="px-5 py-5 align-top text-muted-foreground">{formatDateTimeLabel(request.createdAt)}</td>
                          <td className="px-5 py-5 align-top text-muted-foreground">{formatDateTimeLabel(request.validFrom)}</td>
                          <td className="px-5 py-5 align-top text-muted-foreground">{request.validUntil ? formatDateTimeLabel(request.validUntil) : 'No end date'}</td>
                          <td className="px-5 py-5 align-top text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="grid gap-3 md:hidden">
                  {filteredMyRequests.map((request) => (
                    <article key={request.id} className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]" role="button" tabIndex={0} onClick={() => openRequest(request.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(request.id); } }}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-[15px] font-semibold text-foreground">{myRequestPackages.get(request.packageId)?.name ?? request.packageId}</p>
                          <p className="mt-1 text-[14px] leading-6 text-muted-foreground">Track approval progress and effective dates for this access request.</p>
                        </div>
                        <div className="flex items-center gap-3"><Badge variant={getRequestStatusVariant(request)}>{formatRequestStatus(request)}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                      </div>
                      <div className="mt-4 flex flex-wrap gap-2">
                        <RequestMetaChip>{`created ${formatDateTimeLabel(request.createdAt)}`}</RequestMetaChip>
                        <RequestMetaChip>{`from ${formatDateTimeLabel(request.validFrom)}`}</RequestMetaChip>
                        <RequestMetaChip>{request.validUntil ? `until ${formatDateTimeLabel(request.validUntil)}` : 'no end date'}</RequestMetaChip>
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            ) : null}
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function DateTimeField({ label, value, onChange, disabled = false, placeholder = 'Pick date' }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly disabled?: boolean; readonly placeholder?: string; }) {
  const { date, time } = splitDatetime(value);
  const selectedDate = date ? parseISO(date) : undefined;
  const [open, setOpen] = useState(false);

  return (
    <label className="grid gap-2 text-[14px] font-medium">
      <span>{label}</span>
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="flex-[7]">
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger render={<Button variant="outline" className="w-full justify-start text-left font-normal" disabled={disabled} />}>
              <CalendarIcon className="size-4" aria-hidden="true" />
              {selectedDate ? format(selectedDate, 'MMM d, yyyy') : <span className="text-muted-foreground">{placeholder}</span>}
            </PopoverTrigger>
            <PopoverContent align="start">
              <Calendar
                mode="single"
                selected={selectedDate}
                onSelect={(nextDate) => {
                  if (!nextDate) {
                    return;
                  }

                  onChange(combineDatetime(format(nextDate, 'yyyy-MM-dd'), time));
                  setOpen(false);
                }}
                autoFocus
              />
            </PopoverContent>
          </Popover>
        </div>

        <Input type="time" value={time} onChange={(event) => onChange(combineDatetime(date, event.target.value))} className="flex-[3]" disabled={disabled} />
      </div>
    </label>
  );
}

function SummaryBlock({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div className="rounded-structural border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
      <p className="mt-3 text-[14px] text-foreground">{value}</p>
    </div>
  );
}

function ApprovalPreviewRow({ item, selectedLocationDetails }: { readonly item: ApprovalRequirementsPreviewAccessItemResponse; readonly selectedLocationDetails: Map<string, LocationResponse> }) {
  return (
    <details className="rounded-[18px] border border-border bg-background p-4">
      <summary className="cursor-pointer list-none">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-[15px] font-semibold text-foreground">{item.name}</p>
          <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{item.description ?? 'Access item in selected package.'}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant={item.isComplianceRequired ? 'secondary' : 'success'}>{item.isComplianceRequired ? 'Requires compliance' : 'Compliance not required'}</Badge>
          <Badge variant={item.requirements.length === 0 ? 'success' : 'secondary'}>{item.requirements.length === 0 ? 'Autoapproved' : `${item.requirements.length} approval${item.requirements.length === 1 ? '' : 's'}`}</Badge>
          {item.requirements.length > 0 ? <RequestMetaChip>Show details</RequestMetaChip> : null}
        </div>
      </div>
      </summary>

      {item.requirements.length > 0 ? (
        <div className="mt-4 grid gap-2">
          {item.requirements.map((requirement, index) => (
            <div key={`${requirement.locationId}-${requirement.role}-${index}`} className="rounded-interactive border border-border bg-content p-3">
              <p className="font-medium text-foreground">{getRequirementTitle(requirement)}</p>
              <p className="mt-1 text-[13px] text-muted-foreground">{getRequirementDescription(requirement, selectedLocationDetails.get(requirement.locationId))}</p>
            </div>
          ))}
        </div>
      ) : null}
    </details>
  );
}

function CompliancePreviewRow({ item, compact = false }: { readonly item: PackageRequestPreviewResponse['compliance'][number]; readonly compact?: boolean }) {
  const issueCount = item.requirements.filter((requirement) => requirement.status !== 'Fulfilled').length;

  return (
    <div className="rounded-[18px] border border-border bg-background p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-[15px] font-semibold text-foreground">{item.locationLabel}</p>
          {item.compliantUntil ? <p className="mt-1 text-[14px] text-muted-foreground">Compliant until {formatDateTimeLabel(item.compliantUntil)}</p> : null}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant={getGrantComplianceVariant(item.status)}>{getGrantComplianceLabel(item.status)}</Badge>
          {!compact && issueCount > 0 ? <RequestMetaChip>{`${issueCount} open issue${issueCount === 1 ? '' : 's'}`}</RequestMetaChip> : null}
        </div>
      </div>

      {compact ? null : item.requirements.length === 0 ? <p className="mt-4 text-[14px] text-muted-foreground">No compliance requirements for this location.</p> : (
        <div className="mt-4 grid gap-3">
          {item.requirements.map((requirement) => (
            <div key={requirement.requirementDefinitionId} className="rounded-interactive border border-border bg-content p-3">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-medium text-foreground">{requirement.name}</p>
                  <p className="mt-1 text-[13px] text-muted-foreground">{requirement.code}{requirement.isBlocking ? ' • blocking' : ''}</p>
                </div>
                <Badge variant={requirement.status === 'Fulfilled' ? 'success' : requirement.status === 'Missing' || requirement.status === 'Failed' || requirement.status === 'Expired' ? 'error' : 'secondary'}>{requirement.status}</Badge>
              </div>
              <p className="mt-3 text-[14px] text-muted-foreground">{requirement.reason}</p>
              {requirement.validUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Valid until {formatDateTimeLabel(requirement.validUntil)}</p> : null}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function RequestMetaChip({ children }: { readonly children: React.ReactNode }) {
  return <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{children}</span>;
}

const myRequestFilters = [
  { value: 'all', label: 'All' },
  { value: 'in-progress', label: 'In Progress' },
  { value: 'approved', label: 'Approved' },
  { value: 'partially-approved', label: 'Partially Approved' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'expired', label: 'Expired' },
] as const satisfies readonly { value: MyRequestFilter; label: string }[];

function matchesMyRequestFilter(request: PackageRequestResponse, filter: MyRequestFilter) {
  switch (filter) {
    case 'all':
      return true;
    case 'in-progress':
      return request.status === 'InProgress';
    case 'approved':
      return request.status === 'Completed' && request.subStatus === 'Approved';
    case 'partially-approved':
      return request.status === 'Completed' && request.subStatus === 'PartiallyApproved';
    case 'rejected':
      return request.status === 'Completed' && request.subStatus === 'Rejected';
    case 'expired':
      return request.status === 'Completed' && request.subStatus === 'Expired';
  }
}

function hasComplianceIssue(item: PackageRequestPreviewResponse['compliance'][number]) {
  return item.status !== 'Compliant' || item.requirements.some((requirement) => requirement.status !== 'Fulfilled');
}

function getDefaultDateTime() {
  const current = new Date();
  current.setMinutes(0, 0, 0);
  current.setHours(current.getHours() + 1);
  return toLocalDateTimeValue(current);
}

function splitDatetime(value: string) {
  if (!value) {
    return { date: '', time: '09:00' };
  }

  const [date, time] = value.split('T');
  return { date: date ?? '', time: (time ?? '09:00').slice(0, 5) };
}

function combineDatetime(date: string, time: string) {
  return `${date || format(new Date(), 'yyyy-MM-dd')}T${time || '09:00'}`;
}

function toLocalDateTimeValue(value: Date) {
  const timezoneOffsetMs = value.getTimezoneOffset() * 60000;
  return new Date(value.getTime() - timezoneOffsetMs).toISOString().slice(0, 16);
}

function formatDateTimeLabel(value: string) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function getRequirementTitle(requirement: ApprovalRequirementPreviewResponse) {
  if (requirement.type === 'Destination') {
    return requirement.approvalGroup?.name ? `${requirement.approvalGroup.name} approval` : 'Destination approval';
  }

  return `${requirement.role} approval`;
}

function getRequirementDescription(requirement: ApprovalRequirementPreviewResponse, location: LocationResponse | undefined) {
  const parts = [getLocationLabel(location)];

  if (requirement.approverIdentity?.displayName) {
    parts.push(`Approver: ${requirement.approverIdentity.displayName}`);
  } else if (requirement.approvalGroup?.name) {
    parts.push(`Group: ${requirement.approvalGroup.name}`);
  }

  return parts.join(' • ');
}

function formatRequestStatus(request: PackageRequestResponse) {
  if (request.status === 'InProgress') {
    return 'In Progress';
  }

  return request.subStatus === 'PartiallyApproved'
    ? 'Completed - Partially Approved'
    : request.subStatus === 'Approved'
      ? 'Completed - Approved'
      : request.subStatus === 'Rejected'
        ? 'Completed - Rejected'
        : request.subStatus === 'Expired'
          ? 'Completed - Expired'
          : 'Completed';
}

function getRequestStatusVariant(request: PackageRequestResponse) {
  if (request.status === 'InProgress') {
    return 'secondary';
  }

  switch (request.subStatus) {
    case 'Approved':
      return 'success';
    case 'Rejected':
    case 'Expired':
      return 'error';
    default:
      return 'secondary';
  }
}
