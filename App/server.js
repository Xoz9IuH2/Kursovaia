import express from 'express';
import cors from 'cors';
import jwt from 'jsonwebtoken';
import bcrypt from 'bcryptjs';
import dotenv from 'dotenv';
import db from './src/db.js';

dotenv.config();

const JWT_SECRET = process.env.JWT_SECRET || 'dev-secret';

const app = express();
app.use(cors());
app.use(express.json());

// АУТЕНТИФИКАЦИЯ: вернуть JWT и проверить токен
function authenticateToken(req, res, next) {
  const header = req.headers['authorization'];
  const token = header && header.split(' ')[1];
  if (!token) {
    return res.status(401).json({ error: 'Ошибка авторизации: отсутствует токен' });
  }
  try {
    const user = jwt.verify(token, JWT_SECRET);
    req.user = user;
    next();
  } catch (err) {
    return res.status(403).json({ error: 'Неверный или просроченный токен' });
  }
}

// РОЛЕВАЯ ПОЛИТИКА: доступ только к сотрудникам военкомата (employee, commissar)
function ensureStaff(req, res, next) {
  // Разрешаем доступ администраторам (admin) как суперпользователь
  if (!req.user || !['employee', 'admin'].includes(req.user.role)) {
    return res.status(403).json({ error: 'Доступ запрещен: требуется аккаунт сотрудника' });
  }
  next();
}

function isAdmin(req) {
  return req.user && req.user.role === 'admin';
}

// Простая аудит-логирование действий сотрудников
function logAudit(req, action, table_name, record_id, details) {
  try {
    const user_id = req.user?.id ?? null;
    const stmt = db.prepare(` INSERT INTO audit_log (user_id, action, table_name, record_id, details) VALUES (?, ?, ?, ?, ?) `);
    stmt.run(user_id, action, table_name, record_id, details ?? null);
  } catch (e) {
    // Игнорируем ошибки аудита
  }
}

app.post('/api/login', (req, res) => {
  const { login, password } = req.body;
  const stmt = db.prepare('SELECT * FROM users WHERE login = ?');
  const user = stmt.get(login);
  if (!user || !bcrypt.compareSync(password, user.password)) {
    return res.status(401).json({ error: 'Неверный логин или пароль' });
  }
  // Ограничение доступа: граждане не имеют доступа к системе сотрудников
  if (user.role === 'citizen') {
    return res.status(403).json({ error: 'Доступ запрещен: требуются учетные данные сотрудника' });
  }
  // Выдать JWT для дальнейших обращений
  const token = jwt.sign(
    { id: user.id, login: user.login, role: user.role, name: user.name },
    JWT_SECRET,
    { expiresIn: '24h' }
  );
  res.json({
    user: { id: user.id, login: user.login, role: user.role, name: user.name, email: user.email },
    token
  });
});

// Далее -- доступ только сотрудникам
app.use(authenticateToken);
app.use(ensureStaff);

// Получение текущего пользователя по токену
app.get('/api/me', (req, res) => {
  // req.user заполняется authenticateToken
  res.json({ user: req.user });
});

app.get('/api/users', (req, res) => {
  const stmt = db.prepare('SELECT id, login, name, role, email, created_at FROM users ORDER BY created_at DESC');
  res.json(stmt.all());
});

// Get user by id (admin only)
app.get('/api/users/:id', (req, res) => {
  if (!isAdmin(req)) return res.status(403).json({ error: 'Доступ запрещен' });
  const stmt = db.prepare('SELECT id, login, name, role, email, created_at FROM users WHERE id = ?');
  res.json(stmt.get(req.params.id));
});

