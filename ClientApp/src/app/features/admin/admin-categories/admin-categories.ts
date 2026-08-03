import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../shared/models/category.model';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatIconModule, MatTableModule
  ],
  templateUrl: './admin-categories.html',
  styleUrl: './admin-categories.css'
})
export class AdminCategories implements OnInit {
  private categoryService = inject(CategoryService);

  categories = signal<Category[]>([]);
  editingId = signal<number | null>(null);
  error = signal<string | null>(null);

  columns = ['name', 'slug', 'actions'];

  form = { name: '', slug: '' };

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getCategories().subscribe(data => this.categories.set(data));
  }

  startEdit(category: Category): void {
    this.editingId.set(category.id);
    this.form = { name: category.name, slug: category.slug };
  }

  startCreate(): void {
    this.editingId.set(-1);
    this.form = { name: '', slug: '' };
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.error.set(null);
  }

  save(): void {
    this.error.set(null);

    if (this.editingId() === -1) {
      this.categoryService.createCategory(this.form).subscribe({
        next: () => { this.loadCategories(); this.editingId.set(null); },
        error: (err) => this.error.set(err.error ?? 'Failed to create category.')
      });
    } else {
      const id = this.editingId()!;
      this.categoryService.updateCategory(id, this.form).subscribe({
        next: () => { this.loadCategories(); this.editingId.set(null); },
        error: (err) => this.error.set(err.error ?? 'Failed to update category.')
      });
    }
  }

  deleteCategory(id: number): void {
    if (!confirm('Delete this category?')) return;

    this.categoryService.deleteCategory(id).subscribe({
      next: () => this.loadCategories(),
      error: (err) => this.error.set(err.error ?? 'Failed to delete category. It may still have active products.')
    });
  }
}