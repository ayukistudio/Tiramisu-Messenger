# Tiramisu Messenger

![Tiramisu Logo](https://github.com/ayukistudio/Tiramisu-Messenger/blob/main/img/logo.png)

Tiramisu is a secure, end-to-end encrypted messaging application designed to prioritize user privacy and security. Built with modern technologies, it combines a sleek, dark-themed user interface with robust cryptographic mechanisms, including Elliptic Curve Cryptography (ECC) for secure communication. Tiramisu offers a seamless experience for registering, logging in, managing contacts, and exchanging encrypted messages, all while maintaining a high level of security.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Technical Details](#technical-details)
  - [Backend](#backend)
  - [Frontend](#frontend)
  - [Client Application](#client-application)
- [ECC Encryption Explained](#ecc-encryption-explained)
  - [How ECC Works in Tiramisu](#how-ecc-works-in-tiramisu)
  - [Key Management](#key-management)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **End-to-End Encryption**: Messages are encrypted using ECC to ensure only the intended recipient can read them.
- **User Authentication**: Secure registration and login with password hashing.
- **Avatar Support**: Users can upload PNG/JPEG avatars to personalize their profiles.
- **Real-Time Messaging**: Instant message delivery with polling for new dialogs and messages.
- **Dark Theme UI**: Modern, Material Design-inspired interface with customizable background colors and images.
- **Contact Management**: Search and add users by ID, with local storage of contact details.
- **Key Management**: Generate, share, and store encryption keys securely.
- **Cross-Platform Potential**: Backend supports scalability, with the client built on WebView2 for Windows.

---

## Screenshots

### Main Window
![Main Window](https://github.com/ayukistudio/Tiramisu-Messenger/blob/main/img/1.png)

---

## Architecture

Tiramisu follows a client-server architecture with a clear separation of concerns:

1. **Backend (Flask Server)**:
   - Handles user authentication, dialog management, and message storage.
   - Uses SQLite for persistent storage of users, dialogs, and messages.
   - Serves avatars and manages file uploads.
   - Exposes RESTful APIs for client communication.

2. **Client (C# WinForms with WebView2)**:
   - A Windows desktop application embedding a WebView2 browser for rendering the UI.
   - Communicates with the backend via HTTP requests.
   - Manages local storage of contacts and messages in SQLite.
   - Implements ECC encryption for secure message exchange.

3. **Frontend (HTML/CSS/JavaScript)**:
   - A web-based interface served locally via an embedded HTTP listener.
   - Uses Material Design principles for a modern, responsive UI.
   - Handles user interactions, settings, and real-time updates.

---

## Technical Details

### Backend
The backend is built with **Flask**, a lightweight Python web framework, and uses **SQLite** for data storage. Key components include:

- **Database Schema**:
  - `users`: Stores user IDs, usernames, and hashed passwords.
  - `dialogs`: Manages conversation metadata, including initiator/receiver IDs and shared keys.
  - `messages`: Stores encrypted messages with dialog IDs, sender IDs, and timestamps.
- **Security**:
  - Passwords are hashed using `werkzeug.security`.
  - CORS is enabled for secure cross-origin requests.
  - Avatar uploads are restricted to PNG/JPEG formats and stored in a dedicated directory.
- **Endpoints**:
  - `/register`: Creates a new user account.
  - `/login`: Authenticates users.
  - `/update`: Updates usernames.
  - `/search/<user_id>`: Retrieves user details by ID.
  - `/initiate_dialog`: Creates a new dialog.
  - `/dialogs/<user_id>`: Lists user dialogs.
  - `/messages/<dialog_id>`: Retrieves messages for a dialog.
  - `/send_message`: Sends a new message.
  - `/upload_avatar/<user_id>`: Uploads user avatars.
  - `/avatars/<user_id>.png`: Serves avatars.

### Frontend
The frontend is a single-page application served locally via a C# HTTP listener on `http://localhost:8080`. It uses:

- **HTML/CSS**: Material Design-inspired dark theme with TailwindCSS-like animations.
- **JavaScript**: Handles dynamic interactions, including:
  - User authentication and profile management.
  - Contact search and chat initialization.
  - Real-time message polling and rendering.
  - Background customization (color/image).
- **Libraries**:
  - `JSZip` for generating encrypted key archives.
  - Custom `i18n` module for internationalization.

### Client Application
The client is a **C# WinForms** application using **WebView2** to render the frontend. Key features include:

- **MaterialSkin**: Provides a dark-themed, Material Design UI.
- **SQLite**: Local database for caching contacts and messages.
- **HttpClient**: Communicates with the backend for API calls.
- **NotifyIcon**: System tray integration with notifications for new dialogs.
- **ECCEncryption**: Custom class for generating, storing, and using ECC keys.

---

## ECC Encryption Explained

Elliptic Curve Cryptography (ECC) is a public-key cryptography approach based on the algebraic structure of elliptic curves over finite fields. It offers strong security with smaller key sizes compared to traditional algorithms like RSA, making it ideal for resource-constrained environments.

### How ECC Works in Tiramisu

Tiramisu uses ECC for end-to-end encryption of messages, ensuring that only the sender and recipient can access the message content. The process involves:

1. **Key Pair Generation**:
   - Each user generates an ECC key pair (public and private keys) using the `secp256r1` curve via the **BouncyCastle** library.
   - The private key is stored encrypted with AES-256-CBC, using a static key for simplicity (in production, this should be user-specific).
   - The public key is shared with other users to establish secure communication.

2. **Shared Secret Derivation**:
   - When two users (e.g., Alice and Bob) want to communicate, they exchange public keys.
   - Using the **ECDH (Elliptic Curve Diffie-Hellman)** algorithm, each user derives a shared secret by combining their private key with the other user's public key.
   - This shared secret is used as the encryption key for messages.

3. **Message Encryption**:
   - Messages are encrypted using **AES-CBC** with the shared secret as the key.
   - The encrypted message is encoded in Base64 and stored on the server with the shared key (public key) for decryption by the recipient.
   - The server only sees encrypted data, ensuring end-to-end privacy.

4. **Message Decryption**:
   - The recipient retrieves the encrypted message and the sender's public key.
   - Using their private key, they derive the same shared secret via ECDH.
   - The message is decrypted using AES-CBC with the shared secret.

### Key Management

- **Key Storage**:
  - Private keys are stored in the `keys/` directory, encrypted with AES-256-CBC.
  - Shared keys (public keys) are cached locally in SQLite and stored on the server for each dialog.
  - Keys are loaded on demand and cached in memory for performance.

- **Key Exchange**:
  - Users can generate a shared key via the UI, which is saved locally and sent to the recipient.
  - The `generateKeyArchive` function creates a password-protected ZIP file containing the key and a random password, which can be shared securely out-of-band.

- **Security Considerations**:
  - The static AES key for key encryption is a simplification; production systems should use user-derived keys or a key management service.
  - Key rotation is not implemented but could be added for enhanced security.
  - The application assumes secure key exchange; in practice, this should be done via a trusted channel.

---

## Installation

### Prerequisites
- **Python 3.8+** (for the backend)
- **.NET Framework 4.8** (for the client)
- **Node.js** (optional, for frontend development)
- **WebView2 Runtime** (included with Windows 11 or downloadable)
- **SQLite** (included with Python and C#)

### Steps

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/ayukistudio/Tiramisu-Messenger.git
   cd Tiramisu-Messenger
   ```

2. **Backend Setup**:
   ```bash
   cd server
   pip install flask flask-cors werkzeug
   python server.py
   ```
   The server runs on `http://localhost:5000`.

3. **Client Setup**:
   - Open `TiramisuClient.sln` in Visual Studio.
   - Restore NuGet packages (e.g., `MaterialSkin`, `Microsoft.Web.WebView2`, `Newtonsoft.Json`, `BouncyCastle`).
   - Build and run the solution.
   - Ensure the `gui/` directory contains the frontend files (`index.html`, `chat.html`, `app.js`, etc.).

4. **Frontend Development** (Optional):
   - Modify `gui/` files as needed.
   - Serve locally via the client’s HTTP listener (`http://localhost:8080`).

---

## Usage

1. **Register**:
   - Open the client and navigate to the registration page.
   - Enter a username, password, and optional avatar.
   - Submit to create an account.

2. **Login**:
   - Use your credentials to log in.
   - The main window displays your profile and contacts.

3. **Add Contacts**:
   - Search for users by their ID.
   - Added contacts appear in the sidebar.

4. **Start a Chat**:
   - Click a contact to open a chat window.
   - Generate or set a shared key for encryption.
   - Send messages, which are encrypted and stored securely.

5. **Customize**:
   - Update your username or avatar in the profile section.
   - Change background colors or images in settings.

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add YourFeature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a Pull Request.

Please include tests and update documentation as needed.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

*Built with ❤️ by the AyukiDev*