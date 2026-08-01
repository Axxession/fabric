import { useParams } from '@tanstack/react-router';

import { KeyGroupEditPageContent } from './key-group-form-page';

export default function KeyGroupEditPageDesfireStudio() {
  const { keyGroupId } = useParams({ from: '/desfire-studio/key-groups/$keyGroupId/edit' });

  return <KeyGroupEditPageContent keyGroupId={keyGroupId} />;
}
