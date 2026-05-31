import { useAuth } from '../context/AuthContext';
import { useState, useEffect } from 'react';
import { apiFetch } from '../utils/api';

const getRoleLabel = (role) => {
    const roles = { employee: 'Сотрудник', citizen: 'Гражданин', admin: 'Админ' };
    return roles[role] || role;
};

export default function Home() {
  const { user } = useAuth();
  const [stats, setStats] = useState({});

  useEffect(() => {
    if (user?.role === 'employee') {
      loadStats();
    }
  }, [user]);

  const loadStats = async () => {
    try {
      const data = await apiFetch('/statistics');
      setStats(data);
    } catch (e) {
      console.log(e);
    }
  };

  const getStats = () => {
    if (user.role === 'employee') {
      return (
        <div className="stats-3col">
          <div className="stat-card blue">
            <div className="stat-num">{stats.totalFiles || 0}</div>
            <div className="stat-name">Личных дел</div>
          </div>
          <div className="stat-card orange">
            <div className="stat-num">{stats.pendingSummons || 0}</div>
            <div className="stat-name">Повесток ожидает</div>
          </div>
          <div className="stat-card green">
            <div className="stat-num">{stats.fulfilledSummons || 0}</div>
            <div className="stat-name">Исполнено</div>
          </div>
          <div className="stat-card red">
            <div className="stat-num">{stats.missedSummons || 0}</div>
            <div className="stat-name">Неявок</div>
          </div>
        </div>
      );
    }
    return null;
  };

  return (
    <div className="home">
      <div className="hero">
        <h1>VOENCOM</h1>
        <p>Автоматизированная система военного комиссариата</p>
      </div>
      <div className="home- content">
        <div className="welcome-block">
          <h2>Добро пожаловать, {user?.name}</h2>
          <p>Ваш уровень доступа: <strong>{getRoleLabel(user?.role)}</strong></p>
          <p className="hint">Используйте меню выше для навигации</p>
        </div>
        {getStats()}
      </div>
    </div>
  );
}