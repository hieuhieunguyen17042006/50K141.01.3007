(() => {
    const element = document.getElementById('revenueResultModal');
    if (!element) return;
    // Keep the dialog outside the blurred content's stacking context.
    document.body.append(element);
    const modal = bootstrap.Modal.getOrCreateInstance(element);
    const title = document.getElementById('revenueResultTitle');
    const icon = document.getElementById('revenueResultIcon');
    function show(message, success = false) {
        title.textContent = message;
        icon.className = success ? 'fa-solid fa-circle-check' : 'fa-regular fa-file-lines is-empty';
        modal.show();
    }
    element.addEventListener('show.bs.modal', () => document.body.classList.add('revenue-modal-open'));
    element.addEventListener('hidden.bs.modal', () => document.body.classList.remove('revenue-modal-open'));
    let busy = false;
    document.querySelectorAll('[data-export]').forEach(link => link.addEventListener('click', async event => {
        event.preventDefault();
        if (busy) return;
        busy = true;
        document.querySelectorAll('[data-export]').forEach(button => button.setAttribute('aria-disabled', 'true'));
        try {
            const response = await fetch(link.href, { credentials: 'same-origin' });
            const type = response.headers.get('content-type') || '';
            if (!response.ok) {
                let message = 'Không thể xuất báo cáo. Vui lòng thử lại.';
                if (type.includes('json')) message = (await response.json()).message || message;
                throw new Error(message);
            }
            const expected = link.dataset.export === 'PDF' ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
            if (!type.includes(expected)) throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền xuất báo cáo.');
            const blob = await response.blob();
            if (!blob.size) throw new Error('Tệp báo cáo trống. Vui lòng thử lại.');
            const disposition = response.headers.get('content-disposition') || '';
            const utf8 = disposition.match(/filename\*=UTF-8''([^;]+)/i);
            const basic = disposition.match(/filename="?([^";]+)"?/i);
            const filename = utf8 ? decodeURIComponent(utf8[1]) : basic ? basic[1] : `BaoCao.${link.dataset.export === 'PDF' ? 'pdf' : 'xlsx'}`;
            const url = URL.createObjectURL(blob);
            const download = document.createElement('a');
            download.href = url; download.download = filename;
            document.body.append(download); download.click(); download.remove();
            setTimeout(() => URL.revokeObjectURL(url), 30000);
            show(`Xuất báo cáo ${link.dataset.export} thành công`, true);
        } catch (error) {
            show(error.message || 'Không thể xuất báo cáo. Vui lòng thử lại.');
        } finally {
            busy = false;
            document.querySelectorAll('[data-export]').forEach(button => button.removeAttribute('aria-disabled'));
        }
    }));
    if (document.querySelector('[data-report-empty="true"]')) show('Không có dữ liệu!');
})();
