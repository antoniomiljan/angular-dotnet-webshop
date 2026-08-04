import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

const API_ORIGIN = environment.apiUrl.replace(/\/api\/?$/, '');

// Product.imageUrl can be a full external URL (any image host) or a path served by
// the API itself (e.g. /images/foo.jpg, from Api/wwwroot). Only the latter needs
// the API's origin prefixed on.
@Pipe({ name: 'resolveImageUrl', standalone: true })
export class ResolveImageUrlPipe implements PipeTransform {
  transform(imageUrl: string | null): string | null {
    if (!imageUrl) return null;
    return imageUrl.startsWith('http') ? imageUrl : `${API_ORIGIN}${imageUrl}`;
  }
}
