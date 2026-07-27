import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Card } from '@/shared/components/ui/card';

type ApprovalInboxItemResponse = components['schemas']['ApprovalInboxItemResponse'];

const approvalInboxQueryKey = ['manager', 'approval-inbox'] as const;

export default function ManagerApprovalInboxPage() {
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();
  const identityId = actorQuery.data?.identityId ?? null;

  const inboxQuery = useQuery({
    queryKey: [...approvalInboxQueryKey, identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-inbox', {
        params: { query: { approverIdentityId: identityId ?? '', ids: [], Page: 0, PageSize: 100 } as never },
      });

      if (error || !data) {
        throw new Error('Could not load approval inbox.');
      }

      return data.items ?? [];
    },
  });

  const items = inboxQuery.data ?? [];

  function openRequest(requestId: string) {
    void navigate({ to: '/manager/approval-inbox/$requestId', params: { requestId } });
  }

  return (
    <section className="grid gap-6">
      <Card className="p-6">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Approval Inbox</h2>
          <p className="mt-2 text-[14px] text-muted-foreground">Review approvals you can give, then drill through to the full request.</p>
        </div>

        {actorQuery.isLoading || inboxQuery.isLoading ? <p className="mt-6 text-[14px] text-muted-foreground">Loading approval inbox...</p> : null}
        {actorQuery.isError || inboxQuery.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load approval inbox.</p> : null}
        {!inboxQuery.isLoading && items.length === 0 ? <p className="mt-6 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No approvals waiting for you.</p> : null}

        {items.length > 0 ? (
          <>
            <div className="mt-6 hidden overflow-x-auto md:block">
              <table className="w-full min-w-[72rem] border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Package</th>
                    <th className="px-4 py-3 font-semibold">Beneficiary</th>
                    <th className="px-4 py-3 font-semibold">Approval</th>
                    <th className="px-4 py-3 font-semibold">Site</th>
                    <th className="px-4 py-3 font-semibold">Requested</th>
                    <th className="px-4 py-3 font-semibold">Expires</th>
                    <th className="px-4 py-3 text-right font-semibold">Open</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {items.map((item) => (
                    <tr key={item.approvalRequirementId} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => openRequest(item.requestId)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(item.requestId); } }}>
                      <td className="px-4 py-4 font-medium text-foreground">{item.packageName}</td>
                      <td className="px-4 py-4 text-muted-foreground">{item.beneficiaryDisplayName}</td>
                      <td className="px-4 py-4"><div className="grid gap-1"><span className="font-medium text-foreground">{item.type === 'Destination' ? 'Destination approval' : `${item.role} approval`}</span>{item.approvalGroupName ? <span className="text-[13px] text-muted-foreground">{item.approvalGroupName}</span> : null}</div></td>
                      <td className="px-4 py-4 text-muted-foreground">{item.siteName}</td>
                      <td className="px-4 py-4 text-muted-foreground">{item.requestedLocationLabels.join(', ')}</td>
                      <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(item.expiresAt)}</td>
                      <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-6 grid gap-3 md:hidden">
              {items.map((item) => (
                <article key={item.approvalRequirementId} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => openRequest(item.requestId)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openRequest(item.requestId); } }}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-medium text-foreground">{item.packageName}</p>
                      <p className="mt-1 text-[13px] text-muted-foreground">{item.beneficiaryDisplayName}</p>
                    </div>
                    <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
                  </div>
                  <dl className="mt-4 grid gap-2 text-[14px]">
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Approval</dt><dd className="text-right text-foreground">{item.type === 'Destination' ? 'Destination approval' : `${item.role} approval`}</dd></div>
                    {item.approvalGroupName ? <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Group</dt><dd className="text-right text-foreground">{item.approvalGroupName}</dd></div> : null}
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Site</dt><dd className="text-right text-foreground">{item.siteName}</dd></div>
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Expires</dt><dd className="text-right text-foreground">{formatDateTimeLabel(item.expiresAt)}</dd></div>
                  </dl>
                </article>
              ))}
            </div>
          </>
        ) : null}
      </Card>
    </section>
  );
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}
