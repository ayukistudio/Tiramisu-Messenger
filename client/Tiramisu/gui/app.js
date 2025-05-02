(async () => {
    const { backend } = window.chrome.webview.hostObjects;
    let currentContactId = parseInt(localStorage.getItem('currentContactId')) || null;
    let currentContactUsername = localStorage.getItem('currentContactUsername') || null;
    let sharedKey = '';

    function applyStyles(element, classesToAdd, classesToRemove) {
        element.classList.add(...classesToAdd);
        element.classList.remove(...classesToRemove);
    }

    function showError(element, message) {
        element.textContent = message;
        applyStyles(element, ['animate-fade-in', 'text-red-500'], ['hidden', 'animate-fade-out']);
    }

    function showSuccess(element, message) {
        element.textContent = message;
        applyStyles(element, ['animate-fade-in', 'text-green-500'], ['hidden', 'animate-fade-out', 'text-red-500']);
    }

    function setupAvatarPreview(inputId, previewId, previewImageId) {
        const input = document.getElementById(inputId);
        const preview = document.getElementById(previewId);
        const previewImage = document.getElementById(previewImageId);

        input.addEventListener('change', () => {
            const file = input.files[0];
            if (!file) {
                applyStyles(preview, ['hidden', 'animate-fade-out'], ['animate-fade-in']);
                return;
            }

            if (file.type === 'image/png' || file.type === 'image/jpeg') {
                const reader = new FileReader();
                reader.onload = (e) => {
                    previewImage.src = e.target.result;
                    applyStyles(preview, ['animate-fade-in'], ['hidden', 'animate-fade-out']);
                };
                reader.readAsDataURL(file);
            } else {
                showError(document.getElementById('error'), i18n.t('errors.imageFormat'));
                input.value = '';
                applyStyles(preview, ['hidden', 'animate-fade-out'], ['animate-fade-in']);
            }
        });
    }

    async function fileToBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    function generateRandomPassword(length) {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()';
        return Array.from({ length }, () => chars.charAt(Math.floor(Math.random() * chars.length))).join('');
    }

    async function generateKeyArchive(key) {
        const zip = new JSZip();
        const password = generateRandomPassword(32);
        zip.file('encryption_key.txt', `Encryption Key: ${key}\nPassword: ${password}`);
        const content = await zip.generateAsync({ type: 'blob', password });
        saveAs(content, 'encryption_key_archive.zip');
        return password;
    }

    function loadSettings() {
        const bgColor = localStorage.getItem('bgColor') || '#121212';
        const bgImage = localStorage.getItem('bgImage') || '';

        document.body.style.backgroundColor = bgColor;
        if (bgImage) {
            Object.assign(document.body.style, {
                backgroundImage: `url(${bgImage})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center'
            });
        } else {
            document.body.style.backgroundImage = '';
        }

        document.getElementById('bgColorInput').value = bgColor;
        document.getElementById('languageSelect').value = i18n.getLanguage();
        i18n.applyTranslations();
    }

    if (document.location.pathname.includes('register.html')) {
        const elements = {
            registerBtn: document.getElementById('registerBtn'),
            loginBtn: document.getElementById('loginBtn'),
            username: document.getElementById('username'),
            password: document.getElementById('password'),
            avatar: document.getElementById('avatar'),
            error: document.getElementById('error')
        };

        setupAvatarPreview('avatar', 'avatarPreview', 'previewImage');

        elements.registerBtn.addEventListener('click', async () => {
            const username = elements.username.value.trim();
            const password = elements.password.value.trim();
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (!username || !password) {
                showError(elements.error, i18n.t('errors.emptyFields'));
                return;
            }

            try {
                const response = await backend.Register(username, password);
                const result = JSON.parse(response);

                if (result.success) {
                    if (elements.avatar.files.length > 0) {
                        const file = elements.avatar.files[0];
                        try {
                            const fileBase64 = await fileToBase64(file);
                            const avatarResponse = await backend.UploadAvatar(result.id, fileBase64, file.name);
                            const avatarResult = JSON.parse(avatarResponse);
                            showSuccess(elements.error, avatarResult.success ? i18n.t('success.avatarUploaded') : (avatarResult.error || i18n.t('errors.avatarUpload')));
                        } catch (ex) {
                            showError(elements.error, `${i18n.t('errors.avatarUpload')}: ${ex.message}`);
                        }
                    }
                    showSuccess(elements.error, i18n.t('success.registered'));
                    setTimeout(() => (window.location.href = '/index.html'), 1000);
                } else {
                    showError(elements.error, result.error || i18n.t('errors.register'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.register')}: ${ex.message}`);
            }
        });

        elements.loginBtn.addEventListener('click', () => {
            window.location.href = '/index.html';
        });
    }

    if (document.location.pathname.includes('index.html')) {
        const elements = {
            loginBtn: document.getElementById('loginBtn'),
            registerBtn: document.getElementById('registerBtn'),
            username: document.getElementById('username'),
            password: document.getElementById('password'),
            editProfileBtn: document.getElementById('editProfileBtn'),
            saveProfileBtn: document.getElementById('saveProfileBtn'),
            searchBtn: document.getElementById('searchBtn'),
            searchUserId: document.getElementById('searchUserId'),
            settingsBtn: document.getElementById('settingsBtn'),
            settingsPanel: document.getElementById('settingsPanel'),
            bgColorInput: document.getElementById('bgColorInput'),
            bgImageInput: document.getElementById('bgImageInput'),
            bgPreview: document.getElementById('bgPreview'),
            bgPreviewImage: document.getElementById('bgPreviewImage'),
            languageSelect: document.getElementById('languageSelect'),
            applySettingsBtn: document.getElementById('applySettingsBtn'),
            closeSettingsBtn: document.getElementById('closeSettingsBtn'),
            error: document.getElementById('error'),
            sidebar: document.getElementById('sidebar'),
            resizer: document.querySelector('.resizer')
        };

        loadSettings();
        setupAvatarPreview('newAvatar', 'avatarPreview', 'previewImage');

        elements.settingsBtn.addEventListener('click', () => {
            applyStyles(elements.settingsPanel, ['animate-fade-in'], ['hidden']);
            Object.assign(elements.settingsPanel.style, { zIndex: '50', pointerEvents: 'auto', opacity: '1' });
        });

        elements.closeSettingsBtn.addEventListener('click', () => {
            applyStyles(elements.settingsPanel, ['animate-fade-out'], []);
            setTimeout(() => {
                applyStyles(elements.settingsPanel, ['hidden'], ['animate-fade-in', 'animate-fade-out']);
                Object.assign(elements.settingsPanel.style, { zIndex: '-1', pointerEvents: 'none', opacity: '0' });
            }, 500);
        });

        elements.bgImageInput.addEventListener('change', () => {
            const file = elements.bgImageInput.files[0];
            if (file && (file.type === 'image/png' || file.type === 'image/jpeg')) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    elements.bgPreviewImage.src = e.target.result;
                    applyStyles(elements.bgPreview, ['animate-fade-in'], ['hidden', 'animate-fade-out']);
                };
                reader.readAsDataURL(file);
            } else {
                showError(elements.error, i18n.t('errors.imageFormat'));
                elements.bgImageInput.value = '';
                applyStyles(elements.bgPreview, ['hidden', 'animate-fade-out'], ['animate-fade-in']);
            }
        });

        elements.applySettingsBtn.addEventListener('click', () => {
            const bgColor = elements.bgColorInput.value;
            const bgImage = elements.bgPreviewImage.src && elements.bgPreviewImage.src !== 'data:' ? elements.bgPreviewImage.src : '';
            const lang = elements.languageSelect.value;

            localStorage.setItem('bgColor', bgColor);
            localStorage.setItem('language', lang);
            if (bgImage) {
                localStorage.setItem('bgImage', bgImage);
            } else {
                localStorage.removeItem('bgImage');
            }

            document.body.style.backgroundColor = bgColor;
            if (bgImage) {
                Object.assign(document.body.style, {
                    backgroundImage: `url(${bgImage})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center'
                });
            } else {
                document.body.style.backgroundImage = '';
            }

            i18n.applyTranslations();
            showSuccess(elements.error, i18n.t('success.settingsApplied'));

            applyStyles(elements.settingsPanel, ['animate-fade-out'], []);
            setTimeout(() => {
                applyStyles(elements.settingsPanel, ['hidden'], ['animate-fade-in', 'animate-fade-out']);
                Object.assign(elements.settingsPanel.style, { zIndex: '-1', pointerEvents: 'none', opacity: '0' });
            }, 500);
        });

        let isResizing = false;
        elements.resizer.addEventListener('mousedown', (e) => {
            isResizing = true;
            document.body.style.cursor = 'col-resize';
            e.preventDefault();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isResizing) return;
            const containerWidth = elements.sidebar.parentElement.offsetWidth;
            const newWidth = (e.clientX / containerWidth) * 100;
            if (newWidth >= 20 && newWidth <= 50) {
                elements.sidebar.style.width = `${newWidth}%`;
                localStorage.setItem('sidebarWidth', newWidth);
            }
        });

        document.addEventListener('mouseup', () => {
            if (isResizing) {
                isResizing = false;
                document.body.style.cursor = 'default';
            }
        });

        const savedWidth = localStorage.getItem('sidebarWidth');
        if (savedWidth) elements.sidebar.style.width = `${savedWidth}%`;

        elements.loginBtn.addEventListener('click', async () => {
            const username = elements.username.value.trim();
            const password = elements.password.value.trim();
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (!username || !password) {
                showError(elements.error, i18n.t('errors.emptyFields'));
                return;
            }

            try {
                const response = await backend.Login(username, password);
                const result = JSON.parse(response);
                if (result.id) {
                    applyStyles(document.getElementById('loginSection'), ['hidden', 'animate-fade-out'], ['animate-fade-in']);
                    setTimeout(() => {
                        applyStyles(document.getElementById('mainSection'), ['animate-fade-in'], ['hidden', 'animate-fade-out']);
                    }, 500);
                    await loadProfile();
                    await loadContacts();
                } else {
                    showError(elements.error, result.error || i18n.t('errors.login'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.login')}: ${ex.message}`);
            }
        });

        elements.registerBtn.addEventListener('click', () => {
            window.location.href = '/register.html';
        });

        elements.editProfileBtn.addEventListener('click', () => {
            applyStyles(document.getElementById('profile'), ['hidden', 'animate-fade-out'], ['animate-fade-in']);
            setTimeout(() => {
                applyStyles(document.getElementById('editProfileSection'), ['animate-fade-in'], ['hidden', 'animate-fade-out']);
            }, 500);
        });

        elements.saveProfileBtn.addEventListener('click', async () => {
            const newUsername = document.getElementById('newUsername').value.trim();
            const newAvatarInput = document.getElementById('newAvatar');
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (newUsername) {
                try {
                    const response = await backend.UpdateUsername(newUsername);
                    const result = JSON.parse(response);
                    if (!result.success) {
                        showError(elements.error, result.error || i18n.t('errors.updateUsername'));
                        return;
                    }
                } catch (ex) {
                    showError(elements.error, `${i18n.t('errors.updateUsername')}: ${ex.message}`);
                    return;
                }
            }

            if (newAvatarInput.files.length > 0) {
                const file = newAvatarInput.files[0];
                try {
                    const userInfo = JSON.parse(await backend.GetUserInfo());
                    const fileBase64 = await fileToBase64(file);
                    const avatarResponse = await backend.UploadAvatar(userInfo.id, fileBase64, file.name);
                    const avatarResult = JSON.parse(avatarResponse);
                    if (!avatarResult.success) {
                        showError(elements.error, avatarResult.error || i18n.t('errors.avatarUpload'));
                        return;
                    }
                    showSuccess(elements.error, i18n.t('success.avatarUploaded'));
                    await loadProfile();
                } catch (ex) {
                    showError(elements.error, `${i18n.t('errors.avatarUpload')}: ${ex.message}`);
                    return;
                }
            }

            applyStyles(document.getElementById('editProfileSection'), ['hidden', 'animate-fade-out'], ['animate-fade-in']);
            setTimeout(() => {
                applyStyles(document.getElementById('profile'), ['animate-fade-in'], ['hidden', 'animate-fade-out']);
            }, 500);
            await loadProfile();
        });

        elements.searchBtn.addEventListener('click', async () => {
            const userId = parseInt(elements.searchUserId.value);
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (isNaN(userId)) {
                showError(elements.error, i18n.t('errors.invalidId'));
                return;
            }

            try {
                const response = await backend.SearchUser(userId);
                const result = JSON.parse(response);
                if (result.id) {
                    await loadContacts();
                    showSuccess(elements.error, i18n.t('success.userAdded'));
                } else {
                    showError(elements.error, result.error || i18n.t('errors.userNotFound'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.userNotFound')}: ${ex.message}`);
            }
        });

        window.addEventListener('message', (event) => {
            if (event.data?.type === 'CONTACTS_UPDATED') {
                loadContacts();
            }
        });

        async function loadProfile() {
            try {
                const response = await backend.GetUserInfo();
                const user = JSON.parse(response);
                const avatarUrl = user.avatarUrl || 'https://via.placeholder.com/40';
                const userAvatar = document.getElementById('userAvatar');
                userAvatar.src = avatarUrl;
                userAvatar.onerror = () => (userAvatar.src = 'https://via.placeholder.com/40');
                document.getElementById('userName').textContent = user.username;
                document.getElementById('userId').textContent = `ID: ${user.id}`;
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.loadProfile')}: ${ex.message}`);
            }
        }

        async function loadContacts() {
            try {
                const response = await backend.GetContacts();
                const contacts = JSON.parse(response);
                const contactsDiv = document.getElementById('contacts');
                contactsDiv.innerHTML = '';

                for (const contact of contacts) {
                    const avatarResponse = await backend.GetAvatar(contact.id);
                    const avatarResult = JSON.parse(avatarResponse);
                    const avatarUrl = avatarResult.avatarUrl || 'https://via.placeholder.com/40';
                    const div = document.createElement('div');
                    div.className = 'flex items-center p-3 bg-gray-700 rounded-md cursor-pointer hover:bg-gray-600 transition-all duration-200 animate-fade-in';
                    div.innerHTML = `
                        <img src="${avatarUrl}" class="w-10 h-10 rounded-full mr-3" onerror="this.src='https://via.placeholder.com/40'">
                        <span>${contact.username}</span>
                    `;
                    div.addEventListener('click', () => {
                        currentContactId = contact.id;
                        currentContactUsername = contact.username;
                        localStorage.setItem('currentContactId', contact.id);
                        localStorage.setItem('currentContactUsername', contact.username);
                        backend.OpenChat(currentContactId, currentContactUsername);
                    });
                    contactsDiv.appendChild(div);
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.loadContacts')}: ${ex.message}`);
            }
        }
    }

    if (document.location.pathname.includes('chat.html')) {
        const elements = {
            contactName: document.getElementById('contactName'),
            contactAvatar: document.getElementById('contactAvatar'),
            messages: document.getElementById('messages'),
            messageInput: document.getElementById('messageInput'),
            sendBtn: document.getElementById('sendBtn'),
            sharedKeyInput: document.getElementById('sharedKeyInput'),
            setKeyBtn: document.getElementById('setKeyBtn'),
            generateKeyBtn: document.getElementById('generateKeyBtn'),
            copyKeyBtn: document.getElementById('copyKeyBtn'),
            generateArchiveBtn: document.getElementById('generateArchiveBtn'),
            toggleKeySettings: document.getElementById('toggleKeySettings'),
            keySettings: document.getElementById('keySettings'),
            error: document.getElementById('error')
        };

        currentContactId = window.currentContactId || currentContactId;
        currentContactUsername = window.currentContactUsername || currentContactUsername;

        if (!currentContactId) {
            showError(elements.error, i18n.t('errors.noContactId'));
            return;
        }

        localStorage.setItem('currentContactId', currentContactId);
        localStorage.setItem('currentContactUsername', currentContactUsername);

        elements.contactName.textContent = currentContactUsername || i18n.t('chat.defaultName');
        try {
            const avatarResponse = await backend.GetAvatar(currentContactId);
            const avatarResult = JSON.parse(avatarResponse);
            const avatarUrl = avatarResult.avatarUrl || 'https://via.placeholder.com/40';
            elements.contactAvatar.src = avatarUrl;
            elements.contactAvatar.onerror = () => (elements.contactAvatar.src = 'https://via.placeholder.com/40');
        } catch (ex) {
            showError(elements.error, `${i18n.t('errors.loadAvatar')}: ${ex.message}`);
        }

        const savedBackground = localStorage.getItem('bgImage');
        if (savedBackground) {
            Object.assign(document.body.style, {
                backgroundImage: `url(${savedBackground})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center'
            });
        }

        elements.toggleKeySettings.addEventListener('click', () => {
            if (elements.keySettings.classList.contains('hidden')) {
                applyStyles(elements.keySettings, ['slide-down'], ['hidden', 'slide-up']);
                elements.toggleKeySettings.querySelector('i').classList.replace('fa-chevron-down', 'fa-chevron-up');
            } else {
                applyStyles(elements.keySettings, ['slide-up'], []);
                setTimeout(() => {
                    applyStyles(elements.keySettings, ['hidden'], ['slide-down', 'slide-up']);
                }, 300);
                elements.toggleKeySettings.querySelector('i').classList.replace('fa-chevron-up', 'fa-chevron-down');
            }
        });

        async function loadMessages() {
            try {
                const response = await backend.GetMessages(currentContactId);
                const messages = JSON.parse(response);
                if (!Array.isArray(messages)) throw new Error('Messages must be an array');

                elements.messages.innerHTML = '';
                for (const msg of messages) {
                    const div = document.createElement('div');
                    div.className = `p-3 rounded-md max-w-xs message-enter ${
                        msg.sender_id === currentContactId ? 'bg-gray-700' : 'bg-blue-600 text-white ml-auto'
                    } transition-all duration-200 animate-fade-in`;
                    div.innerHTML = `
                        <p>${msg.message}</p>
                        <p class="text-xs text-gray-300">${msg.timestamp}</p>
                    `;
                    elements.messages.appendChild(div);
                }
                elements.messages.scrollTop = elements.messages.scrollHeight;
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.loadMessages')}: ${ex.message}`);
            }
        }

        elements.sendBtn.addEventListener('click', async () => {
            const message = elements.messageInput.value.trim();
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (!message) {
                showError(elements.error, i18n.t('errors.emptyMessage'));
                return;
            }

            try {
                const response = await backend.SendMessage(currentContactId, message, sharedKey);
                const result = JSON.parse(response);
                if (result.success) {
                    elements.messageInput.value = '';
                    await loadMessages();
                } else {
                    showError(elements.error, result.error || i18n.t('errors.sendMessage'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.sendMessage')}: ${ex.message}`);
            }
        });

        elements.setKeyBtn.addEventListener('click', async () => {
            const key = elements.sharedKeyInput.value.trim();
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);

            if (!key) {
                showError(elements.error, i18n.t('errors.emptyKey'));
                return;
            }

            try {
                const response = await backend.SetSharedKey(currentContactId, key);
                const result = JSON.parse(response);
                if (result.success) {
                    sharedKey = key;
                    elements.sharedKeyInput.value = '';
                    elements.sendBtn.disabled = false;
                    elements.copyKeyBtn.disabled = false;
                    elements.generateArchiveBtn.disabled = false;
                    showSuccess(elements.error, i18n.t('success.keySet'));
                } else {
                    showError(elements.error, result.error || i18n.t('errors.setKey'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.setKey')}: ${ex.message}`);
            }
        });

        elements.generateKeyBtn.addEventListener('click', async () => {
            applyStyles(elements.error, ['hidden', 'animate-fade-out'], ['animate-fade-in']);
            try {
                const response = await backend.GenerateSharedKey(currentContactId);
                const result = JSON.parse(response);
                if (result.sharedKey) {
                    sharedKey = result.sharedKey;
                    elements.sendBtn.disabled = false;
                    elements.copyKeyBtn.disabled = false;
                    elements.generateArchiveBtn.disabled = false;
                    showSuccess(elements.error, i18n.t('success.keyGenerated').replace('{key}', sharedKey));
                } else {
                    showError(elements.error, result.error || i18n.t('errors.generateKey'));
                }
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.generateKey')}: ${ex.message}`);
            }
        });

        elements.copyKeyBtn.addEventListener('click', () => {
            if (sharedKey) {
                navigator.clipboard.writeText(sharedKey).then(() => {
                    showSuccess(elements.error, i18n.t('success.keyCopied'));
                }).catch((ex) => {
                    showError(elements.error, `${i18n.t('errors.copyKey')}: ${ex.message}`);
                });
            } else {
                showError(elements.error, i18n.t('errors.noKey'));
            }
        });

        elements.generateArchiveBtn.addEventListener('click', async () => {
            if (!sharedKey) {
                showError(elements.error, i18n.t('errors.noKey'));
                return;
            }
            try {
                const password = await generateKeyArchive(sharedKey);
                showSuccess(elements.error, i18n.t('success.archiveCreated').replace('{password}', password));
            } catch (ex) {
                showError(elements.error, `${i18n.t('errors.createArchive')}: ${ex.message}`);
            }
        });

        await loadMessages();
        setInterval(loadMessages, 5000);
    }
})();