const API_BASE = '/api';

function getToken() {
  return localStorage.getItem('token');
}

export async function apiFetch(url, options = {}) {
  const token = getToken();
  const headers = {
    'Content-Type': 'application/json',
    ...(token && { 'Authorization': `Bearer ${token}` }),
    ...options.headers
  };

  const fullUrl = url.startsWith('/') ? `${API_BASE}${url}` : `${API_BASE}/${url}`;
  const response = await fetch(fullUrl, { ...options, headers });
  
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Ошибка сервера' }));
    throw new Error(error.message || 'Ошибка');
  }
  
  return response.json();
}

export default { fetch: apiFetch };