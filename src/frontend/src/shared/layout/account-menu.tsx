import { type ReactElement } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { Button } from '@/shared/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { AppLanguageSelect } from '@/shared/i18n/app-language-select';

export function AccountMenu({
  currentUserName,
  currentUserSecondary,
  currentUserInitials,
  trigger,
  contentClassName,
}: {
  currentUserName: string;
  currentUserSecondary?: string;
  currentUserInitials: string;
  trigger: ReactElement;
  contentClassName?: string;
}) {
  const { t } = useTranslation();
  const auth = useAuth();

  return (
    <Popover>
      <PopoverTrigger render={trigger}>{currentUserInitials}</PopoverTrigger>
      <PopoverContent align="end" className={contentClassName ?? 'grid min-w-72 gap-4 border border-[var(--fabric-sidebar-panel-border)] bg-content p-4'}>
        <div className="flex items-start gap-3 border-b border-border pb-4">
          <div className="flex size-12 shrink-0 items-center justify-center rounded-full bg-[var(--fabric-sidebar-rail)] text-[15px] font-semibold tracking-[0.08em] text-white">{currentUserInitials}</div>
          <div className="min-w-0">
            <p className="truncate text-[14px] font-semibold text-foreground">{currentUserName}</p>
            {currentUserSecondary && currentUserSecondary !== currentUserName ? <p className="mt-1 truncate text-[13px] text-muted-foreground">{currentUserSecondary}</p> : null}
          </div>
        </div>
        <div className="grid gap-2">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('common.language')}</p>
          <AppLanguageSelect className="w-full" />
        </div>
        <Button type="button" variant="ghost" className="justify-start" onClick={() => void auth.signoutRedirect().catch(() => auth.removeUser())}>
          {t('common.signOut')}
        </Button>
      </PopoverContent>
    </Popover>
  );
}
