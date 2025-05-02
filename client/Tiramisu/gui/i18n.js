const i18n = {
    translations: {
        ru: {
            'settings.title': 'Настройки',
            'settings.backgroundColor': 'Цвет фона',
            'settings.backgroundImage': 'Фоновое изображение',
            'settings.chooseImage': 'Выбрать изображение',
            'settings.language': 'Язык',
            'settings.preview': 'Предпросмотр',
            'settings.apply': 'Применить',
            'settings.close': 'Закрыть',
            'auth.username': 'Имя пользователя',
            'auth.password': 'Пароль',
            'auth.login': 'Войти',
            'auth.register': 'Зарегистрироваться',
            'profile.newUsername': 'Новое имя',
            'profile.chooseAvatar': 'Выбрать аватар',
            'profile.preview': 'Предпросмотр',
            'profile.save': 'Сохранить',
            'search.userId': 'ID пользователя',
            'search.search': 'Поиск',
            'chat.defaultName': 'Чат',
            'errors.imageFormat': 'Только PNG/JPEG файлы',
            'errors.emptyFields': 'Введите имя пользователя и пароль',
            'errors.login': 'Ошибка входа',
            'errors.register': 'Ошибка регистрации',
            'errors.updateUsername': 'Ошибка обновления имени',
            'errors.avatarUpload': 'Не удалось загрузить аватар',
            'errors.invalidId': 'Введите корректный ID',
            'errors.userNotFound': 'Пользователь не найден',
            'errors.loadProfile': 'Не удалось загрузить профиль',
            'errors.loadContacts': 'Не удалось загрузить контакты',
            'errors.noContactId': 'Ошибка: ID контакта не установлен',
            'errors.loadAvatar': 'Не удалось загрузить аватар',
            'errors.loadMessages': 'Не удалось загрузить сообщения',
            'errors.emptyMessage': 'Введите сообщение',
            'errors.sendMessage': 'Ошибка отправки сообщения',
            'errors.emptyKey': 'Введите ключ',
            'errors.setKey': 'Ошибка установки ключа',
            'errors.generateKey': 'Ошибка генерации ключа',
            'errors.noKey': 'Ключ не установлен',
            'errors.copyKey': 'Не удалось скопировать ключ',
            'errors.createArchive': 'Не удалось создать архив',
            'success.avatarUploaded': 'Аватар успешно загружен',
            'success.registered': 'Регистрация успешна! Перенаправление...',
            'success.userAdded': 'Пользователь добавлен в контакты',
            'success.settingsApplied': 'Настройки применены',
            'success.keySet': 'Ключ успешно установлен',
            'success.keyGenerated': 'Сгенерирован ключ: {key}. Поделитесь им с контактом.',
            'success.keyCopied': 'Ключ скопирован в буфер обмена',
            'success.archiveCreated': 'Архив создан. Пароль: {password}. Сохраните его!'
        },
        en: {
            'settings.title': 'Settings',
            'settings.backgroundColor': 'Background Color',
            'settings.backgroundImage': 'Background Image',
            'settings.chooseImage': 'Choose Image',
            'settings.language': 'Language',
            'settings.preview': 'Preview',
            'settings.apply': 'Apply',
            'settings.close': 'Close',
            'auth.username': 'Username',
            'auth.password': 'Password',
            'auth.login': 'Login',
            'auth.register': 'Register',
            'profile.newUsername': 'New Username',
            'profile.chooseAvatar': 'Choose Avatar',
            'profile.preview': 'Preview',
            'profile.save': 'Save',
            'search.userId': 'User ID',
            'search.search': 'Search',
            'chat.defaultName': 'Chat',
            'errors.imageFormat': 'Only PNG/JPEG files',
            'errors.emptyFields': 'Enter username and password',
            'errors.login': 'Login error',
            'errors.register': 'Registration error',
            'errors.updateUsername': 'Error updating username',
            'errors.avatarUpload': 'Failed to upload avatar',
            'errors.invalidId': 'Enter a valid ID',
            'errors.userNotFound': 'User not found',
            'errors.loadProfile': 'Failed to load profile',
            'errors.loadContacts': 'Failed to load contacts',
            'errors.noContactId': 'Error: Contact ID not set',
            'errors.loadAvatar': 'Failed to load avatar',
            'errors.loadMessages': 'Failed to load messages',
            'errors.emptyMessage': 'Enter a message',
            'errors.sendMessage': 'Error sending message',
            'errors.emptyKey': 'Enter a key',
            'errors.setKey': 'Error setting key',
            'errors.generateKey': 'Error generating key',
            'errors.noKey': 'Key not set',
            'errors.copyKey': 'Failed to copy key',
            'errors.createArchive': 'Failed to create archive',
            'success.avatarUploaded': 'Avatar uploaded successfully',
            'success.registered': 'Registration successful! Redirecting...',
            'success.userAdded': 'User added to contacts',
            'success.settingsApplied': 'Settings applied',
            'success.keySet': 'Key set successfully',
            'success.keyGenerated': 'Generated key: {key}. Share it with the contact.',
            'success.keyCopied': 'Key copied to clipboard',
            'success.archiveCreated': 'Archive created. Password: {password}. Save it!'
        }
    },

    getLanguage() {
        return localStorage.getItem('language') || 'ru';
    },

    t(key, replacements = {}) {
        const lang = this.getLanguage();
        let translation = this.translations[lang][key] || key;
        for (const [placeholder, value] of Object.entries(replacements)) {
            translation = translation.replace(`{${placeholder}}`, value);
        }
        return translation;
    },

    applyTranslations() {
        const lang = this.getLanguage();
        document.querySelectorAll('[data-i18n]').forEach(elem => {
            const key = elem.getAttribute('data-i18n');
            elem.textContent = this.t(key);
        });
        document.querySelectorAll('[data-i18n-placeholder]').forEach(elem => {
            const key = elem.getAttribute('data-i18n-placeholder');
            elem.placeholder = this.t(key);
        });
    }
};

window.i18n = i18n;