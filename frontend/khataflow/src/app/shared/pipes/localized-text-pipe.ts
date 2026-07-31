import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'localizedText', standalone: true, pure: true })
export class LocalizedTextPipe implements PipeTransform {
  transform(en: string | null | undefined, ur: string | null | undefined, lang: string): string {
    if (lang === 'ur' && ur?.trim()) return ur;
    return en ?? '';
  }
}