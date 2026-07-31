import { useParams } from '@tanstack/react-router';

import { TransformationEditPageContent } from './transformation-form-page';

export default function TransformationEditPageDesfireStudio() {
  const { transformationId } = useParams({ from: '/desfire-studio/transformations/$transformationId/edit' });

  return <TransformationEditPageContent transformationId={transformationId} />;
}
