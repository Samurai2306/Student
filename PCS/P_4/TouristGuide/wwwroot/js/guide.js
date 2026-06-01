window.TouristGuide = (function () {
    const ROUTE_KEY = 'tg-route-v1';
    const FAV_KEY = 'tg-favorites-v1';

    function readJson(key, fallback) {
        try {
            const raw = localStorage.getItem(key);
            return raw ? JSON.parse(raw) : fallback;
        } catch {
            return fallback;
        }
    }

    function writeJson(key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    }

    function getRoute() {
        return readJson(ROUTE_KEY, []);
    }

    function setRoute(items) {
        writeJson(ROUTE_KEY, items);
        updateRouteBadge();
    }

    function getFavorites() {
        return readJson(FAV_KEY, []);
    }

    function setFavorites(items) {
        writeJson(FAV_KEY, items);
        updateFavBadge();
    }

    function itemKey(type, id) {
        return `${type}:${id}`;
    }

    function parseItem(el) {
        return {
            type: el.dataset.itemType,
            id: parseInt(el.dataset.itemId, 10),
            name: el.dataset.itemName || '',
            cityName: el.dataset.cityName || '',
            entryFee: el.dataset.entryFee === '' ? null : parseFloat(el.dataset.entryFee),
            imageUrl: el.dataset.imageUrl || '',
            url: el.dataset.itemUrl || '#'
        };
    }

    function updateRouteBadge() {
        const badge = document.getElementById('navRouteBadge');
        if (!badge) return;
        const count = getRoute().length;
        badge.textContent = count;
        badge.classList.toggle('d-none', count === 0);
    }

    function updateFavBadge() {
        const badge = document.getElementById('navFavBadge');
        if (!badge) return;
        const count = getFavorites().length;
        badge.textContent = count;
        badge.classList.toggle('d-none', count === 0);
    }

    function syncFavoriteButtons() {
        const favs = new Set(getFavorites().map(f => itemKey(f.type, f.id)));
        document.querySelectorAll('[data-guide-item]').forEach(el => {
            const key = itemKey(el.dataset.itemType, el.dataset.itemId);
            const btn = el.querySelector('.btn-favorite');
            const active = favs.has(key);
            if (btn) {
                btn.classList.toggle('active', active);
                const label = btn.querySelector('.fav-label');
                if (label) label.textContent = active ? 'В избранном' : 'В избранное';
            }
        });
    }

    function toggleFavorite(el) {
        const item = parseItem(el);
        const key = itemKey(item.type, item.id);
        let favs = getFavorites();
        const idx = favs.findIndex(f => itemKey(f.type, f.id) === key);
        if (idx >= 0) {
            favs.splice(idx, 1);
        } else {
            favs.push(item);
        }
        setFavorites(favs);
        syncFavoriteButtons();
        refreshPlannerUi();
    }

    function addToRoute(el) {
        const item = parseItem(el);
        if (item.type !== 'attraction') return;
        let route = getRoute();
        const key = itemKey(item.type, item.id);
        if (route.some(r => itemKey(r.type, r.id) === key)) return;
        route.push(item);
        setRoute(route);
        refreshPlannerUi();
        flashMessage('Добавлено в маршрут: ' + item.name);
    }

    function flashMessage(text) {
        let toast = document.getElementById('tgToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'tgToast';
            toast.className = 'tg-toast';
            document.body.appendChild(toast);
        }
        toast.textContent = text;
        toast.classList.add('show');
        setTimeout(() => toast.classList.remove('show'), 2500);
    }

    function bindInteractiveActions() {
        document.querySelectorAll('[data-guide-item]').forEach(el => {
            el.querySelector('.btn-favorite')?.addEventListener('click', e => {
                e.preventDefault();
                toggleFavorite(el);
            });
            el.querySelector('.btn-route')?.addEventListener('click', e => {
                e.preventDefault();
                addToRoute(el);
            });
        });
        syncFavoriteButtons();
        updateRouteBadge();
        updateFavBadge();
    }

    function initLiveSearch() {
        const input = document.getElementById('globalSearch');
        const panel = document.getElementById('searchResults');
        if (!input || !panel) return;

        let timer;
        input.addEventListener('input', () => {
            clearTimeout(timer);
            const q = input.value.trim();
            if (q.length < 2) {
                panel.innerHTML = '';
                panel.classList.remove('show');
                return;
            }
            timer = setTimeout(async () => {
                try {
                    const res = await fetch(`/api/guide/search?q=${encodeURIComponent(q)}`);
                    const data = await res.json();
                    panel.innerHTML = renderSearchResults(data);
                    panel.classList.add('show');
                } catch {
                    panel.innerHTML = '<div class="search-item text-danger">Ошибка поиска</div>';
                    panel.classList.add('show');
                }
            }, 280);
        });

        document.addEventListener('click', e => {
            if (!panel.contains(e.target) && e.target !== input) {
                panel.classList.remove('show');
            }
        });
    }

    function renderSearchResults(data) {
        let html = '';
        if (data.cities?.length) {
            html += '<div class="search-group-title">Города</div>';
            data.cities.forEach(c => {
                html += `<a class="search-item" href="${c.url}"><strong>${c.name}</strong><span>${c.region}</span></a>`;
            });
        }
        if (data.attractions?.length) {
            html += '<div class="search-group-title">Места</div>';
            data.attractions.forEach(a => {
                const fee = a.fee != null ? `${a.fee} ₽` : 'бесплатно';
                html += `<a class="search-item" href="${a.url}"><strong>${a.name}</strong><span>${a.city} · ${fee}</span></a>`;
            });
        }
        if (!html) html = '<div class="search-item text-muted">Ничего не найдено</div>';
        return html;
    }

    async function initRandomDiscovery() {
        const btn = document.getElementById('btnRandomPlace');
        const box = document.getElementById('randomPlaceCard');
        if (!btn || !box) return;

        btn.addEventListener('click', async () => {
            btn.disabled = true;
            box.innerHTML = '<div class="text-muted">Ищем идею для поездки...</div>';
            try {
                const res = await fetch('/api/guide/random');
                if (!res.ok) throw new Error();
                const p = await res.json();
                const fee = p.feeText || '';
                box.innerHTML = `
                    <div class="random-result">
                        <img src="${p.imageUrl}" alt="" class="random-img" loading="lazy" onerror="this.src='/images/placeholder-city.svg'" />
                        <div>
                            <h3 class="h5 mb-1"><a href="${p.url}">${p.name}</a></h3>
                            <p class="text-muted small mb-2">${p.city} · ${fee}</p>
                            <p class="mb-2">${p.shortDescription}</p>
                            <div class="d-flex gap-2 flex-wrap">
                                <a href="${p.url}" class="btn btn-sm btn-primary">Подробнее</a>
                                <a href="/Guide/Map?cityId=${p.cityId}" class="btn btn-sm btn-outline-primary">На карте</a>
                            </div>
                        </div>
                    </div>`;
            } catch {
                box.innerHTML = '<div class="text-danger">Не удалось получить место</div>';
            } finally {
                btn.disabled = false;
            }
        });
    }

    function refreshPlannerUi() {
        const route = getRoute();
        const favs = getFavorites();
        const list = document.getElementById('routeList');
        const empty = document.getElementById('routeEmpty');
        const badge = document.getElementById('routeCountBadge');
        const budgetEl = document.getElementById('routeBudget');
        const favList = document.getElementById('favoritesList');
        const favEmpty = document.getElementById('favoritesEmpty');

        if (list) {
            if (route.length === 0) {
                if (empty) empty.style.display = 'block';
                list.querySelectorAll('.route-item').forEach(n => n.remove());
            } else {
                if (empty) empty.style.display = 'none';
                list.innerHTML = route.map((r, i) => `
                    <div class="route-item" data-route-index="${i}">
                        <div class="route-item-num">${i + 1}</div>
                        <div class="flex-grow-1">
                            <a href="${r.url}" class="fw-semibold">${r.name}</a>
                            <div class="small text-muted">${r.cityName} · ${r.entryFee != null ? r.entryFee + ' ₽' : 'бесплатно'}</div>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-danger btn-remove-route" data-index="${i}">Убрать</button>
                    </div>`).join('');
                list.querySelectorAll('.btn-remove-route').forEach(btn => {
                    btn.addEventListener('click', () => {
                        const idx = parseInt(btn.dataset.index, 10);
                        const r = getRoute();
                        r.splice(idx, 1);
                        setRoute(r);
                        refreshPlannerUi();
                    });
                });
            }
        }

        if (badge) badge.textContent = route.length + ' ' + pluralPlaces(route.length);
        if (budgetEl) {
            const sum = route.reduce((s, r) => s + (r.entryFee || 0), 0);
            budgetEl.textContent = sum.toLocaleString('ru-RU') + ' ₽';
        }

        if (favList) {
            if (favs.length === 0) {
                if (favEmpty) favEmpty.style.display = 'block';
                favList.querySelectorAll('.fav-item').forEach(n => n.remove());
            } else {
                if (favEmpty) favEmpty.style.display = 'none';
                favList.innerHTML = favs.map(f => `
                    <a class="fav-item" href="${f.url}">
                        <span class="badge ${f.type === 'city' ? 'text-bg-primary' : 'text-bg-secondary'}">${f.type === 'city' ? 'город' : 'место'}</span>
                        ${f.name}${f.cityName ? ' · ' + f.cityName : ''}
                    </a>`).join('');
            }
        }

        updateRouteBadge();
        updateFavBadge();
        syncFavoriteButtons();
    }

    function pluralPlaces(n) {
        const a = Math.abs(n) % 100;
        const b = a % 10;
        if (a > 10 && a < 20) return 'мест';
        if (b === 1) return 'место';
        if (b >= 2 && b <= 4) return 'места';
        return 'мест';
    }

    let mapInstance;
    async function initMap(containerId) {
        const el = document.getElementById(containerId);
        if (!el || typeof L === 'undefined') return;

        const cityId = el.dataset.cityId || '';
        const url = cityId ? `/api/guide/markers?cityId=${cityId}` : '/api/guide/markers';
        const res = await fetch(url);
        const data = await res.json();

        if (mapInstance) {
            mapInstance.remove();
            mapInstance = null;
        }

        mapInstance = L.map(el, { attributionControl: false }).setView([61, 96], 4);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: ''
        }).addTo(mapInstance);

        const bounds = [];
        data.cities?.forEach(c => {
            const m = L.marker([c.latitude, c.longitude], { title: c.name })
                .addTo(mapInstance)
                .bindPopup(`<strong>${c.name}</strong><br>${c.region}<br><a href="${c.url}">Открыть город</a>`);
            bounds.push([c.latitude, c.longitude]);
        });
        data.attractions?.forEach(a => {
            const icon = L.divIcon({
                className: 'map-pin-attraction',
                html: '<span class="map-pin-dot"></span>',
                iconSize: [14, 14]
            });
            const m = L.marker([a.latitude, a.longitude], { icon, title: a.name })
                .addTo(mapInstance)
                .bindPopup(`<strong>${a.name}</strong><br>${a.city}<br>${a.feeText}<br><a href="${a.url}">Подробнее</a>`);
            bounds.push([a.latitude, a.longitude]);
        });

        if (bounds.length === 1) {
            mapInstance.setView(bounds[0], 12);
        } else if (bounds.length > 1) {
            mapInstance.fitBounds(bounds, { padding: [40, 40] });
        }
    }

    async function initCityMiniMap(containerId, cityId) {
        if (typeof L === 'undefined') return;
        await initMap(containerId);
    }

    function initPlannerPage() {
        document.getElementById('btnClearRoute')?.addEventListener('click', () => {
            if (confirm('Очистить весь маршрут?')) {
                setRoute([]);
                refreshPlannerUi();
            }
        });
    }

    function initAddAllRoute(cityId, attractions) {
        const btn = document.getElementById('btnAddAllRoute');
        if (!btn) return;
        btn.addEventListener('click', () => {
            let route = getRoute();
            attractions.forEach(a => {
                const key = itemKey('attraction', a.id);
                if (!route.some(r => itemKey(r.type, r.id) === key)) {
                    route.push(a);
                }
            });
            setRoute(route);
            refreshPlannerUi();
            flashMessage('Все места города добавлены в маршрут');
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        bindInteractiveActions();
        initLiveSearch();
        initRandomDiscovery();
        initPlannerPage();
        refreshPlannerUi();
    });

    return {
        initMap,
        initCityMiniMap,
        refreshPlannerUi,
        addToRoute,
        getRoute,
        setRoute,
        toggleFavorite,
        flashMessage
    };
})();
