import axios from 'axios';
import { Order, DashboardSummary } from '../types';

const API_BASE = process.env.REACT_APP_API_URL || 'http://localhost:5001';

const api = axios.create({ baseURL: API_BASE });

export const getOrders = (page = 1, pageSize = 50): Promise<Order[]> =>
  api.get(`/api/orders?page=${page}&pageSize=${pageSize}`).then(r => r.data);

export const getOrdersByStatus = (status: string): Promise<Order[]> =>
  api.get(`/api/orders/status/${status}`).then(r => r.data);

export const getOrderById = (id: string): Promise<Order> =>
  api.get(`/api/orders/${id}`).then(r => r.data);

export const getDashboardSummary = (): Promise<DashboardSummary> =>
  api.get('/api/dashboard/summary').then(r => r.data);
