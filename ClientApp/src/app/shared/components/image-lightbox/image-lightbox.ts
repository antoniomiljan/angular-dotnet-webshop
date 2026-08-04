import { Component, HostListener, Inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ResolveImageUrlPipe } from '../../pipes/resolve-image-url.pipe';

export interface ImageLightboxData {
  imageUrls: string[];
  alt: string;
  startIndex: number;
}

@Component({
  selector: 'app-image-lightbox',
  standalone: true,
  imports: [MatDialogModule, MatIconModule, MatButtonModule, ResolveImageUrlPipe],
  templateUrl: './image-lightbox.html',
  styleUrl: './image-lightbox.css'
})
export class ImageLightboxComponent {
  currentIndex = signal(0);

  constructor(@Inject(MAT_DIALOG_DATA) public data: ImageLightboxData) {
    this.currentIndex.set(data.startIndex);
  }

  next(): void {
    this.currentIndex.update(i => (i + 1) % this.data.imageUrls.length);
  }

  prev(): void {
    this.currentIndex.update(i => (i - 1 + this.data.imageUrls.length) % this.data.imageUrls.length);
  }

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight') this.next();
    if (event.key === 'ArrowLeft') this.prev();
  }
}
