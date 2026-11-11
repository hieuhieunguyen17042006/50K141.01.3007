// A logical AJAX submission keeps its key if the connection is retried.
(() => {
    window.thomeEscape = value => String(value ?? '').replace(/[&<>"']/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]));
    const originalFetch = window.fetch.bind(window);
    const submissions = new Map();
    window.fetch = async (url, options = {}) => {
        if ((options.method || 'GET').toUpperCase() !== 'POST') return originalFetch(url, options);
        const body = options.body instanceof FormData ? JSON.stringify([...options.body.entries()].filter(([k]) => k !== '__RequestVerificationToken')) : String(options.body || '');
        const identity = String(url) + body;
        if (!submissions.has(identity)) submissions.set(identity, crypto.randomUUID());
        const headers = new Headers(options.headers);
        const token = document.querySelector('[name="__RequestVerificationToken"]')?.value;
        if (token && !headers.has('RequestVerificationToken')) headers.set('RequestVerificationToken', token);
        if (!headers.has('X-Request-Id')) headers.set('X-Request-Id', submissions.get(identity));
        const response = await originalFetch(url, { ...options, headers });
        const result = await response.clone().json().catch(() => null);
        if (!response.ok || result?.success === false) submissions.delete(identity);
        return response;
    };
    document.addEventListener('submit', event => {
        if (event.defaultPrevented) return;
        const form = event.target;
        if (form.method.toLowerCase() !== 'post') return;
        if (form.dataset.submitting) { event.preventDefault(); return; }
        event.preventDefault();
        form.dataset.submitting = 'true';
        form.querySelectorAll('button[type="submit"]').forEach(button => button.disabled = true);
        HTMLFormElement.prototype.submit.call(form);
    });
    window.addEventListener('pageshow', () => document.querySelectorAll('form[data-submitting]').forEach(form => {
        delete form.dataset.submitting;
        form.querySelectorAll('button[type="submit"]').forEach(button => button.disabled = false);
    }));
})();
