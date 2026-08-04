import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../shared/models/product.model';
import { CartService } from '../../../core/services/cart.service';
import { ResolveImageUrlPipe } from '../../../shared/pipes/resolve-image-url.pipe';
import { ImageLightboxComponent } from '../../../shared/components/image-lightbox/image-lightbox';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    CommonModule, RouterLink, MatCardModule, MatButtonModule,
    MatProgressSpinnerModule, MatIconModule, ResolveImageUrlPipe
  ],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private dialog = inject(MatDialog);

  product = signal<Product | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  selectedImageIndex = signal(0);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {
      this.error.set('Invalid product ID.');
      this.loading.set(false);
      return;
    }

    this.productService.getProduct(id).subscribe({
      next: (data) => {
        this.product.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Product not found.');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  addToCart(product: Product): void {
    this.cartService.addItem(product);
  }

  selectImage(index: number): void {
    this.selectedImageIndex.set(index);
  }

  openImage(product: Product, startIndex: number): void {
    if (product.images.length === 0) return;

    this.dialog.open(ImageLightboxComponent, {
      data: { imageUrls: product.images.map(i => i.imageUrl), alt: product.name, startIndex },
      panelClass: 'image-lightbox-panel',
      maxWidth: '95vw',
      maxHeight: '95vh'
    });
  }
}