export interface OutOfStockProduct {
  id: number;
  name: string;
}

export interface TopProduct {
  productId: number;
  name: string;
  quantitySold: number;
  revenue: number;
}

export interface Dashboard {
  totalRevenue: number;
  totalOrders: number;
  pendingOrdersCount: number;
  activeProductsCount: number;
  outOfStockProducts: OutOfStockProduct[];
  topProducts: TopProduct[];
}
