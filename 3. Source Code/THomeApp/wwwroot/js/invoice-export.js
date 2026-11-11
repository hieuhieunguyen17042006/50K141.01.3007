// This script exists only on the read-only result page reached after the issuance transaction commits.
(async () => {
    const output = document.getElementById('invoiceOutput');
    if (!output || output.dataset.autoPrint !== 'true' || !output.dataset.exportId) return;
    const status = document.getElementById('invoicePrintStatus');
    const key = `thome:invoice-print:${output.dataset.invoiceId}:${output.dataset.exportId}`;
    try {
        if (sessionStorage.getItem(key)) {
            status.textContent = 'Đã yêu cầu in cho lần tạo này. Có thể nhấn In hóa đơn giấy để in lại.';
            return;
        }
        await document.fonts.ready;
        await Promise.all([...document.querySelectorAll('#invoiceDocument img')].map(img => img.decode().catch(() => {})));
        sessionStorage.setItem(key, 'requested');
        status.textContent = 'Đã yêu cầu mở hộp thoại in. Chọn máy in hoặc lưu PDF trong hộp thoại.';
        window.print();
    } catch {
        status.textContent = 'Hóa đơn đã lưu. Nhấn In hóa đơn giấy để mở hộp thoại in.';
    }
})();
