import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getOrderById } from '../services/api';
import { Order } from '../types';
import { StatusBadge, formatCurrency, formatDate } from '../components/StatusBadge';

const OrderDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (id) getOrderById(id).then(setOrder).finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="text-center py-5"><div className="spinner-border text-primary" /></div>;
  if (!order) return <div className="alert alert-warning">Order not found.</div>;

  return (
    <div>
      <Link to="/orders" className="btn btn-outline-secondary btn-sm mb-4">← Back to Orders</Link>

      <div className="d-flex align-items-center gap-3 mb-4">
        <h2 className="mb-0">Order Details</h2>
        <StatusBadge status={order.status} />
      </div>

      {order.failureReason && (
        <div className="alert alert-danger">❌ <strong>Failure:</strong> {order.failureReason}</div>
      )}

      <div className="row g-4">
        {/* Order Info */}
        <div className="col-md-6">
          <div className="card shadow-sm h-100">
            <div className="card-header bg-dark text-white"><strong>📋 Order Info</strong></div>
            <div className="card-body">
              <dl className="row mb-0">
                <dt className="col-sm-4">Order ID</dt><dd className="col-sm-8"><code className="small">{order.id}</code></dd>
                <dt className="col-sm-4">Customer</dt><dd className="col-sm-8">{order.customerName}</dd>
                <dt className="col-sm-4">Email</dt><dd className="col-sm-8">{order.customerEmail}</dd>
                <dt className="col-sm-4">Address</dt><dd className="col-sm-8">{order.shippingAddress}</dd>
                <dt className="col-sm-4">Created</dt><dd className="col-sm-8">{formatDate(order.createdAt)}</dd>
                <dt className="col-sm-4">Updated</dt><dd className="col-sm-8">{formatDate(order.updatedAt)}</dd>
                <dt className="col-sm-4">Total</dt><dd className="col-sm-8 fw-bold text-success">{formatCurrency(order.totalAmount)}</dd>
              </dl>
            </div>
          </div>
        </div>

        {/* Pipeline Status */}
        <div className="col-md-6">
          <div className="card shadow-sm h-100">
            <div className="card-header bg-dark text-white"><strong>⚙️ Processing Pipeline</strong></div>
            <div className="card-body">
              <ul className="list-group list-group-flush">
                <li className="list-group-item d-flex justify-content-between align-items-center">
                  📦 Inventory
                  {order.inventoryRecord
                    ? <span>
                        <span className={`badge ${order.inventoryRecord.success ? 'bg-success' : 'bg-danger'} me-2`}>
                          {order.inventoryRecord.success ? 'Confirmed' : 'Failed'}
                        </span>
                        {order.inventoryRecord.failureReason && <small className="text-danger">{order.inventoryRecord.failureReason}</small>}
                      </span>
                    : <span className="text-muted small">Pending</span>}
                </li>
                <li className="list-group-item d-flex justify-content-between align-items-center">
                  💳 Payment
                  {order.paymentRecord
                    ? <span>
                        <span className={`badge ${order.paymentRecord.success ? 'bg-success' : 'bg-danger'} me-2`}>
                          {order.paymentRecord.success ? 'Approved' : 'Rejected'}
                        </span>
                        {order.paymentRecord.transactionId && <small className="text-muted">{order.paymentRecord.transactionId}</small>}
                        {order.paymentRecord.failureReason && <small className="text-danger">{order.paymentRecord.failureReason}</small>}
                      </span>
                    : <span className="text-muted small">Pending</span>}
                </li>
                <li className="list-group-item d-flex justify-content-between align-items-center">
                  🚚 Shipping
                  {order.shipmentRecord
                    ? <span>
                        <span className="badge bg-success me-2">Created</span>
                        <small className="text-muted">{order.shipmentRecord.trackingNumber}</small>
                      </span>
                    : <span className="text-muted small">Pending</span>}
                </li>
              </ul>
            </div>
          </div>
        </div>

        {/* Items */}
        <div className="col-12">
          <div className="card shadow-sm">
            <div className="card-header bg-dark text-white"><strong>🛒 Order Items</strong></div>
            <div className="card-body p-0">
              <table className="table table-sm mb-0">
                <thead><tr><th>Product</th><th className="text-center">Qty</th><th className="text-end">Unit Price</th><th className="text-end">Total</th></tr></thead>
                <tbody>
                  {order.items.map((item, i) => (
                    <tr key={i}>
                      <td>{item.productName}</td>
                      <td className="text-center">{item.quantity}</td>
                      <td className="text-end">{formatCurrency(item.unitPrice)}</td>
                      <td className="text-end fw-semibold">{formatCurrency(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="table-light">
                    <td colSpan={3} className="text-end fw-bold">Grand Total</td>
                    <td className="text-end fw-bold text-success">{formatCurrency(order.totalAmount)}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderDetails;
