import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formulaSafe',
  standalone: true
})
export class FormulaSafePipe implements PipeTransform {
  transform(value: string | number | null | undefined): string {
    if (value === null || value === undefined) return '';
    const str = String(value);

    // Spec invariant: Escape leading '=', '+', '-', '@' to prevent spreadsheet formula injection
    if (/^[=+\-@]/.test(str)) {
      return `'${str}`;
    }
    return str;
  }
}
