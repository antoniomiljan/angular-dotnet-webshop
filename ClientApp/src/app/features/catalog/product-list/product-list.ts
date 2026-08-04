import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { Product } from '../../../shared/models/product.model';
import { Category } from '../../../shared/models/category.model';
import { ResolveImageUrlPipe } from '../../../shared/pipes/resolve-image-url.pipe';
import { ImageLightboxComponent } from '../../../shared/components/image-lightbox/image-lightbox';
import { RouterLink } from '@angular/router';

const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule,
    ResolveImageUrlPipe, RouterLink
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductListComponent implements OnInit, OnDestroy {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private dialog = inject(MatDialog);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  search = signal('');
  selectedCategoryId = signal<number | null>(null);

  private searchDebounceTimer?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.categoryService.getCategories().subscribe(data => this.categories.set(data));
    this.loadProducts();
  }

  ngOnDestroy(): void {
    clearTimeout(this.searchDebounceTimer);
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => this.loadProducts(), SEARCH_DEBOUNCE_MS);
  }

  onCategoryChange(categoryId: number | null): void {
    this.selectedCategoryId.set(categoryId);
    this.loadProducts();
  }

  // The card itself has [routerLink] to the detail page - stopping propagation here
  // keeps a click on the image opening the lightbox instead of also navigating away.
  openImage(product: Product, event: Event): void {
    if (product.images.length === 0) return;
    event.stopPropagation();
    event.preventDefault();

    this.dialog.open(ImageLightboxComponent, {
      data: { imageUrls: product.images.map(i => i.imageUrl), alt: product.name, startIndex: 0 },
      panelClass: 'image-lightbox-panel',
      maxWidth: '95vw',
      maxHeight: '95vh'
    });
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getProducts(this.selectedCategoryId() ?? undefined, this.search() || undefined).subscribe({
      next: (data) => {
        this.products.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load products. Is the API running?');
        this.loading.set(false);
        console.error(err);
      }
    });
  }
}
