import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';
import './Summons.css';

const SUMMON_REASONS = [
  'Уточнение документов воинского учета: Обновление данных о семейном положении, месте работы, учебы или проживания.',
  'Прохождение медицинского освидетельствования: Определение годности к военной службе.',
  'Призыв на военную службу: Явка на призывную комиссию.',
  'Военные сборы: Вызов для прохождения сборов (для военнообязанных в запасе).'
];

export default function Summons() {
  const { user } = useAuth();
  const [summons, setSummons] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [files, setFiles] = useState([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [sortFilter, setSortFilter] = useState('newest');
  const [formData, setFormData] = useState({
    personalFileId: '',
    summonDate: '',
    summonTime: '',
    reason: '',
    location: ''
  });

  useEffect(() => {
    loadSummons();
    if (user.role === 'employee') {
      loadFiles();
    }
  }, [user]);

  const loadSummons = async () => {
    const data = await apiFetch('/summons');
    setSummons(data.items || data || []);
  };

  const loadFiles = async () => {
    const data = await apiFetch('/PersonalFile?status=active');
    setFiles(data.items || data || []);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (editingId) {
      await apiFetch(`/summons/${editingId}`, {
        method: 'PUT',
        body: JSON.stringify({ 
          personalFileId: formData.personalFileId,
          summonDate: formData.summonDate,
          time: formData.summonTime,
          reason: formData.reason,
          location: formData.location,
          status: 'pending'
        })
      });
    } else {
      await apiFetch('/summons', {
        method: 'POST',
        body: JSON.stringify({ 
          personalFileId: formData.personalFileId,
          title: formData.reason,
          description: formData.reason,
          summonDate: formData.summonDate,
          time: formData.summonTime,
          reason: formData.reason,
          location: formData.location
        })
      });
    }
    setShowForm(false);
    setEditingId(null);
    setFormData({ personalFileId: '', summonDate: '', summonTime: '', reason: '', location: '' });
    loadSummons();
  };

const handleMarkArrived = async (id) => {
    if (confirm('Отметить явку?')) {
      await apiFetch(`/summons/${id}/arrived`, { method: 'POST' });
      loadSummons();
    }
  };

  const handleDelete = async (id) => {
    if (confirm('Удалить повестку?')) {
      await apiFetch(`/summons/${id}`, { method: 'DELETE' });
      loadSummons();
    }
  };

  const handleEdit = (s) => {
    setFormData({
      personalFileId: s.personalFile?.id || s.personalFileId,
      summonDate: s.summonDate,
      summonTime: s.time || s.summonTime,
      reason: s.reason,
      location: s.location
    });
    setEditingId(s.id);
    setShowForm(true);
  };

  const filtered = summons
    .filter(s => !statusFilter || s.status === statusFilter)
    .filter(s => {
      if (!searchQuery) return true;
      const name = `${s.lastName || ''} ${s.firstName || ''} ${s.patronymic || ''}`.toLowerCase();
      return name.includes(searchQuery.toLowerCase());
    })
    .sort((a, b) => {
      const da = a.summonDate || '';
      const db = b.summonDate || '';
      if (sortFilter === 'oldest') return da > db ? 1 : -1;
      if (sortFilter === 'upcoming') {
        const now = new Date().toISOString().split('T')[0];
        const aFuture = da >= now ? 0 : 1;
        const bFuture = db >= now ? 0 : 1;
        if (aFuture !== bFuture) return aFuture - bFuture;
        return da > db ? 1 : -1;
      }
      return da < db ? 1 : -1;
    });

  const getStatusBadge = (status) => {
    const badges = {
      sent: { text: 'Отправлено', class: 'sent' },
      pending: { text: 'Ожидает', class: 'pending' },
      delivered: { text: 'Доставлено', class: 'delivered' },
      arrived: { text: 'Явился', class: 'arrived' },
      completed: { text: 'Исполнено', class: 'completed' },
      'no-show': { text: 'Неявка', class: 'missed' }
    };
    return badges[status] || badges.sent;
  };

  return (
    <div className="summons-page">
      <div className="page-header">
        <h2>Повестки</h2>
        {(user.role === 'employee') && (
          <button onClick={() => setShowForm(!showForm)} className="btn">
            {showForm ? 'Отмена' : '+ Создать'}
          </button>
        )}
      </div>
      <div className="filter-bar">
        <input className="search-input" placeholder="Поиск по ФИО..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
        <select className="filter-select" value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
          <option value="">Все статусы</option>
          <option value="pending">Ожидает</option>
          <option value="sent">Отправлено</option>
          <option value="delivered">Доставлено</option>
          <option value="arrived">Явился</option>
          <option value="completed">Исполнено</option>
          <option value="no-show">Неявка</option>
        </select>
        <select className="filter-select" value={sortFilter} onChange={e => setSortFilter(e.target.value)}>
          <option value="newest">Сначала новые</option>
          <option value="oldest">Сначала старые</option>
          <option value="upcoming">Сначала ближайшие</option>
        </select>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label>Призывник</label>
            <select
              value={formData.personalFileId}
              onChange={(e) => setFormData({ ...formData, personalFileId: e.target.value })}
              required
            >
              <option value="">Выберите призывника</option>
              {files.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.lastName} {f.firstName} {f.patronymic}
                </option>
              ))}
            </select>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Дата</label>
              <input
                type="date"
                min={new Date().toISOString().split('T')[0]}
                value={formData.summonDate}
                onChange={(e) => setFormData({ ...formData, summonDate: e.target.value })}
                required
              />
            </div>
            <div className="form-group">
              <label>Время</label>
              <input
                type="time"
                value={formData.summonTime}
                onChange={(e) => setFormData({ ...formData, summonTime: e.target.value })}
                required
              />
            </div>
          </div>
          <div className="form-group">
            <label>Причина</label>
            <select
              value={formData.reason}
              onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
              required
            >
              <option value="">Выберите причину</option>
              {SUMMON_REASONS.map((reason, index) => (
                <option key={index} value={reason}>{reason}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Место</label>
            <input
              type="text"
              value={formData.location}
              onChange={(e) => setFormData({ ...formData, location: e.target.value })}
              required
            />
          </div>
          <button type="submit" className="btn">{editingId ? 'Сохранить' : 'Создать'}</button>
        </form>
      )}

      <div className="summons-list">
        {summons.length === 0 ? (
          <p className="empty">Повесток пока нет</p>
        ) : (
          filtered.map((s) => (
            <div key={s.id} className="summons-card">
              <div className="summons-info">
                <h3>{s.lastName} {s.firstName} {s.patronymic}</h3>
                <p className="date">{s.summonDate ? new Date(s.summonDate).toLocaleDateString('ru-RU') : ''} в {s.time}</p>
                <p className="reason">{s.reason?.split(':')[0]}</p>
                <p className="location">{s.location}</p>
              </div>
              <div className="summons-status">
                <span className={`badge ${getStatusBadge(s.status).class}`}>
                  {getStatusBadge(s.status).text}
                </span>
              </div>
        {(user.role === 'employee') && (
                <div className="summons-actions">
                  {s.status !== 'arrived' && s.status !== 'completed' && (
                    <button onClick={() => handleMarkArrived(s.id)} className="done-btn">Явился</button>
                  )}
                  {s.status !== 'no-show' && (
                    <button onClick={() => handleDelete(s.id)} className="delete-btn">Удал.</button>
                  )}
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}