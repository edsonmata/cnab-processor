// ========================================
// File: frontend/src/components/Modal.jsx
// Modal dialog component for confirmations, success, and error messages
// ========================================

import './Modal.css';

function Modal({
  isOpen = false,
  type = 'info', // 'confirm', 'success', 'error'
  title = '',
  message = '',
  onConfirm = null,
  onCancel = null,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
}) {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      <div className={`modal-container modal-${type}`}>
        <div className="modal-icon">
          {type === 'confirm' && '❓'}
          {type === 'success' && '✅'}
          {type === 'error' && '❌'}
        </div>

        {title && <h3 className="modal-title">{title}</h3>}
        <p className="modal-message">{message}</p>

        <div className="modal-actions">
          {type === 'confirm' && (
            <>
              <button
                className="modal-btn modal-btn-cancel"
                onClick={onCancel}
              >
                {cancelText}
              </button>
              <button
                className="modal-btn modal-btn-confirm"
                onClick={onConfirm}
              >
                {confirmText}
              </button>
            </>
          )}
          {(type === 'success' || type === 'error') && (
            <button
              className={`modal-btn ${type === 'success' ? 'modal-btn-success' : 'modal-btn-error'}`}
              onClick={onConfirm}
            >
              OK
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

export default Modal;
