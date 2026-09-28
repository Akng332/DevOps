// Базовый URL нашего API (так как фронтенд и бэкенд на одном сервере)
const API_URL = '/api/auth';

// Переключение между вкладками Вход/Регистрация
function switchTab(tab) {
    const loginForm = document.getElementById('login-form');
    const registerForm = document.getElementById('register-form');
    const tabLogin = document.getElementById('tab-login');
    const tabRegister = document.getElementById('tab-register');

    if (tab === 'login') {
        loginForm.classList.remove('hidden');
        registerForm.classList.add('hidden');
        tabLogin.classList.add('active');
        tabRegister.classList.remove('active');
    } else {
        loginForm.classList.add('hidden');
        registerForm.classList.remove('hidden');
        tabLogin.classList.remove('active');
        tabRegister.classList.add('active');
    }
}

// Обработка регистрации
async function handleRegister(event) {
    event.preventDefault();
    const username = document.getElementById('register-username').value;
    const password = document.getElementById('register-password').value;
    const errorP = document.getElementById('register-error');
    errorP.textContent = '';

    try {
        const response = await fetch(`${API_URL}/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data || 'Ошибка регистрации');
        }

        // Сохраняем токен
        localStorage.setItem('token', data.token);
        localStorage.setItem('username', data.username);
        
        // Показываем профиль
        showProfile();
    } catch (error) {
        errorP.textContent = error.message;
    }
}

// Обработка входа
async function handleLogin(event) {
    event.preventDefault();
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const errorP = document.getElementById('login-error');
    errorP.textContent = '';

    try {
        const response = await fetch(`${API_URL}/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data || 'Неверный логин или пароль');
        }

        // Сохраняем токен
        localStorage.setItem('token', data.token);
        localStorage.setItem('username', data.username);

        // Показываем профиль
        showProfile();
    } catch (error) {
        errorP.textContent = error.message;
    }
}

// Загрузка данных профиля (защищенный эндпоинт)
async function showProfile() {
    const token = localStorage.getItem('token');
    
    if (!token) {
        document.getElementById('auth-section').classList.remove('hidden');
        document.getElementById('profile-section').classList.add('hidden');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/me`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Сессия истекла');
        }

        const user = await response.json();

        // Заполняем данные
        document.getElementById('profile-id').textContent = user.id;
        document.getElementById('profile-username').textContent = user.username;
        document.getElementById('profile-role').textContent = user.role;
        document.getElementById('profile-created').textContent = new Date(user.createdAt).toLocaleString('ru-RU');

        // Переключаем экраны
        document.getElementById('auth-section').classList.add('hidden');
        document.getElementById('profile-section').classList.remove('hidden');
        
    } catch (error) {
        // Если токен невалидный - разлогиниваем
        handleLogout();
    }
}

// Выход из системы
function handleLogout() {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    document.getElementById('auth-section').classList.remove('hidden');
    document.getElementById('profile-section').classList.add('hidden');
    
    // Очищаем формы
    document.getElementById('login-form').reset();
    document.getElementById('register-form').reset();
}

// Проверяем статус при загрузке страницы
window.addEventListener('DOMContentLoaded', showProfile);