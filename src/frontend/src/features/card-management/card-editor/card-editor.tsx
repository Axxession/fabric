import { forwardRef, useEffect, useImperativeHandle, useMemo, useRef, useState, type ReactNode } from 'react';
import { Canvas, FabricImage, FabricObject, Rect, Textbox } from 'fabric';
import { Download, Eraser, ImagePlus, Save, Type } from 'lucide-react';

import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';

import type { CardEditorFieldType, CardEditorProps, CardSize, TemplateJson } from './card-editor.types';
import { buildTemplateJson, computeCanvasScale, DEFAULT_CANVAS_HEIGHT, DEFAULT_CANVAS_WIDTH, EXTRA_PROPS, formatCardSizeOption, getProfileCopy, mmToPx, resolveFonts, resolveTemplateMedia } from './card-editor.utils';

type CardEditorObject = FabricObject & {
  dataField?: string;
  fieldType?: CardEditorFieldType;
  isBackground?: boolean;
};

type MenuItem = {
  label?: string;
  action?: () => void;
  children?: MenuItem[];
  separator?: boolean;
};

type MenuState = {
  open: boolean;
  x: number;
  y: number;
};

declare global {
  interface Window {
    __fabricCardEditorPatched?: boolean;
  }
}

export type CardEditorHandle = {
  serialize: () => string;
};

