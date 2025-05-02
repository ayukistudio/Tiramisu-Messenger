from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
import sqlite3
import os
from werkzeug.security import generate_password_hash, check_password_hash

app = Flask(__name__)
CORS(app)
DATABASE = 'server.db'
AVATARS_DIR = 'avatars'

if not os.path.exists(AVATARS_DIR):
    os.makedirs(AVATARS_DIR)
    os.chmod(AVATARS_DIR, 0o775)

def init_db():
    with sqlite3.connect(DATABASE) as conn:
        conn.execute('''CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            password TEXT NOT NULL
        )''')
        conn.execute('''CREATE TABLE IF NOT EXISTS dialogs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            initiator_id INTEGER,
            receiver_id INTEGER,
            shared_key TEXT,
            UNIQUE(initiator_id, receiver_id)
        )''')
        conn.execute('''CREATE TABLE IF NOT EXISTS messages (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            dialog_id INTEGER,
            sender_id INTEGER,
            message TEXT,
            encrypted_key TEXT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
        )''')

def get_db():
    conn = sqlite3.connect(DATABASE, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    return conn

@app.route('/register', methods=['POST'])
def register():
    try:
        data = request.get_json()
        username = data.get('username')
        password = data.get('password')
        if not username or not password:
            return jsonify({'error': 'Username and password are required'}), 400

        with get_db() as conn:
            conn.execute('INSERT INTO users (username, password) VALUES (?, ?)',
                         (username, generate_password_hash(password)))
            conn.commit()
            user_id = conn.execute('SELECT last_insert_rowid()').fetchone()[0]
            return jsonify({'success': True, 'id': user_id, 'username': username})
    except sqlite3.IntegrityError:
        return jsonify({'error': 'Username already exists'}), 400
    except Exception:
        return jsonify({'error': 'Registration failed'}), 500

@app.route('/login', methods=['POST'])
def login():
    try:
        data = request.get_json()
        username = data.get('username')
        password = data.get('password')
        with get_db() as conn:
            user = conn.execute('SELECT id, username, password FROM users WHERE username = ?', (username,)).fetchone()
            if user and check_password_hash(user['password'], password):
                return jsonify({'id': user['id'], 'username': user['username']})
            return jsonify({'error': 'Invalid credentials'}), 401
    except Exception:
        return jsonify({'error': 'Login failed'}), 500

@app.route('/update', methods=['POST'])
def update():
    try:
        data = request.get_json()
        user_id = data.get('id')
        username = data.get('username')
        with get_db() as conn:
            result = conn.execute('UPDATE users SET username = ? WHERE id = ?', (username, user_id))
            if result.rowcount == 0:
                return jsonify({'error': 'User not found'}), 404
            conn.commit()
            return jsonify({'success': True})
    except sqlite3.IntegrityError:
        return jsonify({'error': 'Username already exists'}), 400
    except Exception:
        return jsonify({'error': 'Update failed'}), 500

@app.route('/search/<int:user_id>', methods=['GET'])
def search(user_id):
    try:
        with get_db() as conn:
            user = conn.execute('SELECT id, username FROM users WHERE id = ?', (user_id,)).fetchone()
            if user:
                return jsonify({'id': user['id'], 'username': user['username']})
            return jsonify({'error': 'User not found'}), 404
    except Exception:
        return jsonify({'error': 'Search failed'}), 500

@app.route('/initiate_dialog', methods=['POST'])
def initiate_dialog():
    try:
        data = request.get_json()
        initiator_id = data.get('initiator_id')
        receiver_id = data.get('receiver_id')
        shared_key = data.get('shared_key')
        if not all([initiator_id, receiver_id, shared_key]):
            missing = [k for k, v in {'initiator_id': initiator_id, 'receiver_id': receiver_id, 'shared_key': shared_key}.items() if not v]
            return jsonify({'error': f'Missing required parameters: {", ".join(missing)}'}), 400

        with get_db() as conn:
            existing_dialog = conn.execute(
                'SELECT id FROM dialogs WHERE (initiator_id = ? AND receiver_id = ?) OR (initiator_id = ? AND receiver_id = ?)',
                (initiator_id, receiver_id, receiver_id, initiator_id)
            ).fetchone()
            if existing_dialog:
                return jsonify({'error': 'Dialog already exists', 'dialog_id': existing_dialog['id']}), 400

            conn.execute('INSERT INTO dialogs (initiator_id, receiver_id, shared_key) VALUES (?, ?, ?)',
                         (initiator_id, receiver_id, shared_key))
            conn.commit()
            dialog_id = conn.execute('SELECT last_insert_rowid()').fetchone()[0]
            return jsonify({'success': True, 'dialog_id': dialog_id})
    except Exception:
        return jsonify({'error': 'Dialog creation failed'}), 500

@app.route('/dialogs/<int:user_id>', methods=['GET'])
def get_dialogs(user_id):
    try:
        with get_db() as conn:
            dialogs = conn.execute(
                'SELECT id, initiator_id, receiver_id, shared_key FROM dialogs WHERE initiator_id = ? OR receiver_id = ?',
                (user_id, user_id)
            ).fetchall()
            return jsonify([{
                'id': row['id'],
                'initiator_id': row['initiator_id'],
                'receiver_id': row['receiver_id'],
                'shared_key': row['shared_key']
            } for row in dialogs])
    except Exception:
        return jsonify({'error': 'Failed to retrieve dialogs'}), 500

@app.route('/messages/<int:dialog_id>', methods=['GET'])
def get_messages(dialog_id):
    try:
        with get_db() as conn:
            messages = conn.execute(
                'SELECT id, sender_id, message, encrypted_key, timestamp FROM messages WHERE dialog_id = ? ORDER BY timestamp',
                (dialog_id,)
            ).fetchall()
            return jsonify([{
                'id': row['id'],
                'sender_id': row['sender_id'],
                'message': row['message'],
                'encrypted_key': row['encrypted_key'],
                'timestamp': row['timestamp']
            } for row in messages])
    except Exception:
        return jsonify({'error': 'Failed to retrieve messages'}), 500

@app.route('/send_message', methods=['POST'])
def send_message():
    try:
        data = request.get_json()
        dialog_id = data.get('dialog_id')
        sender_id = data.get('sender_id')
        message = data.get('message')
        encrypted_key = data.get('encrypted_key')
        if not all([dialog_id, sender_id, message, encrypted_key]):
            return jsonify({'error': 'Missing required parameters'}), 400

        with get_db() as conn:
            conn.execute('INSERT INTO messages (dialog_id, sender_id, message, encrypted_key) VALUES (?, ?, ?, ?)',
                         (dialog_id, sender_id, message, encrypted_key))
            conn.commit()
            return jsonify({'success': True})
    except Exception:
        return jsonify({'error': 'Failed to send message'}), 500

@app.route('/upload_avatar/<int:user_id>', methods=['POST'])
def upload_avatar(user_id):
    try:
        if 'avatar' not in request.files:
            return jsonify({'error': 'No avatar file provided'}), 400

        file = request.files['avatar']
        if not file.filename:
            return jsonify({'error': 'No file selected'}), 400

        if file.filename.endswith(('.png', '.jpg')):
            avatar_path = os.path.join(AVATARS_DIR, f'{user_id}.png')
            file.save(avatar_path)
            avatar_url = f'http://77.105.161.171:5000/avatars/{user_id}.png'
            return jsonify({'success': True, 'avatarUrl': avatar_url})
        return jsonify({'error': 'Invalid file format. Only PNG/JPEG allowed'}), 400
    except Exception:
        return jsonify({'error': 'Failed to upload avatar'}), 500

@app.route('/avatars/<int:user_id>.png', methods=['GET'])
def get_avatar(user_id):
    avatar_path = os.path.join(AVATARS_DIR, f'{user_id}.png')
    if os.path.exists(avatar_path):
        return send_file(avatar_path, mimetype='image/png')
    return '', 404

if __name__ == '__main__':
    init_db()
    app.run(host='0.0.0.0', port=5000, threaded=True)