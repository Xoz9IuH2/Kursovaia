import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';
import './Applications.css';

export default function Applications() {
  const { user } = useAuth();
  const [applications, setApplications] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [formData, setFormData] = useState({ title: '', content: '' });

  useEffect(() => {
    loadApplications();
  }, [user]);

  const loadApplications = async () => {
    const data = await apiFetch('/application');
    setApplications(data.items || data || []);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    await apiFetch('/application', {
      method: 'POST',
      body: JSON.stringify(formData)
    });
    setShowForm(false);
    setFormData({ title: '', content: '' });
    loadApplications();
  };

  const handleReview = async (id, status) => {
    await apiFetch(`/application/${id}/review`, {
      method: 'POST',
      body: JSON.stringify({ status, rejectionReason: '' })
    });
    loadApplications();
  };

  const getStatusBadge = (status) => {
    const badges = {
      pending: { text: 'На рассмотрении', class: 'pending' },
      reviewed: { text: 'Рассмотрено', class: 'reviewed' },
      approved: { text: 'Одобрено', class: 'approved' },
      rejected: { text: 'Отклонено', class: 'rejected' }
    };
    return badges[status] || badges.pending;
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    try { return new Date(dateStr).toLocaleDateString(); }
    catch { return dateStr; }
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <h2>Заявления</h2>
        {user.role === 'citizen' && (
          <button onClick={() => setShowForm(!showForm)} className="btn">
            {showForm ? 'Отмена' : 'Подать заявление'}
          </button>
        )}
      </div>
      <div className="filter-bar">
        <input className="search-input" placeholder="Поиск по заголовку..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
        <select className="filter-select" value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
          <option value="">Все статусы</option>
          <option value="pending">На рассмотрении</option>
          <option value="approved">Одобрено</option>
          <option value="rejected">Отклонено</option>
        </select>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label>Тема</label>
            <input
              type="text"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              required
            />
          </div>
          <div className="form-group">
            <label>Содержание</label>
            <textarea
              value={formData.content}
              onChange={(e) => setFormData({ ...formData, content: e.target.value })}
              rows="5"
              required
            />
          </div>
          <button type="submit" className="btn">Отправить</button>
        </form>
      )}

      <div className="applications-list">
        {applications.filter(a => !statusFilter || a.status === statusFilter).filter(a => !searchQuery || (a.title || '').toLowerCase().includes(searchQuery.toLowerCase())).length === 0 ? (
          <p className="empty">Заявлений пока нет</p>
        ) : (
          applications.filter(a => !statusFilter || a.status === statusFilter).filter(a => !searchQuery || (a.title || '').toLowerCase().includes(searchQuery.toLowerCase())).map((app) => (
            <div key={app.id} className="application-card">
              <div className="app-info">
                <h3>{app.title}</h3>
                <p className="content">{app.content}</p>
                <p className="meta">
                  От: {app.userName || 'Гражданин'} | {formatDate(app.createdAt)}
                </p>
                {app.rejectionReason && (
                  <div className="response">
                    <strong>Ответ:</strong> {app.rejectionReason}
                  </div>
                )}
              </div>
              <div className="app-status">
                <span className={`badge ${getStatusBadge(app.status).class}`}>
                  {getStatusBadge(app.status).text}
                </span>
              </div>
              {user.role === 'employee' && app.status === 'pending' && (
                <div className="app-actions">
                  <button onClick={() => handleReview(app.id, 'approved')}>Одобрить</button>
                  <button onClick={() => handleReview(app.id, 'rejected')}>Отклонить</button>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}
