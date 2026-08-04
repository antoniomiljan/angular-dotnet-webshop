import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { Product } from '../../../shared/models/product.model';
import { Category } from '../../../shared/models/category.model';
import { RouterLink } from '@angular/router';

const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, RouterLink
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductListComponent implements OnInit, OnDestroy {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);

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
