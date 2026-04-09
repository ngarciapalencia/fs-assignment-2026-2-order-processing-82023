import React from 'react';
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import OrdersTable from './pages/OrdersTable';
import OrderDetails from './pages/OrderDetails';
import FailedOrders from './pages/FailedOrders';

const App: React.FC = () => (
  <BrowserRouter>
    <div className="min-vh-100 bg-light">
      <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
        <div className="container-fluid">
          <span className="navbar-brand fw-bold">⚙️ SportsStore Admin</span>
          <div className="navbar-nav flex-row gap-3">
            <NavLink to="/" end className={({ isActive }) => `nav-link ${isActive ? 'text-white fw-bold' : 'text-secondary'}`}>
              Dashboard
            </NavLink>
            <NavLink to="/orders" className={({ isActive }) => `nav-link ${isActive ? 'text-white fw-bold' : 'text-secondary'}`}>
              Orders
            </NavLink>
            <NavLink to="/failed" className={({ isActive }) => `nav-link ${isActive ? 'text-danger fw-bold' : 'text-secondary'}`}>
              Failed
            </NavLink>
          </div>
        </div>
      </nav>

      <div className="container-fluid py-4 px-4">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/orders" element={<OrdersTable />} />
          <Route path="/orders/:id" element={<OrderDetails />} />
          <Route path="/failed" element={<FailedOrders />} />
        </Routes>
      </div>
    </div>
  </BrowserRouter>
);

export default App;
