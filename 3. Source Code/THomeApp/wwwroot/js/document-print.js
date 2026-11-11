(() => {
    function clear() {
        document.querySelectorAll('.print-document-path').forEach(element => element.classList.remove('print-document-path'));
    }
    function prepare(id) {
        clear();
        const root = id ? document.getElementById(id) : document.querySelector('[data-print-root]');
        if (!root) return null;
        for (let ancestor = root.parentElement; ancestor; ancestor = ancestor.parentElement) ancestor.classList.add('print-document-path');
        return root;
    }
    window.thomePreparePrint = prepare;
    window.addEventListener('beforeprint', () => prepare());
    window.addEventListener('afterprint', clear);
    document.addEventListener('click', async event => {
        const button = event.target.closest('[data-print-document]');
        if (!button) return;
        event.preventDefault();
        if (button.dataset.printing) return;
        button.dataset.printing = 'true';
        try {
            const root = prepare(button.dataset.printDocument);
            if (!root) return;
            await Promise.all([...root.querySelectorAll('img')].map(image => image.decode()));
            window.print();
        } catch {
            alert('Chưa tải được mã QR để in. Vui lòng tải lại trang và thử lại.');
            clear();
        } finally { delete button.dataset.printing; }
    });
})();
