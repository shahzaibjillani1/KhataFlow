export enum InvoiceTemplateStyle {
  Classic = 0,
  Modern = 1,
  Minimal = 2,
}

export interface InvoiceSettingsResponse {
  id: string;
  logoUrl: string | null;
  primaryColorHex: string;
  accentColorHex: string;
  footerNote: string | null;
  showBusinessAddress: boolean;
  fontFamily: string;
  style: InvoiceTemplateStyle;
}

export interface InvoiceSettingsRequest {
  logoUrl: string | null;
  primaryColorHex: string;
  accentColorHex: string;
  footerNote: string | null;
  showBusinessAddress: boolean;
  fontFamily: string;
  style: InvoiceTemplateStyle;
}