"""Step 1 integration tests. Build backend first; uses isolated SQLite fixtures on port 5053."""
import base64
import hashlib
import http.cookiejar
import json
import os
from pathlib import Path
import secrets
import sqlite3
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'backend/bin/Debug/net8.0/CoffeeApi.dll'
PASSWORD = secrets.token_urlsafe(24)


def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def call(opener, path, body=None, headers=None):
    headers = dict(headers or {})
    data = None if body is None else json.dumps(body).encode()
    if data is not None:
        headers['Content-Type'] = 'application/json'
    request = urllib.request.Request('http://127.0.0.1:5053/api/' + path, data=data, headers=headers)
    try:
        response = opener.open(request, timeout=4)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read()
        return response.status, json.loads(raw) if raw else None


class Server:
    def __init__(self, directory, **settings):
        self.directory = Path(directory)
        self.settings = settings
        self.process = None

    def start(self, should_fail=False):
        env = {key: value for key, value in os.environ.items()
               if not key.startswith(('BootstrapAdmin__', 'Database__')) and key != 'AdminKey'}
        env.update(ASPNETCORE_URLS='http://127.0.0.1:5053', Payments__Mode='Demo')
        env.update(self.settings)
        self.log = tempfile.TemporaryFile()
        self.process = subprocess.Popen(['dotnet', str(DLL)], cwd=self.directory, env=env,
                                        stdout=self.log, stderr=self.log)
        try:
            for _ in range(120):
                if self.process.poll() is not None:
                    assert should_fail and self.process.returncode != 0, 'Unexpected startup failure'
                    return
                try:
                    if call(client(), 'health')[0] == 200:
                        assert not should_fail, 'Startup should have failed'
                        return
                except (OSError, urllib.error.URLError):
                    time.sleep(.1)
            raise AssertionError('API startup timeout')
        except BaseException:
            self.stop()
            raise

    def stop(self):
        if self.process is not None:
            if self.process.poll() is None:
                self.process.terminate()
                self.process.wait(timeout=10)
            self.log.seek(0)
            logs = self.log.read()
            self.log.close()
            self.process = None
            assert PASSWORD.encode() not in logs, 'Password leaked to logs'
            assert b'Bootstrap Person' not in logs, 'Bootstrap personal information leaked to logs'


def legacy_fixture(directory):
    path = Path(directory) / 'data/coffee.db'
    path.parent.mkdir()
    salt = secrets.token_bytes(16)
    hashed = hashlib.pbkdf2_hmac('sha256', PASSWORD.encode(), salt, 210000, 32)
    password_hash = base64.b64encode(salt).decode() + ':' + base64.b64encode(hashed).decode()
    with sqlite3.connect(path) as db:
        db.executescript('''
            CREATE TABLE Users (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, Email TEXT NOT NULL, PasswordHash TEXT NOT NULL);
            CREATE TABLE Orders (Id TEXT NOT NULL PRIMARY KEY, Session TEXT NOT NULL, Customer TEXT NOT NULL,
                Concept TEXT NOT NULL, Total TEXT NOT NULL, Discount TEXT NOT NULL, ItemsJson TEXT NOT NULL, Created TEXT NOT NULL);
            CREATE TABLE Documents (Id TEXT NOT NULL PRIMARY KEY, Session TEXT NOT NULL, Status TEXT NOT NULL,
                Extension TEXT NOT NULL, Created TEXT NOT NULL);
            INSERT INTO Orders VALUES ('legacy-order', 'legacy', 'Test', 'candy', '165', '0', '[]', '2026-01-01 10:00:00');
            INSERT INTO Documents VALUES ('legacy-doc', 'legacy', 'pending', '.pdf', '2026-01-01 10:00:00');
        ''')
        db.execute('INSERT INTO Users VALUES (?, ?, ?, ?)', ('legacy', 'Legacy User', 'legacy@example.com', password_hash))
    return path


