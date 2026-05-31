import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';
import './Appointments.css';

export default function Appointments() {
  const { user } = useAuth();
  const [appointments, setAppointments] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [staff, setStaff] = useState([]);
  const [dateFilter, setDateFilter] = useState('');
  const [formData, setFormData] = useState({
    user_id: '',
    appointment_date: '',
    appointment_time: '',
    purpose: '',
    notes: ''
  });

  useEffect(() => {
    loadAppointments();
    loadStaff();
  }, [user]);

  const loadAppointments = async () => {
    const data = await apiFetch('/appointments');
    setAppointments(data.items || data || []);
  };

  const loadStaff = async () => {
    const data = await apiFetch('/users');
    setStaff((data || []).filter(u => u.role !== 'citizen'));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    await apiFetch('/appointments', {
      method: 'POST',
      body: JSON.stringify(formData)
    });
    setShowForm(false);
    loadAppointments();
  };

  const formatDate = (date) => {
    if (!date) return '';
    try { return new Date(date).toISOString().split('T')[0]; }
    catch { return date; }
  };

  const getStatusBadge = (status) => {
    const badges = {
      scheduled: { text: 'Запланировано', class: 'scheduled' },
      completed: { text: 'Завершено', class: 'completed' },
      cancelled: { text: 'Отменено', class: 'cancelled' }
    };
    return badges[status] || badges.scheduled;
  };

  return (
    <div className="appointments-page">
      <div className="page-header">
        <h2>Запись на приём</h2>
        {user.role === 'citizen' && (
          <button onClick={() => setShowForm(!showForm)} className="btn">
            {showForm ? 'Отмена' : 'Записаться'}
          </button>
        )}
      </div>
      <div className="filter-bar">
        <input className="search-input" type="date" value={dateFilter} onChange={e => setDateFilter(e.target.value)} />
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label>К специалисту</label>
            <select
              value={formData.user_id}
              onChange={(e) => setFormData({ ...formData, user_id: e.target.value })}
              required
            >
              <option value="">Выберите специалиста</option>
              {staff.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Дата</label>
              <input
                type="date"
                value={formData.appointment_date}
                onChange={(e) => setFormData({ ...formData, appointment_date: e.target.value })}
                required
              />
            </div>
            <div className="form-group">
              <label>Время</label>
              <input
                type="time"
                value={formData.appointment_time}
                onChange={(e) => setFormData({ ...formData, appointment_time: e.target.value })}
                required
              />
            </div>
          </div>
          <div className="form-group">
            <label>Цель визита</label>
            <input
              type="text"
              value={formData.purpose}
              onChange={(e) => setFormData({ ...formData, purpose: e.target.value })}
              required
            />
          </div>
          <div className="form-group">
            <label>Заметки</label>
            <textarea
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            />
          </div>
          <button type="submit" className="btn">Записаться</button>
        </form>
      )}

      <div className="appointments-list">
          {appointments.filter(a => !dateFilter || formatDate(a.appointmentDate) === dateFilter).length === 0 ? (
          <p className="empty">Записей пока нет</p>
        ) : (
          appointments.filter(a => !dateFilter || formatDate(a.appointmentDate) === dateFilter).map((a) => (
            <div key={a.id} className="appointment-card">
              <div className="app-info">
                <h3>{a.purpose}</h3>
                <p className="date">{formatDate(a.appointmentDate)} в {a.time}</p>
                <p className="meta">Кто прислал: {a.userName}</p>
                {a.notes && <p className="notes">{a.notes}</p>}
              </div>
              <span className={`badge ${getStatusBadge(a.status).class}`}>
                {getStatusBadge(a.status).text}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}