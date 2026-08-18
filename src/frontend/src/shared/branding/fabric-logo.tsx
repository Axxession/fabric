export function FabricLogo({ logoUrl }: { logoUrl?: string }) {
  if (logoUrl) {
    return <img src={logoUrl} alt="" className="size-10 rounded-interactive object-contain" aria-hidden="true" />;
  }

  return (
    <div className="flex size-10 items-center justify-center text-primary" aria-hidden="true">
      <svg viewBox="0 0 40 40" className="size-10" fill="none" stroke="currentColor">
        <circle cx="20" cy="20" r="16.5" strokeWidth="1.4" />
        <circle cx="20" cy="20" r="11.5" strokeWidth="2" />
        <circle cx="20" cy="20" r="6.5" strokeWidth="2.4" />
        <circle cx="20" cy="20" r="2" fill="currentColor" stroke="none" />
      </svg>
    </div>
  );
}
