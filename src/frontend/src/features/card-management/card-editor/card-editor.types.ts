export type CardSize = {
  label: string;
  width: number;
  height: number;
  orientation: string;
};

export type CardEditorProfile = 'generic' | 'id-card';

export type CardEditorProps = {
  cardSizes: CardSize[];
  fonts?: string[];
  displayScale?: number | null;
  profile?: CardEditorProfile;
  initialTemplate?: string | null;
  onSave: (json: string) => void;
};

export type TemplateJson = {
  version: 2;
  media: CardSize;
  dpi: number;
  objects: object[];
};

export type CardEditorFieldType = 'text' | 'image-placeholder' | 'image-fixed';

export type ProfileCopy = {
  canvasHeader: string;
  cardSizePlaceholder: string;
  clearButton: string;
  templateHeader: string;
  importButton: string;
  saveButton: string;
  exportButton: string;
  addHeader: string;
  addPlaceholderButton: string;
  exportFilename: string;
};
