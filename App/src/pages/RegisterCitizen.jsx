import { useState, useEffect } from 'react';
import './Employees.css';

export default function RegisterCitizen() {
  const [searchSeries, setSearchSeries] = useState('');
  const [searchNumber, setSearchNumber] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [selectedFile, setSelectedFile] = useState(null);
  const [login, setLogin] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [searching, setSearching] = useState(false);

  const searchFiles = async () => {
    if (!searchSeries && !searchNumber) {
      setSearchResults([]);
      return;
    }
    setSearching(true);
    try {
      const q = `${searchSeries} ${searchNumber}`.trim();
      const res = await fetch(`/api/PersonalFileSearch/search?q=${encodeURIComponent(q)}`);
      if (res.ok) {
        const data = await res.json();
        setSearchResults(data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setSearching(false);
    }
  };

  useEffect(() => {
    const timer = setTimeout(searchFiles, 300);
    return () => clearTimeout(timer);
  }, [searchSeries, searchNumber]);

  const selectFile = async (file) => {
    setSelectedFile(file);
    setSearchResults([]);
    setSearchSeries(file.militaryTicketSeries || '');
    setSearchNumber(file.militaryTicketNumber || '');
    
    if (file.email) {
      setLogin(file.email);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedFile) {
      setError('Выберите личное дело');
      return;
    }
    if (!login) {
      setError('Введите email (логин)');
      return;
    }
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const res = await fetch('/api/auth/register-citizen', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          login: login,
          personalFileId: selectedFile.id
        })
      });

      const data = await res.json();

      if (!res.ok) {
        setError(data.message || 'Ошибка');
        return;
      }

      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const reset = () => {
    setSelectedFile(null);
    setSearchSeries('');
    setSearchNumber('');
    setLogin('');
    setResult(null);
    setError(null);
  };

  return (
    <div className="employees-page">
      <div className="page-header">
        <h2>Регистрация гражданина</h2>
      </div>

      {result && (
        <div className="success-message">
          <h3>✅ Гражданин зарегистрирован!</h3>
          <p><strong>Email:</strong> {result.email || login}</p>
          <p><strong>Временный пароль:</strong> <code>{result.tempPassword}</code></p>
          <p className="hint">Сообщите пароль гражданину для первого входа</p>
          <button onClick={reset} className="btn">Зарегистрировать ещё</button>
        </div>
      )}

      {error && <div className="error-message">{error}</div>}

      {!result && (
        <form onSubmit={handleSubmit} className="form">
          {!selectedFile ? (
            <>
              <h3>Поиск личного дела</h3>
              <p className="hint">Введите данные из военного билета</p>
              <div className="form-row">
                <div className="form-group">
                  <label>Серия (2 буквы)</label>
                  <input 
                    value={searchSeries} 
                    onChange={(e) => {
                      setSearchSeries(e.target.value.toUpperCase().replace(/[^А-Яа-яA-Za-z]/g, '').slice(0, 2));
                      setSelectedFile(null);
                    }} 
                    placeholder="АВ"
                    maxLength={2}
                    autoFocus
                  />
                </div>
                <div className="form-group">
                  <label>Номер (6 цифр)</label>
                  <input 
                    value={searchNumber} 
                    onChange={(e) => {
                      setSearchNumber(e.target.value.replace(/\D/g, '').slice(0, 6));
                      setSelectedFile(null);
                    }} 
                    placeholder="123456"
                    maxLength={6}
                  />
                </div>
              </div>
              
              {searching && <div className="loading">Поиск...</div>}
              
              {searchResults.length > 0 && (
                <div className="search-results">
                  {searchResults.map(file => (
                    <div 
                      key={file.id} 
                      className="file-select-card"
                      onClick={() => selectFile(file)}
                    >
                      <div className="file-select-header">
                        <span className="file-select-icon">👤</span>
                        <div>
                          <strong>{file.lastName} {file.firstName} {file.patronymic}</strong>
                          <span className="file-select-status">Активен</span>
                        </div>
                      </div>
                      <div className="file-select-body">
                        <div className="file-select-row"><span>Дата рождения</span><span>{file.birthDate}</span></div>
                        <div className="file-select-row"><span>Военный билет</span><span>{file.militaryTicketSeries} №{file.militaryTicketNumber}</span></div>
                        {file.address && <div className="file-select-row"><span>Адрес</span><span>{file.address}</span></div>}
                        {file.phone && <div className="file-select-row"><span>Телефон</span><span>{file.phone}</span></div>}
                      </div>
                    </div>
                  ))}
                </div>
              )}
              
              {searchSeries.length >= 2 && searchNumber.length >= 6 && searchResults.length === 0 && !searching && (
                <p className="empty">Личное дело с таким военным билетом не найдено</p>
              )}
            </>
          ) : (
            <>
              <h3>Выбрано личное дело</h3>
              <div className="selected-file">
                <p><strong>{selectedFile.lastName} {selectedFile.firstName} {selectedFile.patronymic}</strong></p>
                <p>Дата рождения: {selectedFile.birthDate}</p>
                <p>Военный билет: {selectedFile.militaryTicketSeries} №{selectedFile.militaryTicketNumber}</p>
                <button type="button" onClick={() => { setSelectedFile(null); setSearchSeries(''); setSearchNumber(''); }} className="btn-small">
                  Изменить выбор
                </button>
              </div>

              <h3>Создание аккаунта</h3>
              <div className="form-group">
                <label>Email (логин)</label>
                <input 
                  value={login} 
                  onChange={(e) => setLogin(e.target.value)} 
                  placeholder="ivanov@mail.ru" 
                  type="email"
                  required 
                />
              </div>

              <button type="submit" className="btn" disabled={loading}>
                {loading ? 'Регистрация...' : 'Создать аккаунт'}
              </button>
            </>
          )}
        </form>
      )}
    </div>
  );
}