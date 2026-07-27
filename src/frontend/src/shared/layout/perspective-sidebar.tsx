import { Link, useLocation } from '@tanstack/react-router';

import type { AppPerspective } from '@/shared/perspectives/app-perspectives';

export function PerspectiveSidebar({ perspectives, version }: { perspectives: readonly AppPerspective[]; version: string }) {
  const location = useLocation();
  const activePerspective = perspectives.find((perspective) => location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`));

  return (
    <aside className="flex w-80 shrink-0 flex-col border-r border-border bg-content p-4 md:sticky md:top-[73px] md:h-[calc(100vh-73px)]">
      <div className="mb-4 px-1">
        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Perspectives</p>
      </div>
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
        <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Menu</p>
      </div>
      <nav aria-label="Perspective navigation" className="mt-3 grid gap-2">
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

      <div className="mt-auto px-1 pt-6 text-[12px] text-muted-foreground">v{version}</div>
    </aside>
  );
}

function isMenuItemActive(pathname: string, itemPath: string) {
  if (pathname === itemPath) {
    return true;
  }

  return itemPath !== '/employee' && pathname.startsWith(`${itemPath}/`);
}