with tempfile.TemporaryDirectory(prefix='coffee-roles-') as directory:
    path = legacy_fixture(directory)
    server = Server(directory, BootstrapAdmin__Enabled='true', BootstrapAdmin__Email='legacy@example.com',
                    BootstrapAdmin__UseExistingAccount='true')
    try:
        server.start()
        anon, admin, customer = client(), client(), client()
        assert call(anon, 'admin/documents', headers={'X-Admin-Key': 'x' * 64})[0] == 401
        assert call(anon, 'orders')[0] == 401
        assert call(anon, 'student')[0] == 401
        assert call(anon, 'checkout', {})[0] == 401
        assert call(anon, 'menu')[0] == 200
        assert call(admin, 'auth/login', {'email': 'legacy@example.com', 'password': PASSWORD})[0] == 200
        assert set(call(admin, 'auth/me')[1]['roles']) == {'Customer', 'Admin'}
        assert call(admin, 'admin/documents')[0] == 200
        assert call(admin, 'orders')[1][0]['id'] == 'legacy-order'
        status, account = call(customer, 'auth/register', {
            'name': 'Customer', 'email': 'customer@example.com', 'password': PASSWORD,
            'role': 'Admin', 'roles': ['Admin', 'Barista'], 'isAdmin': True})
        assert status == 200 and account['roles'] == ['Customer']
        assert call(customer, 'orders')[1] == []
        assert call(customer, 'auth/register', {'name': 'Duplicate', 'email': 'customer@example.com', 'password': PASSWORD})[0] == 409
        assert call(customer, 'admin/documents', headers={'X-Admin-Key': 'x' * 64})[0] == 403
        assert call(customer, 'admin/documents/legacy-doc/file')[0] == 403
        assert call(customer, 'admin/documents/legacy-doc/review', {'status': 'approved'})[0] == 403
        with sqlite3.connect(path) as db:
            assert db.execute('PRAGMA user_version').fetchone() == (1,)
            assert db.execute('SELECT PreparationStatus, PaymentStatus, UpdatedAt, Version FROM Orders').fetchone() == (
                'Delivered', 'DemoPaid', '2026-01-01 10:00:00', 1)
            assert db.execute('SELECT COUNT(*) FROM Documents').fetchone() == (1,)
            customer_id = db.execute("SELECT Id FROM Users WHERE Email = 'customer@example.com'").fetchone()[0]
            # Fixture-only role changes: staff management is implemented in step 2.
            db.execute('DELETE FROM UserRoles WHERE UserId = ?', (customer_id,))
            db.execute('INSERT INTO UserRoles VALUES (?, ?)', (customer_id, 'Barista'))
        assert call(customer, 'auth/me')[1]['roles'] == ['Barista']
        for route in ('orders', 'student', 'admin/documents'):
            assert call(customer, route)[0] == 403
        assert call(customer, 'checkout', {})[0] == 403
        with sqlite3.connect(path) as db:
            db.execute('INSERT INTO UserRoles VALUES (?, ?)', (customer_id, 'Admin'))
        assert call(customer, 'admin/documents')[0] == 200
        with sqlite3.connect(path) as db:
            db.execute("DELETE FROM UserRoles WHERE UserId = ? AND Role = 'Admin'", (customer_id,))
        assert call(customer, 'admin/documents')[0] == 403
        backups = list((Path(directory) / 'data/backups').glob('*.db'))
        assert len(backups) == 1
        with sqlite3.connect(backups[0]) as backup:
            assert backup.execute('PRAGMA user_version').fetchone() == (0,)
            assert backup.execute('SELECT COUNT(*) FROM Orders').fetchone() == (1,)
            assert backup.execute("SELECT COUNT(*) FROM sqlite_master WHERE name='UserRoles'").fetchone() == (0,)
        server.stop()
        # Reusing the same cookies also checks principal refresh across restart.
        server = Server(directory, Database__SqlUpgrades__Enabled='false', BootstrapAdmin__Enabled='true',
                        BootstrapAdmin__Email='customer@example.com', BootstrapAdmin__UseExistingAccount='true')
        server.start()
        assert call(admin, 'admin/documents')[0] == 200
        assert call(customer, 'admin/documents')[0] == 403  # bootstrap does not add a second admin
        assert call(customer, 'auth/me')[1]['roles'] == ['Barista']
        assert len(list((Path(directory) / 'data/backups').glob('*.db'))) == 1
    finally:
        server.stop()

with tempfile.TemporaryDirectory(prefix='coffee-bootstrap-') as directory:
    server = Server(directory, BootstrapAdmin__Enabled='true', BootstrapAdmin__Email='admin@example.com',
                    BootstrapAdmin__Name='Bootstrap Person', BootstrapAdmin__Password=PASSWORD)
    try:
        server.start()
        admin = client()
        assert call(admin, 'auth/login', {'email': 'admin@example.com', 'password': PASSWORD})[0] == 200
        assert call(admin, 'admin/documents')[0] == 200
    finally:
        server.stop()

for settings in (
    {'BootstrapAdmin__Email': 'legacy@example.com'},  # existing-account opt-in is mandatory
    {'BootstrapAdmin__Email': 'missing@example.com', 'BootstrapAdmin__UseExistingAccount': 'true'},
    {'BootstrapAdmin__Email': 'new@example.com', 'BootstrapAdmin__Name': 'Bootstrap Person', 'BootstrapAdmin__Password': 'short'},
):
    with tempfile.TemporaryDirectory(prefix='coffee-bootstrap-failure-') as directory:
        path = legacy_fixture(directory)
        server = Server(directory, BootstrapAdmin__Enabled='true', **settings)
        try:
            server.start(should_fail=True)
            with sqlite3.connect(path) as db:
                assert db.execute("SELECT COUNT(*) FROM UserRoles WHERE Role='Admin'").fetchone() == (0,)
                assert db.execute('SELECT COUNT(*) FROM Users').fetchone() == (1,)
        finally:
            server.stop()

print('PASS: legacy upgrade/backup, role persistence, registration escalation blocked, 401/403, live role refresh, bootstrap new/existing/failures/idempotence, no secret logging.')
