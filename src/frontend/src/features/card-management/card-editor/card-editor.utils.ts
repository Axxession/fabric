import type { CardEditorProfile, CardSize, ProfileCopy, TemplateJson } from './card-editor.types';

export const CARD_EDITOR_DPI = 300;
export const DEFAULT_CANVAS_WIDTH = 1012;
export const DEFAULT_CANVAS_HEIGHT = 638;
export const EXTRA_PROPS = ['dataField', 'fieldType', 'isBackground', 'scaleX', 'scaleY', 'angle'] as const;
export const FALLBACK_CARD_SIZE: CardSize = { label: 'Default', width: 85.6, height: 54, orientation: 'Landscape' };
export const FALLBACK_FONTS = [
  'Arial',
  'Georgia',
  'Times New Roman',
  'Courier New',
  'Verdana',
  'Trebuchet MS',
  'DejaVu Serif',
  'DejaVu Sans',
] as const;

export function mmToPx(mm: number) {
  return Math.round((mm / 25.4) * CARD_EDITOR_DPI);
}

export function resolveFonts(fonts?: string[]) {
  return fonts && fonts.length > 0 ? fonts : [...FALLBACK_FONTS];
}

export function formatCardSizeOption(cardSize: CardSize) {
  return `${cardSize.label} - ${cardSize.width}x${cardSize.height} mm (${cardSize.orientation})`;
}

export function computeCanvasScale(width: number, height: number, contentWidth: number, contentHeight: number) {
  const containerWidth = Math.max(contentWidth - 32, 1);
  const containerHeight = Math.max(contentHeight - 32, 1);
  const scaleByWidth = containerWidth / width;
  const scaleByHeight = containerHeight / height;
  return Math.min(scaleByWidth, scaleByHeight);
}

export function getProfileCopy(profile: CardEditorProfile = 'generic'): ProfileCopy {
  if (profile === 'id-card') {
    return {
      canvasHeader: 'Card Format',
      cardSizePlaceholder: 'Select CR80 or CR79',
      clearButton: 'Clear Card',
      templateHeader: 'Card Design',
      importButton: 'Import Design',
      saveButton: 'Save Design',
      exportButton: 'Export Design',
      addHeader: 'Design Tools',
      addPlaceholderButton: 'Add Photo Placeholder',
      exportFilename: 'id-card-template.json',
    };
  }

  return {
    canvasHeader: 'Canvas',
    cardSizePlaceholder: 'Select a size',
    clearButton: 'Clear Canvas',
    templateHeader: 'Template',
    importButton: 'Import',
    saveButton: 'Save',
    exportButton: 'Export',
    addHeader: 'Add',
    addPlaceholderButton: 'Add Image Placeholder',
    exportFilename: 'card-template.json',
  };
}

export function resolveTemplateMedia(template: Partial<TemplateJson>, cardSizes: CardSize[], selectedCardSize: CardSize | null) {
  const fallbackSize = selectedCardSize ?? cardSizes[0] ?? FALLBACK_CARD_SIZE;
  const importedMedia = isCardSizeLike(template.media) ? template.media : fallbackSize;
  const matchedCardSize = cardSizes.find((cardSize) => (
    Math.abs(cardSize.width - importedMedia.width) <= 0.5
    && Math.abs(cardSize.height - importedMedia.height) <= 0.5
    && cardSize.orientation === importedMedia.orientation
  ));
  return matchedCardSize ?? importedMedia;
}

export function buildTemplateJson(media: CardSize, objects: object[]): TemplateJson {
  return {
    version: 2,
    media,
    dpi: CARD_EDITOR_DPI,
    objects,
  };
}

function isCardSizeLike(value: unknown): value is CardSize {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Partial<CardSize>;
  return typeof candidate.label === 'string'
    && typeof candidate.width === 'number'
    && typeof candidate.height === 'number'
    && typeof candidate.orientation === 'string';
}
