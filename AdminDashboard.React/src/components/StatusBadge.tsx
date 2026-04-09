import React from 'react';

const badgeClass: Record<string, string> = {
  Completed: 'bg-success',
  Failed: 'bg-danger',
  InventoryFailed: 'bg-danger',
  PaymentFailed: 'bg-danger',
  Submitted: 'bg-info',
  InventoryPending: 'bg-warning text-dark',
  InventoryConfirmed: 'bg-info',
  PaymentPending: 'bg-warning text-dark',
  PaymentApproved: 'bg-info',
  ShippingPending: 'bg-warning text-dark',
  ShippingCreated: 'bg-primary',
};

export const StatusBadge: React.FC<{ status: string }> = ({ status }) => (
  <span className={`badge ${badgeClass[status] ?? 'bg-secondary'}`}>{status}</span>
);

export const formatCurrency = (n: number) =>
  new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' }).format(n);

export const formatDate = (d: string) =>
  new Date(d).toLocaleString('en-IE', { dateStyle: 'medium', timeStyle: 'short' });
