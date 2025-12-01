// ========================================
// File: frontend/src/components/Loading.jsx
// Loading spinner component with dynamic message
// ========================================

import './Loading.css';

function Loading({ message = 'Processing...' }) {
  return (
    <div className="loading-overlay">
      <div className="loading-container">
        <div className="spinner"></div>
        <p>{message}</p>
      </div>
    </div>
  );
}

export default Loading;
