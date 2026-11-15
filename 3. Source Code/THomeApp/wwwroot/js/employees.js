(() => {
    const page = document.querySelector('.employee-page');
    if (!page) return;
    const modals = {};
    document.querySelectorAll('.employee-modal').forEach(element => {
        // Dialogs must stay outside the blurred content's stacking context.
        document.body.append(element);
        modals[element.id] = bootstrap.Modal.getOrCreateInstance(element);
        element.addEventListener('show.bs.modal', () => document.body.classList.add('employee-modal-open'));
        element.addEventListener('hide.bs.modal', event => {
            if (element.querySelector('form[data-saving]')) event.preventDefault();
        });
        element.addEventListener('hidden.bs.modal', () => {
            if (!document.querySelector('.employee-modal.show')) document.body.classList.remove('employee-modal-open');
        });
    });
    function clearErrors(form) {
        form.querySelectorAll('[data-error-for]').forEach(element => element.textContent = '');
        form.querySelectorAll('.is-invalid').forEach(element => { element.classList.remove('is-invalid'); element.removeAttribute('aria-invalid'); });
        const summary = form.querySelector('.employee-form-error'); summary.textContent = ''; summary.hidden = true;
    }
    function startForm(form) {
        form.reset(); clearErrors(form);
        form.elements.requestId.value = crypto.randomUUID();
    }
    function showError(form, message, errors = {}) {
        const summary = form.querySelector('.employee-form-error'); summary.textContent = message; summary.hidden = false;
        for (const [name, messages] of Object.entries(errors)) {
            const field = form.querySelector(`[name="${CSS.escape(name)}"]`);
            const hint = form.querySelector(`[data-error-for="${CSS.escape(name)}"]`);
            if (field) { field.classList.add('is-invalid'); field.setAttribute('aria-invalid', 'true'); }
            if (hint) hint.textContent = messages.join(' ');
        }
        form.querySelector('.is-invalid')?.focus();
    }
    document.getElementById('employeeAddButton').addEventListener('click', () => {
        startForm(document.querySelector('#employeeAddModal form'));
        modals.employeeAddModal.show();
    });
    async function employee(id) {
        const response = await fetch('/NhanVien/ThongTin?id=' + encodeURIComponent(id), { headers:{Accept:'application/json'} });
        if (!response.ok || !(response.headers.get('content-type') || '').includes('application/json'))
            throw new Error('Không thể tải thông tin nhân viên. Vui lòng tải lại trang.');
        return response.json();
    }
    document.querySelectorAll('[data-edit-employee], [data-lock-employee]').forEach(button => button.addEventListener('click', async () => {
        button.disabled = true;
        try {
            const editing = button.hasAttribute('data-edit-employee');
            const data = await employee(editing ? button.dataset.editEmployee : button.dataset.lockEmployee);
            const form = document.querySelector(editing ? '#employeeEditModal form' : '#employeeLockModal form');
            startForm(form); form.elements.id.value = data.maNV;
            if (editing) {
                document.getElementById('employeeEditCode').value = data.maNV;
                for (const [name,key] of Object.entries({HoTen:'hoTen',SDT:'sdt',CCCD:'cccd',DiaChi:'diaChi'})) form.elements[name].value = data[key] || '';
                const owner = data.viTri === 'Chủ cửa hàng';
                const role = document.getElementById('employeeEditRole'); role.hidden = owner; role.disabled = owner;
                document.getElementById('employeeEditOwnerRole').hidden = !owner;
                document.getElementById('employeeEditOwnerRoleValue').disabled = !owner;
                if (!owner) role.value = data.viTri;
            } else {
                form.querySelectorAll('[data-lock-field]').forEach(input => {
                    const value = data[input.dataset.lockField] || '';
                    input.value = input.dataset.lockField === 'viTri' ? value === 'Nhân viên bán hàng' ? 'Bán hàng' : value === 'Kế toán bán hàng' ? 'Kế toán' : value : value;
                });
            }
            modals[editing ? 'employeeEditModal' : 'employeeLockModal'].show();
        } catch (error) {
            document.getElementById('employeeResultTitle').textContent = error.message;
            modals.employeeResultModal.show();
        } finally { button.disabled = false; }
    }));
    document.querySelectorAll('.employee-profile-form, .employee-lock-form').forEach(form => form.addEventListener('submit', async event => {
        event.preventDefault();
        if (form.dataset.saving) return;
        form.dataset.saving = 'true';
        const save = form.querySelector('button[type=submit]'); save.disabled = true;
        form.querySelectorAll('[data-bs-dismiss]').forEach(button => button.disabled = true);
        clearErrors(form);
        try {
            const response = await fetch(form.action, { method:'POST', body:new FormData(form), headers:{Accept:'application/json','X-Request-Id':form.elements.requestId.value} });
            if (!(response.headers.get('content-type') || '').includes('application/json'))
                throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền thực hiện thao tác này.');
            const result = await response.json();
            if (!response.ok || !result.success) {
                showError(form, result.message || 'Không thể lưu thông tin. Vui lòng thử lại.', result.errors);
                return;
            }
            // The transaction has committed before loading the refreshed list and
            // the one-time account notice from encrypted TempData.
            location.assign('/NhanVien/Index');
        } catch (error) { showError(form, error.message || 'Không thể kết nối. Vui lòng thử lại.'); }
        finally {
            delete form.dataset.saving; save.disabled = false;
            form.querySelectorAll('[data-bs-dismiss]').forEach(button => button.disabled = false);
        }
    }));
    if (page.dataset.employeeSuccess) modals.employeeResultModal.show();
})();
