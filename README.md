# Tiramisu Messenger

<img src="https://github.com/ayukistudio/Tiramisu-Messenger/blob/main/img/logo.png" alt="Tiramisu Logo" width="150">

**Tiramisu** is an experimental, end-to-end encrypted messaging application developed as a learning project to explore secure communication and web application development. Built with modern technologies, it features a sleek, dark-themed interface and robust cryptographic mechanisms, including Elliptic Curve Cryptography (ECC). This project is purely a proof-of-concept and serves as a testbed for implementing secure messaging in a web-based environment.

> **Note**: Tiramisu is an educational experiment and not intended for production use. It was created to deepen understanding of encryption, client-server architecture, and UI development.

**Developer**: AyukiDev

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

- **End-to-End Encryption**: Messages are encrypted using ECC for secure communication.
- **User Authentication**: Secure registration and login with password hashing.
- **Avatar Support**: Users can upload PNG/JPEG avatars for profile personalization.
- **Real-Time Messaging**: Instant message delivery with polling for dialogs and messages.
- **Dark Theme UI**: Material Design-inspired interface with customizable backgrounds.
- **Contact Management**: Search and add users by ID, with local contact storage.
- **Key Management**: Generate, share, and store encryption keys securely.
- **Experimental Nature**: Designed as a learning tool for encryption and web app development.

---

## Screenshots

