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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { ImageService } from '../../../core/services/image.service';
import { Product } from '../../../shared/models/product.model';
import { ProductImage } from '../../../shared/models/product-image.model';
import { Category } from '../../../shared/models/category.model';
import { ResolveImageUrlPipe } from '../../../shared/pipes/resolve-image-url.pipe';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule,
    MatTableModule, MatSlideToggleModule, MatProgressSpinnerModule, ResolveImageUrlPipe
  ],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.css'
})
export class AdminProducts implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private imageService = inject(ImageService);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  editingId = signal<number | null>(null);
  error = signal<string | null>(null);
  uploadingImage = signal(false);

  // Images of whichever product is currently open in the edit form. Separate from
  // `form` because images are managed via their own immediate add/remove/reorder
  // calls, not as part of the Save button's payload.
  currentImages = signal<ProductImage[]>([]);

  columns = ['image', 'name', 'price', 'inStock', 'category', 'actions'];

  form = {
    name: '',
    description: '',
    price: 0,
    inStock: true,
    categoryId: 0
  };

  loading = signal(true);

  ngOnInit(): void {
    this.loadProducts();
    this.categoryService.getCategories().subscribe(data => this.categories.set(data));
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getProducts().subscribe({
      next: (data) => {
        this.products.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load products.');
        this.loading.set(false);
      }
    });
  }

  startEdit(product: Product): void {
    this.editingId.set(product.id);
    this.currentImages.set(product.images);
    this.form = {
      name: product.name,
      description: product.description,
      price: product.price,
      inStock: product.inStock,
      categoryId: product.categoryId
    };
  }

  startCreate(): void {
    this.editingId.set(-1);
    this.currentImages.set([]);
    this.form = { name: '', description: '', price: 0, inStock: true, categoryId: this.categories()[0]?.id ?? 0 };
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.currentImages.set([]);
    this.error.set(null);
  }

  // Images can only be attached to a product that already has an id, so this is
  // disabled in the template until a new product has been saved once.
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // clears the picked file so re-selecting the same one still fires (change)

    const productId = this.editingId();
    if (!file || !productId || productId === -1) return;

    this.uploadingImage.set(true);
    this.imageService.upload(file).subscribe({
      next: (uploaded) => {
        this.productService.addImage(productId, uploaded.imageUrl).subscribe({
          next: (image) => {
            this.currentImages.update(images => [...images, image]);
            this.uploadingImage.set(false);
            this.loadProducts();
          },
          error: (err) => {
            this.error.set(err.error ?? 'Failed to attach image.');
            this.uploadingImage.set(false);
          }
        });
      },
      error: (err) => {
        this.error.set(err.error ?? 'Failed to upload image.');
        this.uploadingImage.set(false);
      }
    });
  }

  removeImage(image: ProductImage): void {
    const productId = this.editingId();
    if (!productId || productId === -1) return;
    if (!confirm('Remove this image?')) return;

    this.productService.removeImage(productId, image.id).subscribe({
      next: () => {
        this.currentImages.update(images => images.filter(i => i.id !== image.id));
        this.loadProducts();
      },
      error: () => this.error.set('Failed to remove image.')
    });
  }

  moveImage(index: number, direction: -1 | 1): void {
    const productId = this.editingId();
    const images = [...this.currentImages()];
    const newIndex = index + direction;
    if (!productId || productId === -1 || newIndex < 0 || newIndex >= images.length) return;

    [images[index], images[newIndex]] = [images[newIndex], images[index]];
    this.currentImages.set(images);

    this.productService.reorderImages(productId, images.map(i => i.id)).subscribe({
      next: () => this.loadProducts(),
      error: () => this.error.set('Failed to reorder images.')
    });
  }

  save(): void {
    this.error.set(null);

    if (this.editingId() === -1) {
      this.productService.createProduct(this.form).subscribe({
        next: (created) => {
          // Keeps the form open, now in edit mode for the just-created product,
          // so images can be added immediately instead of re-opening the form.
          this.editingId.set(created.id);
          this.currentImages.set(created.images);
          this.loadProducts();
        },
        error: (err) => this.error.set(err.error ?? 'Failed to create product.')
      });
    } else {
      const id = this.editingId()!;
      this.productService.updateProduct(id, this.form).subscribe({
        next: () => { this.loadProducts(); this.editingId.set(null); },
        error: (err) => this.error.set(err.error ?? 'Failed to update product.')
      });
    }
  }

  toggleInStock(product: Product): void {
    const payload = {
      name: product.name,
      description: product.description,
      price: product.price,
      inStock: !product.inStock,
      categoryId: product.categoryId
    };

    this.productService.updateProduct(product.id, payload).subscribe({
      next: () => this.loadProducts(),
      error: () => this.error.set('Failed to update stock status.')
    });
  }

  deleteProduct(id: number): void {
    if (!confirm('Delete this product?')) return;

    this.productService.deleteProduct(id).subscribe({
      next: () => this.loadProducts(),
      error: () => this.error.set('Failed to delete product.')
    });
  }
}
