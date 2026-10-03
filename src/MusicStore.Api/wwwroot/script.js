const API_URL = '/api/auth';
const DISCS_API = '/api/discs';
const SALES_API = '/api/sales';

let currentUserRole = null;
let editingDiscId = null;
let allDiscs = [];

function showPage(pageName) {
    document.querySelectorAll('.page').forEach(p => p.classList.add('hidden'));
    const target = document.getElementById(`page-${pageName}`);
    if (target) target.classList.remove('hidden');
    if (pageName === 'catalog') loadCatalog();
    if (pageName === 'profile') loadSalesHistory();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function switchTab(tab) {
    const lf = document.getElementById('login-form');
    const rf = document.getElementById('register-form');
    const tl = document.getElementById('tab-login');
    const tr = document.getElementById('tab-register');
    if (tab === 'login') {
        lf.classList.remove('hidden'); rf.classList.add('hidden');
        tl.classList.add('active'); tr.classList.remove('active');
    } else {
        lf.classList.add('hidden'); rf.classList.remove('hidden');
        tl.classList.remove('active'); tr.classList.add('active');
    }
}

async function handleRegister(event) {
    event.preventDefault();
    const username = document.getElementById('register-username').value;
    const password = document.getElementById('register-password').value;
    const errorP = document.getElementById('register-error');
    errorP.textContent = '';
    try {
        const r = await fetch(`${API_URL}/register`, {
            method:'POST', headers:{'Content-Type':'application/json'},
            body: JSON.stringify({ username, password })
        });
        const data = await r.json();
        if (!r.ok) {
            let msg = 'Ошибка регистрации';
            if (typeof data === 'string') msg = data;
            else if (data.title) msg = data.title;
            else if (data.message) msg = data.message;
            throw new Error(msg);
        }
        localStorage.setItem('token', data.token);
        localStorage.setItem('username', data.username);
        document.getElementById('register-form').reset();
        await updateAuthUI();
        showPage('catalog');
    } catch (e) { errorP.textContent = e.message; }
}

async function handleLogin(event) {
    event.preventDefault();
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const errorP = document.getElementById('login-error');
    errorP.textContent = '';
    try {
        const r = await fetch(`${API_URL}/login`, {
            method:'POST', headers:{'Content-Type':'application/json'},
            body: JSON.stringify({ username, password })
        });
        const data = await r.json();
        if (!r.ok) {
            let msg = 'Неверный логин или пароль';
            if (typeof data === 'string') msg = data;
            else if (data.title) msg = data.title;
            else if (data.message) msg = data.message;
            throw new Error(msg);
        }
        localStorage.setItem('token', data.token);
        localStorage.setItem('username', data.username);
        document.getElementById('login-form').reset();
        await updateAuthUI();
        showPage('catalog');
    } catch (e) { errorP.textContent = e.message; }
}

function handleLogout() {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    currentUserRole = null;
    updateAuthUI();
    showPage('catalog');
}

async function updateAuthUI() {
    const token = localStorage.getItem('token');
    const navLogin = document.getElementById('nav-auth');
    const navProfile = document.getElementById('nav-profile');
    const addBtn = document.getElementById('add-disc-btn');

    if (!token) {
        navLogin.classList.remove('hidden');
        navProfile.classList.add('hidden');
        addBtn.classList.add('hidden');
        currentUserRole = null;
        return;
    }
    try {
        const r = await fetch(`${API_URL}/me`, { headers:{ 'Authorization': `Bearer ${token}` }});
        if (!r.ok) throw new Error();
        const user = await r.json();
        currentUserRole = user.role;
        document.getElementById('profile-id').textContent = user.id;
        document.getElementById('profile-username').textContent = user.username;
        document.getElementById('profile-role').textContent = user.role;
        document.getElementById('profile-created').textContent =
            new Date(user.createdAt).toLocaleString('ru-RU');
        navLogin.classList.add('hidden');
        navProfile.classList.remove('hidden');
        if (currentUserRole === 'admin') addBtn.classList.remove('hidden');
        else addBtn.classList.add('hidden');
    } catch (e) { handleLogout(); }
}

async function loadCatalog() {
    const grid = document.getElementById('catalog-grid');
    if (!grid) return;
    grid.innerHTML = '<p>Загрузка...</p>';
    try {
        const r = await fetch(DISCS_API);
        allDiscs = await r.json();
        grid.innerHTML = '';
        if (!allDiscs.length) { grid.innerHTML = '<p>Ассортимент пока пуст.</p>'; return; }

        allDiscs.forEach(disc => {
            const card = document.createElement('div');
            card.className = 'disc-card' + (disc.quantity <= 0 ? ' out-of-stock' : '');

            let actionButtons = '';
            if (currentUserRole === 'admin') {
                const sT = (disc.title||'').replace(/'/g,"\\'");
                const sA = (disc.authorName||'').replace(/'/g,"\\'");
                const sI = (disc.imageUrl||'').replace(/'/g,"\\'");
                actionButtons = `
                    <button class="btn-edit" onclick="editDisc(${disc.id},'${sT}','${sA}','${sI}',${disc.price},${disc.quantity})">✎</button>
                    <button class="btn-delete" onclick="deleteDisc(${disc.id})">🗑</button>
                `;
            } else if (currentUserRole && disc.quantity > 0) {
                actionButtons = `<button class="btn-buy" onclick="addToCart(${disc.id})">🛒 В корзину</button>`;
            } else if (currentUserRole) {
                actionButtons = `<button class="btn-buy" disabled>Нет в наличии</button>`;
            }

            let stockClass = 'stock-ok', stockText = `В наличии: ${disc.quantity}`;
            if (disc.quantity <= 0) { stockClass = 'stock-out'; stockText = 'Нет в наличии'; }
            else if (disc.quantity < 5) { stockClass = 'stock-low'; stockText = `Осталось: ${disc.quantity}`; }

            card.onclick = () => openDetail(disc);
            card.innerHTML = `
                <img src="${disc.imageUrl || 'https://placehold.co/400x400/cccccc/666?text=No+Cover'}"
                     class="disc-image"
                     onerror="this.src='https://placehold.co/400x400/cccccc/666?text=No+Cover'">
                <div class="disc-info">
                    <h3>${disc.title}</h3>
                    <p>${disc.authorName}</p>
                    <div class="disc-price">${disc.price} ₽</div>
                    <div class="stock-badge ${stockClass}" style="margin-top:8px;">${stockText}</div>
                </div>
                <div class="card-actions" onclick="event.stopPropagation()">
                    ${actionButtons}
                </div>`;
            grid.appendChild(card);
        });
    } catch (e) { grid.innerHTML = '<p>Ошибка загрузки.</p>'; }
}

function openModal() {
    document.getElementById('modal').classList.remove('hidden');
    document.getElementById('modal-title').textContent = 'Добавить альбом';
    editingDiscId = null;
    ['disc-title','disc-author','disc-image','disc-price','disc-quantity'].forEach(id => {
        const el = document.getElementById(id); if (el) el.value = '';
    });
    let q = document.getElementById('disc-quantity');
    if (!q) {
        const inp = document.createElement('input');
        inp.type = 'number'; inp.id = 'disc-quantity'; inp.placeholder = 'Количество на складе';
        document.getElementById('disc-price').after(inp);
    }
}

function closeModal() {
    document.getElementById('modal').classList.add('hidden');
    editingDiscId = null;
}

function editDisc(id, title, author, image, price, quantity) {
    editingDiscId = id;
    document.getElementById('modal-title').textContent = 'Редактировать альбом';
    document.getElementById('disc-title').value = title;
    document.getElementById('disc-author').value = author;
    document.getElementById('disc-image').value = image;
    document.getElementById('disc-price').value = price;
    let q = document.getElementById('disc-quantity');
    if (!q) {
        const inp = document.createElement('input');
        inp.type = 'number'; inp.id = 'disc-quantity'; inp.placeholder = 'Количество на складе';
        document.getElementById('disc-price').after(inp);
        q = inp;
    }
    q.value = quantity;
    document.getElementById('modal').classList.remove('hidden');
}

async function saveDisc() {
    const title = document.getElementById('disc-title').value.trim();
    const author = document.getElementById('disc-author').value.trim();
    const image = document.getElementById('disc-image').value.trim();
    const price = parseFloat(document.getElementById('disc-price').value);
    const quantityEl = document.getElementById('disc-quantity');
    const quantity = quantityEl ? parseInt(quantityEl.value) : 0;

    if (!title || !author || isNaN(price)) { alert('Заполните все поля!'); return; }

    const token = localStorage.getItem('token');
    const url = editingDiscId ? `${DISCS_API}/${editingDiscId}` : DISCS_API;
    const method = editingDiscId ? 'PUT' : 'POST';
    try {
        const r = await fetch(url, {
            method: method,
            headers: {'Content-Type':'application/json','Authorization':`Bearer ${token}`},
            body: JSON.stringify({
                id: editingDiscId || 0, title, authorName: author,
                imageUrl: image, price, quantity
            })
        });
        if (r.ok) { closeModal(); await loadCatalog(); }
        else { alert('Ошибка: ' + await r.text()); }
    } catch (e) { alert('Ошибка сети.'); }
}

async function deleteDisc(id) {
    if (!confirm('Удалить этот альбом?')) return;
    const token = localStorage.getItem('token');
    try {
        const r = await fetch(`${DISCS_API}/${id}`, {
            method:'DELETE', headers:{'Authorization':`Bearer ${token}`}
        });
        if (r.ok) await loadCatalog();
        else alert('Ошибка удаления.');
    } catch (e) { alert('Ошибка сети.'); }
}

// ============= КОРЗИНА =============
function getCart() {
    return JSON.parse(localStorage.getItem('cart') || '[]');
}
function saveCart(cart) {
    localStorage.setItem('cart', JSON.stringify(cart));
    renderCartUI();
}
function addToCart(id) {
    const cart = getCart();
    const existing = cart.find(i => i.discId === id);
    if (existing) { existing.quantity += 1; }
    else { cart.push({ discId: id, quantity: 1 }); }
    saveCart(cart);
    alert('Добавлено в корзину!');
}
function removeFromCart(id) {
    let cart = getCart();
    cart = cart.filter(i => i.discId !== id);
    saveCart(cart);
}
function clearCart() {
    if (!confirm('Очистить корзину?')) return;
    saveCart([]);
}
function renderCartUI() {
    const cart = getCart();
    const counter = document.getElementById('cart-count');
    const listEl = document.getElementById('cart-list');
    const totalEl = document.getElementById('cart-total');
    let total = 0, html = '';

    cart.forEach(item => {
        const disc = allDiscs.find(d => d.id === item.discId);
        if (!disc) return;
        const sum = disc.price * item.quantity;
        total += sum;
        html += `<div class="cart-item">
            <strong>${disc.title}</strong> — ${item.quantity} шт. × ${disc.price} ₽ = ${sum} ₽
            <button onclick="removeFromCart(${disc.id})" style="float:right;background:#dc3545;color:#fff;border:none;border-radius:4px;padding:2px 8px;cursor:pointer;">✕</button>
        </div>`;
    });

    if (counter) counter.textContent = cart.reduce((s,i) => s+i.quantity, 0);
    if (listEl) listEl.innerHTML = html || 'Корзина пуста';
    if (totalEl) totalEl.textContent = total;
}

async function checkout() {
    const cart = getCart();
    if (!cart.length) { alert('Корзина пуста!'); return; }
    const token = localStorage.getItem('token');
    if (!token) { showPage('auth'); return; }
    try {
        const r = await fetch(`${SALES_API}/checkout`, {
            method:'POST',
            headers:{'Content-Type':'application/json','Authorization':`Bearer ${token}`},
            body: JSON.stringify(cart)
        });
        if (r.ok) {
            const data = await r.json();
            alert(`Покупка оформлена! Сумма: ${data.total} ₽`);
            saveCart([]);
            await loadCatalog();
            await loadSalesHistory();
        } else {
            alert('Ошибка: ' + await r.text());
        }
    } catch (e) { alert('Ошибка сети.'); }
}

async function loadSalesHistory() {
    const el = document.getElementById('sales-list');
    if (!el) return;
    const token = localStorage.getItem('token');
    if (!token) { el.innerHTML = 'Войдите, чтобы увидеть историю.'; return; }
    try {
        const r = await fetch(`${SALES_API}/my`, { headers:{'Authorization':`Bearer ${token}`} });
        if (!r.ok) { el.innerHTML = 'Ошибка загрузки.'; return; }
        const sales = await r.json();
        if (!sales.length) { el.innerHTML = 'Покупок пока нет.'; return; }
        el.innerHTML = sales.map(s => `
            <div class="sale-item">
                <strong>${s.discTitle}</strong> — ${s.quantity} шт. на ${s.totalAmount} ₽
                <div style="font-size:12px;color:#888;">${new Date(s.saleDate).toLocaleString('ru-RU')}</div>
            </div>
        `).join('');
    } catch (e) { el.innerHTML = 'Ошибка.'; }
}

// ============= ДЕТАЛИ АЛЬБОМА =============
function openDetail(disc) {
    const img = document.getElementById('detail-image');
    img.src = disc.imageUrl || 'https://placehold.co/400x400/cccccc/666?text=No+Cover';
    img.onerror = function(){ this.src='https://placehold.co/400x400/cccccc/666?text=No+Cover'; };
    document.getElementById('detail-title').textContent = disc.title;
    document.getElementById('detail-author').textContent = disc.authorName;
    document.getElementById('detail-price').textContent = disc.price + ' ₽';

    let stockText = `В наличии: ${disc.quantity}`;
    if (disc.quantity <= 0) stockText = 'Нет в наличии';
    document.getElementById('detail-price').innerHTML =
        `${disc.price} ₽ <span style="font-size:14px;color:#666;font-weight:normal;display:block;margin-top:5px;">${stockText}</span>`;

    const actionsDiv = document.getElementById('detail-actions');
    let buttons = '';
    if (currentUserRole === 'admin') {
        const sT = (disc.title||'').replace(/'/g,"\\'");
        const sA = (disc.authorName||'').replace(/'/g,"\\'");
        const sI = (disc.imageUrl||'').replace(/'/g,"\\'");
        buttons = `
            <button class="btn-edit" onclick="editFromDetail(${disc.id},'${sT}','${sA}','${sI}',${disc.price},${disc.quantity})">✎ Редактировать</button>
            <button class="btn-delete" onclick="deleteFromDetail(${disc.id})">🗑 Удалить</button>`;
    } else if (currentUserRole && disc.quantity > 0) {
        buttons = `<button class="btn-buy" onclick="addToCart(${disc.id})">🛒 В корзину</button>`;
    } else if (currentUserRole) {
        buttons = `<p style="color:#888;">Товара нет в наличии</p>`;
    } else {
        buttons = `<p style="color:#888;">Войдите, чтобы добавить в корзину</p>`;
    }
    actionsDiv.innerHTML = buttons;
    document.getElementById('detail-modal').classList.remove('hidden');
}
function closeDetail() { document.getElementById('detail-modal').classList.add('hidden'); }
function editFromDetail(id,t,a,i,p,q) { closeDetail(); editDisc(id,t,a,i,p,q); }
function deleteFromDetail(id) { closeDetail(); deleteDisc(id); }

document.addEventListener('click', (e) => {
    const m = document.getElementById('detail-modal');
    if (m && !m.classList.contains('hidden') && e.target === m) closeDetail();
});

window.addEventListener('DOMContentLoaded', async () => {
    await updateAuthUI();
    await loadCatalog();
    renderCartUI();
    showPage('catalog');
});