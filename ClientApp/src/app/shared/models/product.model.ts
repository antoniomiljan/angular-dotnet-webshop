import { ProductImage } from './product-image.model';
import { ProductSpec } from './product-spec.model';

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  inStock: boolean;
  images: ProductImage[];
  specs: ProductSpec[];
  categoryId: number;
  categoryName: string;
}