import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';

export default function AdminPanel() {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [form, setForm] = useState({ login: '', password: '', name: '', role: 'employee', email: '' });
  const [editingUser, setEditingUser] = useState(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user?.role !== 'admin') return;
    loadUsers();
  }, [user]);

  const loadUsers = async () => {
    setLoading(true);
    const res = await fetch('/api/users');
    const data = await res.json();
    setUsers(data);
    setLoading(false);
  };

  const createUser = async (e) => {
    e.preventDefault();
    const res = await fetch('/api/users', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form)
    });
    if (res.ok) {
      await loadUsers();
      setForm({ login: '', password: '', name: '', role: 'employee', email: '' });
    } else {
      const err = await res.json();
      alert('Ошибка: ' + (err?.error ?? 'неизвестная'));
    }
  };

  const startEdit = (u) => {
    setEditingUser(u);
    setForm({ login: u.login, password: '', name: u.name, role: u.role, email: u.email || '' });
  };

  const updateUser = async (e) => {
    e.preventDefault();
    if (!editingUser) return;
    const payload = { login: form.login, name: form.name, role: form.role, email: form.email };
    if (form.password) payload.password = form.password;
    const res = await fetch(`/api/users/${editingUser.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (res.ok) {
      await loadUsers();
      setEditingUser(null);
      setForm({ login: '', password: '', name: '', role: 'employee', email: '' });
    } else {
      const err = await res.json();
      alert('Ошибка обновления: ' + (err?.error ?? 'неизвестная'));
    }
  };

  const deleteUser = async (id) => {
    if (!confirm('Удалить пользователя?')) return;
    await fetch(`/api/users/${id}`, { method: 'DELETE' });
    await loadUsers();
  };

  if (!user || user.role !== 'admin') {
    return <div>Доступ запрещён. Страница администратора доступна только суперпользователю.</div>;
  }

  return (
    <div className="admin-panel">
      <h2>Администратор</h2>
      <section className="admin-section">
        <h3>Управление пользователями</h3>
        <form onSubmit={editingUser ? updateUser : createUser} className="form">
          <div className="form-row">
            <div className="form-group"><label>Логин</label><input value={form.login} onChange={e=>setForm({...form, login:e.target.value})} required/></div>
            <div className="form-group"><label>Пароль</label><input type="password" value={form.password} onChange={e=>setForm({...form, password:e.target.value})} required={!editingUser} /></div>
          </div>
          <div className="form-row">
            <div className="form-group"><label>Имя</label><input value={form.name} onChange={e=>setForm({...form, name:e.target.value})} required/></div>
            <div className="form-group"><label>Роль</label>
              <select value={form.role} onChange={e=>setForm({...form, role:e.target.value})}>
                <option value="employee">Сотрудник</option>
                <option value="admin">Администратор</option>
              </select>
            </div>
          </div>
          <div className="form-group"><label>Email</label><input value={form.email} onChange={e=>setForm({...form, email:e.target.value})}/></div>
          <button type="submit" className="btn">{editingUser ? 'Сохранить' : 'Создать'}</button>
        </form>
        <div className="users-list">
          {loading ? (<div>Загрузка...</div>) : (
            users.map(u => (
              <div key={u.id} className="user-card">
                <div>
                  <strong>{u.name}</strong> <span className="badge">{u.role}</span>
                  <div>Login: {u.login}</div>
                  <div>Email: {u.email}</div>
                </div>
                {u.role !== 'admin' && (
                  <div className="user-actions">
                    <button className="edit-btn" onClick={()=>startEdit(u)}>Редактировать</button>
                    <button className="delete-btn" onClick={()=>deleteUser(u.id)}>Удалить</button>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}
