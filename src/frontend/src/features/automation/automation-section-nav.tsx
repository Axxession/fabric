import { Link, useLocation } from '@tanstack/react-router';

import { cn } from '@/shared/utils/cn';

type AutomationSection = 'workflow' | 'kiosk';

const sectionItems: readonly { readonly id: AutomationSection; readonly label: string; readonly to: string }[] = [
  { id: 'workflow', label: 'Workflow', to: '/administration/automation/workflow' },
  { id: 'kiosk', label: 'Kiosks', to: '/administration/automation/kiosk' },
];

export function AutomationSectionNav() {
  const location = useLocation();
  const activeSection = getActiveSection(location.pathname);

  return (
    <div className="inline-flex h-auto w-fit max-w-full flex-wrap justify-start gap-1 rounded-interactive bg-hover-gray p-1 text-muted-foreground" aria-label="Automation sections" role="tablist">
        {sectionItems.map((item) => (
          <Link
            key={item.id}
            to={item.to}
            className={cn(
              'inline-flex h-8 min-w-24 items-center justify-center gap-1.5 rounded-interactive px-3 text-[14px] font-medium whitespace-nowrap transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-primary/20',
              activeSection === item.id ? 'bg-content text-foreground shadow-sm' : 'text-muted-foreground hover:bg-hover-blue hover:text-foreground',
            )}
          >
            {item.label}
          </Link>
        ))}
    </div>
  );
}

function getActiveSection(pathname: string): AutomationSection {
  return pathname.startsWith('/administration/automation/kiosk') ? 'kiosk' : 'workflow';
}
