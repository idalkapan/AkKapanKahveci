"""API integration check, isolated temporary database; build backend first."""
import http.cookiejar, json, os, secrets, subprocess, tempfile, time, urllib.request, urllib.error
from pathlib import Path
root = Path(__file__).resolve().parents[1]
admin_password = secrets.token_urlsafe(24)
with tempfile.TemporaryDirectory(prefix='coffee-test-') as directory:
    env = dict(os.environ, ASPNETCORE_URLS='http://127.0.0.1:5052', Payments__Mode='Demo', BootstrapAdmin__Enabled='true', BootstrapAdmin__Name='Test Admin', BootstrapAdmin__Email='admin@example.com', BootstrapAdmin__Password=admin_password, BootstrapAdmin__UseExistingAccount='false')
    process = subprocess.Popen(['dotnet', str(root/'backend/bin/Debug/net8.0/CoffeeApi.dll')], cwd=directory, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def call(path, body=None, headers=None, raw=None):
        h = headers or {}
        data = raw if raw is not None else json.dumps(body).encode() if body is not None else None
        if body is not None: h['Content-Type'] = 'application/json'
        request = urllib.request.Request('http://127.0.0.1:5052/api/'+path, data=data, headers=h)
        try:
            with client.open(request, timeout=4) as response: return response.status, json.loads(response.read())
        except urllib.error.HTTPError as e:
            if e.code == 429:
                assert e.headers.get("Retry-After") == "60"
            return e.code, e.read()
    try:
        for _ in range(60):
            try:
                if call('health')[0] == 200: break
            except (OSError, urllib.error.URLError): time.sleep(.25)
        else: raise RuntimeError('API did not start')
        status, menu = call('menu')
        assert status == 200 and len([p for p in menu if p['category']=='coffee']) == 10
        assert len([p for p in menu if p['category']=='dessert']) == 4
        assert call('student')[0] == 401
        assert call('auth/register', {'name':'Test User','email':'test@example.com','password':'coffee-test-123'})[0] == 200
        assert call('auth/me')[1]['name'] == 'Test User'
        order = {'customer':'Test','concept':'candy','items':[{'productId':'rose','quantity':2,'customCup':True,'cupText':'Ada','cupColor':'pink'}]}
        status, result = call('checkout', order)
        assert status == 200 and result['total'] == 420 and result['discount'] == 0
        order['items'][0]['quantity'] = -1
        assert call('checkout', order)[0] == 400
        order['items'][0]['quantity'] = 2
        order['items'][0]['productId'] = 'unknown'
        assert call('checkout', order)[0] == 400
        order['items'][0]['productId'] = 'rose'
        for items in ([None], [], None, [{'productId':'rose','quantity':21}], [{'productId':'rose','quantity':1,'customCup':True,'cupColor':'blue'}]):
            status, error = call('checkout', dict(order, items=items))
            assert status == 400 and json.loads(error)['error']
        for raw in (b'{broken', b'null', b'{"items":"invalid"}'):
            status, error = call('checkout', raw=raw, headers={'Content-Type':'application/json'})
            assert status == 400 and json.loads(error)['error']
        assert call('checkout', dict(order, customer=None))[0] == 400
        assert call('admin/documents')[0] == 403
        def upload(content):
            boundary = 'coffee-test-boundary'
            data = (f'--{boundary}\r\nContent-Disposition: form-data; name="document"; filename="student.pdf"\r\nContent-Type: application/pdf\r\n\r\n').encode()+content+f'\r\n--{boundary}--\r\n'.encode()
            return call('student', headers={'Content-Type':f'multipart/form-data; boundary={boundary}'}, raw=data)
        assert upload(b'not a document')[0] == 400
        assert upload(b'%PDF-1.4\n%%EOF')[0] == 200
        assert call('student')[1]['status'] == 'pending'
        assert call('checkout', order)[1]['discount'] == 0
        assert upload(b'%PDF-1.4\n%%EOF')[0] == 409
        customer_client = client
        client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        assert call('auth/login', {'email':'admin@example.com','password':admin_password})[0] == 200
        admin_client = client
        client = customer_client
        def admin_call(path, body=None):
            global client
            previous = client
            try:
                client = admin_client
                return call(path, body)
            finally:
                client = previous
        status, documents = admin_call('admin/documents')
        assert status == 200
        doc = documents[0]['id']
        assert admin_call('admin/documents/'+doc+'/review', {'status':'approved'})[0] == 200
        assert call('student')[1]['status'] == 'approved'
        status, result = call('checkout', order)
        assert status == 200 and result['total'] == 387 and result['discount'] == 33
        assert admin_call('admin/documents/'+doc+'/review', {'status':'rejected'})[0] == 200
        assert call('checkout', order)[1]['discount'] == 0
        first_orders = call('orders')[1]
        assert len(first_orders) == 4
        first_client = client
        client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        assert call('auth/register', {'name':'Second User','email':'second@example.com','password':'coffee-test-456'})[0] == 200
        assert call('orders')[1] == []
        assert call('student')[1]['status'] == 'none'
        assert call('admin/documents')[0] == 403
        assert call('admin/documents/'+doc+'/file')[0] == 403
        assert call('admin/documents/'+doc+'/review', {'status':'approved'})[0] == 403
        assert upload(b'%PDF-' + b'x' * (5 * 1024 * 1024 - 5))[0] == 200
        assert call('student')[1]['status'] == 'pending'
        status, second_order = call('checkout', order)
        assert status == 200 and second_order['discount'] == 0
        assert len(call('orders')[1]) == 1
        client = first_client
        assert call('orders')[1] == first_orders
        assert call('student')[1]['status'] == 'rejected'
        assert call('auth/logout', {})[0] == 200
        assert call('auth/me')[0] == 401
        assert call('checkout', order)[0] == 401
        assert call('auth/login', {'email':'test@example.com','password':'wrong'})[0] == 401
        assert call('auth/login', {'email':'test@example.com','password':'coffee-test-123'})[0] == 200
        assert len(call('orders')[1]) > 0
        assert call('student')[1]['status'] == 'rejected'
        # A new account has independent upload/checkout quotas.
        client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        assert call('auth/register', {'name':'Limit User','email':'limits@example.com','password':'coffee-test-789'})[0] == 200
        assert upload(b'%PDF-' + b'x' * (5 * 1024 * 1024))[0] == 413
        assert upload(b'%PDF-' + b'x' * (6 * 1024 * 1024))[0] == 413
        assert call('student', raw=b'bad form', headers={'Content-Type':'multipart/form-data'})[0] == 400
        assert upload(b'invalid document')[0] == 400
        assert upload(b'invalid document')[0] == 400
        status, error = upload(b'invalid document')
        assert status == 429 and json.loads(error)['error']
        for _ in range(20):
            assert call('checkout', order)[0] == 200
        status, error = call('checkout', order)
        assert status == 429 and json.loads(error)['error']
        assert len(call('orders')[1]) == 20
        client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        assert call('auth/register', {'name':'Independent User','email':'independent@example.com','password':'coffee-test-000'})[0] == 200
        assert call('checkout', order)[0] == 200
        print('PASS: account registration, login, logout, authorization, persistent ownership, menu, pricing, custom cup, invalid cart, document validation, pending/approved/rejected discount, admin authorization, cross-account isolation, malformed JSON/null cart, upload size boundaries, per-account rate limits and Retry-After.')
    finally:
        process.terminate()
        try: process.wait(timeout=5)
        except subprocess.TimeoutExpired: process.kill(); process.wait()
