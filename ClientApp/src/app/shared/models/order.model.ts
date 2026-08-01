export interface OrderItemRequest {
  productId: number;
  quantity: number;
}

export interface CreateOrderRequest {
  items: OrderItemRequest[];
  guestEmail: string;
}

export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPriceAtPurchase: number;
}

export interface Order {
  id: number;
  status: string;
  totalAmount: number;
  createdAt: string;
  guestEmail: string | null;
  items: OrderItem[];
}