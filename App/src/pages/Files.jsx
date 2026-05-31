import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';
import './Files.css';

const FITNESS_CATEGORIES = ['А-1', 'А-2', 'А-3', 'Б-1', 'Б-2', 'Б-3', 'Б-4', 'В', 'Г', 'Д'];

export default function Files() {
  const { user } = useAuth();
  const [files, setFiles] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [errors, setErrors] = useState({});
  const [serverError, setServerError] = useState('');
  const [selectedFileId, setSelectedFileId] = useState(null);
  const [formData, setFormData] = useState({
    last_name: '',
    first_name: '',
    middle_name: '',
    birth_date: '',
    passport_series: '',
    passport_number: '',
    passport_issue_authority: '',
    passport_issue_date: '',
    birth_place: '',
    address: '',
    phone: '',
    education: '',
    work_place: '',
    fitness_category: '',
    military_ticket_series: '',
    military_ticket_number: ''
  });

  useEffect(() => {
    loadFiles();
  }, []);

  const loadFiles = async () => {
    const data = await apiFetch('/PersonalFile');
    setFiles(data.items || []);
  };

  const validateForm = () => {
    const newErrors = {};
    const currentYear = new Date().getFullYear();
    
    if (!formData.last_name.trim()) {
      newErrors.last_name = 'Фамилия обязательна';
    }
    
    if (!formData.first_name.trim()) {
      newErrors.first_name = 'Имя обязательно';
    }
    
    if (formData.birth_date) {
      const birthYear = new Date(formData.birth_date).getFullYear();
      if (currentYear - birthYear < 17) {
        newErrors.birth_date = 'Призывнику должно быть 17 лет или исполниться в этом году';
      }
    }
    
    if (formData.passport_series && !/^\d{4}$/.test(formData.passport_series)) {
      newErrors.passport_series = 'Серия паспорта - 4 цифры';
    }
    
    if (formData.passport_number && !/^\d{6}$/.test(formData.passport_number)) {
      newErrors.passport_number = 'Номер паспорта - 6 цифр';
    }
    
    if (formData.phone && !/^\+8\d{10}$/.test(formData.phone)) {
      newErrors.phone = 'Телефон: +8 и 10 цифр после (например +89001234567)';
    }
    
    if (formData.military_ticket_series && !/^[А-Яа-яA-Z]{2}$/.test(formData.military_ticket_series)) {
      newErrors.military_ticket_series = 'Серия - 2 русские буквы';
    }
    
    if (formData.military_ticket_number && !/^\d{6}$/.test(formData.military_ticket_number)) {
      newErrors.military_ticket_number = 'Номер - 6 цифр';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const toCamelCase = (obj) => {
    const result = {};
    for (const key in obj) {
      const camelKey = key.replace(/_([a-z])/g, (_, letter) => letter.toUpperCase());
      result[camelKey] = obj[key];
    }
    return result;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setServerError('');
    if (!validateForm()) return;
    
    const data = toCamelCase(formData);
    data.gender = 'male';
    data.birthPlace = formData.birth_place || '';
    data.passportIssueAuthority = formData.passport_issue_authority || '';
    data.passportIssueDate = formData.passport_issue_date || new Date().toISOString().split('T')[0];
    data.education = formData.education || '';
    data.workPlace = formData.work_place || '';
    data.militaryRank = 'призывник';
    data.fitnessCategory = formData.fitness_category || '';
    data.militaryTicketSeries = formData.military_ticket_series || '';
    data.militaryTicketNumber = formData.military_ticket_number || '';
    
    try {
      if (editingId) {
        await apiFetch(`/PersonalFile/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(data)
        });
      } else {
        await apiFetch('/PersonalFile', {
          method: 'POST',
          body: JSON.stringify(data)
        });
      }
      setShowForm(false);
      setEditingId(null);
      resetForm();
      loadFiles();
    } catch (err) {
      setServerError(err.message || 'Ошибка при сохранении');
    }
  };

  const toSnakeCase = (obj) => {
    const result = {};
    for (const key in obj) {
      let value = obj[key];
      if (value && typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
        value = value.split('T')[0];
      }
      if (value === null || value === undefined) {
        value = '';
      }
      const snakeKey = key.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`);
      result[snakeKey] = value;
    }
    return result;
  };

  const handleEdit = (file) => {
    resetForm();
    setFormData(prev => ({ ...prev, ...toSnakeCase(file) }));
    setEditingId(file.id);
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (confirm('Удалить дело?')) {
      await apiFetch(`/PersonalFile/${id}`, { method: 'DELETE' });
      loadFiles();
    }
  };

  const handleArchive = async (id) => {
    if (confirm('Заархивировать дело? Оно будет удалено через 5 дней.')) {
      try {
        await apiFetch(`/PersonalFile/${id}/archive`, { method: 'POST' });
        loadFiles();
      } catch (err) {
        alert(err.message || 'Ошибка при архивировании');
      }
    }
  };

  const resetForm = () => {
    setFormData({
      last_name: '',
      first_name: '',
      middle_name: '',
      birth_date: '',
      passport_series: '',
      passport_number: '',
      passport_issue_authority: '',
      passport_issue_date: '',
      birth_place: '',
      address: '',
      phone: '',
      education: '',
      work_place: '',
      fitness_category: '',
      military_ticket_series: '',
      military_ticket_number: ''
    });
    setErrors({});
    setServerError('');
  };

  const filteredFiles = files.filter(f => 
    `${f.lastName || f.last_name || ''} ${f.firstName || f.first_name || ''} ${f.patronymic || f.middle_name || ''}`.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="files-page">
      <div className="page-header">
        <h2>Личные дела</h2>
        <div className="header-actions">
          <input
            type="text"
            placeholder="Поиск..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={() => { resetForm(); setEditingId(null); setShowForm(!showForm); }} className="btn">
            {showForm ? 'Отмена' : 'Создать дело'}
          </button>
        </div>
      </div>

      {showForm && (
        <>
        {serverError && <div className="error-banner">{serverError}</div>}
        <form onSubmit={handleSubmit} className="form">
          <div className="form-row">
            <div className="form-group">
              <label>Фамилия <span className="required">*</span></label>
              <input 
                value={formData.last_name} 
                onChange={(e) => setFormData({...formData, last_name: e.target.value})} 
                className={errors.last_name ? 'error' : ''}
              />
              {errors.last_name && <span className="error-text">{errors.last_name}</span>}
            </div>
            <div className="form-group">
              <label>Имя <span className="required">*</span></label>
              <input 
                value={formData.first_name} 
                onChange={(e) => setFormData({...formData, first_name: e.target.value})}
                className={errors.first_name ? 'error' : ''}
              />
              {errors.first_name && <span className="error-text">{errors.first_name}</span>}
            </div>
            <div className="form-group">
              <label>Отчество</label>
              <input value={formData.middle_name} onChange={(e) => setFormData({...formData, middle_name: e.target.value})} />
            </div>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Дата рождения</label>
              <input 
                type="date" 
                value={formData.birth_date} 
                onChange={(e) => setFormData({...formData, birth_date: e.target.value})}
                className={errors.birth_date ? 'error' : ''}
              />
              {errors.birth_date && <span className="error-text">{errors.birth_date}</span>}
            </div>
            <div className="form-group">
              <label>Паспорт (серия)</label>
              <input 
                value={formData.passport_series} 
                onChange={(e) => setFormData({...formData, passport_series: e.target.value})}
                placeholder="1234"
                maxLength={4}
                className={errors.passport_series ? 'error' : ''}
              />
              {errors.passport_series && <span className="error-text">{errors.passport_series}</span>}
            </div>
            <div className="form-group">
              <label>Паспорт (номер)</label>
              <input 
                value={formData.passport_number} 
                onChange={(e) => setFormData({...formData, passport_number: e.target.value})}
                placeholder="123456"
                maxLength={6}
                className={errors.passport_number ? 'error' : ''}
              />
              {errors.passport_number && <span className="error-text">{errors.passport_number}</span>}
            </div>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Кем выдан</label>
              <input value={formData.passport_issue_authority} onChange={(e) => setFormData({...formData, passport_issue_authority: e.target.value})} />
            </div>
            <div className="form-group">
              <label>Дата выдачи</label>
              <input type="date" value={formData.passport_issue_date} onChange={(e) => setFormData({...formData, passport_issue_date: e.target.value})} />
            </div>
          </div>
          <div className="form-group">
            <label>Место рождения</label>
            <input 
              value={formData.birth_place} 
              onChange={(e) => setFormData({...formData, birth_place: e.target.value})}
              placeholder="г. Москва, ул. Ленина, д. 10"
            />
          </div>
          <div className="form-group">
            <label>Адрес прописки</label>
            <input 
              value={formData.address} 
              onChange={(e) => setFormData({...formData, address: e.target.value})}
              placeholder="г. Москва, ул. Ленина, д. 10, кв. 5"
            />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Телефон</label>
              <input 
                value={formData.phone} 
                onChange={(e) => setFormData({...formData, phone: e.target.value})}
                placeholder="+89001234567"
                className={errors.phone ? 'error' : ''}
              />
              {errors.phone && <span className="error-text">{errors.phone}</span>}
            </div>
            <div className="form-group">
              <label>Образование</label>
              <input value={formData.education} onChange={(e) => setFormData({...formData, education: e.target.value})} />
            </div>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Место работы</label>
              <input value={formData.work_place} onChange={(e) => setFormData({...formData, work_place: e.target.value})} />
            </div>
            <div className="form-group">
              <label>Категория годности</label>
              <select 
                value={formData.fitness_category} 
                onChange={(e) => setFormData({...formData, fitness_category: e.target.value})}
              >
                <option value="">Выберите категорию</option>
                {FITNESS_CATEGORIES.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>
          </div>
          <h3>Военный билет</h3>
          <div className="form-row">
            <div className="form-group">
              <label>Серия</label>
              <input 
                value={formData.military_ticket_series} 
                onChange={(e) => setFormData({...formData, military_ticket_series: e.target.value.toUpperCase()})}
                placeholder="АВ"
                maxLength={2}
              />
            </div>
            <div className="form-group">
              <label>Номер</label>
              <input 
                value={formData.military_ticket_number} 
                onChange={(e) => setFormData({...formData, military_ticket_number: e.target.value})}
                placeholder="123456"
                maxLength={6}
              />
            </div>
          </div>
          <button type="submit" className="btn">{editingId ? 'Сохранить' : 'Создать'}</button>
        </form>
        </>
      )}

      <div className="files-grid">
          {filteredFiles.length === 0 ? (
          <p className="empty">Дел пока нет</p>
        ) : (
          filteredFiles.map((f) => (
              <div key={f.id} className={`file-card ${f.status === 'archived' ? 'archived' : ''}`}>
                <div className="file-header">
                  <h3>{f.lastName} {f.firstName} {f.patronymic || ''}</h3>
                  {f.status === 'archived' && <span className="archive-badge">В архиве</span>}
                </div>
                <p><strong>Дата рождения:</strong> {f.birthDate ? new Date(f.birthDate).toLocaleDateString('ru-RU') : '-'}</p>
                <p><strong>Паспорт:</strong> {f.passportSeries} {f.passportNumber}</p>
                <p><strong>Адрес прописки:</strong> {f.address || '-'}</p>
                <p><strong>Телефон:</strong> {f.phone || '-'}</p>
                <p><strong>Категория годности:</strong> {f.fitnessCategory || '-'}</p>
                {f.militaryTicketSeries || f.militaryTicketNumber ? (
                  <p><strong>Военный билет:</strong> {f.militaryTicketSeries} №{f.militaryTicketNumber}</p>
                ) : null}

                <div className="file-actions">
                  {f.status !== 'archived' && (
                    <>
                      <button onClick={() => handleEdit(f)}>Редактировать</button>
                      <button onClick={() => handleArchive(f.id)} className="archive-btn">В архив</button>
                    </>
                  )}
                  {user?.role === 'employee' && (
                    <button onClick={() => handleDelete(f.id)} className="delete-btn">Удалить</button>
                  )}
                </div>
              </div>
          ))
        )}
      </div>
    </div>
  );
}