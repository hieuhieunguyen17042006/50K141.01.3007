(() => {
    const dialogs = {};
    document.querySelectorAll('.catalog-modal').forEach(element => {
        document.body.append(element);
        dialogs[element.id] = bootstrap.Modal.getOrCreateInstance(element);
        element.addEventListener('show.bs.modal', () => document.body.classList.add('catalog-modal-open'));
        element.addEventListener('hide.bs.modal', event => { if(element.querySelector('form[data-saving]')) event.preventDefault(); });
        element.addEventListener('hidden.bs.modal', () => { if(!document.querySelector('.catalog-modal.show')) document.body.classList.remove('catalog-modal-open'); });
    });
    function clearErrors(form) {
        form.querySelectorAll('[data-error-for]').forEach(element => element.textContent = '');
        form.querySelectorAll('.is-invalid').forEach(element => {element.classList.remove('is-invalid');element.removeAttribute('aria-invalid');});
        const summary=form.querySelector('.catalog-form-error');if(summary){summary.hidden=true;summary.textContent='';}
    }
    function reset(form) { form.reset();clearErrors(form);if(form.elements.requestId)form.elements.requestId.value=crypto.randomUUID(); }
    function errors(form,message,fields={}) {
        const summary=form.querySelector('.catalog-form-error');if(summary){summary.textContent=message;summary.hidden=false;}
        for(const [name,messages] of Object.entries(fields)) {
            const input=[...form.querySelectorAll('[name]')].find(element=>element.name.toLowerCase()===name.toLowerCase());
            const hint=[...form.querySelectorAll('[data-error-for]')].find(element=>element.dataset.errorFor.toLowerCase()===name.toLowerCase());
            if(input){input.classList.add('is-invalid');input.setAttribute('aria-invalid','true');}
            if(hint)hint.textContent=Array.isArray(messages)?messages.join(' '):String(messages);
        }
        form.querySelector('.is-invalid')?.focus();
    }
    function notify(message) {
        const title=document.getElementById('catalogResultTitle');if(!title)return;
        title.textContent=message;dialogs.catalogResultModal.show();
    }
    document.querySelectorAll('.catalog-form').forEach(form=>form.addEventListener('submit',async event=>{
        event.preventDefault();if(form.dataset.saving||form.dataset.readOnly==='true')return;
        if(form.hasAttribute('data-decision-form'))form.elements.namedItem('action').value=event.submitter?.value||'';
        const body=new FormData(form); // Capture controls before disabling buttons.
        clearErrors(form);form.dataset.saving='true';
        const buttons=form.querySelectorAll('button[type=submit],[data-bs-dismiss]');buttons.forEach(button=>button.disabled=true);
        try {
            const response=await fetch(form.getAttribute('action')||location.pathname,{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':form.elements.requestId.value}});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền thực hiện thao tác này.');
            const result=await response.json();
            if(!response.ok||!result.success){errors(form,result.message||result.title||'Không thể thực hiện thao tác.',result.errors);return;}
            location.assign(result.redirect||location.pathname);
        }catch(error){errors(form,error.message||'Không thể kết nối. Vui lòng thử lại.');}
        finally{delete form.dataset.saving;buttons.forEach(button=>button.disabled=false);}
    }));
    document.querySelectorAll('[data-open-modal]').forEach(button=>button.addEventListener('click',()=>{
        const element=document.getElementById(button.dataset.openModal);const form=element.querySelector('form');if(form)reset(form);dialogs[element.id].show();
    }));
    function activateRows(selector,callback) {
        document.querySelectorAll(selector).forEach(row=>{row.addEventListener('click',()=>callback(row));row.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();callback(row);}});});
    }
    let selectedProduct;
    activateRows('[data-product-code]',row=>{
        document.querySelectorAll('[data-product-code]').forEach(element=>element.setAttribute('aria-selected',String(element===row)));
        selectedProduct=row.dataset.productCode;
        const link=document.getElementById('editSelectedProduct');link.href='/CuaHang/SuaSanPham/'+encodeURIComponent(selectedProduct);link.classList.remove('disabled');link.removeAttribute('aria-disabled');
        document.getElementById('deleteSelectedProduct').disabled=false;document.getElementById('productSelectionHint').textContent='Đang chọn: '+selectedProduct;
    });
    document.getElementById('deleteSelectedProduct')?.addEventListener('click',()=>{
        if(!selectedProduct)return;const form=document.querySelector('#productDeleteModal form');reset(form);form.elements.maSP.value=selectedProduct;
        document.getElementById('productDeleteDescription').textContent='Sản phẩm: '+selectedProduct;dialogs.productDeleteModal.show();
    });
    function discountForm(button) {
        const form=document.querySelector('#discountModal form');reset(form);
        const editing=!!button?.dataset.discountEdit;
        form.action=editing?'/CuaHang/SuaChietKhau':'/CuaHang/ThemChietKhau';
        document.getElementById('discountTitle').textContent=editing?'Sửa ưu đãi chiết khấu':'Thêm ưu đãi chiết khấu mới';
        document.getElementById('discountReason').hidden=!editing;form.elements.LyDo.disabled=!editing;form.elements.LyDo.required=editing;
        if(editing){form.elements.MaCK.value=button.dataset.discountEdit;form.elements.MocTichLuy.value=String(Number(button.dataset.threshold));form.elements.TyLeChietKhau.value=String(Number(button.dataset.rate));}
        dialogs.discountModal.show();
    }
    document.getElementById('newDiscountButton')?.addEventListener('click',()=>discountForm());
    document.querySelectorAll('[data-discount-edit]').forEach(button=>button.addEventListener('click',()=>discountForm(button)));
    document.querySelectorAll('[data-discount-delete]').forEach(button=>button.addEventListener('click',()=>{const form=document.querySelector('#discountDeleteModal form');reset(form);form.elements.maCK.value=button.dataset.discountDelete;dialogs.discountDeleteModal.show();}));
    activateRows('[data-unlock-request]',row=>{
        const data=JSON.parse(row.dataset.unlockRequest);const form=document.querySelector('#unlockReviewModal form');reset(form);form.elements.maYC.value=data.id;
        form.querySelectorAll('[data-review]').forEach(element=>{const key=element.dataset.review;const value=key==='name'?data.name+' · '+(data.role||''):key==='count'?data.count+' lần':data[key]||'';if(element.tagName==='TEXTAREA')element.value=value;else element.textContent=value;});
        const pending=data.status==='Chờ duyệt';form.querySelector('.review-actions').hidden=!pending;form.elements.ghiChuDuyet.readOnly=!pending;form.elements.ghiChuDuyet.value=data.note||'';
        dialogs.unlockReviewModal.show();
    });
    function company(form) {
        const business=form.querySelector('[name=LoaiKhach]').value==='Doanh nghiệp';
        form.querySelectorAll('[data-company-field]').forEach(label=>{label.hidden=!business;});
    }
    document.querySelectorAll('.customer-fields [name=LoaiKhach]').forEach(select=>select.addEventListener('change',()=>company(select.closest('form'))));
    document.getElementById('newCustomerButton')?.addEventListener('click',()=>{const form=document.querySelector('#customerAddModal form');reset(form);company(form);dialogs.customerAddModal.show();});
    function enableContactEditing() {
        const form=document.querySelector('#customerDetailModal form');
        form.dataset.readOnly='false';
        form.querySelectorAll('[data-customer-contact]').forEach(input=>input.readOnly=false);
        form.querySelector('.customer-edit-reason').hidden=false;form.elements.LyDo.disabled=false;document.getElementById('saveCustomerButton').disabled=false;
        form.querySelector('[data-customer-contact]')?.focus();
    }
    document.getElementById('editCustomerButton')?.addEventListener('click',enableContactEditing);
    async function showCustomer(id,edit=false) {
        try {
            const response=await fetch('/KhachHang/ThongTin?id='+encodeURIComponent(id),{headers:{Accept:'application/json'}});
            if(!response.ok||!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể tải thông tin khách hàng. Vui lòng tải lại trang.');
            const data=await response.json();const form=document.querySelector('#customerDetailModal form');reset(form);
            form.dataset.readOnly='true';
            form.elements.maKH.value=data.maKH;
            form.querySelectorAll('[data-customer-field]').forEach(input=>{const key=input.dataset.customerField;input.value=key==='ngaySinh'?(data[key]||'').slice(0,10):data[key]??'';});
            form.querySelectorAll('[data-customer-contact]').forEach(input=>input.readOnly=true);
            form.querySelector('.customer-edit-reason').hidden=true;form.elements.LyDo.disabled=true;document.getElementById('saveCustomerButton').disabled=true;
            document.getElementById('customerCode').value=data.maKH;document.getElementById('customerLifetimeTotal').value=Number(data.tongTichLuy||0).toLocaleString('vi-VN')+' đ';
            document.getElementById('customerPurchasesLink').href='/KhachHang/HoaDonDaMua?id='+encodeURIComponent(data.maKH);
            company(form);dialogs.customerDetailModal.show();if(edit)enableContactEditing();
        }catch(error){notify(error.message);}
    }
    activateRows('[data-customer-id]',row=>showCustomer(row.dataset.customerId));
    const query=new URLSearchParams(location.search);
    if(document.querySelector('.customer-page')&&query.get('customer')&&document.getElementById('customerDetailModal'))showCustomer(query.get('customer'),query.get('edit')==='1');
    if(query.get('add')==='1')document.getElementById('newCustomerButton')?.click();
    const notice=document.getElementById('catalogResultModal')?.dataset.notice;if(notice)notify(notice);
})();
