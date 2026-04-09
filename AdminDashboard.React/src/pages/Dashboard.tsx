import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getDashboardSummary } from '../services/api';
import { DashboardSummary } from '../types';
import { formatCurrency } from '../components/StatusBadge';

const Dashboard: React.FC = () => {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDashboardSummary()
      .then(setSummary)
      .catch(() => setError('Cannot reach Order API. Is it running?'));

    const interval = setInterval(() => {
      getDashboardSummary().then(setSummary).catch(() => {});
    }, 10000);
    return () => clearInterval(interval);
  }, []);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!summary) return <div className="text-center py-5"><div className="spinner-border text-primary" /></div>;

  const cards = [
    { label: 'Total Orders', value: summary.totalOrders, color: 'primary', icon: '📋', link: '/orders' },
    { label: 'Completed', value: summary.completedOrders, color: 'success', icon: '✅', link: '/orders?status=Completed' },
    { label: 'Failed', value: summary.failedOrders, color: 'danger', icon: '❌', link: '/failed' },
    { label: 'Pending', value: summary.pendingOrders, color: 'warning', icon: '⏳', link: '/orders' },
    { label: 'Revenue', value: formatCurrency(summary.totalRevenue), color: 'info', icon: '💰', link: '/orders?status=Completed' },
  ];

  return (
    <div>
      <h2 className="mb-4">📊 Operations Dashboard</h2>
      <div className="row g-3 mb-4">
        {cards.map(c => (
          <div className="col-6 col-md-4 col-lg-2-custom" key={c.label}>
            <Link to={c.link} className="text-decoration-none">
              <div className={`card border-${c.color} shadow-sm h-100`}>
                <div className="card-body text-center">
                  <div className="fs-2">{c.icon}</div>
                  <div className={`text-${c.color} fw-bold fs-4`}>{c.value}</div>
                  <div className="text-muted small">{c.label}</div>
                </div>
              </div>
            </Link>
          </div>
        ))}
      </div>

      <div className="card shadow-sm">
        <div className="card-header bg-dark text-white"><strong>Orders by Status</strong></div>
        <div className="card-body">
          <div className="row g-2">
            {Object.entries(summary.ordersByStatus).map(([status, count]) => (
              <div className="col-6 col-md-3" key={status}>
                <div className="d-flex justify-content-between align-items-center border rounded p-2">
                  <span className="small fw-semibold">{status}</span>
                  <span className="badge bg-dark">{count}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
