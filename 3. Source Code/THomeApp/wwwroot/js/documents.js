// A logical AJAX submission keeps its key if the connection is retried.
(() => {
    window.thomeEscape = value => String(value ?? '').replace(/[&<>"']/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]));
    window.thomeNewRequestId=()=>{
        const bytes=new Uint8Array(16);
        if(globalThis.crypto?.getRandomValues)globalThis.crypto.getRandomValues(bytes);
        else for(let i=0;i<bytes.length;i++)bytes[i]=Math.floor(Math.random()*256);
        bytes[6]=(bytes[6]&15)|64;bytes[8]=(bytes[8]&63)|128;
        const hex=[...bytes].map(n=>n.toString(16).padStart(2,'0')).join('');
        return `${hex.slice(0,8)}-${hex.slice(8,12)}-${hex.slice(12,16)}-${hex.slice(16,20)}-${hex.slice(20)}`;
    };
    window.thomeFormRequestId=(form,renew=false)=>{
        let input=form.elements.namedItem('requestId');
        if(!input){input=document.createElement('input');input.type='hidden';input.name='requestId';form.append(input);}
        if(renew||!input.value)input.value=window.thomeNewRequestId();return input.value;
    };
    const originalFetch = window.fetch.bind(window);
    const submissions = new Map();
    window.fetch = async (url, options = {}) => {
        if ((options.method || 'GET').toUpperCase() !== 'POST') return originalFetch(url, options);
        const body = options.body instanceof FormData ? JSON.stringify([...options.body.entries()].filter(([k]) => k !== '__RequestVerificationToken')) : String(options.body || '');
        const identity = String(url) + body;
        if (!submissions.has(identity)) submissions.set(identity, window.thomeNewRequestId());
        const headers = new Headers(options.headers);
        const token = document.querySelector('[name="__RequestVerificationToken"]')?.value;
        if (token && !headers.has('RequestVerificationToken')) headers.set('RequestVerificationToken', token);
        if (!headers.has('X-Request-Id')) headers.set('X-Request-Id', submissions.get(identity));
        const response = await originalFetch(url, { ...options, headers });
        const result = await response.clone().json().catch(() => null);
        if(response.ok && result?.success && String(url).includes('/BanHang/'))try{localStorage.setItem('thome:sales-change',String(Date.now()));}catch{}
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
    // Return links and the sidebar restore the last filter of the intended sales tab.
    document.addEventListener('DOMContentLoaded',()=>{
        try{
            document.querySelectorAll('a[href]').forEach(a=>{
                const target=new URL(a.href,location.href);if(target.origin!==location.origin||!/^\/BanHang(?:\/Index)?\/?$/.test(target.pathname))return;
                const tab=target.searchParams.get('tab')||sessionStorage.getItem('thome:sales-active-tab')||'dontamtinh';
                const saved=sessionStorage.getItem('thome:sales-filter:'+tab);if(saved?.startsWith('?'))a.href='/BanHang/Index'+saved;
            });
        }catch{/* Navigation still works without browser storage. */}
    });
})();
