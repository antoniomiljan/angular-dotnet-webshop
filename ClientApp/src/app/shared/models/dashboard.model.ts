export interface LowStockProduct {
  id: number;
  name: string;
  stock: number;
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
  lowStockProducts: LowStockProduct[];
  topProducts: TopProduct[];
}
