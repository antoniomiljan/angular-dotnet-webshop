import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../../shared/models/product.model';
import { ProductImage } from '../../shared/models/product-image.model';
import { ProductSpec } from '../../shared/models/product-spec.model';
import { environment } from '../../../environments/environment';

type ProductPayload = Omit<Product, 'id' | 'categoryName' | 'images' | 'specs'>;

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/products`;

  getProducts(categoryId?: number, search?: string): Observable<Product[]> {
    let params: Record<string, string> = {};
    if (categoryId) params['categoryId'] = categoryId.toString();
    if (search) params['search'] = search;

    return this.http.get<Product[]>(this.baseUrl, { params });
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  createProduct(product: ProductPayload): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, product);
  }

  updateProduct(id: number, product: ProductPayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, product);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addImage(productId: number, imageUrl: string): Observable<ProductImage> {
    return this.http.post<ProductImage>(`${this.baseUrl}/${productId}/images`, { imageUrl });
  }

  removeImage(productId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${productId}/images/${imageId}`);
  }

  reorderImages(productId: number, imageIds: number[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${productId}/images/reorder`, { imageIds });
  }

  addSpec(productId: number, label: string, value: string): Observable<ProductSpec> {
    return this.http.post<ProductSpec>(`${this.baseUrl}/${productId}/specs`, { label, value });
  }

  updateSpec(productId: number, specId: number, label: string, value: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${productId}/specs/${specId}`, { label, value });
  }

  removeSpec(productId: number, specId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${productId}/specs/${specId}`);
  }

  reorderSpecs(productId: number, specIds: number[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${productId}/specs/reorder`, { specIds });
  }
}
