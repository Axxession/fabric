import { useParams } from '@tanstack/react-router';

import { ChipDesignEditPageContent } from './chip-design-form-page';

export default function ChipDesignEditPageDesfireStudio() {
  const { chipDesignId } = useParams({ from: '/desfire-studio/chip-designs/$chipDesignId/edit' });

  return <ChipDesignEditPageContent chipDesignId={chipDesignId} />;
}
