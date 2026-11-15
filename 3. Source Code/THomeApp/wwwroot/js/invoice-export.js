// One-shot notice on the creation screen after the issuance transaction commits.
// Reloading this GET or printing never issues another invoice.
(() => {
    const root = document.querySelector('.invoice-creation');
    const notice = JSON.parse(document.getElementById('invoiceCreatedData')?.textContent || 'null');
    if (!root || !notice || notice.maHD !== root.dataset.invoiceId) return;
    const form = document.getElementById('createInvoiceForm');
    for (const name of ['inHoaDonGiay', 'guiZalo', 'guiEmail']) form.elements[name].checked = !!notice[name];
    const popup = document.getElementById('invoiceSuccessModal');
    const status = document.getElementById('invoiceExportStatus');
    const key = `thome:invoice-export:${notice.maHD}:${notice.exportId}`;
    let exported = false;
    function report(message) {
        const line = document.createElement('p'); line.textContent = message;
        status.append(line); status.hidden = false;
    }
    async function exportSavedInvoice() {
        if (exported) return;
        exported = true;
        try { if (sessionStorage.getItem(key)) return; sessionStorage.setItem(key, 'requested'); } catch { /* Local guard and one-shot TempData still apply. */ }
        if (notice.guiZalo) report('Chưa cấu hình dịch vụ gửi Zalo. Hóa đơn đã lưu, chưa gửi qua Zalo.');
        if (notice.guiEmail) report('Chưa cấu hình dịch vụ gửi Email. Hóa đơn đã lưu, chưa gửi qua Email.');
        if (!notice.inHoaDonGiay) return;
        try {
            await document.fonts.ready;
            await Promise.all([...document.querySelectorAll('#invoiceDocument img')].map(img => img.decode()));
            window.print();
        } catch { report('Hóa đơn đã lưu. Chưa tải được mã QR để in; mở Chi tiết hóa đơn để in lại.'); }
    }
    // Let Bootstrap and the shared dialog manager remove the backdrop and focus trap first.
    popup.addEventListener('hidden.bs.modal', () => requestAnimationFrame(() => requestAnimationFrame(exportSavedInvoice)), { once: true });
    bootstrap.Modal.getOrCreateInstance(popup).show();
})();
