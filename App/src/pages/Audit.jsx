import { useState, useEffect } from 'react';
import './Audit.css';

function downloadCSV(data, filename) {
  const sep = ';';
  const keys = Object.keys(data[0]);
  const rows = data.map(r => keys.map(k => `"${(r[k] ?? '').toString().replace(/"/g, '""')}"`).join(sep));
  const csv = [keys.join(sep), ...rows].join('\r\n');
  const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
}

export default function Audit() {
  const [logs, setLogs] = useState([]);

  useEffect(() => {
    loadLogs();
  }, []);

  const loadLogs = async () => {
    const res = await fetch('/api/audit');
    setLogs(await res.json());
  };

  const handleDownload = () => {
    if (logs.length === 0) return;
    downloadCSV(logs.map(l => ({
      'Дата': new Date(l.created_at).toLocaleString('ru-RU'),
      'Пользователь': l.user_name ?? '',
      'Роль': l.user_role ?? '',
      'Действие': l.action,
      'Таблица': l.table_name ?? '',
      'Детали': l.details ?? ''
    })), 'audit.csv');
  };

  return (
    <div className="audit-page">
      <div className="page-header">
        <h2>Журнал аудита</h2>
        {logs.length > 0 && <button className="btn" onClick={handleDownload}>Скачать CSV</button>}
      </div>
      <div className="audit-list">
        {logs.length === 0 ? (
          <p className="empty">Записей пока нет</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Дата</th>
                <th>Пользователь</th>
                <th>Роль</th>
                <th>Действие</th>
                <th>Таблица</th>
                <th>Детали</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr key={log.id}>
                  <td>{new Date(log.created_at).toLocaleString()}</td>
                  <td>{log.user_name}</td>
                  <td>{log.user_role}</td>
                  <td>{log.action}</td>
                  <td>{log.table_name}</td>
                  <td>{log.details}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}