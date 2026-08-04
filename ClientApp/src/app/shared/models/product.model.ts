export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  inStock: boolean;
  imageUrl: string | null;
  categoryId: number;
  categoryName: string;
}