import { useTranslation } from 'react-i18next';

import { PrintDesignList } from './print-design-list';

export default function CardEditorPage() {
  const { t } = useTranslation();

  return (
    <PrintDesignList
      surfaceKind="Card"
      title={t('cardManagement.cardEditor.title')}
      description={t('cardManagement.cardEditor.description')}
      createTo="/desfire-studio/card-editor/new"
      editTo={(designId) => `/desfire-studio/card-editor/${designId}/edit`}
      emptyTitle={t('cardManagement.cardEditor.emptyTitle')}
      emptyDescription={t('cardManagement.cardEditor.emptyDescription')}
    />
  );
}