app.post('/api/users', (req, res) => {
  const { login, password, name, role, email } = req.body;
  // Разрешаем создание администратора только суперпользователем admin
  if (role === 'admin' && !(req.user && req.user.role === 'admin')) {
    return res.status(403).json({ error: 'Доступ запрещен: создание admin только суперпользователем' });
  }
  // Разрешаем создание сотрудников и комиссаров; роли admin тоже допустимы в рамках суперпользователя
  if (!['employee', 'admin'].includes(role)) {
    return res.status(400).json({ error: 'Недопустимая роль для нового пользователя' });
  }
  
  try {
    const hashed = bcrypt.hashSync(password, 10);
    const stmt = db.prepare('INSERT INTO users (login, password, name, role, email) VALUES (?, ?, ?, ?, ?)');
    const result = stmt.run(login, hashed, name, role, email);
    logAudit(req, 'CREATE', 'users', result.lastInsertRowid, JSON.stringify({ login, role }));
    res.json({ id: result.lastInsertRowid });
  } catch (err) {
    res.status(400).json({ error: 'Пользователь уже существует' });
  }
});

app.delete('/api/users/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM users WHERE id = ? AND role != "admin"');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'users', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/personal-files', (req, res) => {
  const stmt = db.prepare(`
    SELECT pf.*, u.name as citizen_name 
    FROM personal_files pf 
    LEFT JOIN users u ON pf.user_id = u.id 
    ORDER BY pf.created_at DESC
  `);
res.json(stmt.all());
});

// Delete personal file
app.delete('/api/personal-files/:id', (req, res) => {
  const id = req.params.id;
  const stmt = db.prepare('DELETE FROM personal_files WHERE id = ?');
  stmt.run(id);
  logAudit(req, 'DELETE', 'personal_files', id, null);
  res.json({ success: true });
});

// ------------- Admin/Staff CRUD: Users -------------
app.put('/api/users/:id', (req, res) => {
  if (!isAdmin(req)) return res.status(403).json({ error: 'Доступ запрещен' });
  const { login, password, name, role, email } = req.body;
  let passwordClause = '';
  let params = [];
  if (password) {
    const hashed = bcrypt.hashSync(password, 10);
    passwordClause = ', password = ?';
    params.push(hashed);
  }
  const sql = `UPDATE users SET login = ?, name = ?, role = ?${passwordClause}, email = ? WHERE id = ?`;
  if (password) {
    params = [login, name, role, email, req.params.id, ...params];
  } else {
    params = [login, name, role, email, req.params.id];
  }
  try {
    const stmt = db.prepare(sql);
    stmt.run(...params);
    logAudit(req, 'UPDATE', 'users', req.params.id, JSON.stringify({ login, role }));
    res.json({ success: true });
  } catch (e) {
    res.status(400).json({ error: 'Ошибка обновления' });
  }
});


app.get('/api/personal-files/:id', (req, res) => {
  const stmt = db.prepare(`
    SELECT pf.*, u.name as citizen_name 
    FROM personal_files pf 
    LEFT JOIN users u ON pf.user_id = u.id 
    WHERE pf.id = ?
  `);
  res.json(stmt.get(req.params.id));
});

