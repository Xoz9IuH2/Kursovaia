import { useState, useEffect } from 'react';
import { apiFetch } from '../utils/api';
import './Documents.css';

const API_ORIGIN = window.location.origin;

function imgUrl(path) {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  return API_ORIGIN + path;
}

function fmtDate(d) {
  if (!d) return '—';
  try { return new Date(d).toLocaleDateString('ru-RU'); }
  catch { return d; }
}

export default function Documents() {
  const [pendingDocs, setPendingDocs] = useState([]);
  const [photoUsers, setPhotoUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedDoc, setSelectedDoc] = useState(null);
  const [selectedPhoto, setSelectedPhoto] = useState(null);
  const [selectedPhotoType, setSelectedPhotoType] = useState(null);
  const [rejectReason, setRejectReason] = useState('');
  const [verifying, setVerifying] = useState(false);

  useEffect(() => { loadAll(); }, []);

  const loadAll = async () => {
    setLoading(true);
    try {
      const [pending, photos] = await Promise.all([
        apiFetch('/documents/pending').catch(() => []),
        apiFetch('/profile/photos/pending').catch(() => []),
      ]);
      setPendingDocs(pending);
      setPhotoUsers(photos);
    } catch (err) {
      console.error('Failed to load:', err);
    }
    setLoading(false);
  };

  const handleVerify = async (docId, status) => {
    if (status === 'rejected' && !rejectReason.trim()) {
      alert('Укажите причину отклонения');
      return;
    }
    setVerifying(true);
    try {
      await apiFetch(`/documents/${docId}/verify`, {
        method: 'POST',
        body: JSON.stringify({
          status,
          rejection_reason: status === 'rejected' ? rejectReason : null
        })
      });
      setSelectedDoc(null);
      setRejectReason('');
      loadAll();
    } catch (err) {
      alert(err.message || 'Ошибка верификации');
    }
    setVerifying(false);
  };

  const handlePhotoVerify = async (userId, type) => {
    setVerifying(true);
    try {
      await apiFetch(`/profile/photo/verify/${userId}`, {
        method: 'POST',
        body: JSON.stringify({ type })
      });
      setSelectedPhoto(null);
      loadAll();
    } catch (e) { alert(e.message || 'Ошибка'); }
    setVerifying(false);
  };

  const handlePhotoReject = async (userId, type) => {
    setVerifying(true);
    try {
      await apiFetch(`/profile/photo/reject/${userId}`, {
        method: 'POST',
        body: JSON.stringify({ type })
      });
      setSelectedPhoto(null);
      loadAll();
    } catch (e) { alert(e.message || 'Ошибка'); }
    setVerifying(false);
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'pending':
        return <span className="badge badge-warning">На проверке</span>;
      case 'verified':
        return <span className="badge badge-success">Верифицирован</span>;
      case 'rejected':
        return <span className="badge badge-danger">Отклонён</span>;
      default:
        return <span className="badge">{status}</span>;
    }
  };

  const getDocTypeLabel = (type) => {
    switch (type) {
      case 'passport': return 'Паспорт';
      case 'snils': return 'СНИЛС';
      case 'military_ticket': return 'Военный билет';
      default: return 'Документ';
    }
  };

  const getDocIcon = (type) => {
    switch (type) {
      case 'passport': return '🪪';
      case 'snils': return '🆔';
      case 'military_ticket': return '🎖️';
      default: return '📄';
    }
  };

  if (loading) {
    return <div className="loading">Загрузка документов...</div>;
  }

  return (
    <div className="documents-page">
      <div className="page-header">
        <h2>Документы на проверке</h2>
        <button className="btn" onClick={loadAll}>Обновить</button>
      </div>

      {pendingDocs.length === 0 && photoUsers.length === 0 ? (
        <div className="empty-state">
          <span className="empty-icon">✅</span>
          <p>Всё проверено</p>
        </div>
      ) : (
        <div className="documents-grid">
          {pendingDocs.map(doc => (
            <div key={doc.id} className="doc-card pending">
              <div className="doc-header">
                <span className="doc-icon">{getDocIcon(doc.file_type)}</span>
                <div className="doc-info">
                  <h4>{doc.name}</h4>
                  <p className="doc-type">{getDocTypeLabel(doc.file_type)}</p>
                </div>
                {getStatusBadge(doc.status)}
              </div>
              <div className="doc-owner">
                <strong>Владелец:</strong> {doc.last_name} {doc.first_name} {doc.middle_name || ''}
              </div>
              {doc.uploaded_by_name && (
                <div className="doc-meta">
                  Загружено: {doc.uploaded_by_name} ({fmtDate(doc.uploaded_at)})
                </div>
              )}
              <div className="doc-actions">
                <button className="btn btn-primary" onClick={() => setSelectedDoc(doc)}>
                  Рассмотреть
                </button>
              </div>
            </div>
          ))}
          {photoUsers.map(u => (
            <div key={'photo-' + u.id} className="doc-card pending">
              <div className="doc-header">
                <span className="doc-icon">📷</span>
                <div className="doc-info">
                  <h4>{u.name}</h4>
                  <p className="doc-type">Фото документа</p>
                </div>
              </div>
              {u.passportPhotoStatus === 'pending' && (
                <div className="doc-owner"><strong>Паспорт:</strong> ожидает проверки</div>
              )}
              {u.militaryPhotoStatus === 'pending' && (
                <div className="doc-owner"><strong>Военный билет:</strong> ожидает проверки</div>
              )}
              <div className="doc-actions">
                {u.passportPhotoStatus === 'pending' && (
                  <button className="btn btn-primary" onClick={() => { setSelectedPhoto(u); setSelectedPhotoType('passport'); }}>
                    Паспорт
                  </button>
                )}
                {u.militaryPhotoStatus === 'pending' && (
                  <button className="btn btn-primary" onClick={() => { setSelectedPhoto(u); setSelectedPhotoType('military'); }}>
                    Военный билет
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedDoc && (
        <div className="modal-overlay" onClick={() => setSelectedDoc(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Проверка документа</h3>
              <button className="close-btn" onClick={() => setSelectedDoc(null)}>×</button>
            </div>
            <div className="modal-body">
              <div className="doc-details">
                <p><strong>Тип:</strong> {getDocTypeLabel(selectedDoc.file_type)}</p>
                <p><strong>Название:</strong> {selectedDoc.name}</p>
                <p><strong>Владелец:</strong> {selectedDoc.last_name} {selectedDoc.first_name} {selectedDoc.middle_name || ''}</p>
              </div>
              {selectedDoc.file_path && (
                <div className="doc-image-container">
                  <img src={imgUrl(selectedDoc.file_path)} alt={selectedDoc.name} className="doc-image"
                    onError={e => { e.target.style.display = 'none'; e.target.nextSibling.style.display = 'flex'; }} />
                  <div className="image-error" style={{ display: 'none' }}><span>Не удалось загрузить изображение</span></div>
                </div>
              )}
              <div className="verification-form">
                <h4>Решение:</h4>
                <div className="decision-buttons">
                  <button className="btn btn-success" onClick={() => handleVerify(selectedDoc.id, 'verified')} disabled={verifying}>✓ Подтвердить</button>
                  <button className="btn btn-danger" onClick={() => handleVerify(selectedDoc.id, 'rejected')} disabled={verifying}>✗ Отклонить</button>
                </div>
                <div className="reject-reason">
                  <label>Причина отклонения:</label>
                  <textarea value={rejectReason} onChange={e => setRejectReason(e.target.value)} placeholder="Укажите причину отклонения документа..." rows={3} />
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {selectedPhoto && (
        <div className="modal-overlay" onClick={() => setSelectedPhoto(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Проверка фото — {selectedPhoto.name}</h3>
              <button className="close-btn" onClick={() => setSelectedPhoto(null)}>×</button>
            </div>
            <div className="modal-body">
              {selectedPhotoType === 'passport' && selectedPhoto.passportPhotoPath && (
                <div className="doc-image-container">
                  <img src={imgUrl(selectedPhoto.passportPhotoPath)} alt="Паспорт" className="doc-image"
                    onError={e => { e.target.style.display = 'none'; e.target.nextSibling.style.display = 'flex'; }} />
                  <div className="image-error" style={{ display: 'none' }}><span>Не удалось загрузить</span></div>
                </div>
              )}
              {selectedPhotoType === 'military' && selectedPhoto.militaryPhotoPath && (
                <div className="doc-image-container">
                  <img src={imgUrl(selectedPhoto.militaryPhotoPath)} alt="Военный билет" className="doc-image"
                    onError={e => { e.target.style.display = 'none'; e.target.nextSibling.style.display = 'flex'; }} />
                  <div className="image-error" style={{ display: 'none' }}><span>Не удалось загрузить</span></div>
                </div>
              )}
              {selectedPhoto.passport_series && (
                <div className="doc-details" style={{ marginTop: 12 }}>
                  <p><strong>Серия паспорта:</strong> {selectedPhoto.passport_series}</p>
                  <p><strong>Номер паспорта:</strong> {selectedPhoto.passport_number}</p>
                  <p><strong>Кем выдан:</strong> {selectedPhoto.passport_issued || '—'}</p>
                  <p><strong>Дата выдачи:</strong> {fmtDate(selectedPhoto.passport_date)}</p>
                </div>
              )}
              <div className="verification-form">
                <h4>Решение:</h4>
                <div className="decision-buttons">
                  <button className="btn btn-success" onClick={() => handlePhotoVerify(selectedPhoto.id, selectedPhotoType)} disabled={verifying}>✓ Подтвердить</button>
                  <button className="btn btn-danger" onClick={() => handlePhotoReject(selectedPhoto.id, selectedPhotoType)} disabled={verifying}>✗ Отклонить</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
