import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getOrdersByStatus } from '../services/api';
import { Order } from '../types';
import { formatCurrency, formatDate } from '../components/StatusBadge';

const FailedOrders: React.FC = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      getOrdersByStatus('Failed'),
      getOrdersByStatus('InventoryFailed'),
      getOrdersByStatus('PaymentFailed'),
    ])
      .then(([a, b, c]) => setOrders([...a, ...b, ...c].sort((x, y) =>
        new Date(y.createdAt).getTime() - new Date(x.createdAt).getTime()
      )))
      .finally(() => setLoading(false));
  }, []);

  const stageIcon = (status: string) => {
    if (status === 'InventoryFailed') return '📦 Inventory';
    if (status === 'PaymentFailed') return '💳 Payment';
    return '❌ General';
  };

  if (loading) return <div className="text-center py-5"><div className="spinner-border text-danger" /></div>;

  return (
    <div>
      <h2 className="mb-1 text-danger">❌ Failed Orders</h2>
      <p className="text-muted mb-4">{orders.length} failed order(s) requiring attention</p>

      {orders.length === 0 ? (
        <div className="alert alert-success">🎉 No failed orders!</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-danger">
              <tr>
                <th>Order ID</th>
                <th>Customer</th>
                <th>Date</th>
                <th className="text-end">Amount</th>
                <th>Failed Stage</th>
                <th>Reason</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.id}>
                  <td><code className="small">{o.id.substring(0, 8)}…</code></td>
                  <td>
                    <div className="fw-semibold">{o.customerName}</div>
                    <div className="text-muted small">{o.customerEmail}</div>
                  </td>
                  <td className="small text-muted">{formatDate(o.createdAt)}</td>
                  <td className="text-end">{formatCurrency(o.totalAmount)}</td>
                  <td><span className="badge bg-warning text-dark">{stageIcon(o.status)}</span></td>
                  <td className="small text-danger">{o.failureReason ?? '—'}</td>
                  <td>
                    <Link to={`/orders/${o.id}`} className="btn btn-outline-danger btn-sm">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default FailedOrders;
