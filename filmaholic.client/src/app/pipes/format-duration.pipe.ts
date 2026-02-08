import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'formatDuration' })
export class FormatDurationPipe implements PipeTransform {
  transform(value: number | null | undefined, unit: 'min' | 'hours' = 'min'): string {
    if (value == null || value < 0 || isNaN(value)) return '0 min';
    let totalMin = unit === 'hours' ? Math.round(value * 60) : Math.round(value);
    if (totalMin === 0) return '0 min';

    const min = totalMin % 60;
    const totalHours = Math.floor(totalMin / 60);
    const hours = totalHours % 24;
    const totalDays = Math.floor(totalHours / 24);
    const days = totalDays % 30;
    const months = Math.floor(totalDays / 30);

    const parts: string[] = [];

    if (months > 0) {
      parts.push(months === 1 ? '1 mês' : `${months} meses`);
      if (days > 0) parts.push(days === 1 ? '1 dia' : `${days} dias`);
    } else if (totalDays > 0) {
      parts.push(totalDays === 1 ? '1 dia' : `${totalDays} dias`);
      if (hours > 0 || min > 0) {
        const hPart = hours > 0 ? `${hours}h` : '';
        const mPart = min > 0 ? `${min} min` : '';
        if (hPart || mPart) parts.push([hPart, mPart].filter(Boolean).join(' '));
      }
    } else if (totalHours > 0) {
      parts.push(`${totalHours}h`);
      if (min > 0) parts.push(`${min} min`);
    } else {
      return min === 1 ? '1 min' : `${min} min`;
    }

    return parts.join(' ');
  }
}
