import { useState, useEffect } from 'react';
import './Statistics.css';

export default function Statistics() {
  const [stats, setStats] = useState(null);

  useEffect(() => {
    loadStats();
  }, []);

  const loadStats = async () => {
    const res = await fetch('/api/statistics');
    setStats(await res.json());
  };

  if (!stats) return <div>Загрузка...</div>;

  return (
    <div className="statistics-page">
      <h2>Статистика</h2>
      <div className="stats-grid">
        <div className="stat-card">
          <h3>Личных дел</h3>
          <p className="value">{stats.totalFiles}</p>
        </div>
        <div className="stat-card">
          <h3>Всего повесток</h3>
          <p className="value">{stats.totalSummons}</p>
        </div>
        <div className="stat-card">
          <h3>Ожидают</h3>
          <p className="value pending">{stats.pendingSummons}</p>
        </div>
        <div className="stat-card">
          <h3>Исполнено</h3>
          <p className="value fulfilled">{stats.fulfilledSummons}</p>
        </div>
        <div className="stat-card">
          <h3>Не явились</h3>
          <p className="value missed">{stats.missedSummons}</p>
        </div>
        <div className="stat-card">
          <h3>Новых заявлений</h3>
          <p className="value">{stats.pendingApplications}</p>
        </div>
      </div>
    </div>
  );
}