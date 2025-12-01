// ========================================
// File: frontend/src/components/Transactions.jsx
// Copy this entire file to src/components/Transactions.jsx
// ========================================

import { useState, useEffect } from 'react';
import cnabService from '../services/cnabService';
import './Transactions.css';

function Transactions({
  refresh,
  isUploading = false,
  isDeleting = false,
  onLoadingComplete = null,
  onDeletingStart = null,
  onDeletingEnd = null,
  onShowModal = null,
  onCloseModal = null,
}) {
  const [stores, setStores] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [storePagination, setStorePagination] = useState({});
  const pageSize = 50;

  useEffect(() => {
    loadStoreBalances();
  }, [refresh]);

  // Notify parent when loading is complete (after upload)
  useEffect(() => {
    if (!loading && isUploading && onLoadingComplete) {
      onLoadingComplete();
    }
  }, [loading, isUploading, onLoadingComplete]);

  const loadStoreBalances = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await cnabService.getStoreBalances();
      setStores(data);
    } catch (err) {
      setError(err.response?.data?.message || 'Error loading transactions');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteAll = async () => {
    const totalTransactions = stores.reduce((sum, store) => sum + store.transactionCount, 0);

    if (onShowModal) {
      onShowModal({
        type: 'confirm',
        title: 'Delete All Transactions?',
        message: `Are you sure you want to delete ALL ${totalTransactions} transactions?\n\nThis action CANNOT be undone!`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        onConfirm: executeDelete,
        onCancel: () => {
          if (onCloseModal) {
            onCloseModal();
          }
        },
      });
    }
  };

  const executeDelete = async () => {
    // Close confirmation modal first
    if (onCloseModal) {
      onCloseModal();
    }

    // Give React time to update the UI and close the modal
    await new Promise(resolve => setTimeout(resolve, 100));

    try {
      setDeleting(true);
      if (onDeletingStart) {
        onDeletingStart();
      }
      setError(null);

      const result = await cnabService.deleteAllTransactions();
      await loadStoreBalances();

      if (onShowModal) {
        onShowModal({
          type: 'success',
          title: 'Success',
          message: result.message,
          onConfirm: () => {
            if (onCloseModal) {
              onCloseModal();
            }
          },
        });
      }
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Failed to delete transactions';
      setError(errorMsg);

      if (onShowModal) {
        onShowModal({
          type: 'error',
          title: 'Error',
          message: errorMsg,
          onConfirm: () => {
            if (onCloseModal) {
              onCloseModal();
            }
          },
        });
      }
    } finally {
      setDeleting(false);
      if (onDeletingEnd) {
        onDeletingEnd();
      }
    }
  };

  const formatCurrency = (value) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('pt-BR');
  };

  const getStorePage = (storeName) => {
    return storePagination[storeName] || 1;
  };

  const setStorePage = (storeName, page) => {
    setStorePagination(prev => ({ ...prev, [storeName]: page }));
  };

  const getStoreTransactions = (store) => {
    const currentPage = getStorePage(store.storeName);
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return store.transactions.slice(startIndex, endIndex);
  };

  const getStoreTotalPages = (store) => {
    return Math.ceil(store.transactions.length / pageSize);
  };

  const renderStorePagination = (store) => {
    const totalPages = getStoreTotalPages(store);
    if (totalPages <= 1) return null;

    const currentPage = getStorePage(store.storeName);
    const isDisabled = loading || isUploading || deleting || isDeleting;

    return (
      <div className="store-pagination">
        <button
          onClick={() => setStorePage(store.storeName, 1)}
          disabled={currentPage === 1 || isDisabled}
          className="pagination-btn"
          title="First page"
        >
          «
        </button>
        <button
          onClick={() => setStorePage(store.storeName, currentPage - 1)}
          disabled={currentPage === 1 || isDisabled}
          className="pagination-btn"
          title="Previous page"
        >
          ‹
        </button>

        <span className="pagination-info">
          Page {currentPage} of {totalPages}
        </span>

        <button
          onClick={() => setStorePage(store.storeName, currentPage + 1)}
          disabled={currentPage === totalPages || isDisabled}
          className="pagination-btn"
          title="Next page"
        >
          ›
        </button>
        <button
          onClick={() => setStorePage(store.storeName, totalPages)}
          disabled={currentPage === totalPages || isDisabled}
          className="pagination-btn"
          title="Last page"
        >
          »
        </button>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="loading">
        <div className="spinner"></div>
        <p>Loading transactions...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-error">
        ❌ {error}
      </div>
    );
  }

  if (stores.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-icon">📭</div>
        <h3>No transactions found</h3>
        <p>Upload a CNAB file to see transactions here</p>
      </div>
    );
  }

  const totalTransactions = stores.reduce((sum, store) => sum + store.transactionCount, 0);

  return (
    <div className="transactions-container">
      <div className="transactions-header">
        <h2>📊 Transactions</h2>
        <button
          className="btn-delete-all"
          onClick={handleDeleteAll}
          disabled={deleting || loading || isUploading || isDeleting}
        >
          {deleting || isDeleting ? '🗑️ Deleting...' : '🗑️ Delete All'}
        </button>
      </div>

      <div className="summary">
        <div className="summary-card">
          <span className="summary-label">Total Stores</span>
          <span className="summary-value">{stores.length}</span>
        </div>
        <div className="summary-card">
          <span className="summary-label">Total Transactions</span>
          <span className="summary-value">{totalTransactions}</span>
        </div>
      </div>

      {stores.map((store) => (
        <div key={store.storeName} className="store-card">
          <div className="store-header">
            <div className="store-name">🏪 {store.storeName}</div>
            <div className={`balance ${store.totalBalance >= 0 ? 'positive' : 'negative'}`}>
              Balance: {formatCurrency(store.totalBalance)}
            </div>
          </div>

          <div className="store-stats">
            <span>Transactions: {store.transactionCount}</span>
            <span>Income: {formatCurrency(store.totalIncome)}</span>
            <span>Expenses: {formatCurrency(store.totalExpenses)}</span>
          </div>

          {renderStorePagination(store) && (
            <div style={{ marginBottom: '1rem' }}>
              {renderStorePagination(store)}
            </div>
          )}

          <div className="transactions-table-wrapper">
            <table className="transactions-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Time</th>
                  <th>Type</th>
                  <th>Amount</th>
                  <th>CPF</th>
                  <th>Card</th>
                </tr>
              </thead>
              <tbody>
                {getStoreTransactions(store).map((transaction) => (
                  <tr key={transaction.id}>
                    <td>{formatDate(transaction.date)}</td>
                    <td>{transaction.time}</td>
                    <td>
                      <span className={`badge badge-${transaction.nature.toLowerCase()}`}>
                        {transaction.typeDescription}
                      </span>
                    </td>
                    <td className={transaction.signedAmount >= 0 ? 'amount-positive' : 'amount-negative'}>
                      {formatCurrency(transaction.signedAmount)}
                    </td>
                    <td>{transaction.cpf}</td>
                    <td>{transaction.cardNumber}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {renderStorePagination(store) && (
            <div style={{ marginTop: '1rem' }}>
              {renderStorePagination(store)}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

export default Transactions;