### Main Window
![Main Window](https://github.com/ayukistudio/Tiramisu-Messenger/blob/main/img/1.png)

---

## Architecture

Tiramisu employs a client-server architecture, designed to simulate a secure messaging platform:

1. **Backend (Flask Server)**:
   - Manages user authentication, dialogs, and message storage.
   - Uses SQLite for data persistence.
   - Serves avatars and handles file uploads.
   - Provides RESTful APIs for client interaction.

2. **Client (C# WinForms with WebView2)**:
   - A Windows desktop app embedding WebView2 for rendering the UI.
   - Communicates with the backend via HTTP requests.
   - Stores contacts and messages locally in SQLite.
   - Implements ECC encryption for secure messaging.

3. **Frontend (HTML/CSS/JavaScript)**:
   - A web-based interface served locally via an HTTP listener.
   - Features a responsive, Material Design-inspired UI.
   - Handles user interactions and real-time updates.

---

## Technical Details

### Backend
The backend is powered by **Flask** and uses **SQLite** for storage. Key components include:

- **Database Schema**:
  - `users`: Stores user IDs, usernames, and hashed passwords.
  - `dialogs`: Tracks conversation metadata (initiator/receiver IDs, shared keys).
  - `messages`: Stores encrypted messages with dialog IDs, sender IDs, and timestamps.
- **Security**:
  - Passwords are hashed using `werkzeug.security`.
  - CORS is enabled for cross-origin requests.
  - Avatar uploads are restricted to PNG/JPEG and stored in a dedicated directory.
- **Endpoints**:
  - `/register`: Creates new user accounts.
  - `/login`: Authenticates users.
  - `/update`: Updates usernames.
  - `/search/<user_id>`: Retrieves user details by ID.
  - `/initiate_dialog`: Creates new dialogs.
  - `/dialogs/<user_id>`: Lists user dialogs.
  - `/messages/<dialog_id>`: Retrieves dialog messages.
  - `/send_message`: Sends messages.
  - `/upload_avatar/<user_id>`: Uploads avatars.
  - `/avatars/<user_id>.png`: Serves avatars.

### Frontend
The frontend is a single-page application served locally on `http://localhost:8080`. It includes:

- **HTML/CSS**: Dark-themed, Material Design UI with smooth animations.
- **JavaScript**: Manages:
  - User authentication and profile updates.
  - Contact search and chat initialization.
  - Real-time message polling and display.
  - Customizable background settings.
- **Libraries**:
  - `JSZip` for creating encrypted key archives.
  - Custom `i18n` module for internationalization.

### Client Application
The client is a **C# WinForms** application using **WebView2**. Features include:

- **MaterialSkin**: Dark-themed, Material Design UI.
- **SQLite**: Local storage for contacts and messages.
- **HttpClient**: Handles backend API communication.
- **NotifyIcon**: System tray notifications for new dialogs.
- **ECCEncryption**: Manages ECC key generation and encryption.

---

## ECC Encryption Explained

Elliptic Curve Cryptography (ECC) is a public-key cryptography method based on elliptic curves over finite fields. It provides strong security with smaller key sizes compared to RSA, making it efficient for secure messaging.

### How ECC Works in Tiramisu

Tiramisu uses ECC to ensure end-to-end encryption, protecting messages from unauthorized access. The process includes:

1. **Key Pair Generation**:
   - Each user generates an ECC key pair (public/private) using the `secp256r1` curve via **BouncyCastle**.
   - Private keys are encrypted with AES-256-CBC and stored locally.
   - Public keys are shared to enable secure communication.

2. **Shared Secret Derivation**:
   - Users exchange public keys to communicate securely.
   - The **ECDH (Elliptic Curve Diffie-Hellman)** algorithm derives a shared secret by combining one user’s private key with the other’s public key.
   - This shared secret serves as the encryption key.

3. **Message Encryption**:
   - Messages are encrypted with **AES-CBC** using the shared secret.
   - Encrypted messages are Base64-encoded and stored on the server alongside the public key.
   - The server handles only encrypted data, ensuring privacy.

4. **Message Decryption**:
   - The recipient retrieves the encrypted message and sender’s public key.
   - Using their private key, they derive the shared secret via ECDH.
   - The message is decrypted with AES-CBC.

### Key Management

- **Key Storage**:
  - Private keys are AES-256-CBC encrypted and stored in the `keys/` directory.
  - Shared keys (public keys) are cached in SQLite and stored on the server per dialog.
  - Keys are cached in memory for performance.

- **Key Exchange**:
  - Users generate shared keys via the UI, which are saved locally and shared with recipients.
  - The `generateKeyArchive` function creates a password-protected ZIP file for secure key sharing.

- **Security Notes**:
  - The static AES key for key encryption is a simplification for this experimental project.
  - Key rotation is not implemented but could enhance security.
  - Secure key exchange assumes a trusted out-of-band channel.

---

## Installation

### Prerequisites
- **Python 3.8+** (backend)
- **.NET Framework 4.8** (client)
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
   - Restore NuGet packages (`MaterialSkin`, `Microsoft.Web.WebView2`, `Newtonsoft.Json`, `BouncyCastle`).
   - Build and run the solution.
   - Ensure the `gui/` directory contains frontend files (`index.html`, `chat.html`, `app.js`, etc.).

4. **Frontend Development** (Optional):
   - Edit `gui/` files as needed.
   - Serve locally via the client’s HTTP listener (`http://localhost:8080`).

---

## Usage

1. **Register**:
   - Open the client and navigate to the registration page.
   - Enter a username, password, and optional avatar.

2. **Login**:
   - Log in with your credentials.
   - View your profile and contacts in the main window.

3. **Add Contacts**:
   - Search users by ID to add them to your contacts.
   - Contacts appear in the sidebar.

4. **Start a Chat**:
   - Click a contact to open a chat window.
   - Generate or set a shared key for encryption.
   - Send encrypted messages.

5. **Customize**:
   - Update your username or avatar.
   - Adjust background colors or images in settings.

---

## Contributing

Contributions are welcome to enhance this educational project! To contribute:

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/YourFeature`).
3. Commit changes (`git commit -m 'Add YourFeature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a Pull Request.

Please include tests and update documentation.

## TODO List

|  Need to fix           |     State     |
| ---------------------- | ------------- |
| Add serialization      |      ❌       |
| User manipulation Fix  |      ❌       |
| Add JWT Token          |      ❌       |
| Add DB protection      |      ❌       |
| Fix HTML Injections    |      ❌       |

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

*Built with ❤️ by the AyukiDev*
