import Database from 'better-sqlite3';
import path from 'path';
import { fileURLToPath } from 'url';
import bcrypt from 'bcryptjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const dbPath = path.join(__dirname, 'voenkom.db');

const db = new Database(dbPath);

// Migration: upgrade users table to allow admin role if needed
try {
  // Add document verification columns if they don't exist
  const docColumns = db.prepare("PRAGMA table_info(documents)").all();
  const hasStatus = docColumns.find(c => c.name === 'status');
  if (!hasStatus) {
    db.exec("ALTER TABLE documents ADD COLUMN status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'verified', 'rejected'))");
    db.exec("ALTER TABLE documents ADD COLUMN rejection_reason TEXT");
    db.exec("ALTER TABLE documents ADD COLUMN verified_by INTEGER");
    db.exec("ALTER TABLE documents ADD COLUMN verified_at DATETIME");
    db.exec("ALTER TABLE documents ADD COLUMN uploaded_by INTEGER");
    console.log('Migration: added document verification columns');
  }

  // If admin role already exists, skip
  const adminExists = db.prepare("SELECT 1 FROM users WHERE role = 'admin' LIMIT 1").get();
  if (!adminExists) {
    // Perform schema migration for admin role support
    db.exec('PRAGMA foreign_keys = OFF;');
    db.exec('ALTER TABLE users RENAME TO users_old;');
    db.exec(`CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      login TEXT UNIQUE NOT NULL,
      password TEXT NOT NULL,
      role TEXT NOT NULL CHECK(role IN ('citizen','employee','admin')),
      name TEXT NOT NULL,
      email TEXT,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    );`);
    db.exec('PRAGMA foreign_keys = ON;');
    db.exec(`INSERT INTO users (id, login, password, role, name, email, created_at) SELECT id, login, password, role, name, email, created_at FROM users_old;`);
    db.exec('DROP TABLE users_old;');
    // Ensure an admin user exists after migration
    const adminCheck = db.prepare("SELECT 1 FROM users WHERE role = 'admin' LIMIT 1").get();
    if (!adminCheck) {
      const adminLogin = 'admin';
      const adminPasswordHash = bcrypt.hashSync('admin', 10);
      db.exec(`INSERT INTO users (id, login, password, role, name, email, created_at) VALUES (4, '${adminLogin}', '${adminPasswordHash}', 'admin', 'Администратор', 'admin@voenkom.ru', CURRENT_TIMESTAMP)`);
    }
    
    console.log('Migration: updated users table to include admin role support');
  }
} catch (e) {
  // Migration not critical; log and continue
  console.log('Migration check skipped or failed', e?.message);
}

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    login TEXT UNIQUE NOT NULL,
    password TEXT NOT NULL,
    role TEXT NOT NULL CHECK(role IN ('citizen', 'employee', 'admin')),
    name TEXT NOT NULL,
    email TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE IF NOT EXISTS personal_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    last_name TEXT NOT NULL,
    first_name TEXT NOT NULL,
    middle_name TEXT,
    birth_date DATE NOT NULL,
    passport_series TEXT,
    passport_number TEXT,
    passport_issued_by TEXT,
    passport_issue_date DATE,
    birth_place TEXT,
    address TEXT,
    phone TEXT,
    education TEXT,
    work_place TEXT,
    category INTEGER,
    fitness_category TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS summons (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personal_file_id INTEGER NOT NULL,
    summom_date DATE NOT NULL,
    summom_time TEXT NOT NULL,
    reason TEXT NOT NULL,
    location TEXT NOT NULL,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'sent', 'delivered', 'fulfilled', 'missed')),
    created_by INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (personal_file_id) REFERENCES personal_files(id),
    FOREIGN KEY (created_by) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS applications (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'reviewed', 'approved', 'rejected')),
    response_text TEXT,
    reviewed_by INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (reviewed_by) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS documents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personal_file_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_type TEXT,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'verified', 'rejected')),
    rejection_reason TEXT,
    verified_by INTEGER,
    verified_at DATETIME,
    uploaded_by INTEGER,
    uploaded_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (personal_file_id) REFERENCES personal_files(id),
    FOREIGN KEY (verified_by) REFERENCES users(id),
    FOREIGN KEY (uploaded_by) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS appointments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    appointment_date DATE NOT NULL,
    appointment_time TEXT NOT NULL,
    purpose TEXT,
    status TEXT DEFAULT 'scheduled' CHECK(status IN ('scheduled', 'completed', 'cancelled')),
    notes TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS notifications (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    is_read INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS calendar (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    description TEXT,
    event_date DATE NOT NULL,
    event_time TEXT NOT NULL,
    location TEXT,
    created_by INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (created_by) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS attendance (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    summons_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    attended INTEGER DEFAULT 0,
    notes TEXT,
    attendance_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (summons_id) REFERENCES summons(id),
    FOREIGN KEY (user_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS protocols (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personal_file_id INTEGER NOT NULL,
    protocol_number TEXT NOT NULL,
    protocol_date DATE NOT NULL,
    content TEXT NOT NULL,
    created_by INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (personal_file_id) REFERENCES personal_files(id),
    FOREIGN KEY (created_by) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS audit_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    action TEXT NOT NULL,
    table_name TEXT,
    record_id INTEGER,
    details TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
  );
`);

const checkStmt = db.prepare(`
  SELECT COUNT(*) as count FROM users
`);
const userCount = checkStmt.get().count;

if (userCount === 0) {
  const hashed1 = bcrypt.hashSync('123', 10);
  const hashed2 = bcrypt.hashSync('123', 10);
  const hashed3 = bcrypt.hashSync('123', 10);
  const hashedAdmin = bcrypt.hashSync('admin', 10);
  db.exec(`
    INSERT INTO users (login, password, role, name, email) VALUES 
    ('employee1', '${hashed1}', 'employee', 'Иванов Иван Иванович', 'employee@voenkom.ru'),
    ('employee2', '${hashed2}', 'employee', 'Петров Петр Петрович', 'employee2@voenkom.ru'),
    ('citizen1', '${hashed3}', 'citizen', 'Сидоров Алексей Сергеевич', 'citizen@mail.ru'),
    ('admin', '${hashedAdmin}', 'admin', 'Администратор', 'admin@voenkom.ru');
  `);
  console.log('Default users created');
}

export default db;
