import { Link, useLocation } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import type { ResolvedAppPerspective } from '@/shared/perspectives/app-perspectives';

export function PerspectiveSidebar({ perspectives, version }: { perspectives: readonly ResolvedAppPerspective[]; version: string }) {
  const location = useLocation();
  const { t } = useTranslation();
  const activePerspective = perspectives.find((perspective) => location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`));
  const displayVersion = getDisplayVersion(version);

  return (
    <aside className="flex w-80 shrink-0 flex-col border-r border-border bg-content p-4 md:sticky md:top-[73px] md:h-[calc(100vh-73px)]">
      <div className="grid grid-cols-2 gap-2">
        {perspectives.map((perspective) => {
          const isActive = location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`);

          return (
            <Link
              key={perspective.id}
              to={perspective.to}
              className={isActive ? 'rounded-interactive bg-active-blue px-3 py-2 text-center text-[13px] font-semibold text-foreground' : 'rounded-interactive border border-border px-3 py-2 text-center text-[13px] font-semibold text-muted-foreground transition hover:bg-hover-blue hover:text-foreground'}
            >
              {perspective.shortLabel}
            </Link>
          );
        })}
      </div>

      <div className="mt-6 px-1">
        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('common.menu')}</p>
      </div>
      <nav aria-label={t('shell.perspectiveNavigation')} className="mt-3 grid gap-2">
        {activePerspective?.menuItems.map((item) => {
          const isActive = isMenuItemActive(location.pathname, item.to);

          return (
            <Link key={item.to} to={item.to} className={isActive ? 'flex gap-3 rounded-interactive bg-active-blue p-3 text-foreground' : 'flex gap-3 rounded-interactive p-3 text-foreground transition hover:bg-hover-blue'}>
              <span className="min-w-0">
                <span className="block font-semibold">{item.label}</span>
                <span className="mt-1 block text-[13px] leading-5 text-muted-foreground">{item.description}</span>
              </span>
            </Link>
          );
        })}
      </nav>

      <div className="mt-auto px-1 pt-6 text-[12px] text-muted-foreground" title={`v${version}`}>v{displayVersion}</div>
    </aside>
  );
}

function isMenuItemActive(pathname: string, itemPath: string) {
  if (pathname === itemPath) {
    return true;
  }

  if (isPerspectiveRootPath(itemPath)) {
    return false;
  }

  return pathname.startsWith(`${itemPath}/`);
}

function isPerspectiveRootPath(itemPath: string) {
  return itemPath === '/employee'
    || itemPath === '/manager'
    || itemPath === '/security-officer'
    || itemPath === '/administration';
}

function getDisplayVersion(version: string) {
  const buildMetadataIndex = version.indexOf('+');
  return buildMetadataIndex === -1 ? version : version.slice(0, buildMetadataIndex);
}
