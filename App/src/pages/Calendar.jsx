import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';
import './Calendar.css';

const EVENT_TYPES = [
  { 
    title: 'Явка на медосвидетельствование', 
    description: 'Медицинское освидетельствование призывников для определения годности к военной службе.' 
  },
  { 
    title: 'Заседание призывной комиссии', 
    description: 'Заседание призывной комиссии по рассмотрению дел призывников.' 
  },
  { 
    title: 'Постановка на учёт', 
    description: 'Постановка на воинский учёт граждан, подлежащих призыву.' 
  },
  { 
    title: 'Сверка учёта', 
    description: 'Сверка документов воинского учёта с гражданами.' 
  },
  { 
    title: 'Уточнение документов', 
    description: 'Уточнение документов воинского учёта: семейное положение, место работы, учёбы, проживания.' 
  },
  { 
    title: 'Инструктаж', 
    description: 'Инструктаж по воинскому учёту для сотрудников.' 
  },
  { 
    title: 'День призывника', 
    description: 'День призывника - знакомство с военной службой.' 
  },
  { 
    title: 'Зарница / Юнармия', 
    description: 'Военно-патриотическое мероприятие для youth организации.' 
  },
  { 
    title: 'Сборы', 
    description: 'Военные сборы для военнообязанных.' 
  }
];

export default function Calendar() {
  const { user } = useAuth();
  const [events, setEvents] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [typeFilter, setTypeFilter] = useState('');
  const [formData, setFormData] = useState({ 
    title: '', 
    description: '', 
    eventDate: '', 
    startTime: '', 
    endTime: '',
    location: '',
    eventType: ''
  });

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    const data = await apiFetch('/calendar');
    setEvents(data || []);
  };

  const handleTypeChange = (e) => {
    const selectedType = EVENT_TYPES.find(t => t.title === e.target.value);
    setFormData({
      ...formData,
      eventType: e.target.value,
      title: e.target.value,
      description: selectedType?.description || ''
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    await apiFetch('/calendar', {
      method: 'POST',
      body: JSON.stringify({ 
        title: formData.title,
        description: formData.description,
        eventDate: formData.eventDate,
        startTime: formData.startTime,
        endTime: formData.endTime,
        location: formData.location,
        eventType: formData.eventType
      })
    });
    setShowForm(false);
    setFormData({ title: '', description: '', eventDate: '', startTime: '', endTime: '', location: '', eventType: '' });
    loadEvents();
  };

  return (
    <div className="calendar-page">
      <div className="page-header">
        <h2>Календарь мероприятий</h2>
        <button onClick={() => setShowForm(!showForm)} className="btn">
          {showForm ? 'Отмена' : 'Добавить'}
        </button>
      </div>
      <div className="filter-bar">
        <select className="filter-select" value={typeFilter} onChange={e => setTypeFilter(e.target.value)}>
          <option value="">Все типы</option>
          {EVENT_TYPES.map(t => <option key={t.title} value={t.title}>{t.title}</option>)}
        </select>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label>Тип мероприятия</label>
            <select value={formData.eventType} onChange={handleTypeChange}>
              <option value="">Выберите тип</option>
              {EVENT_TYPES.map((type, index) => (
                <option key={index} value={type.title}>{type.title}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Название</label>
            <input 
              value={formData.title} 
              onChange={(e) => setFormData({...formData, title: e.target.value})} 
              required 
            />
          </div>
          <div className="form-group">
            <label>Описание</label>
            <textarea 
              value={formData.description} 
              onChange={(e) => setFormData({...formData, description: e.target.value})}
            />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Дата</label>
              <input 
                type="date" 
                min={new Date().toISOString().split('T')[0]}
                value={formData.eventDate} 
                onChange={(e) => setFormData({...formData, eventDate: e.target.value})} 
                required 
              />
            </div>
            <div className="form-group">
              <label>Время с</label>
              <input 
                type="time" 
                value={formData.startTime} 
                onChange={(e) => setFormData({...formData, startTime: e.target.value})} 
                required 
              />
            </div>
            <div className="form-group">
              <label>Время до</label>
              <input 
                type="time" 
                value={formData.endTime} 
                onChange={(e) => setFormData({...formData, endTime: e.target.value})} 
              />
            </div>
          </div>
          <div className="form-group">
            <label>Место</label>
            <input 
              value={formData.location} 
              onChange={(e) => setFormData({...formData, location: e.target.value})}
            />
          </div>
          <button type="submit" className="btn">Добавить</button>
        </form>
      )}

      <div className="events-list">
        {events.filter(e => !typeFilter || e.eventType === typeFilter || e.title === typeFilter).length === 0 ? (
          <p className="empty">Мероприятий пока нет</p>
        ) : (
          events.filter(e => !typeFilter || e.eventType === typeFilter || e.title === typeFilter).sort((a, b) => (a.eventDate || '') > (b.eventDate || '') ? 1 : -1).map((e) => (
            <div key={e.id} className="event-card">
              <div className="event-info">
                <h3>{e.title}</h3>
                <p className="date">{e.eventDate ? new Date(e.eventDate).toLocaleDateString('ru-RU') : ''} с {e.startTime} до {e.endTime || '-'}</p>
                <p className="location">{e.location}</p>
                <p className="description">{e.description}</p>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}