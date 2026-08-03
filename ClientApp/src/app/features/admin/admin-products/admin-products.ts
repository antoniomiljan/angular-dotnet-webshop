import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { Product } from '../../../shared/models/product.model';
import { Category } from '../../../shared/models/category.model';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, MatTableModule
  ],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.css'
})
export class AdminProducts implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  editingId = signal<number | null>(null);
  error = signal<string | null>(null);

  columns = ['name', 'price', 'stock', 'category', 'actions'];

  form = {
    name: '',
    description: '',
    price: 0,
    stock: 0,
    imageUrl: '',
    categoryId: 0
  };

  ngOnInit(): void {
    this.loadProducts();
    this.categoryService.getCategories().subscribe(data => this.categories.set(data));
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe(data => this.products.set(data));
  }

  startEdit(product: Product): void {
    this.editingId.set(product.id);
    this.form = {
      name: product.name,
      description: product.description,
      price: product.price,
      stock: product.stock,
      imageUrl: product.imageUrl ?? '',
      categoryId: product.categoryId
    };
  }

  startCreate(): void {
    this.editingId.set(-1);
    this.form = { name: '', description: '', price: 0, stock: 0, imageUrl: '', categoryId: this.categories()[0]?.id ?? 0 };
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.error.set(null);
  }

  save(): void {
    this.error.set(null);
    const payload = { ...this.form, imageUrl: this.form.imageUrl || null };

    if (this.editingId() === -1) {
      this.productService.createProduct(payload as any).subscribe({
        next: () => { this.loadProducts(); this.editingId.set(null); },
        error: (err) => this.error.set(err.error ?? 'Failed to create product.')
      });
    } else {
      const id = this.editingId()!;
      this.productService.updateProduct(id, payload as any).subscribe({
        next: () => { this.loadProducts(); this.editingId.set(null); },
        error: (err) => this.error.set(err.error ?? 'Failed to update product.')
      });
    }
  }

  deleteProduct(id: number): void {
    if (!confirm('Delete this product?')) return;

    this.productService.deleteProduct(id).subscribe({
      next: () => this.loadProducts(),
      error: () => this.error.set('Failed to delete product.')
    });
  }
}