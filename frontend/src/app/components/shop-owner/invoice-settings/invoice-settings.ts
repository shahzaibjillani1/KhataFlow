import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { InvoiceSettingsService } from '../../../services/invoice-settings';
import {
  InvoiceSettingsRequest,
  InvoiceTemplateStyle,
} from '../../../core/models/invoice-settings-model';

const MAX_LOGO_BYTES = 2 * 1024 * 1024; // 2MB
const ACCEPTED_LOGO_TYPES = ['image/png', 'image/jpeg', 'image/svg+xml'];

@Component({
  selector: 'app-invoice-settings',
  imports: [], // signal-driven form — no FormsModule needed
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-settings.html',
  styleUrl: './invoice-settings.css',
})
export class InvoiceSettings implements OnInit, OnDestroy {
  private readonly invoiceSettingsService = inject(InvoiceSettingsService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly loading = this.invoiceSettingsService.loading;
  readonly saving = this.invoiceSettingsService.saving;

  readonly form = signal<InvoiceSettingsRequest>({
    logoUrl: null,
    primaryColorHex: '#7C3AED',
    accentColorHex: '#F3E8FF',
    footerNote: null,
    showBusinessAddress: true,
    fontFamily: 'Inter',
    style: InvoiceTemplateStyle.Classic,
  });

  readonly previewUrl = signal<string | null>(null);
  readonly logoPreviewUrl = signal<string | null>(null);
  readonly logoUploadSupported = true;
  readonly logoInputMode = signal<'url' | 'file'>('url');
  readonly logoError = signal<string | null>(null);

  readonly safePreviewUrl = computed(() => {
    const url = this.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  readonly styleOptions = [
    { value: InvoiceTemplateStyle.Classic, label: 'Classic' },
    { value: InvoiceTemplateStyle.Modern, label: 'Modern' },
    { value: InvoiceTemplateStyle.Minimal, label: 'Minimal' },
  ];

  private readonly hexPattern = /^#([0-9A-Fa-f]{6})$/;
  readonly isValidHex = computed(() => {
    const f = this.form();
    return this.hexPattern.test(f.primaryColorHex) && this.hexPattern.test(f.accentColorHex);
  });

  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);

  async ngOnInit(): Promise<void> {
    try {
      const loaded = await this.invoiceSettingsService.load();
      this.form.set({
        logoUrl: loaded.logoUrl,
        primaryColorHex: loaded.primaryColorHex,
        accentColorHex: loaded.accentColorHex,
        footerNote: loaded.footerNote,
        showBusinessAddress: loaded.showBusinessAddress,
        fontFamily: loaded.fontFamily,
        style: loaded.style,
      });

      // If a logo URL was already loaded (either a real URL or a saved base64
      // data URL), default the toggle to whichever mode matches it.
      this.logoInputMode.set(loaded.logoUrl?.startsWith('data:') ? 'file' : 'url');
    } catch {
      this.saveError.set('Could not load invoice settings.');
    }
  }

  updateField<K extends keyof InvoiceSettingsRequest>(
    key: K,
    value: InvoiceSettingsRequest[K],
  ): void {
    this.form.update((f) => ({ ...f, [key]: value }));
    this.saveSuccess.set(false);
    this.saveError.set(null);
  }

  onColorInput(event: Event, field: 'primaryColorHex' | 'accentColorHex'): void {
    const value = (event.target as HTMLInputElement).value;
    this.updateField(field, value);
  }

  onTextInput(event: Event, field: 'fontFamily' | 'footerNote'): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement).value;
    this.updateField(field, value);
  }

  onCheckboxChange(event: Event, field: 'showBusinessAddress'): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.updateField(field, checked);
  }

  onStyleChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value) as InvoiceTemplateStyle;
    this.updateField('style', value);
  }

  setLogoInputMode(mode: 'url' | 'file'): void {
    this.logoInputMode.set(mode);
    this.logoError.set(null);
  }

  onLogoUrlInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value.trim();

    // Switching to a pasted URL invalidates any local file preview blob.
    const previous = this.logoPreviewUrl();
    if (previous) {
      URL.revokeObjectURL(previous);
      this.logoPreviewUrl.set(null);
    }

    this.updateField('logoUrl', value || null);
  }

  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.logoError.set(null);

    if (!ACCEPTED_LOGO_TYPES.includes(file.type)) {
      this.logoError.set('Please choose a PNG, JPEG, or SVG image.');
      input.value = '';
      return;
    }

    if (file.size > MAX_LOGO_BYTES) {
      this.logoError.set('Logo must be smaller than 2MB.');
      input.value = '';
      return;
    }

    const previous = this.logoPreviewUrl();
    if (previous) URL.revokeObjectURL(previous);

    const objectUrl = URL.createObjectURL(file);
    this.logoPreviewUrl.set(objectUrl); // fast local preview

    const reader = new FileReader();
    reader.onload = () => {
      const base64 = reader.result as string; // "data:image/png;base64,...."
      this.updateField('logoUrl', base64); // this is what actually gets saved
    };
    reader.onerror = () => {
      this.logoError.set('Could not read the selected file.');
    };
    reader.readAsDataURL(file);

    input.value = '';
  }

  async save(): Promise<void> {
    if (!this.isValidHex() || this.saving()) return;

    this.saveError.set(null);
    this.saveSuccess.set(false);
    try {
      await this.invoiceSettingsService.update(this.form());
      this.saveSuccess.set(true);
    } catch {
      this.saveError.set('Failed to save changes. Please try again.');
    }
  }

  async runPreview(): Promise<void> {
    if (!this.isValidHex()) return;

    this.saveError.set(null);
    try {
      const blob = await this.invoiceSettingsService.preview(this.form());
      const url = URL.createObjectURL(blob);

      const previous = this.previewUrl();
      if (previous) URL.revokeObjectURL(previous);

      this.previewUrl.set(url);
    } catch {
      this.saveError.set('Preview is not available yet.');
    }
  }

  ngOnDestroy(): void {
    const previewUrl = this.previewUrl();
    if (previewUrl) URL.revokeObjectURL(previewUrl);

    const logoUrl = this.logoPreviewUrl();
    if (logoUrl) URL.revokeObjectURL(logoUrl);
  }
}
