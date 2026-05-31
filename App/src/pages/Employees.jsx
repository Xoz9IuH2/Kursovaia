import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import './Employees.css';

export default function Employees() {
  const [users, setUsers] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [tab, setTab] = useState('employees');
  const [formData, setFormData] = useState({ login: '', password: '', name: '', email: '' });

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    const res = await fetch('/api/users');
    setUsers(await res.json());
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const body = { ...formData, role: 'employee' };
    const res = await fetch('/api/users', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    const data = await res.json();
    if (res.ok) {
      alert('Сотрудник создан!');
    } else {
      alert(data.message || 'Ошибка создания');
    }
    setShowForm(false);
    setFormData({ login: '', password: '', name: '', email: '' });
    loadUsers();
  };

  const handleDelete = async (id) => {
    if (confirm('Удалить сотрудника?')) {
      await fetch(`/api/users/${id}`, { method: 'DELETE' });
      loadUsers();
    }
  };

  const getRoleBadge = (role) => {
    const badges = {
      employee: { text: 'Сотрудник', class: 'employee' },
      admin: { text: 'Админ', class: 'admin' },
      citizen: { text: 'Призывник', class: 'citizen' }
    };
    return badges[role] || { text: role, class: 'citizen' };
  };

  const employees = users.filter(u => u.role === 'employee' || u.role === 'admin');
  const citizens = users.filter(u => u.role === 'citizen');

  return (
    <div className="employees-page">
      <div className="page-header">
        <h2>Пользователи</h2>
      </div>

      <div className="tabs">
        <button className={`tab ${tab === 'employees' ? 'active' : ''}`} onClick={() => setTab('employees')}>Сотрудники ({employees.length})</button>
        <button className={`tab ${tab === 'citizens' ? 'active' : ''}`} onClick={() => setTab('citizens')}>Пользователи ({citizens.length})</button>
      </div>

      {tab === 'employees' && (
        <>
          <div className="section-header">
            <button onClick={() => setShowForm(!showForm)} className="btn add-btn">
              {showForm ? 'Отмена' : '+ Добавить'}
            </button>
          </div>

          {showForm && (
            <form onSubmit={handleSubmit} className="form">
              <div className="form-row">
                <div className="form-group">
                  <label>Логин</label>
                  <input value={formData.login} onChange={(e) => setFormData({...formData, login: e.target.value})} required />
                </div>
                <div className="form-group">
                  <label>Пароль</label>
                  <input type="password" value={formData.password} onChange={(e) => setFormData({...formData, password: e.target.value})} required />
                </div>
              </div>
              <div className="form-group">
                <label>Имя</label>
                <input value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value})} required />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value})} />
              </div>
              <button type="submit" className="btn">Создать</button>
            </form>
          )}

          <div className="filter-bar">
            <input className="search-input" placeholder="Поиск сотрудника..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
          </div>
          <div className="employees-list">
            {(() => {
              const filtered = employees.filter(e => {
                if (!searchQuery) return true;
                const name = `${e.name || ''} ${e.login || ''}`.toLowerCase();
                return name.includes(searchQuery.toLowerCase());
              });
              if (filtered.length === 0) return <p className="empty">{searchQuery ? 'Ничего не найдено' : 'Сотрудников пока нет'}</p>;
              return filtered.map(emp => (
                <div key={emp.id} className="employee-card">
                  <div className="emp-info">
                    <h3>{emp.name}</h3>
                    <p>Логин: {emp.login}</p>
                    <p>Email: {emp.email}</p>
                  </div>
                  <span className={`badge ${getRoleBadge(emp.role).class}`}>
                    {getRoleBadge(emp.role).text}
                  </span>
                  {emp.role !== 'admin' && (
                    <button onClick={() => handleDelete(emp.id)} className="delete-btn">Удалить</button>
                  )}
                </div>
              ));
            })()}
          </div>
        </>
      )}

      {tab === 'citizens' && (
        <>
          <div className="filter-bar">
            <input className="search-input" placeholder="Поиск пользователя..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
          </div>
          <div className="employees-list">
            {(() => {
              const filtered = citizens.filter(u => {
                if (!searchQuery) return true;
                const name = `${u.name || ''} ${u.login || ''} ${u.email || ''}`.toLowerCase();
                return name.includes(searchQuery.toLowerCase());
              });
              if (filtered.length === 0) return <p className="empty">{searchQuery ? 'Ничего не найдено' : 'Пользователей пока нет'}</p>;
              return filtered.map(u => (
                <div key={u.id} className="employee-card">
                  <div className="emp-info">
                    <h3>{u.name}</h3>
                    <p>Логин: {u.login}</p>
                    {u.email && <p>Email: {u.email}</p>}
                    {u.phone && <p>Телефон: {u.phone}</p>}
                  </div>
                  <span className={`badge ${getRoleBadge(u.role).class}`}>
                    {getRoleBadge(u.role).text}
                  </span>
                </div>
              ));
            })()}
          </div>
        </>
      )}
    </div>
  );
}