app.post('/api/personal-files', (req, res) => {
  const { user_id, last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, category, fitness_category } = req.body;
  
  
  const stmt = db.prepare(`
    INSERT INTO personal_files (user_id, last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, category, fitness_category)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);
  const result = stmt.run(user_id, last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, category, fitness_category);
  logAudit(req, 'CREATE', 'personal_files', result.lastInsertRowid, JSON.stringify({ user_id }));
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/personal-files/:id', (req, res) => {
  const { last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, category, fitness_category } = req.body;
  
  const stmt = db.prepare(`
    UPDATE personal_files 
    SET last_name = ?, first_name = ?, middle_name = ?, birth_date = ?, passport_series = ?, passport_number = ?, passport_issued_by = ?, passport_issue_date = ?, birth_place = ?, address = ?, phone = ?, education = ?, work_place = ?, category = ?, fitness_category = ?, updated_at = CURRENT_TIMESTAMP
    WHERE id = ?
  `);
  stmt.run(last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, category, fitness_category, req.params.id);
  logAudit(req, 'UPDATE', 'personal_files', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/summons', (req, res) => {
  const { personal_file_id, status } = req.query;
  
  let query = 'SELECT s.*, pf.last_name, pf.first_name, pf.middle_name FROM summons s LEFT JOIN personal_files pf ON s.personal_file_id = pf.id';
  const params = [];
  const conditions = [];
  
  if (personal_file_id) {
    conditions.push('s.personal_file_id = ?');
    params.push(personal_file_id);
  }
  if (status) {
    conditions.push('s.status = ?');
    params.push(status);
  }
  
  if (conditions.length > 0) {
    query += ' WHERE ' + conditions.join(' AND ');
  }
  query += ' ORDER BY s.summom_date DESC';
  
  const stmt = db.prepare(query);
  res.json(stmt.all(...params));
});

app.post('/api/summons', (req, res) => {
  const { personal_file_id, summom_date, summom_time, reason, location, created_by } = req.body;
  
  const stmt = db.prepare(`
    INSERT INTO summons (personal_file_id, summom_date, summom_time, reason, location, created_by)
    VALUES (?, ?, ?, ?, ?, ?)
  `);
  const result = stmt.run(personal_file_id, summom_date, summom_time, reason, location, created_by);
  logAudit(req, 'CREATE', 'summons', result.lastInsertRowid, JSON.stringify({ personal_file_id }));
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/summons/:id', (req, res) => {
  const { summom_date, summom_time, reason, location, status } = req.body;
  
  const stmt = db.prepare(`
    UPDATE summons 
    SET summom_date = ?, summom_time = ?, reason = ?, location = ?, status = ?
    WHERE id = ?
  `);
  stmt.run(summom_date, summom_time, reason, location, status, req.params.id);
  logAudit(req, 'UPDATE', 'summons', req.params.id, null);
  res.json({ success: true });
});

app.delete('/api/summons/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM summons WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'summons', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/applications', (req, res) => {
  const { user_id, status } = req.query;
  
  let query = 'SELECT a.*, u.name as user_name FROM applications a LEFT JOIN users u ON a.user_id = u.id';
  const params = [];
  const conditions = [];
  
  if (user_id) {
    conditions.push('a.user_id = ?');
    params.push(user_id);
  }
  if (status) {
    conditions.push('a.status = ?');
    params.push(status);
  }
  
  if (conditions.length > 0) {
    query += ' WHERE ' + conditions.join(' AND ');
  }
  query += ' ORDER BY a.created_at DESC';
  
  const stmt = db.prepare(query);
  res.json(stmt.all(...params));
});

// Get application by id (admin/employee)
app.get('/api/applications/:id', (req, res) => {
  const stmt = db.prepare('SELECT a.*, u.name as user_name FROM applications a LEFT JOIN users u ON a.user_id = u.id WHERE a.id = ?');
  res.json(stmt.get(req.params.id));
});

// Delete application (admin/employee)
app.delete('/api/applications/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM applications WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'applications', req.params.id, null);
  res.json({ success: true });
});

app.post('/api/applications', (req, res) => {
  const { user_id, title, content } = req.body;
  
  const stmt = db.prepare('INSERT INTO applications (user_id, title, content) VALUES (?, ?, ?)');
  const result = stmt.run(user_id, title, content);
  logAudit(req, 'CREATE', 'applications', result.lastInsertRowid, JSON.stringify({ title }));
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/applications/:id', (req, res) => {
  const { status, response_text, reviewed_by } = req.body;
  
  const stmt = db.prepare('UPDATE applications SET status = ?, response_text = ?, reviewed_by = ? WHERE id = ?');
  stmt.run(status, response_text, reviewed_by, req.params.id);
  logAudit(req, 'UPDATE', 'applications', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/appointments', (req, res) => {
  const { user_id } = req.query;
  
  let query = 'SELECT a.*, u.name as user_name FROM appointments a LEFT JOIN users u ON a.user_id = u.id';
  const params = [];
  
  if (user_id) {
    query += ' WHERE a.user_id = ?';
    params.push(user_id);
  }
  query += ' ORDER BY a.appointment_date DESC';
  
  const stmt = db.prepare(query);
  res.json(stmt.all(...params));
});

// Get appointment by id
app.get('/api/appointments/:id', (req, res) => {
  const stmt = db.prepare('SELECT a.*, u.name as user_name FROM appointments a LEFT JOIN users u ON a.user_id = u.id WHERE a.id = ?');
  res.json(stmt.get(req.params.id));
});

// Delete appointment
app.delete('/api/appointments/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM appointments WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'appointments', req.params.id, null);
  res.json({ success: true });
});

app.post('/api/appointments', (req, res) => {
  const { user_id, appointment_date, appointment_time, purpose, notes } = req.body;
  
  const stmt = db.prepare('INSERT INTO appointments (user_id, appointment_date, appointment_time, purpose, notes) VALUES (?, ?, ?, ?, ?)');
  const result = stmt.run(user_id, appointment_date, appointment_time, purpose, notes);
  logAudit(req, 'CREATE', 'appointments', result.lastInsertRowid, JSON.stringify({ user_id }));
  res.json({ id: result.lastInsertRowid });
});

// Update appointment
app.put('/api/appointments/:id', (req, res) => {
  const { appointment_date, appointment_time, purpose, notes } = req.body;
  const stmt = db.prepare('UPDATE appointments SET appointment_date = ?, appointment_time = ?, purpose = ?, notes = ? WHERE id = ?');
  stmt.run(appointment_date, appointment_time, purpose, notes, req.params.id);
  logAudit(req, 'UPDATE', 'appointments', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/notifications', (req, res) => {
  const { user_id } = req.query;
  
  let query = 'SELECT * FROM notifications';
  const params = [];
  
  if (user_id) {
    query += ' WHERE user_id = ?';
    params.push(user_id);
  }
  query += ' ORDER BY created_at DESC';
  
  const stmt = db.prepare(query);
  res.json(stmt.all(...params));
});

// Get notification by id
app.get('/api/notifications/:id', (req, res) => {
  const stmt = db.prepare('SELECT * FROM notifications WHERE id = ?');
  res.json(stmt.get(req.params.id));
});

// Create notification
app.post('/api/notifications', (req, res) => {
  const { user_id, title, content } = req.body;
  const stmt = db.prepare('INSERT INTO notifications (user_id, title, content) VALUES (?, ?, ?)');
  const result = stmt.run(user_id, title, content);
  logAudit(req, 'CREATE', 'notifications', result.lastInsertRowid, JSON.stringify({ user_id, title }));
  res.json({ id: result.lastInsertRowid });
});

// Update notification (mark as read)
app.put('/api/notifications/:id', (req, res) => {
  const { is_read } = req.body;
  const stmt = db.prepare('UPDATE notifications SET is_read = ? WHERE id = ?');
  stmt.run(is_read ? 1 : 0, req.params.id);
  logAudit(req, 'UPDATE', 'notifications', req.params.id, JSON.stringify({ is_read }));
  res.json({ success: true });
});

// Delete notification
app.delete('/api/notifications/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM notifications WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'notifications', req.params.id, null);
  res.json({ success: true });
});

app.get('/api/calendar', (req, res) => {
  const stmt = db.prepare('SELECT * FROM calendar ORDER BY event_date DESC');
  res.json(stmt.all());
});

// Documents CRUD
app.get('/api/documents', (req, res) => {
  const { personal_file_id } = req.query;
  let query = 'SELECT d.*, pf.last_name, pf.first_name, pf.middle_name FROM documents d LEFT JOIN personal_files pf ON d.personal_file_id = pf.id';
  const params = [];
  if (personal_file_id) {
    query += ' WHERE d.personal_file_id = ?';
    params.push(personal_file_id);
  }
  query += ' ORDER BY d.uploaded_at DESC';
  const stmt = db.prepare(query);
  res.json(stmt.all(...params));
});

// Documents with pending verification (for staff)
app.get('/api/documents/pending', (req, res) => {
  const stmt = db.prepare(`
    SELECT d.*, pf.last_name, pf.first_name, pf.middle_name, u.name as uploaded_by_name
    FROM documents d
    LEFT JOIN personal_files pf ON d.personal_file_id = pf.id
    LEFT JOIN users u ON d.uploaded_by = u.id
    WHERE d.status = 'pending'
    ORDER BY d.uploaded_at DESC
  `);
  res.json(stmt.all());
});

app.get('/api/documents/:id', (req, res) => {
  const stmt = db.prepare(`
    SELECT d.*, pf.last_name, pf.first_name, pf.middle_name, u.name as uploaded_by_name
    FROM documents d
    LEFT JOIN personal_files pf ON d.personal_file_id = pf.id
    LEFT JOIN users u ON d.uploaded_by = u.id
    WHERE d.id = ?
  `);
  res.json(stmt.get(req.params.id));
});

app.post('/api/documents', (req, res) => {
  const { personal_file_id, name, file_path, file_type } = req.body;
  const stmt = db.prepare('INSERT INTO documents (personal_file_id, name, file_path, file_type, status, uploaded_by) VALUES (?, ?, ?, ?, ?, ?)');
  const result = stmt.run(personal_file_id, name, file_path, file_type, 'pending', req.user?.id);
  logAudit(req, 'CREATE', 'documents', result.lastInsertRowid, null);
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/documents/:id', (req, res) => {
  const { personal_file_id, name, file_path, file_type } = req.body;
  const stmt = db.prepare('UPDATE documents SET personal_file_id = ?, name = ?, file_path = ?, file_type = ? WHERE id = ?');
  stmt.run(personal_file_id, name, file_path, file_type, req.params.id);
  logAudit(req, 'UPDATE', 'documents', req.params.id, null);
  res.json({ success: true });
});

app.delete('/api/documents/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM documents WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'documents', req.params.id, null);
  res.json({ success: true });
});

// Verify document (staff only)
app.post('/api/documents/:id/verify', (req, res) => {
  const { status, rejection_reason } = req.body;
  if (!['verified', 'rejected'].includes(status)) {
    return res.status(400).json({ error: 'Invalid status. Must be "verified" or "rejected"' });
  }
  
  const stmt = db.prepare('UPDATE documents SET status = ?, rejection_reason = ?, verified_by = ?, verified_at = CURRENT_TIMESTAMP WHERE id = ?');
  stmt.run(status, rejection_reason || null, req.user?.id, req.params.id);
  logAudit(req, 'VERIFY', 'documents', req.params.id, JSON.stringify({ status, rejection_reason }));
  
  // Get document info for notification
  const doc = db.prepare('SELECT d.*, pf.user_id FROM documents d LEFT JOIN personal_files pf ON d.personal_file_id = pf.id WHERE d.id = ?').get(req.params.id);
  
  if (doc && doc.user_id) {
    const notifTitle = status === 'verified' ? 'Документ верифицирован' : 'Документ отклонён';
    const notifContent = status === 'verified' 
      ? `Ваш документ "${doc.name}" был успешно верифицирован.`
      : `Ваш документ "${doc.name}" был отклонён. Причина: ${rejection_reason || 'не указана'}`;
    
    db.prepare('INSERT INTO notifications (user_id, title, content) VALUES (?, ?, ?)').run(doc.user_id, notifTitle, notifContent);
  }
  
  res.json({ success: true });
});

// Attendance CRUD
app.get('/api/attendance', (req, res) => {
  const stmt = db.prepare('SELECT * FROM attendance ORDER BY attendance_date DESC');
  res.json(stmt.all());
});

app.get('/api/attendance/:id', (req, res) => {
  const stmt = db.prepare('SELECT * FROM attendance WHERE id = ?');
  res.json(stmt.get(req.params.id));
});

app.post('/api/attendance', (req, res) => {
  const { summons_id, user_id, attended, notes } = req.body;
  const stmt = db.prepare('INSERT INTO attendance (summons_id, user_id, attended, notes) VALUES (?, ?, ?, ?)');
  const result = stmt.run(summons_id, user_id, attended, notes);
  logAudit(req, 'CREATE', 'attendance', result.lastInsertRowid, null);
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/attendance/:id', (req, res) => {
  const { attended, notes } = req.body;
  const stmt = db.prepare('UPDATE attendance SET attended = ?, notes = ? WHERE id = ?');
  stmt.run(attended, notes, req.params.id);
  logAudit(req, 'UPDATE', 'attendance', req.params.id, null);
  res.json({ success: true });
});

app.delete('/api/attendance/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM attendance WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'attendance', req.params.id, null);
  res.json({ success: true });
});

// Protocols CRUD
app.get('/api/protocols', (req, res) => {
  const stmt = db.prepare('SELECT * FROM protocols ORDER BY created_at DESC');
  res.json(stmt.all());
});

app.get('/api/protocols/:id', (req, res) => {
  const stmt = db.prepare('SELECT * FROM protocols WHERE id = ?');
  res.json(stmt.get(req.params.id));
});

app.post('/api/protocols', (req, res) => {
  const { personal_file_id, protocol_number, protocol_date, content, created_by } = req.body;
  const stmt = db.prepare('INSERT INTO protocols (personal_file_id, protocol_number, protocol_date, content, created_by) VALUES (?, ?, ?, ?, ?)');
  const result = stmt.run(personal_file_id, protocol_number, protocol_date, content, created_by);
  logAudit(req, 'CREATE', 'protocols', result.lastInsertRowid, null);
  res.json({ id: result.lastInsertRowid });
});

app.put('/api/protocols/:id', (req, res) => {
  const { personal_file_id, protocol_number, protocol_date, content } = req.body;
  const stmt = db.prepare('UPDATE protocols SET personal_file_id = ?, protocol_number = ?, protocol_date = ?, content = ? WHERE id = ?');
  stmt.run(personal_file_id, protocol_number, protocol_date, content, req.params.id);
  logAudit(req, 'UPDATE', 'protocols', req.params.id, null);
  res.json({ success: true });
});

app.delete('/api/protocols/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM protocols WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'protocols', req.params.id, null);
  res.json({ success: true });
});

// Get calendar event by id
app.get('/api/calendar/:id', (req, res) => {
  const stmt = db.prepare('SELECT * FROM calendar WHERE id = ?');
  res.json(stmt.get(req.params.id));
});

// Update calendar event
app.put('/api/calendar/:id', (req, res) => {
  const { title, description, event_date, event_time, location } = req.body;
  const stmt = db.prepare('UPDATE calendar SET title = ?, description = ?, event_date = ?, event_time = ?, location = ? WHERE id = ?');
  stmt.run(title, description, event_date, event_time, location, req.params.id);
  logAudit(req, 'UPDATE', 'calendar', req.params.id, null);
  res.json({ success: true });
});

// Delete calendar event
app.delete('/api/calendar/:id', (req, res) => {
  const stmt = db.prepare('DELETE FROM calendar WHERE id = ?');
  stmt.run(req.params.id);
  logAudit(req, 'DELETE', 'calendar', req.params.id, null);
  res.json({ success: true });
});

app.post('/api/calendar', (req, res) => {
  const { title, description, event_date, event_time, location, created_by } = req.body;
  
  const stmt = db.prepare('INSERT INTO calendar (title, description, event_date, event_time, location, created_by) VALUES (?, ?, ?, ?, ?, ?)');
  const result = stmt.run(title, description, event_date, event_time, location, created_by);
  logAudit(req, 'CREATE', 'calendar', result.lastInsertRowid, JSON.stringify({ title }));
  res.json({ id: result.lastInsertRowid });
});

app.get('/api/statistics', (req, res) => {
  const totalFiles = db.prepare('SELECT COUNT(*) as count FROM personal_files').get();
  const totalSummons = db.prepare('SELECT COUNT(*) as count FROM summons').get();
  const pendingSummons = db.prepare("SELECT COUNT(*) as count FROM summons WHERE status = 'pending'").get();
  const fulfilledSummons = db.prepare("SELECT COUNT(*) as count FROM summons WHERE status = 'fulfilled'").get();
  const missedSummons = db.prepare("SELECT COUNT(*) as count FROM summons WHERE status = 'missed'").get();
  const pendingApplications = db.prepare("SELECT COUNT(*) as count FROM applications WHERE status = 'pending'").get();
  
  res.json({
    totalFiles: totalFiles.count,
    totalSummons: totalSummons.count,
    pendingSummons: pendingSummons.count,
    fulfilledSummons: fulfilledSummons.count,
    missedSummons: missedSummons.count,
    pendingApplications: pendingApplications.count
  });
});

app.get('/api/audit', (req, res) => {
  const stmt = db.prepare(`
    SELECT al.*, u.name as user_name, u.role as user_role
    FROM audit_log al
    LEFT JOIN users u ON al.user_id = u.id
    ORDER BY al.created_at DESC
    LIMIT 100
  `);
  res.json(stmt.all());
});

// ---- .NET-style route aliases for web frontend compatibility ----

// PersonalFile CRUD
app.get('/api/PersonalFile', (req, res) => {
  const { search, status } = req.query;
  let query = 'SELECT pf.*, u.name as citizen_name FROM personal_files pf LEFT JOIN users u ON pf.user_id = u.id';
  const params = [];
  const conditions = [];
  if (status) { conditions.push('pf.status = ?'); params.push(status); }
  if (search) { conditions.push('(pf.last_name LIKE ? OR pf.first_name LIKE ?)'); params.push(`%${search}%`, `%${search}%`); }
  if (conditions.length) query += ' WHERE ' + conditions.join(' AND ');
  query += ' ORDER BY pf.created_at DESC';
  const items = db.prepare(query).all(...params);
  res.json({ items, total: items.length, page: 1, pageSize: items.length });
});

app.post('/api/PersonalFile', (req, res) => {
  const { lastName, firstName, patronymic, birthDate, passport_series, passport_number, passport_issued_by, passport_issue_date, birthPlace, address, phone, education, workPlace, fitnessCategory, gender, militaryTicketSeries, militaryTicketNumber, militaryRank, passportIssueAuthority, passportIssueDate } = req.body;
  const stmt = db.prepare(`INSERT INTO personal_files (last_name, first_name, middle_name, birth_date, passport_series, passport_number, passport_issued_by, passport_issue_date, birth_place, address, phone, education, work_place, fitness_category, military_ticket_series, military_ticket_number, status, created_at) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,'active',datetime('now'))`);
  const result = stmt.run(lastName || '', firstName || '', patronymic || '', birthDate || '', passport_series || '', passport_number || '', passportIssueAuthority || passport_issued_by || '', passportIssueDate || passport_issue_date || '', birthPlace || '', address || '', phone || '', education || '', workPlace || '', fitnessCategory || '', militaryTicketSeries || '', militaryTicketNumber || '');
  res.json({ message: 'Дело создано', file: { id: result.lastInsertRowid } });
});

app.put('/api/PersonalFile/:id', (req, res) => {
  const { lastName, firstName, patronymic, birthDate, passport_series, passport_number, passport_issued_by, passport_issue_date, birthPlace, address, phone, education, workPlace, fitnessCategory, militaryTicketSeries, militaryTicketNumber } = req.body;
  db.prepare(`UPDATE personal_files SET last_name=?, first_name=?, middle_name=?, birth_date=?, passport_series=?, passport_number=?, passport_issued_by=?, passport_issue_date=?, birth_place=?, address=?, phone=?, education=?, work_place=?, fitness_category=?, military_ticket_series=?, military_ticket_number=?, updated_at=datetime('now') WHERE id=?`).run(lastName||'', firstName||'', patronymic||'', birthDate||'', passport_series||'', passport_number||'', passport_issued_by||'', passport_issue_date||'', birthPlace||'', address||'', phone||'', education||'', workPlace||'', fitnessCategory||'', militaryTicketSeries||'', militaryTicketNumber||'', req.params.id);
  res.json({ message: 'Дело обновлено', file: { id: parseInt(req.params.id) } });
});

app.delete('/api/PersonalFile/:id', (req, res) => {
  db.prepare('DELETE FROM personal_files WHERE id = ?').run(req.params.id);
  res.json({ success: true });
});

app.post('/api/PersonalFile/:id/archive', (req, res) => {
  db.prepare("UPDATE personal_files SET status='archived', updated_at=datetime('now') WHERE id=?").run(req.params.id);
  res.json({ success: true });
});

// Summon CRUD
app.get('/api/Summon', (req, res) => {
  const { search, status } = req.query;
  let query = 'SELECT s.*, pf.last_name, pf.first_name, pf.middle_name FROM summons s LEFT JOIN personal_files pf ON s.personal_file_id = pf.id';
  const params = [];
  const conditions = [];
  if (status) { conditions.push('s.status = ?'); params.push(status); }
  if (search) { conditions.push('(pf.last_name LIKE ? OR pf.first_name LIKE ?)'); params.push(`%${search}%`, `%${search}%`); }
  if (conditions.length) query += ' WHERE ' + conditions.join(' AND ');
  query += ' ORDER BY s.created_at DESC';
  const items = db.prepare(query).all(...params);
  res.json({ items, total: items.length, page: 1, pageSize: items.length });
});

app.post('/api/Summon', (req, res) => {
  const { personalFileId, summonDate, time, reason, location } = req.body;
  const result = db.prepare('INSERT INTO summons (personal_file_id, summom_date, summom_time, reason, location, status, created_at) VALUES (?,?,?,?,?,\'pending\',datetime(\'now\'))').run(personalFileId, summonDate, time, reason, location);
  res.json({ message: 'Повестка создана', summon: { id: result.lastInsertRowid } });
});

app.put('/api/Summon/:id', (req, res) => {
  const { personalFileId, summonDate, time, reason, location, status } = req.body;
  db.prepare('UPDATE summons SET personal_file_id=?, summom_date=?, summom_time=?, reason=?, location=?, status=? WHERE id=?').run(personalFileId, summonDate, time, reason, location, status||'pending', req.params.id);
  res.json({ success: true });
});

app.delete('/api/Summon/:id', (req, res) => {
  db.prepare('DELETE FROM summons WHERE id = ?').run(req.params.id);
  res.json({ success: true });
});

app.post('/api/Summon/:id/arrived', (req, res) => {
  db.prepare("UPDATE summons SET status='arrived' WHERE id=?").run(req.params.id);
  res.json({ success: true });
});

// Auth extras
app.post('/api/auth/register-citizen', (req, res) => {
  res.status(501).json({ message: 'Регистрация граждан временно недоступна. Используйте .NET API.' });
});

app.get('/api/auth/me', (req, res) => {
  const user = req.user;
  if (!user) return res.status(401).json({ error: 'Не авторизован' });
  res.json({ id: user.id, login: user.login, name: user.name, role: user.role, email: user.email });
});

app.get('/api/auth/login', (req, res) => {
  res.redirect(307, '/api/login');
});

// PersonalFileSearch
app.get('/api/PersonalFileSearch/search', (req, res) => {
  const q = (req.query.q || '').trim();
  if (!q) return res.json([]);
  const parts = q.split(' ');
  let results;
  if (parts.length >= 2 && parts[0].length <= 2 && parts[1].length >= 4) {
    results = db.prepare("SELECT * FROM personal_files WHERE military_ticket_series = ? AND military_ticket_number = ? AND status = 'active'").all(parts[0], parts[1]);
  } else {
    results = db.prepare("SELECT * FROM personal_files WHERE (last_name LIKE ? OR first_name LIKE ?) AND status = 'active' LIMIT 20").all(`%${q}%`, `%${q}%`);
  }
  res.json(results);
});

app.listen(3001, () => {
  console.log('Server running on http://localhost:3001');
});
