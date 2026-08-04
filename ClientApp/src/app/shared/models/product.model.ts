import { ProductImage } from './product-image.model';

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  inStock: boolean;
  images: ProductImage[];
  categoryId: number;
  categoryName: string;
}