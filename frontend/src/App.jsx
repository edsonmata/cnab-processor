// ========================================
// File: frontend/src/App.jsx
// Replace the existing App.jsx file with this one
// ========================================

import { useState, useEffect } from 'react';
import Upload from './components/Upload';
import Transactions from './components/Transactions';
import Login from './components/Login';
import Loading from './components/Loading';
import Modal from './components/Modal';
import './App.css';

function App() {
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeTab, setActiveTab] = useState('upload');
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [username, setUsername] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [modalConfig, setModalConfig] = useState({
    isOpen: false,
    type: 'info',
    title: '',
    message: '',
    onConfirm: null,
    onCancel: null,
  });

  // Check if user is authenticated on mount
  useEffect(() => {
    const token = localStorage.getItem('token');
    const storedUsername = localStorage.getItem('username');
    const tokenExpiry = localStorage.getItem('tokenExpiry');

    if (token && tokenExpiry) {
      const expiryDate = new Date(tokenExpiry);
      const now = new Date();

      // Check if token is still valid
      if (expiryDate > now) {
        setIsAuthenticated(true);
        setUsername(storedUsername || '');
      } else {
        // Token expired, clear storage
        handleLogout();
      }
    }
  }, []);

  const handleUploadStart = () => {
    setIsUploading(true);
  };

  const handleUploadSuccess = () => {
    // Keep isUploading = true until data is fully loaded
    // It will be set to false when Transactions component finishes loading
    // Refresh transactions list
    setRefreshKey((prev) => prev + 1);
    // Switch to transactions tab
    setActiveTab('transactions');
  };

  const handleUploadError = () => {
    setIsUploading(false);
  };

  const handleLoginSuccess = (data) => {
    setIsAuthenticated(true);
    setUsername(data.username);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('tokenExpiry');
    setIsAuthenticated(false);
    setUsername('');
  };

  const handleDeletingStart = () => {
    setIsDeleting(true);
  };

  const handleDeletingEnd = () => {
    setIsDeleting(false);
  };

  const showModal = (config) => {
    setModalConfig({
      ...config,
      isOpen: true,
    });
  };

  const closeModal = () => {
    setModalConfig({
      ...modalConfig,
      isOpen: false,
    });
  };

  // Show login screen if not authenticated
  if (!isAuthenticated) {
    return <Login onLoginSuccess={handleLoginSuccess} />;
  }

  return (
    <div className="app">
      <header className="app-header">
        <div>
          <h1>🏦 CNAB Processor</h1>
          <p>Brazilian Financial Transaction File Processing System</p>
        </div>
        <div className="user-info">
          <span className="username">👤 {username}</span>
          <button className="btn-logout" onClick={handleLogout}>
            🚪 Logout
          </button>
        </div>
      </header>

      <nav className="app-nav">
        <button
          className={`nav-btn ${activeTab === 'upload' ? 'active' : ''}`}
          onClick={() => setActiveTab('upload')}
          disabled={isUploading || isDeleting}
        >
          📤 Uploads
        </button>
        <button
          className={`nav-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
          disabled={isUploading || isDeleting}
        >
          📊 Transactions
        </button>
      </nav>

      <main className="app-main">
        {activeTab === 'upload' && (
          <Upload
            onUploadStart={handleUploadStart}
            onUploadSuccess={handleUploadSuccess}
            onUploadError={handleUploadError}
            isUploading={isUploading}
          />
        )}

        {activeTab === 'transactions' && (
          <Transactions
            refresh={refreshKey}
            isUploading={isUploading}
            isDeleting={isDeleting}
            onLoadingComplete={() => setIsUploading(false)}
            onDeletingStart={handleDeletingStart}
            onDeletingEnd={handleDeletingEnd}
            onShowModal={showModal}
            onCloseModal={closeModal}
          />
        )}
      </main>

      <footer className="app-footer">
        <p>
          Backend API: <a href="http://localhost:5099/swagger" target="_blank" rel="noopener noreferrer">
            Swagger Documentation
          </a>
        </p>
        <p>CNAB Processor v1.0 - Edson Mata</p>
      </footer>

      {isUploading && <Loading message="Importing transactions..." />}
      {isDeleting && <Loading message="Deleting transactions..." />}
      <Modal {...modalConfig} />
    </div>
  );
}

export default App;