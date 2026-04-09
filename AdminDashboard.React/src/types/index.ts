export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface InventoryRecord {
  success: boolean;
  failureReason?: string;
  processedAt: string;
}

export interface PaymentRecord {
  success: boolean;
  transactionId?: string;
  failureReason?: string;
  amount: number;
  processedAt: string;
}

export interface ShipmentRecord {
  trackingNumber: string;
  estimatedDispatch: string;
  createdAt: string;
}

export interface Order {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  shippingAddress: string;
  status: string;
  totalAmount: number;
  failureReason?: string;
  trackingNumber?: string;
  paymentTransactionId?: string;
  estimatedDispatch?: string;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
  inventoryRecord?: InventoryRecord;
  paymentRecord?: PaymentRecord;
  shipmentRecord?: ShipmentRecord;
}

export interface DashboardSummary {
  totalOrders: number;
  completedOrders: number;
  failedOrders: number;
  pendingOrders: number;
  totalRevenue: number;
  ordersByStatus: Record<string, number>;
}
