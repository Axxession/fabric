import type { QueryClient } from '@tanstack/react-query';

import type { components } from '@/shared/api/generated/schema';

type ContractorJobAssignmentResponse = components['schemas']['ContractorJobAssignmentResponse'];
type ContractorJobAssignmentStatus = components['schemas']['ContractorJobAssignmentStatus'];
type ContractorJobResponse = components['schemas']['ContractorJobResponse'];
type ContractorJobStatus = components['schemas']['ContractorJobStatus'];

export type ContractorJobFormState = {
  companyId: string;
  jobTypeId: string;
  locationId: string | null;
  name: string;
  description: string;
  plannedStart: string;
  plannedEnd: string;
};

export type ContractorAssignmentFormState = {
  contractorId: string;
  assignedFrom: string;
  assignedUntil: string;
};

export function getEmptyContractorJobFormState(): ContractorJobFormState {
  return {
    companyId: '',
    jobTypeId: '',
    locationId: null,
    name: '',
    description: '',
    plannedStart: '',
    plannedEnd: '',
  };
}

export function getContractorJobFormState(job: ContractorJobResponse): ContractorJobFormState {
  return {
    companyId: job.companyId,
    jobTypeId: job.jobTypeId,
    locationId: job.locationId,
    name: job.name,
    description: job.description ?? '',
    plannedStart: toLocalDateTimeValue(job.plannedStart),
    plannedEnd: toLocalDateTimeValue(job.plannedEnd),
  };
}

export function getEmptyContractorAssignmentFormState(job: ContractorJobResponse): ContractorAssignmentFormState {
  return {
    contractorId: '',
    assignedFrom: toLocalDateTimeValue(job.plannedStart),
    assignedUntil: toLocalDateTimeValue(job.plannedEnd),
  };
}

export function getContractorAssignmentFormState(assignment: ContractorJobAssignmentResponse): ContractorAssignmentFormState {
  return {
    contractorId: assignment.contractorId,
    assignedFrom: toLocalDateTimeValue(assignment.assignedFrom),
    assignedUntil: toLocalDateTimeValue(assignment.assignedUntil),
  };
}

export function toLocalDateTimeValue(value: string) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  const timezoneOffsetMs = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - timezoneOffsetMs).toISOString().slice(0, 16);
}

export function toApiDateTimeValue(value: string) {
  return new Date(value).toISOString();
}

export function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

export function getContractorJobStatusVariant(status: ContractorJobStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Cancelled':
      return 'error';
    case 'Completed':
      return 'outline';
    default:
      return 'secondary';
  }
}

export function getAssignmentStatusVariant(status: ContractorJobAssignmentStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Cancelled':
      return 'error';
    case 'Completed':
      return 'outline';
    default:
      return 'secondary';
  }
}

export function DetailRow({ label, value }: { label: string; value: string }) {
  return <div className="flex items-center justify-between gap-3"><dt>{label}</dt><dd className="text-right text-foreground">{value}</dd></div>;
}

export function ErrorText({ message }: { message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

export function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

export function EmptyText({ message }: { message: string }) {
  return <p className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

export async function invalidateContractorJobQueries(queryClient: QueryClient, jobId: string) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', jobId, 'detail'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', 'companies'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', 'job-types'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', 'locations'] }),
  ]);
}
