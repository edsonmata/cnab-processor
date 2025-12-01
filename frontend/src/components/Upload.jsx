// ========================================
// File: frontend/src/components/Upload.jsx
// Copy this entire file to src/components/Upload.jsx
// ========================================

import { useState, useRef, useEffect } from 'react';
import cnabService from '../services/cnabService';
import './Upload.css';

function Upload({ onUploadStart, onUploadSuccess, onUploadError, isUploading: parentIsUploading = false }) {
  const [selectedFile, setSelectedFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState(null);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef(null);
  const previousParentUploadingRef = useRef(parentIsUploading);

  // Clear selected file when upload and data loading are completely finished
  useEffect(() => {
    // Only clear when parentIsUploading changes from true to false
    if (previousParentUploadingRef.current === true && parentIsUploading === false && selectedFile) {
      setSelectedFile(null);
    }
    previousParentUploadingRef.current = parentIsUploading;
  }, [parentIsUploading, selectedFile]);

  const handleFileSelect = (event) => {
    const file = event.target.files[0];
    if (file) {
      validateAndSetFile(file);
    }
  };

  const validateAndSetFile = (file) => {
    // Validate file type
    if (!file.name.endsWith('.txt')) {
      setMessage({ type: 'error', text: 'Only .txt files are allowed' });
      setSelectedFile(null);
      return;
    }

    // Validate file size (10MB)
    if (file.size > 10 * 1024 * 1024) {
      setMessage({ type: 'error', text: 'File size must not exceed 10MB' });
      setSelectedFile(null);
      return;
    }

    setSelectedFile(file);
    setMessage(null);
  };

  const handleDragOver = (e) => {
    if (uploading) return;
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e) => {
    if (uploading) return;
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e) => {
    if (uploading) return;
    e.preventDefault();
    setIsDragging(false);

    const file = e.dataTransfer.files[0];
    if (file) {
      validateAndSetFile(file);
    }
  };

  const handleDropZoneClick = () => {
    if (uploading) return;
    document.getElementById('file-input').click();
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      setMessage({ type: 'error', text: 'Please select a file' });
      return;
    }

    setUploading(true);
    setMessage(null);

    // Notify parent that upload is starting
    if (onUploadStart) {
      onUploadStart();
    }

    try {
      const response = await cnabService.uploadFile(selectedFile);

      setMessage({
        type: 'success',
        text: `Success! ${response.transactionsImported} transactions imported`,
      });

      // Clear the input value to allow uploading the same file again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }

      // Notify parent component
      if (onUploadSuccess) {
        onUploadSuccess(response);
      }
    } catch (error) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Error uploading file',
      });

      // Notify parent that upload failed
      if (onUploadError) {
        onUploadError();
      }
    } finally {
      setUploading(false);
      // Clear input even on error so user can retry with the same file
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      // Only clear selectedFile when parent finishes loading (parentIsUploading = false)
      // This keeps the Clear button disabled during the entire upload and data loading process
    }
  };

  const clearSelection = () => {
    setSelectedFile(null);
    setMessage(null);
    // CRITICAL: Clear the input value so onChange fires even for the same file
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className="upload-container">
      <h2>📤 Upload CNAB File</h2>

      <div
        className={`drop-zone ${isDragging ? 'dragging' : ''} ${uploading ? 'disabled' : ''}`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={handleDropZoneClick}
      >
        <input
          ref={fileInputRef}
          id="file-input"
          type="file"
          accept=".txt"
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />

        <div className="drop-zone-content">
          <div className="upload-icon">📁</div>

          {!selectedFile ? (
            <>
              <p className="drop-text">
                <strong>Click to select</strong> or drag and drop CNAB file here
              </p>
              <p className="file-hint">Only .txt files (max 10MB)</p>
            </>
          ) : (
            <div className="selected-file">
              <p>
                <strong>Selected:</strong> {selectedFile.name}
              </p>
              <p className="file-size">
                {(selectedFile.size / 1024).toFixed(2)} KB
              </p>
            </div>
          )}
        </div>
      </div>

      <div className="actions">
        <button
          className="btn btn-primary"
          onClick={handleUpload}
          disabled={!selectedFile || uploading}
        >
          {uploading ? 'Uploading...' : 'Upload File'}
        </button>

        {selectedFile && (
          <button
            className="btn btn-secondary"
            onClick={clearSelection}
            disabled={uploading}
          >
            Clear
          </button>
        )}
      </div>

      {message && (
        <div className={`alert alert-${message.type}`}>
          {message.type === 'success' ? '✅' : '❌'} {message.text}
        </div>
      )}
    </div>
  );
}

export default Upload;