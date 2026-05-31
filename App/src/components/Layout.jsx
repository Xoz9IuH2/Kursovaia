import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useState, useEffect } from 'react';
import './Layout.css';

const navConfig = [
  { section: 'Основное', links: [
    { to: '/register', label: 'Регистрация', icon: '\u2795' },
    { to: '/files', label: 'Личные дела', icon: '\uD83D\uDCC1' },
  ]},
  { section: 'Работа', links: [
    { to: '/summons', label: 'Повестки', icon: '\uD83D\uDCE8' },
    { to: '/applications', label: 'Заявления', icon: '\uD83D\uDCDD' },
    { to: '/calendar', label: 'Календарь', icon: '\uD83D\uDCC5' },
    { to: '/appointments', label: 'Запись', icon: '\uD83D\uDCD6' },
    { to: '/documents', label: 'Документы', icon: '\uD83D\uDCC4' },
  ]},
  { section: 'Управление', links: [
    { to: '/employees', label: 'Пользователи', icon: '\uD83D\uDC65' },
    { to: '/statistics', label: 'Статистика', icon: '\uD83D\uDCCA' },
    { to: '/reports', label: 'Отчёты', icon: '\uD83D\uDCC8' },
    { to: '/audit', label: 'Аудит', icon: '\uD83D\uDD0D' },
  ]},
];

const adminNav = { section: 'Админ', links: [
  { to: '/admin', label: 'Админ', icon: '\u2699\uFE0F' },
]};

export default function Layout({ children }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifLoading, setNotifLoading] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifList, setNotifList] = useState([]);

  const loadNotifs = () => {
    if (!user) return;
    setNotifLoading(true);
    fetch(`/api/notifications?user_id=${user.id}`)
      .then(r => r.json())
      .then(data => {
        const items = data.items || data || [];
        setNotifList(items);
        setUnreadCount(items.filter(n => !n.isRead).length);
      })
      .catch(() => {})
      .finally(() => setNotifLoading(false));
  };

  useEffect(() => { loadNotifs(); }, [user]);

  useEffect(() => {
    if (!notifOpen) return;
    const close = (e) => {
      if (!e.target.closest('.notif-bell') && !e.target.closest('.notif-dropdown')) {
        setNotifOpen(false);
      }
    };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [notifOpen]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path) => location.pathname === path;

  const navSections = user?.role === 'admin'
    ? [...navConfig, adminNav]
    : navConfig;

  if (location.pathname === '/login') {
    return <>{children}</>;
  }

  return (
    <div className="layout">
      <header className="topbar">
        <div className="topbar-left">
          <button className="sidebar-toggle" onClick={() => setCollapsed(!collapsed)}>
            {collapsed ? '\u2630' : '\u2715'}
          </button>
          <Link to="/files" className="logo">VOENCOM</Link>
        </div>
        <div className="user-info">
          <div style={{ position: 'relative' }}>
            <button className="notif-bell" onClick={() => {
              if (!notifOpen) loadNotifs();
              setNotifOpen(!notifOpen);
            }} title="Уведомления">
              <span className="bell-icon">{'\uD83D\uDD14'}</span>
              {unreadCount > 0 ? (
                <span className="notif-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
              ) : (
                !notifLoading && <span className="notif-badge zero">0</span>
              )}
            </button>
            {notifOpen && (
              <div className="notif-dropdown">
                <div className="notif-dropdown-header">Уведомления</div>
                <div className="notif-dropdown-list">
                  {notifList.length === 0 ? (
                    <div className="notif-dropdown-empty">Нет уведомлений</div>
                  ) : (
                    notifList.map(n => (
                      <div key={n.id} className="notif-dropdown-item">
                        <div className="notif-dd-title">{n.title}</div>
                        <div className="notif-dd-msg">{n.message || n.content || ''}</div>
                        <div className="notif-dd-time">{n.createdAt ? new Date(n.createdAt).toLocaleString('ru-RU') : ''}</div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}
          </div>
          <span className="user-name">{user?.name}</span>
          <button className="logout-btn" onClick={handleLogout}>Выход</button>
        </div>
      </header>
      <div className="body">
        <aside className={`sidebar${collapsed ? ' collapsed' : ''}`}>
          <nav className="sidebar-nav">
            {navSections.map(section => (
              <div key={section.section}>
                <div className="nav-section-title">{section.section}</div>
                {section.links.map(link => (
                  <Link
                    key={link.to}
                    to={link.to}
                    className={isActive(link.to) ? 'active' : ''}
                  >
                    <span className="nav-icon">{link.icon}</span>
                    <span className="nav-label">{link.label}</span>
                  </Link>
                ))}
              </div>
            ))}
          </nav>
        </aside>
        <main className="main">{children}</main>
      </div>
    </div>
  );
}
