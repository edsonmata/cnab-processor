// ========================================
// File: frontend/src/services/cnabService.js
// Purpose: Service to communicate with CNAB API
// ========================================

import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5099/api';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to include JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor to handle 401 errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid, clear storage and redirect to login
      localStorage.removeItem('token');
      localStorage.removeItem('username');
      localStorage.removeItem('tokenExpiry');
      window.location.reload();
    }
    return Promise.reject(error);
  }
);

export const cnabService = {
  /**
   * Upload CNAB file
   */
  async uploadFile(file) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/cnab/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    return response.data;
  },

  /**
   * Get all store balances (no pagination - aggregated data)
   */
  async getStoreBalances() {
    const response = await api.get('/cnab/balances');
    return response.data;
  },

  /**
   * Get transactions for a specific store (PAGINATED)
   * @param {string} storeName - Store name
   * @param {number} pageNumber - Page number (1-based)
   * @param {number} pageSize - Items per page (default: 50)
   */
  async getStoreTransactions(storeName, pageNumber = 1, pageSize = 50) {
    const response = await api.get(`/cnab/store/${encodeURIComponent(storeName)}/paged`, {
      params: { pageNumber, pageSize }
    });
    return response.data;
  },

  /**
   * Get global statistics
   */
  async getStatistics() {
    const response = await api.get('/cnab/stats');
    return response.data;
  },

  /**
   * Get all transactions (PAGINATED)
   * @param {number} pageNumber - Page number (1-based)
   * @param {number} pageSize - Items per page (default: 100)
   */
  async getAllTransactions(pageNumber = 1, pageSize = 100) {
    const response = await api.get('/cnab/transactions/paged', {
      params: { pageNumber, pageSize }
    });
    return response.data;
  },

  /**
   * Delete all transactions
   */
  async deleteAllTransactions() {
    const response = await api.delete('/cnab/transactions');
    return response.data;
  }
};

export default cnabService;
