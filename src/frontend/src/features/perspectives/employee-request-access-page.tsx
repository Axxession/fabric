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
          <Card className="p-6">
            <ol className="grid gap-4 md:grid-cols-4">
              {requestSteps.map((item, index) => {
                const isActive = step === index;
                const isComplete = step > index || submittedRequest !== null;

                return (
                  <li key={item.title} className={isActive ? 'rounded-structural border border-primary bg-active-blue p-4' : 'rounded-structural border border-border p-4'}>
                    <div className="flex items-center gap-3">
                      <span className={isComplete ? 'flex size-8 items-center justify-center rounded-full bg-primary text-white' : isActive ? 'flex size-8 items-center justify-center rounded-full bg-primary text-white' : 'flex size-8 items-center justify-center rounded-full bg-hover-gray text-muted-foreground'}>{isComplete ? <CheckCircle2 className="size-4" aria-hidden="true" /> : index + 1}</span>
                      <div>
                        <p className="text-[14px] font-semibold text-foreground">{item.title}</p>
                        <p className="text-[12px] text-muted-foreground">{item.description}</p>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ol>
          </Card>

          {submittedRequest ? (
            <Card className="p-6 sm:p-8">
              <div className="flex max-w-2xl flex-col gap-4">
                <div className="flex items-center gap-3 text-success">
                  <CheckCircle2 className="size-6" aria-hidden="true" />
                  <h2 className="text-[24px] font-semibold tracking-tight text-foreground">Requests sent</h2>
                </div>
                <p className="text-[14px] leading-6 text-muted-foreground">Your request for <span className="font-medium text-foreground">{selectedPackage?.name ?? 'selected package'}</span> was submitted with status <span className="font-medium text-foreground">{submittedRequest.status}</span>.</p>
                <div className="flex flex-wrap gap-3 pt-2">
                  <Button type="button" onClick={resetForm}>New request</Button>
                  <Button type="button" variant="outline" onClick={() => setActiveTab('my-requests')}>Open my requests</Button>
                </div>
              </div>
            </Card>
          ) : (
            <>
              <Card className="p-6">
                {step === 0 ? (
                  <div className="grid gap-5">
                    <div>
                      <h2 className="text-[20px] font-semibold tracking-tight">Step 1. Select package</h2>
                      <p className="mt-2 text-[14px] text-muted-foreground">Choose any requestable package from active catalogues.</p>
                    </div>

                    {requestablePackagesQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load requestable packages.</p> : null}
                    {requestablePackagesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading requestable packages...</p> : null}

                    {!requestablePackagesQuery.isLoading ? (
                      <label className="grid gap-2 text-[14px] font-medium md:max-w-xl">
                        <span>Package</span>
                        <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedPackageId} onChange={(event) => { setSelectedPackageId(event.target.value); setStepError(null); }} disabled={!hasActorContext || requestablePackages.length === 0}>
                          <option value="">Select package</option>
                          {requestablePackages.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                        </select>
                      </label>
                    ) : null}

                    {selectedPackage ? (
                      <div className="rounded-structural border border-border p-4">
                        <p className="font-medium text-foreground">{selectedPackage.name}</p>
                        <p className="mt-1 text-[14px] text-muted-foreground">{selectedPackage.description ?? 'No package description.'}</p>
                      </div>
                    ) : null}
                  </div>
                ) : null}

                {step === 1 ? (
                  <div className="grid gap-6">
                    <div>
                      <h2 className="text-[20px] font-semibold tracking-tight">Step 2. Time period</h2>
                      <p className="mt-2 text-[14px] text-muted-foreground">Set how long access is needed, confirm locations, and explain business need.</p>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                      <button type="button" className={durationKind === 'Permanent' ? 'rounded-structural border border-primary bg-active-blue p-4 text-left' : 'rounded-structural border border-border p-4 text-left transition hover:bg-hover-blue'} onClick={() => { setDurationKind('Permanent'); setStepError(null); }}>
                        <span className="block font-semibold text-foreground">Permanent</span>
                        <span className="mt-1 block text-[13px] text-muted-foreground">Keep access without end date.</span>
                      </button>
                      <button type="button" className={durationKind === 'Temporary' ? 'rounded-structural border border-primary bg-active-blue p-4 text-left' : 'rounded-structural border border-border p-4 text-left transition hover:bg-hover-blue'} onClick={() => { setDurationKind('Temporary'); setStepError(null); }}>
                        <span className="block font-semibold text-foreground">Range</span>
                        <span className="mt-1 block text-[13px] text-muted-foreground">Grant access for a fixed date range.</span>
                      </button>
                    </div>

                    {durationKind === 'Temporary' ? (
                      <div className="grid gap-5 md:grid-cols-2">
                        <DateTimeField label="Valid from" value={validFrom} onChange={(value) => { setValidFrom(value); setStepError(null); }} />
                        <DateTimeField label="Valid until" value={validUntil} onChange={(value) => { setValidUntil(value); setStepError(null); }} placeholder="Pick end date" />
                      </div>
                    ) : null}

                    <div className="grid gap-4">
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
                        <div className="grid gap-4 rounded-structural border border-border p-4">
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
                            <div key={locationId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                              <div className="min-w-0">
                                <p className="font-medium text-foreground">{getLocationLabel(selectedLocationDetails.get(locationId))}</p>
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
                    </div>

                    <label className="grid gap-2 text-[14px] font-medium">
                      <span>Business justification</span>
                      <Textarea value={businessJustification} onChange={(event) => { setBusinessJustification(event.target.value); setStepError(null); }} rows={5} placeholder="Explain why you need this access." />
                    </label>
                  </div>
                ) : null}

                {step === 2 ? (
                  <div className="grid gap-6">
                    <div>
                      <h2 className="text-[20px] font-semibold tracking-tight">Step 3. Approval and compliance</h2>
                      <p className="mt-2 text-[14px] text-muted-foreground">Review approvals and current compliance state for the selected request.</p>
                    </div>

                    {approvalPreviewQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load approval preview.</p> : null}
                    {approvalPreviewQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading approval preview...</p> : null}

                    {!approvalPreviewQuery.isLoading ? (
                      <div className="grid gap-4">
                        {approvalPreview.map((item) => (
                          <div key={item.accessItemId} className="rounded-structural border border-border p-4">
                            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                              <div>
                                <p className="font-medium text-foreground">{item.name}</p>
                                <p className="mt-1 text-[14px] text-muted-foreground">{item.description ?? 'Access item in selected package.'}</p>
                              </div>
                              <div className="flex flex-wrap items-center gap-2">
                                <Badge variant={item.isComplianceRequired ? 'secondary' : 'success'}>
                                  {item.isComplianceRequired ? 'Requires compliance' : 'Compliance not required'}
                                </Badge>
                                <Badge variant={item.requirements.length === 0 ? 'success' : 'secondary'}>{item.requirements.length === 0 ? 'Autoapproved' : `${item.requirements.length} approval${item.requirements.length === 1 ? '' : 's'}`}</Badge>
                              </div>
                            </div>

                            {item.requirements.length === 0 ? null : (
                              <div className="mt-4 grid gap-3">
                                {item.requirements.map((requirement, index) => (
                                  <div key={`${requirement.locationId}-${requirement.role}-${index}`} className="rounded-structural border border-border bg-hover-gray/50 p-3">
                                    <p className="font-medium text-foreground">{getRequirementTitle(requirement)}</p>
                                    <p className="mt-1 text-[14px] text-muted-foreground">{getRequirementDescription(requirement, selectedLocationDetails.get(requirement.locationId))}</p>
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}

                        {approvalPreview.length === 0 && !approvalPreviewQuery.isError ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No approval requirements returned. This request will autoapprove.</p> : null}

                        <div className="mt-2 rounded-structural border border-border p-4">
                          <div>
                            <h3 className="text-[18px] font-semibold tracking-tight">Compliance check</h3>
                            <p className="mt-2 text-[14px] text-muted-foreground">Requests can still be approved while compliance is pending. Provisioning waits until compliance is met.</p>
                          </div>
                          <div className="mt-4 grid gap-4">
                            {compliancePreview.map((item) => (
                              <div key={item.locationId} className="rounded-structural border border-border bg-hover-gray/30 p-4">
                                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                                  <div>
                                    <p className="font-medium text-foreground">{item.locationLabel}</p>
                                    {item.compliantUntil ? <p className="mt-1 text-[14px] text-muted-foreground">Compliant until {formatDateTimeLabel(item.compliantUntil)}</p> : null}
                                  </div>
                                  <Badge variant={getGrantComplianceVariant(item.status)}>{getGrantComplianceLabel(item.status)}</Badge>
                                </div>
                                {item.requirements.length === 0 ? (
                                  <p className="mt-4 text-[14px] text-muted-foreground">No compliance requirements for this location.</p>
                                ) : (
                                  <div className="mt-4 grid gap-3">
                                    {item.requirements.map((requirement) => (
                                      <div key={requirement.requirementDefinitionId} className="rounded-structural border border-border bg-background p-3">
                                        <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                                          <div>
                                            <p className="font-medium text-foreground">{requirement.name}</p>
                                            <p className="mt-1 text-[14px] text-muted-foreground">{requirement.code}{requirement.isBlocking ? ' • blocking' : ''}</p>
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
                            ))}
                            {compliancePreview.length === 0 && !approvalPreviewQuery.isError ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No compliance preview returned.</p> : null}
                          </div>
                        </div>
                      </div>
                    ) : null}
                  </div>
                ) : null}

                {step === 3 ? (
                  <div className="grid gap-6">
                    <div>
                      <h2 className="text-[20px] font-semibold tracking-tight">Step 4. Submit</h2>
                      <p className="mt-2 text-[14px] text-muted-foreground">Confirm details before sending the request.</p>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                      <SummaryBlock label="Package" value={selectedPackage?.name ?? '-'} />
                      <SummaryBlock label="Time period" value={durationKind === 'Permanent' ? 'Permanent' : 'Range'} />
                      <SummaryBlock label="Valid from" value={formatDateTimeLabel(validFrom)} />
                      <SummaryBlock label="Valid until" value={durationKind === 'Permanent' ? 'No end date' : formatDateTimeLabel(validUntil)} />
                    </div>

                    <div className="rounded-structural border border-border p-4">
                      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Locations</p>
                      <div className="mt-3 grid gap-2">
                        {selectedLocationIds.map((locationId) => <p key={locationId} className="text-[14px] text-foreground">{getLocationLabel(selectedLocationDetails.get(locationId))}</p>)}
                      </div>
                    </div>

                    <div className="rounded-structural border border-border p-4">
                      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Business justification</p>
                      <p className="mt-3 text-[14px] leading-6 text-foreground whitespace-pre-wrap">{businessJustification.trim()}</p>
                    </div>

                    <div className="rounded-structural border border-border p-4">
                      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Approval summary</p>
                      <div className="mt-3 grid gap-2">
                        {approvalPreview.length === 0 ? <p className="text-[14px] text-foreground">Autoapproved</p> : approvalPreview.map((item) => <p key={item.accessItemId} className="text-[14px] text-foreground">{item.name}: {item.requirements.length === 0 ? 'Autoapproved' : `${item.requirements.length} approval${item.requirements.length === 1 ? '' : 's'} required`}{item.isComplianceRequired ? '' : ' • compliance not required'}</p>)}
                      </div>
                    </div>

                    <div className="rounded-structural border border-border p-4">
                      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Compliance summary</p>
                      <div className="mt-3 grid gap-2">
                        {compliancePreview.length === 0 ? <p className="text-[14px] text-foreground">No compliance preview.</p> : compliancePreview.map((item) => <p key={item.locationId} className="text-[14px] text-foreground">{item.locationLabel}: {getGrantComplianceLabel(item.status)}{item.compliantUntil ? ` until ${formatDateTimeLabel(item.compliantUntil)}` : ''}</p>)}
                      </div>
                    </div>
                  </div>
                ) : null}

                {stepError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{stepError}</p> : null}
                {submitRequest.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not submit request.</p> : null}
              </Card>

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
            </>
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

            {myRequestsQuery.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load requests.</p> : null}
            {myRequestsQuery.isLoading ? <p className="mt-6 text-[14px] text-muted-foreground">Loading requests...</p> : null}

            {!myRequestsQuery.isLoading && myRequests.length === 0 ? <p className="mt-6 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No requests yet.</p> : null}

            {myRequests.length > 0 ? (
              <>
                <div className="mt-6 hidden overflow-x-auto md:block">
                  <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
                    <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                      <tr>
                        <th className="px-4 py-3 font-semibold">Package</th>
                        <th className="px-4 py-3 font-semibold">Status</th>
                        <th className="px-4 py-3 font-semibold">Created</th>
                        <th className="px-4 py-3 font-semibold">Valid from</th>
                        <th className="px-4 py-3 font-semibold">Valid until</th>
                        <th className="px-4 py-3 text-right font-semibold">Open</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {myRequests.map((request) => (
                        <tr key={request.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => openRequest(request.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(request.id); } }}>
                          <td className="px-4 py-4 font-medium text-foreground">{myRequestPackages.get(request.packageId)?.name ?? request.packageId}</td>
                          <td className="px-4 py-4"><Badge variant={getRequestStatusVariant(request)}>{formatRequestStatus(request)}</Badge></td>
                          <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(request.createdAt)}</td>
                          <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(request.validFrom)}</td>
                          <td className="px-4 py-4 text-muted-foreground">{request.validUntil ? formatDateTimeLabel(request.validUntil) : 'No end date'}</td>
                          <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="mt-6 grid gap-3 md:hidden">
                  {myRequests.map((request) => (
                    <article key={request.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => openRequest(request.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(request.id); } }}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="font-medium text-foreground">{myRequestPackages.get(request.packageId)?.name ?? request.packageId}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">Created {formatDateTimeLabel(request.createdAt)}</p>
                        </div>
                        <div className="flex items-center gap-3"><Badge variant={getRequestStatusVariant(request)}>{formatRequestStatus(request)}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                      </div>
                      <dl className="mt-4 grid gap-2 text-[14px]">
                        <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Valid from</dt><dd className="text-right text-foreground">{formatDateTimeLabel(request.validFrom)}</dd></div>
                        <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Valid until</dt><dd className="text-right text-foreground">{request.validUntil ? formatDateTimeLabel(request.validUntil) : 'No end date'}</dd></div>
                      </dl>
                    </article>
                  ))}
                </div>
              </>
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
