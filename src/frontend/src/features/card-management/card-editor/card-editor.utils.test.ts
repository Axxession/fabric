import { describe, expect, it } from 'vitest';

import { buildTemplateJson, computeCanvasScale, formatCardSizeOption, getProfileCopy, mmToPx, resolveFonts, resolveTemplateMedia } from './card-editor.utils';

describe('card-editor utils', () => {
  it('converts millimeters to 300 dpi pixels', () => {
    expect(mmToPx(85.6)).toBe(1011);
    expect(mmToPx(54)).toBe(638);
  });

  it('uses fallback fonts when no fonts provided', () => {
    expect(resolveFonts()).toContain('Arial');
    expect(resolveFonts([])).toContain('Georgia');
    expect(resolveFonts(['Inter'])).toEqual(['Inter']);
  });

  it('formats card size labels for select options', () => {
    expect(formatCardSizeOption({ label: 'CR80', width: 85.6, height: 54, orientation: 'Landscape' })).toBe('CR80 - 85.6x54 mm (Landscape)');
  });

  it('matches imported media by tolerant dimensions and orientation', () => {
    const cardSizes = [
      { label: 'CR80', width: 85.6, height: 54, orientation: 'Landscape' },
      { label: 'CR79', width: 84.1, height: 52.4, orientation: 'Landscape' },
    ];

    expect(resolveTemplateMedia({ media: { label: 'Imported', width: 85.8, height: 54.2, orientation: 'Landscape' }, version: 2, dpi: 300, objects: [] }, cardSizes, null)).toEqual(cardSizes[0]);
    expect(resolveTemplateMedia({ media: { label: 'Portrait', width: 85.6, height: 54, orientation: 'Portrait' }, version: 2, dpi: 300, objects: [] }, cardSizes, null)).toEqual({ label: 'Portrait', width: 85.6, height: 54, orientation: 'Portrait' });
  });

  it('builds template json with expected metadata', () => {
    expect(buildTemplateJson({ label: 'CR80', width: 85.6, height: 54, orientation: 'Landscape' }, [{ type: 'textbox' }])).toEqual({
      version: 2,
      media: { label: 'CR80', width: 85.6, height: 54, orientation: 'Landscape' },
      dpi: 300,
      objects: [{ type: 'textbox' }],
    });
  });

  it('computes scaled fit inside padded container', () => {
    expect(computeCanvasScale(1000, 500, 1032, 532)).toBeCloseTo(1, 5);
    expect(computeCanvasScale(1000, 500, 532, 532)).toBeCloseTo(0.5, 5);
  });

  it('returns profile-specific copy', () => {
    expect(getProfileCopy('generic').exportFilename).toBe('card-template.json');
    expect(getProfileCopy('id-card').addPlaceholderButton).toBe('Add Photo Placeholder');
  });
});
