import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { getOrders, getOrdersByStatus } from '../services/api';
import { Order } from '../types';
import { StatusBadge, formatCurrency, formatDate } from '../components/StatusBadge';

const STATUSES = ['All', 'Submitted', 'InventoryPending', 'InventoryConfirmed', 'InventoryFailed',
  'PaymentPending', 'PaymentApproved', 'PaymentFailed', 'ShippingPending', 'ShippingCreated', 'Completed', 'Failed'];

const OrdersTable: React.FC = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();
  const statusFilter = searchParams.get('status') ?? 'All';

  const fetchOrders = async (status: string) => {
    setLoading(true);
    try {
      const data = status === 'All' ? await getOrders() : await getOrdersByStatus(status);
      setOrders(data);
    } catch { setOrders([]); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchOrders(statusFilter); }, [statusFilter]);

  const filtered = orders.filter(o =>
    search === '' ||
    o.customerName.toLowerCase().includes(search.toLowerCase()) ||
    o.id.toLowerCase().includes(search.toLowerCase()) ||
    o.status.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>📋 Orders</h2>
        <button className="btn btn-outline-secondary btn-sm" onClick={() => fetchOrders(statusFilter)}>🔄 Refresh</button>
      </div>

      <div className="d-flex flex-wrap gap-2 mb-3">
        {STATUSES.map(s => (
          <button key={s}
            className={`btn btn-sm ${statusFilter === s ? 'btn-dark' : 'btn-outline-secondary'}`}
            onClick={() => setSearchParams(s === 'All' ? {} : { status: s })}>
            {s}
          </button>
        ))}
      </div>

      <div className="mb-3">
        <input className="form-control" placeholder="Search by name, ID or status..."
          value={search} onChange={e => setSearch(e.target.value)} />
      </div>

      {loading ? (
        <div className="text-center py-5"><div className="spinner-border text-primary" /></div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-dark">
              <tr>
                <th>Order ID</th>
                <th>Customer</th>
                <th>Date</th>
                <th className="text-end">Total</th>
                <th className="text-center">Status</th>
                <th>Inventory</th>
                <th>Payment</th>
                <th>Tracking</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr><td colSpan={9} className="text-center text-muted py-4">No orders found</td></tr>
              ) : filtered.map(o => (
                <tr key={o.id}>
                  <td><code className="small">{o.id.substring(0, 8)}…</code></td>
                  <td>
                    <div className="fw-semibold">{o.customerName}</div>
                    <div className="text-muted small">{o.customerEmail}</div>
                  </td>
                  <td className="small text-muted">{formatDate(o.createdAt)}</td>
                  <td className="text-end fw-bold">{formatCurrency(o.totalAmount)}</td>
                  <td className="text-center"><StatusBadge status={o.status} /></td>
                  <td className="text-center">
                    {o.inventoryRecord
                      ? <span className={`badge ${o.inventoryRecord.success ? 'bg-success' : 'bg-danger'}`}>
                          {o.inventoryRecord.success ? '✅' : '❌'}
                        </span>
                      : <span className="text-muted">—</span>}
                  </td>
                  <td className="text-center">
                    {o.paymentRecord
                      ? <span className={`badge ${o.paymentRecord.success ? 'bg-success' : 'bg-danger'}`}>
                          {o.paymentRecord.success ? '✅' : '❌'}
                        </span>
                      : <span className="text-muted">—</span>}
                  </td>
                  <td className="small">{o.trackingNumber ?? '—'}</td>
                  <td>
                    <Link to={`/orders/${o.id}`} className="btn btn-outline-primary btn-sm">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="text-muted small">Showing {filtered.length} of {orders.length} orders</p>
        </div>
      )}
    </div>
  );
};

export default OrdersTable;
