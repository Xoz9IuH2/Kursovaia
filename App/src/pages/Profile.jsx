import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiFetch } from '../utils/api';

export default function Profile() {
  const { user } = useAuth();
  const [me, setMe] = useState(null);
  useEffect(() => {
    apiFetch('/auth/me')
      .then(data => setMe(data));
  }, []);

  if (!me) return <div>Загрузка профиля...</div>;
  return (
    <div className="profile-page">
      <h2>Профиль</h2>
      <ul>
        <li>Имя: {me.name}</li>
        <li>Логин: {me.login}</li>
        <li>Роль: {me.role}</li>
        <li>Email: {me.email}</li>
      </ul>
    </div>
  );
}