export const CardEditor = forwardRef<CardEditorHandle, CardEditorProps>(function CardEditor({ cardSizes, fonts, displayScale, profile = 'generic', initialTemplate, onSave }, ref) {
  const canvasElementRef = useRef<HTMLCanvasElement | null>(null);
  const contentAreaRef = useRef<HTMLDivElement | null>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);
  const templateInputRef = useRef<HTMLInputElement | null>(null);
  const imageInputRef = useRef<HTMLInputElement | null>(null);
  const fabricCanvasRef = useRef<Canvas | null>(null);
  const resizeObserverRef = useRef<ResizeObserver | null>(null);
  const pendingFieldSubmitRef = useRef<((value: string) => void) | null>(null);
  const pendingDialogObjectRef = useRef<CardEditorObject | null>(null);

  const [selectedCardSize, setSelectedCardSize] = useState<CardSize | null>(null);
  const [selectedObject, setSelectedObject] = useState<CardEditorObject | null>(null);
  const [canvasScale, setCanvasScale] = useState(displayScale ?? 1);
  const [contextMenuItems, setContextMenuItems] = useState<MenuItem[]>([]);
  const [contextMenu, setContextMenu] = useState<MenuState>({ open: false, x: 0, y: 0 });
  const [fontSizeDialogOpen, setFontSizeDialogOpen] = useState(false);
  const [textColorDialogOpen, setTextColorDialogOpen] = useState(false);
  const [textBackgroundDialogOpen, setTextBackgroundDialogOpen] = useState(false);
  const [fieldNameDialogOpen, setFieldNameDialogOpen] = useState(false);
  const [pendingFontSize, setPendingFontSize] = useState(24);
  const [pendingColor, setPendingColor] = useState('#000000');
  const [pendingTextBackgroundColor, setPendingTextBackgroundColor] = useState('#ffffff');
  const [pendingFieldName, setPendingFieldName] = useState('');
  const [openSubmenuLabel, setOpenSubmenuLabel] = useState<string | null>(null);

  const copy = useMemo(() => getProfileCopy(profile), [profile]);
  const resolvedFonts = useMemo(() => resolveFonts(fonts), [fonts]);
  const defaultCanvasWidth = selectedCardSize ? mmToPx(selectedCardSize.width) : DEFAULT_CANVAS_WIDTH;
  const defaultCanvasHeight = selectedCardSize ? mmToPx(selectedCardSize.height) : DEFAULT_CANVAS_HEIGHT;

  useImperativeHandle(ref, () => ({
    serialize: serializeCanvas,
  }));

  useEffect(() => {
    patchFabricSerialization();

    if (!canvasElementRef.current) {
      return undefined;
    }

    const canvas = new Canvas(canvasElementRef.current, {
      width: defaultCanvasWidth,
      height: defaultCanvasHeight,
      backgroundColor: '#ffffff',
      preserveObjectStacking: true,
    });

    fabricCanvasRef.current = canvas;
    bindCanvasEvents(canvas);

    const handleContextMenu = (event: MouseEvent) => {
      event.preventDefault();

      const target = findTargetFromEvent(canvas, event);
      if (target) {
        canvas.setActiveObject(target, event);
        canvas.requestRenderAll();
        setSelectedObject(target);
        setContextMenuItems(buildContextMenuItems(target));
      } else {
        const activeObject = toCardEditorObject(canvas.getActiveObject());
        if (activeObject) {
          setSelectedObject(activeObject);
          setContextMenuItems(buildContextMenuItems(activeObject));
        } else {
          setSelectedObject(null);
          setContextMenuItems([]);
        }
      }

      setContextMenu({ open: true, x: event.clientX, y: event.clientY });
      setOpenSubmenuLabel(null);
    };

    canvas.upperCanvasEl.addEventListener('contextmenu', handleContextMenu);
    queueCanvasMeasurement(canvas);

    return () => {
      canvas.upperCanvasEl.removeEventListener('contextmenu', handleContextMenu);
      resizeObserverRef.current?.disconnect();
      resizeObserverRef.current = null;
      canvas.dispose();
      fabricCanvasRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (displayScale == null) {
      recalcScale();
      return;
    }

    resizeObserverRef.current?.disconnect();
    resizeObserverRef.current = null;
    setCanvasScale(displayScale);
    queueCanvasMeasurement();
  }, [displayScale]);

  useEffect(() => {
    if (displayScale != null || !contentAreaRef.current) {
      return undefined;
    }

    const observer = new ResizeObserver(() => {
      recalcScale();
      queueCanvasMeasurement();
    });

    observer.observe(contentAreaRef.current);
    resizeObserverRef.current = observer;
    recalcScale();

    return () => {
      observer.disconnect();
      if (resizeObserverRef.current === observer) {
        resizeObserverRef.current = null;
      }
    };
  }, [displayScale, selectedCardSize]);

  useEffect(() => {
    if (!initialTemplate || !fabricCanvasRef.current) {
      return;
    }

    void loadTemplateFromString(initialTemplate);
  }, [initialTemplate]);

  useEffect(() => {
    if (!contextMenu.open) {
      return undefined;
    }

    const closeMenu = () => {
      setContextMenu((current) => ({ ...current, open: false }));
      setOpenSubmenuLabel(null);
    };

    const closeMenuOnOutsidePointerDown = (event: PointerEvent) => {
      const menuElement = contextMenuRef.current;
      if (menuElement && event.target instanceof Node && menuElement.contains(event.target)) {
        return;
      }

      closeMenu();
    };

    const closeMenuOnOutsideContext = (event: MouseEvent) => {
      const menuElement = contextMenuRef.current;
      if (menuElement && event.target instanceof Node && menuElement.contains(event.target)) {
        return;
      }

      closeMenu();
    };

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeMenu();
      }
    };

    window.addEventListener('pointerdown', closeMenuOnOutsidePointerDown);
    window.addEventListener('contextmenu', closeMenuOnOutsideContext);
    window.addEventListener('keydown', closeOnEscape);
    return () => {
      window.removeEventListener('pointerdown', closeMenuOnOutsidePointerDown);
      window.removeEventListener('contextmenu', closeMenuOnOutsideContext);
      window.removeEventListener('keydown', closeOnEscape);
    };
  }, [contextMenu.open]);

  function bindCanvasEvents(canvas: Canvas) {
    canvas.on('selection:created', () => syncSelectionFromCanvas(canvas));
    canvas.on('selection:updated', () => syncSelectionFromCanvas(canvas));
    canvas.on('selection:cleared', () => {
      setSelectedObject(null);
      setContextMenuItems([]);
      setContextMenu((current) => ({ ...current, open: false }));
    });
    canvas.on('object:moving', ({ target }) => {
      const object = toCardEditorObject(target);
      if (!object) {
        return;
      }

      const boundingRect = object.getBoundingRect();
      object.set({
        left: Math.min(Math.max(object.left ?? 0, 0), canvas.getWidth() - boundingRect.width),
        top: Math.min(Math.max(object.top ?? 0, 0), canvas.getHeight() - boundingRect.height),
      });
    });
    canvas.on('object:scaling', ({ target }) => {
      const object = toCardEditorObject(target);
      if (!object || typeof object.width !== 'number' || typeof object.height !== 'number') {
        return;
      }

      const nextWidth = object.width * (object.scaleX ?? 1);
      const nextHeight = object.height * (object.scaleY ?? 1);
      if (nextWidth > canvas.getWidth() && object.width > 0) {
        object.scaleX = canvas.getWidth() / object.width;
      }
      if (nextHeight > canvas.getHeight() && object.height > 0) {
        object.scaleY = canvas.getHeight() / object.height;
      }
    });
    canvas.on('object:modified', ({ target }) => {
      if (!(target instanceof Textbox)) {
        return;
      }

      target.set({ width: (target.width ?? 0) * (target.scaleX ?? 1), scaleX: 1 });
      target.initDimensions();
      canvas.requestRenderAll();
    });
  }

  function syncSelectionFromCanvas(canvas: Canvas) {
    const activeObject = toCardEditorObject(canvas.getActiveObject());
    setSelectedObject(activeObject);
    setContextMenuItems(activeObject ? buildContextMenuItems(activeObject) : []);
  }

  function recalcScale() {
    if (displayScale != null) {
      setCanvasScale(displayScale);
      return;
    }

    const canvas = fabricCanvasRef.current;
    const contentArea = contentAreaRef.current;
    if (!canvas || !contentArea) {
      return;
    }

    setCanvasScale(computeCanvasScale(canvas.getWidth(), canvas.getHeight(), contentArea.clientWidth, contentArea.clientHeight));
  }

  function queueCanvasMeasurement(canvas = fabricCanvasRef.current) {
    if (!canvas) {
      return;
    }

    requestAnimationFrame(() => {
      canvas.calcOffset();
      recalcScale();
    });
  }

  function buildContextMenuItems(object: CardEditorObject): MenuItem[] {
    const layeringItems: MenuItem[] = [
      { label: 'Bring Forward', action: () => { fabricCanvasRef.current?.bringObjectForward(object); fabricCanvasRef.current?.requestRenderAll(); } },
      { label: 'Bring to Front', action: () => { fabricCanvasRef.current?.bringObjectToFront(object); fabricCanvasRef.current?.requestRenderAll(); } },
      { label: 'Send Backward', action: () => { fabricCanvasRef.current?.sendObjectBackwards(object); fabricCanvasRef.current?.requestRenderAll(); } },
      { label: 'Send to Back', action: () => { fabricCanvasRef.current?.sendObjectToBack(object); fabricCanvasRef.current?.requestRenderAll(); } },
    ];
    const deleteItem: MenuItem = { label: 'Delete', action: () => deleteObject(object) };

    if (object.fieldType === 'text' && object instanceof Textbox) {
      return [
        { label: 'Bold', action: () => toggleTextboxFlag(object, 'fontWeight', object.fontWeight === 'bold' ? 'normal' : 'bold') },
        { label: 'Italic', action: () => toggleTextboxFlag(object, 'fontStyle', object.fontStyle === 'italic' ? 'normal' : 'italic') },
        { label: 'Underline', action: () => toggleTextboxFlag(object, 'underline', !object.underline) },
        { label: 'Font', children: resolvedFonts.map((font) => ({ label: font, action: () => setTextboxFont(object, font) })) },
        { label: 'Font Size...', action: () => openFontSizeDialog(object) },
        { label: 'Text Color...', action: () => openTextColorDialog(object) },
        { label: 'Text Background...', action: () => openTextBackgroundDialog(object) },
        { separator: true },
        ...layeringItems,
        { separator: true },
        deleteItem,
      ];
    }

    if (object.fieldType === 'image-placeholder') {
      return [
        { label: 'Rename Field...', action: () => openRenameFieldDialog(object) },
        { separator: true },
        ...layeringItems,
        { separator: true },
        deleteItem,
      ];
    }

    if (object.fieldType === 'image-fixed') {
      return [
        { label: 'Set as Background', action: () => setImageAsBackground(object) },
        { separator: true },
        ...layeringItems,
        { separator: true },
        deleteItem,
      ];
    }

    return [...layeringItems, { separator: true }, deleteItem];
  }

  function applyCanvasSize(nextCardSize: CardSize) {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    canvas.clear();
    canvas.setDimensions({ width: mmToPx(nextCardSize.width), height: mmToPx(nextCardSize.height) });
    canvas.backgroundColor = '#ffffff';
    canvas.requestRenderAll();
    setSelectedObject(null);
    setContextMenuItems([]);
    setContextMenu((current) => ({ ...current, open: false }));
    queueCanvasMeasurement(canvas);
  }

  function handleCardSizeChange(nextCardSize: CardSize) {
    const canvas = fabricCanvasRef.current;
    const hasObjects = Boolean(canvas && canvas.getObjects().length > 0);
    if (hasObjects && !window.confirm('Changing the card size will clear the canvas. Continue?')) {
      return;
    }

    setSelectedCardSize(nextCardSize);
    if (canvas) {
      applyCanvasSize(nextCardSize);
    }
  }

  function clearCanvas() {
    const canvas = fabricCanvasRef.current;
    if (!canvas || canvas.getObjects().length === 0) {
      return;
    }

    if (!window.confirm('This will remove all objects from the canvas. Continue?')) {
      return;
    }

    canvas.clear();
    canvas.backgroundColor = '#ffffff';
    canvas.requestRenderAll();
    setSelectedObject(null);
    setContextMenuItems([]);
  }

  function addText() {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    const textbox = new Textbox('Text', {
      left: 100,
      top: 100,
      originX: 'left',
      originY: 'top',
      fontSize: 24,
      fontFamily: resolvedFonts[0],
      fill: '#000000',
    }) as CardEditorObject & Textbox;

    textbox.fieldType = 'text';
    applyTextboxControls(textbox);
    canvas.add(textbox);
    canvas.setActiveObject(textbox);
    canvas.requestRenderAll();
    syncSelectionFromCanvas(canvas);
  }

  function addImage() {
    imageInputRef.current?.click();
  }

  function addImagePlaceholder() {
    setPendingFieldName('');
    pendingFieldSubmitRef.current = (value) => {
      const canvas = fabricCanvasRef.current;
      if (!canvas) {
        return;
      }

      const placeholder = new Rect({
        left: 100,
        top: 100,
        width: 140,
        height: 160,
        fill: 'rgba(100,149,237,0.15)',
        stroke: '#6495ed',
        strokeDashArray: [4, 4],
        strokeWidth: 2,
        evented: true,
        selectable: true,
        hoverCursor: 'move',
      }) as CardEditorObject;

      placeholder.fieldType = 'image-placeholder';
      placeholder.dataField = value;
      canvas.add(placeholder);
      canvas.setActiveObject(placeholder);
      canvas.requestRenderAll();
      syncSelectionFromCanvas(canvas);
    };
    setFieldNameDialogOpen(true);
  }

  async function handleImageSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    const dataUrl = await fileToDataUrl(file);
    const image = await FabricImage.fromURL(dataUrl, undefined, {
      left: 100,
      top: 100,
      originX: 'left',
      originY: 'top',
    });
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    const object = image as CardEditorObject;
    object.fieldType = 'image-fixed';
    canvas.add(object);
    canvas.setActiveObject(object);
    canvas.requestRenderAll();
    syncSelectionFromCanvas(canvas);
  }

  function triggerTemplateImport() {
    const canvas = fabricCanvasRef.current;
    if (canvas && canvas.getObjects().length > 0 && !window.confirm('Importing a template will replace the current canvas. Continue?')) {
      return;
    }

    if (templateInputRef.current) {
      templateInputRef.current.value = '';
      templateInputRef.current.click();
    }
  }

  async function handleTemplateSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    try {
      await loadTemplateFromString(await file.text());
    } catch {
      // Keep Angular behavior: ignore malformed JSON silently.
    }
  }

  async function loadTemplateFromString(value: string) {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    let template: TemplateJson;
    try {
      template = JSON.parse(value) as TemplateJson;
    } catch {
      return;
    }

    const media = resolveTemplateMedia(template, cardSizes, selectedCardSize);
    setSelectedCardSize(media);
    canvas.clear();
    canvas.setDimensions({ width: mmToPx(media.width), height: mmToPx(media.height) });
    canvas.backgroundColor = '#ffffff';
    await canvas.loadFromJSON({ version: '7.0.0', objects: template.objects ?? [] });
    canvas.getObjects().forEach((object) => {
      const cardObject = toCardEditorObject(object);
      if (!cardObject) {
        return;
      }

      if (cardObject.fieldType === 'text' && object instanceof Textbox) {
        applyTextboxControls(object);
      }
      if (cardObject.fieldType === 'image-placeholder') {
        cardObject.set({ evented: true, selectable: true, hoverCursor: 'move' });
      }
      if (cardObject.isBackground === true) {
        cardObject.set({ selectable: false, evented: false });
      }
    });
    canvas.calcOffset();
    canvas.requestRenderAll();
    recalcScale();
    syncSelectionFromCanvas(canvas);
    queueCanvasMeasurement(canvas);
  }

  function serializeCanvas() {
    const canvas = fabricCanvasRef.current;
    if (!canvas || !selectedCardSize) {
      return '';
    }

    canvas.getObjects().forEach((object) => {
      const cardObject = toCardEditorObject(object);
      if (!cardObject || cardObject.fieldType === 'image-fixed') {
        return;
      }

      const width = typeof cardObject.width === 'number' ? cardObject.width : undefined;
      const height = typeof cardObject.height === 'number' ? cardObject.height : undefined;
      const scaleX = cardObject.scaleX ?? 1;
      const scaleY = cardObject.scaleY ?? 1;

      cardObject.set({
        width: width == null ? cardObject.width : width * scaleX,
        height: height == null ? cardObject.height : height * scaleY,
        scaleX: 1,
        scaleY: 1,
      });
      if (cardObject instanceof Textbox) {
        cardObject.initDimensions();
      }
    });

    canvas.requestRenderAll();
    const serializedCanvas = canvas.toObject([...EXTRA_PROPS]);
    return JSON.stringify(buildTemplateJson(selectedCardSize, serializedCanvas.objects ?? []));
  }

  function saveTemplate() {
    onSave(serializeCanvas());
  }

  function exportTemplate() {
    const json = serializeCanvas();
    if (!json) {
      return;
    }

    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = copy.exportFilename;
    link.click();
    URL.revokeObjectURL(url);
  }

  function toggleTextboxFlag<TKey extends keyof Textbox>(object: Textbox, key: TKey, value: Textbox[TKey]) {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    object.set(key, value);
    canvas.requestRenderAll();
    setContextMenuItems(buildContextMenuItems(object));
  }

  function setTextboxFont(object: Textbox, fontFamily: string) {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    object.set('fontFamily', fontFamily);
    object.initDimensions();
    canvas.requestRenderAll();
  }

  function openFontSizeDialog(object: Textbox) {
    pendingDialogObjectRef.current = object;
    setPendingFontSize(typeof object.fontSize === 'number' ? object.fontSize : 24);
    setFontSizeDialogOpen(true);
  }

  function openTextColorDialog(object: Textbox) {
    pendingDialogObjectRef.current = object;
    setPendingColor(typeof object.fill === 'string' ? object.fill : '#000000');
    setTextColorDialogOpen(true);
  }

  function openTextBackgroundDialog(object: Textbox) {
    pendingDialogObjectRef.current = object;
    setPendingTextBackgroundColor(object.textBackgroundColor || '#ffffff');
    setTextBackgroundDialogOpen(true);
  }

  function openRenameFieldDialog(object: CardEditorObject) {
    if (object.fieldType !== 'image-placeholder') {
      return;
    }

    pendingDialogObjectRef.current = object;
    setPendingFieldName(object.dataField ?? '');
    pendingFieldSubmitRef.current = (value) => {
      const placeholder = pendingDialogObjectRef.current;
      if (!placeholder) {
        return;
      }

      placeholder.dataField = value;
      fabricCanvasRef.current?.requestRenderAll();
    };
    setFieldNameDialogOpen(true);
  }

  function confirmFontSize() {
    const object = pendingDialogObjectRef.current;
    const canvas = fabricCanvasRef.current;
    if (!(object instanceof Textbox) || !canvas) {
      return;
    }

    object.set('fontSize', pendingFontSize);
    object.initDimensions();
    canvas.requestRenderAll();
    setFontSizeDialogOpen(false);
    pendingDialogObjectRef.current = null;
  }

  function confirmTextColor() {
    const object = pendingDialogObjectRef.current;
    const canvas = fabricCanvasRef.current;
    if (!(object instanceof Textbox) || !canvas) {
      return;
    }

    object.set('fill', pendingColor);
    canvas.requestRenderAll();
    setTextColorDialogOpen(false);
    pendingDialogObjectRef.current = null;
  }

  function confirmTextBackgroundColor() {
    const object = pendingDialogObjectRef.current;
    const canvas = fabricCanvasRef.current;
    if (!(object instanceof Textbox) || !canvas) {
      return;
    }

    object.set('textBackgroundColor', pendingTextBackgroundColor);
    canvas.requestRenderAll();
    setTextBackgroundDialogOpen(false);
    pendingDialogObjectRef.current = null;
  }

  function confirmFieldName() {
    const trimmedValue = pendingFieldName.trim();
    if (!trimmedValue) {
      return;
    }

    setFieldNameDialogOpen(false);
    pendingFieldSubmitRef.current?.(trimmedValue);
    pendingFieldSubmitRef.current = null;
    pendingDialogObjectRef.current = null;
  }

  function cancelFieldNameDialog() {
    setFieldNameDialogOpen(false);
    pendingFieldSubmitRef.current = null;
    pendingDialogObjectRef.current = null;
  }

  function setImageAsBackground(object: CardEditorObject) {
    const canvas = fabricCanvasRef.current;
    if (object.fieldType !== 'image-fixed' || !canvas) {
      return;
    }

    canvas.sendObjectToBack(object);
    object.set({ selectable: false, evented: false });
    object.isBackground = true;
    canvas.discardActiveObject();
    canvas.requestRenderAll();
    setSelectedObject(null);
    setContextMenuItems([]);
  }

  function deleteObject(object: CardEditorObject) {
    const canvas = fabricCanvasRef.current;
    if (!canvas) {
      return;
    }

    canvas.remove(object);
    canvas.discardActiveObject();
    canvas.requestRenderAll();
    setSelectedObject(null);
    setContextMenuItems([]);
    setContextMenu((current) => ({ ...current, open: false }));
  }

  return (
    <div className="flex min-h-[44rem] min-w-0 overflow-hidden rounded-structural border border-border bg-content">
      <div className="w-full max-w-[320px] min-w-[220px] shrink-0 border-r border-border bg-content p-4">
        <div className="grid gap-4">
          <EditorSection title={copy.canvasHeader}>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Card Size</span>
              <select
                className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary"
                value={selectedCardSize ? cardSizes.findIndex((cardSize) => cardSize.label === selectedCardSize.label && cardSize.width === selectedCardSize.width && cardSize.height === selectedCardSize.height && cardSize.orientation === selectedCardSize.orientation) : ''}
                onChange={(event) => {
                  const nextCardSize = cardSizes[Number(event.target.value)];
                  if (nextCardSize) {
                    handleCardSizeChange(nextCardSize);
                  }
                }}
              >
                <option value="">{copy.cardSizePlaceholder}</option>
                {cardSizes.map((cardSize, index) => (
                  <option key={`${cardSize.label}-${cardSize.orientation}-${cardSize.width}-${cardSize.height}`} value={index}>
                    {formatCardSizeOption(cardSize)}
                  </option>
                ))}
              </select>
            </label>
            <Button type="button" variant="outline" className="w-full justify-start" onClick={clearCanvas}>
              <Eraser className="size-4" aria-hidden="true" />
              {copy.clearButton}
            </Button>
          </EditorSection>

          <EditorSection title={copy.templateHeader}>
            <div className="grid gap-2">
              <Button type="button" variant="outline" className="w-full justify-start" onClick={triggerTemplateImport}>{copy.importButton}</Button>
              <Button type="button" variant="outline" className="w-full justify-start" onClick={saveTemplate}>
                <Save className="size-4" aria-hidden="true" />
                {copy.saveButton}
              </Button>
              <Button type="button" variant="outline" className="w-full justify-start" onClick={exportTemplate}>
                <Download className="size-4" aria-hidden="true" />
                {copy.exportButton}
              </Button>
            </div>
          </EditorSection>

          <EditorSection title={copy.addHeader}>
            <div className="grid gap-2">
              <Button type="button" variant="outline" className="w-full justify-start" onClick={addText}>
                <Type className="size-4" aria-hidden="true" />
                Add Text
              </Button>
              <Button type="button" variant="outline" className="w-full justify-start" onClick={addImage}>
                <ImagePlus className="size-4" aria-hidden="true" />
                Add Image
              </Button>
              <Button type="button" variant="outline" className="w-full justify-start" onClick={addImagePlaceholder}>{copy.addPlaceholderButton}</Button>
            </div>
          </EditorSection>
        </div>
      </div>

      <div ref={contentAreaRef} className="relative flex min-h-[44rem] flex-1 items-center justify-center overflow-auto bg-background p-4">
        <div className="shrink-0 rounded-structural border border-border bg-white shadow-sm" style={{ width: defaultCanvasWidth * canvasScale, height: defaultCanvasHeight * canvasScale }}>
          <div className="overflow-hidden" style={{ width: defaultCanvasWidth, height: defaultCanvasHeight, transform: `scale(${canvasScale})`, transformOrigin: 'top left' }}>
            <canvas ref={canvasElementRef} />
          </div>
        </div>

        {contextMenu.open && contextMenuItems.length > 0 ? (
          <div ref={contextMenuRef} className="fixed z-50 min-w-52 overflow-visible rounded-interactive border border-border bg-content py-1 shadow-md" style={{ left: contextMenu.x, top: contextMenu.y }}>
            {contextMenuItems.map((item, index) => {
              if (item.separator) {
                return <div key={`separator-${index}`} className="my-1 border-t border-border" />;
              }

              if (!item.label) {
                return null;
              }

              const hasChildren = Boolean(item.children && item.children.length > 0);

              return (
                <div
                  key={`${item.label}-${index}`}
                  className="relative"
                  onPointerEnter={() => setOpenSubmenuLabel(hasChildren ? item.label ?? null : null)}
                >
                  <button
                    type="button"
                    className="flex w-full items-center justify-between px-3 py-2 text-left text-[14px] transition hover:bg-hover-blue"
                    onClick={(event) => {
                      event.stopPropagation();
                      if (hasChildren) {
                        setOpenSubmenuLabel(item.label ?? null);
                        return;
                      }

                      item.action?.();
                      setContextMenu((current) => ({ ...current, open: false }));
                    }}
                  >
                    <span>{item.label}</span>
                    {hasChildren ? <span className="text-muted-foreground">&gt;</span> : null}
                  </button>

                  {hasChildren && openSubmenuLabel === item.label ? (
                    <div className="absolute top-0 left-full ml-1 min-w-48 rounded-interactive border border-border bg-content py-1 shadow-md">
                      {item.children?.map((child) => (
                        <button
                          key={child.label}
                          type="button"
                          className="block w-full px-3 py-2 text-left text-[14px] transition hover:bg-hover-blue"
                          onClick={(event) => {
                            event.stopPropagation();
                            child.action?.();
                            setContextMenu((current) => ({ ...current, open: false }));
                          }}
                        >
                          {child.label}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        ) : null}

        <input ref={templateInputRef} type="file" accept=".json" className="hidden" onChange={(event) => { void handleTemplateSelected(event); }} />
        <input ref={imageInputRef} type="file" accept="image/*" className="hidden" onChange={(event) => { void handleImageSelected(event); }} />
      </div>

      <Dialog open={fieldNameDialogOpen} title="Field Name" onCancel={cancelFieldNameDialog} onConfirm={confirmFieldName} confirmLabel="Confirm">
        <div className="grid gap-2">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>Field name</span>
            <Input value={pendingFieldName} onChange={(event) => setPendingFieldName(event.target.value)} autoFocus />
          </label>
        </div>
      </Dialog>

      <Dialog open={fontSizeDialogOpen} title="Font Size" onCancel={() => setFontSizeDialogOpen(false)} onConfirm={confirmFontSize} confirmLabel="Apply">
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Font size</span>
          <Input type="number" min={1} value={String(pendingFontSize)} onChange={(event) => setPendingFontSize(Number(event.target.value) || 24)} autoFocus />
        </label>
      </Dialog>

      <Dialog open={textColorDialogOpen} title="Text Color" onCancel={() => setTextColorDialogOpen(false)} onConfirm={confirmTextColor} confirmLabel="Apply">
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Color</span>
          <input type="color" value={pendingColor} onChange={(event) => setPendingColor(event.target.value)} className="h-10 w-full rounded-interactive border border-border bg-content p-1" autoFocus />
        </label>
      </Dialog>

      <Dialog open={textBackgroundDialogOpen} title="Text Background Color" onCancel={() => setTextBackgroundDialogOpen(false)} onConfirm={confirmTextBackgroundColor} confirmLabel="Apply">
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Background color</span>
          <input type="color" value={pendingTextBackgroundColor} onChange={(event) => setPendingTextBackgroundColor(event.target.value)} className="h-10 w-full rounded-interactive border border-border bg-content p-1" autoFocus />
        </label>
      </Dialog>
    </div>
  );
  function toCardEditorObject(value: FabricObject | undefined | null) {
    return value ? value as CardEditorObject : null;
  }
});

function EditorSection({ title, children }: { readonly title: string; readonly children: ReactNode }) {
  return (
    <details open className="rounded-interactive border border-border bg-background/60">
      <summary className="cursor-pointer px-3 py-2 text-[14px] font-semibold text-foreground">{title}</summary>
      <div className="grid gap-3 border-t border-border p-3">{children}</div>
    </details>
  );
}

function Dialog({ open, title, children, onCancel, onConfirm, confirmLabel }: { readonly open: boolean; readonly title: string; readonly children: ReactNode; readonly onCancel: () => void; readonly onConfirm: () => void; readonly confirmLabel: string }) {
  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onPointerDown={onCancel}>
      <div className="w-full max-w-sm rounded-structural border border-border bg-content p-4 shadow-xl" onPointerDown={(event) => event.stopPropagation()}>
        <h3 className="text-[18px] font-semibold tracking-tight">{title}</h3>
        <div className="mt-4">{children}</div>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
          <Button type="button" onClick={onConfirm}>{confirmLabel}</Button>
        </div>
      </div>
    </div>
  );
}

function patchFabricSerialization() {
  if (window.__fabricCardEditorPatched) {
    return;
  }

  const originalToObject = FabricObject.prototype.toObject;
  FabricObject.prototype.toObject = function patchedToObject(propertiesToInclude?: string[]) {
    const mergedProps = Array.from(new Set([...(propertiesToInclude ?? []), ...EXTRA_PROPS]));
    return originalToObject.call(this, mergedProps);
  };
  window.__fabricCardEditorPatched = true;
}

function applyTextboxControls(textbox: Textbox) {
  textbox.setControlsVisibility({
    ml: true,
    mr: true,
    mtr: true,
    mt: false,
    mb: false,
    tl: false,
    tr: false,
    bl: false,
    br: false,
  });
}

function findTargetFromEvent(canvas: Canvas, event: MouseEvent) {
  const targetInfo = canvas.findTarget(event) as { target?: FabricObject } | FabricObject | undefined;
  if (!targetInfo) {
    return null;
  }

  if ('target' in targetInfo) {
    return targetInfo.target ? targetInfo.target as CardEditorObject : null;
  }

  return targetInfo as CardEditorObject;
}

async function fileToDataUrl(file: File) {
  return await new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(typeof reader.result === 'string' ? reader.result : '');
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}
