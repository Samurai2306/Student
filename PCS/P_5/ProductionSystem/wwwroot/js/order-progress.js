(function () {
    const tracked = document.querySelectorAll('[data-order-progress]');
    if (tracked.length === 0) {
        return;
    }

    let hadInProgress = tracked.length > 0;

    function formatEta(iso) {
        if (!iso) return '';
        const end = new Date(iso);
        return end.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
    }

    function applyProgress(item) {
        const root = document.querySelector(`[data-order-progress][data-order-id="${item.id}"]`);
        if (!root) return;

        root.querySelectorAll('[data-progress-bar]').forEach(bar => {
            bar.style.width = `${item.percent}%`;
            bar.setAttribute('aria-valuenow', item.percent);
        });
        root.querySelectorAll('[data-progress-label]').forEach(label => {
            label.textContent = `${item.percent}%`;
        });
        root.querySelectorAll('[data-progress-eta]').forEach(eta => {
            if (item.estimatedEndDate) {
                eta.textContent = `Окончание: ${formatEta(item.estimatedEndDate)}`;
            }
        });
    }

    async function refresh() {
        try {
            const response = await fetch('/api/orders/progress', { cache: 'no-store' });
            if (!response.ok) return;

            const items = await response.json();
            items.forEach(applyProgress);

            if (hadInProgress && items.length === 0) {
                window.location.reload();
                return;
            }

            hadInProgress = items.length > 0;
        } catch {
            // сеть недоступна — следующий опрос
        }
    }

    refresh();
    setInterval(refresh, 2000);
})();
