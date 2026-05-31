import { useState, useEffect } from 'react';
import './Reports.css';

function downloadCSV(rows, filename) {
  if (rows.length === 0) return;
  const sep = ';';
  const keys = Object.keys(rows[0]);
  const csv = [keys.join(sep), ...rows.map(r => keys.map(k => `"${(r[k] ?? '').toString().replace(/"/g, '""')}"`).join(sep))].join('\r\n');
  const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
}

export default function Reports() {
  const [stats, setStats] = useState(null);

  useEffect(() => {
    loadStats();
  }, []);

  const loadStats = async () => {
    const res = await fetch('/api/statistics');
    setStats(await res.json());
  };

  const downloadReport = () => {
    if (!stats) return;
    const date = new Date().toLocaleDateString('ru-RU');
    downloadCSV([
      { 'Дата': date, 'Показатель': 'Личные дела', 'Всего': stats.totalFiles ?? 0, 'В работе': '-', 'Исполнено': '-', 'Не явились': '-', 'Новых': '-' },
      { 'Дата': date, 'Показатель': 'Повестки', 'Всего': stats.totalSummons ?? 0, 'В работе': stats.pendingSummons ?? 0, 'Исполнено': stats.fulfilledSummons ?? 0, 'Не явились': stats.missedSummons ?? 0, 'Новых': '-' },
      { 'Дата': date, 'Показатель': 'Заявления', 'Всего': '-', 'В работе': '-', 'Исполнено': '-', 'Не явились': '-', 'Новых': stats.pendingApplications ?? 0 },
    ], `otchyot_voenkom_${new Date().toISOString().split('T')[0]}.csv`);
  };

  if (!stats) return <div className="loading">Загрузка...</div>;

  return (
    <div className="reports-page">
      <h2>Отчёты</h2>

      <div className="report-card">
        <h3>📋 Отчёт по деятельности военкомата</h3>
        <p>Сводка по личным делам, повесткам и заявлениям</p>
        <button onClick={downloadReport} className="btn">Скачать Excel</button>
      </div>

      <div className="report-preview">
        <h3>Предпросмотр</h3>
        <table>
          <thead><tr><th>Показатель</th><th>Значение</th></tr></thead>
          <tbody>
            <tr><td>Личные дела</td><td>{stats.totalFiles}</td></tr>
            <tr><td>Всего повесток</td><td>{stats.totalSummons}</td></tr>
            <tr><td>Ожидают</td><td>{stats.pendingSummons}</td></tr>
            <tr><td>Исполнено</td><td>{stats.fulfilledSummons}</td></tr>
            <tr><td>Не явились</td><td>{stats.missedSummons}</td></tr>
            <tr><td>Новых заявлений</td><td>{stats.pendingApplications}</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}