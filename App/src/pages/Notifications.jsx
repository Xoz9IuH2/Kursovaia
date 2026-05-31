import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';

import './Notifications.css';

export default function Notifications() {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    loadNotifications();
  }, [user]);

  const loadNotifications = async () => {
    const res = await fetch(`/api/notifications?user_id=${user.id}`);
    const data = await res.json();
    setNotifications(data.items || data || []);
  };

  return (
    <div className="notifications-page">
      <h2>Уведомления</h2>
      <div className="notifications-list">
        {notifications.length === 0 ? (
          <p className="empty">Уведомлений пока нет</p>
        ) : (
          notifications.map((n) => (
            <div key={n.id} className={`notification-card ${n.isRead ? '' : 'unread'}`}>
              <h3>{n.title}</h3>
              <p>{n.message}</p>
              <p className="date">{new Date(n.createdAt).toLocaleString()}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
}