// Global state
let isAuthenticated = false;
let userData = null;

// API endpoints
const API = {
    baseUrl: 'http://localhost:5000/api',
    auth: {
        login: '/auth/login',
        logout: '/auth/logout',
    },
    tasks: '/tasks',
    requests: '/requests'
};

// DOM Elements
const loginButton = document.getElementById('loginButton');
const userNameElement = document.getElementById('userName');

// Event Listeners
document.addEventListener('DOMContentLoaded', () => {
    checkAuthStatus();
    setupEventListeners();
});

function setupEventListeners() {
    loginButton.addEventListener('click', handleLoginClick);
}

// Auth functions
async function checkAuthStatus() {
    const token = localStorage.getItem('token');
    if (token) {
        try {
            const response = await fetch(`${API.baseUrl}/auth/verify`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (response.ok) {
                const data = await response.json();
                handleAuthSuccess(data);
            } else {
                handleAuthFailure();
            }
        } catch (error) {
            console.error('Auth check failed:', error);
            handleAuthFailure();
        }
    }
}

function handleLoginClick() {
    if (isAuthenticated) {
        logout();
    } else {
        showLoginModal();
    }
}

function handleAuthSuccess(data) {
    isAuthenticated = true;
    userData = data;
    userNameElement.textContent = data.userName;
    loginButton.textContent = 'Выйти';
    updateUI();
}

function handleAuthFailure() {
    isAuthenticated = false;
    userData = null;
    userNameElement.textContent = 'Гость';
    loginButton.textContent = 'Войти';
    localStorage.removeItem('token');
    updateUI();
}

// UI functions
function updateUI() {
    const protectedElements = document.querySelectorAll('.protected-content');
    protectedElements.forEach(element => {
        element.style.display = isAuthenticated ? 'block' : 'none';
    });
}

function showLoginModal() {
    // Implementation of login modal
}

// API functions
async function fetchTasks() {
    try {
        const response = await fetch(`${API.baseUrl}${API.tasks}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        if (response.ok) {
            const tasks = await response.json();
            renderTasks(tasks);
        }
    } catch (error) {
        console.error('Failed to fetch tasks:', error);
    }
}

async function createRequest(requestData) {
    try {
        const response = await fetch(`${API.baseUrl}${API.requests}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(requestData)
        });
        if (response.ok) {
            showNotification('Заявка успешно создана', 'success');
        } else {
            throw new Error('Failed to create request');
        }
    } catch (error) {
        console.error('Request creation failed:', error);
        showNotification('Ошибка при создании заявки', 'error');
    }
}

// Utility functions
function showNotification(message, type = 'info') {
    // Implementation of notification system
}

// Initialize on page load
window.addEventListener('load', () => {
    checkAuthStatus();
});