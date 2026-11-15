(() => {
    const dialogs = {};
    const existingProductCodes=new Set(JSON.parse(document.getElementById('existingProductCodes')?.textContent||'[]').map(code=>code.trim().toUpperCase()));
    const deleting=form=>form.getAttribute('action')?.includes('XoaSanPham');
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
        window.THomeUi.clear(form);const summary=form.querySelector('.catalog-form-error:not(.form-error-bottom)');if(summary){summary.hidden=true;summary.textContent='';}
    }
    function reset(form) { form.reset();clearErrors(form);delete form.dataset.completed;form.querySelectorAll('button').forEach(b=>b.disabled=false);if(form.elements.requestId)window.thomeFormRequestId(form,true); }
    function errors(form,message,fields={}) {
        if(deleting(form))message=Object.values(fields).flat()[0]||message;
        if(form.querySelector('.form-error-bottom')){
            const productAdd=form.classList.contains('product-add-form');
            const duplicate=productAdd&&(message==='Mã sản phẩm đã tồn tại!'||(fields.MaSP||[]).some(m=>m.includes('đã tồn tại')));
            const text=duplicate?'Mã sản phẩm đã tồn tại!':productAdd&&Object.keys(fields).length?'Thông tin không hợp lệ!':deleting(form)||!Object.keys(fields).length?message:'Không hợp lệ!';
            window.THomeUi.errors(form,duplicate?{MaSP:fields.MaSP||['Mã sản phẩm đã tồn tại!']}:fields,text);return;
        }
        const summary=form.querySelector('.catalog-form-error');if(summary){summary.textContent=message;summary.hidden=false;}
        for(const [name,messages] of Object.entries(fields)) {
            const input=[...form.querySelectorAll('[name]')].find(element=>element.name.toLowerCase()===name.toLowerCase());
            const hint=[...form.querySelectorAll('[data-error-for]')].find(element=>element.dataset.errorFor.toLowerCase()===name.toLowerCase());
            if(input){input.classList.add('is-invalid');input.setAttribute('aria-invalid','true');}

        }
        form.querySelector('.is-invalid')?.focus();
    }
    function notify(message) {
        const title=document.getElementById('catalogResultTitle');if(!title)return;
        title.textContent=message;dialogs.catalogResultModal.show();
    }
    document.querySelectorAll('.catalog-form:not([data-customer-form]):not([data-product-reason])').forEach(form=>form.addEventListener('submit',async event=>{
        event.preventDefault();if(form.dataset.saving||form.dataset.completed||form.dataset.readOnly==='true')return;
        if(form.hasAttribute('data-decision-form'))form.elements.namedItem('action').value=event.submitter?.value||'';
        clearErrors(form);

        if(deleting(form)&&!form.elements.lyDoXoa.value.trim()){
            errors(form,'Vui lòng điền đầy đủ lý do xóa!',{lyDoXoa:['Vui lòng điền đầy đủ lý do xóa!']});return;
        }
        if(form.hasAttribute('data-discount-form')){
            const invalid={};for(const input of form.querySelectorAll('[required]'))if(!input.value.trim()||!input.validity.valid)invalid[input.name]=['Không hợp lệ!'];
            const threshold=Number(form.elements.MocTichLuy.value),rate=Number(form.elements.TyLeChietKhau.value),id=Number(form.elements.MaCK.value);
            const rules=[...document.querySelectorAll('[data-discount-edit]')].map(b=>({id:Number(b.dataset.discountEdit),threshold:Number(b.dataset.threshold),rate:Number(b.dataset.rate)})).filter(r=>r.id!==id);
            if(rules.some(r=>r.threshold===threshold))invalid.MocTichLuy=['Không hợp lệ!'];
            if(rules.some(r=>r.rate===rate))invalid.TyLeChietKhau=['Không hợp lệ!'];
            const lower=rules.filter(r=>r.threshold<threshold).sort((a,b)=>b.threshold-a.threshold||b.rate-a.rate)[0];
            const upper=rules.filter(r=>r.threshold>threshold).sort((a,b)=>a.threshold-b.threshold||a.rate-b.rate)[0];
            if((lower&&rate<=lower.rate)||(upper&&rate>=upper.rate))invalid.TyLeChietKhau=['Không hợp lệ!'];
            if(Object.keys(invalid).length){errors(form,'Không hợp lệ!',invalid);return;}
        }
        const invalid={};for(const input of form.querySelectorAll('[required]'))if(!input.readOnly&&(!input.value.trim()||!input.validity.valid))invalid[input.name]=['Không hợp lệ!'];
        if(form.classList.contains('product-add-form')&&existingProductCodes.has(form.elements.MaSP.value.trim().toUpperCase()))invalid.MaSP=['Mã sản phẩm đã tồn tại!'];
        if(Object.keys(invalid).length){errors(form,'Không hợp lệ!',invalid);return;}
        const body=new FormData(form); // Capture controls before disabling buttons.
        clearErrors(form);form.dataset.saving='true';
        const buttons=form.querySelectorAll('button[type=submit],[data-bs-dismiss]');buttons.forEach(button=>button.disabled=true);
        try {
            const response=await fetch(form.getAttribute('action')||location.pathname,{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':window.thomeFormRequestId(form)}});
            if(response.status>=500)throw new Error('Không thể xử lý lúc này. Vui lòng thử lại.');
            if(!(response.headers.get('content-type')||'').includes('json')){
                const authenticationFailure=response.status===401||response.status===403||(response.redirected&&new URL(response.url).pathname.startsWith('/Auth/'));
                throw new Error(authenticationFailure?'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền thực hiện thao tác này.':'Phản hồi từ máy chủ không hợp lệ. Vui lòng thử lại.');
            }
            const result=await response.json();
            if(deleting(form)&&response.status===409&&result.code==='PRODUCT_IN_USE'){
                // This is a business refusal, not a reason-field validation error.
                const element=form.closest('.modal');delete form.dataset.saving;
                await new Promise(resolve=>{element.addEventListener('hidden.bs.modal',resolve,{once:true});dialogs[element.id].hide();});
                await new Promise(requestAnimationFrame);
                const region=document.getElementById('productListMessage');region.querySelector('[data-form-error-message]').textContent='Không thể xóa!';region.hidden=false;
                document.getElementById('productSelectionHint').hidden=true;document.getElementById('deleteSelectedProduct').focus();return;
            }
            if(!response.ok||!result.success){errors(form,result.message||result.title||'Không thể thực hiện thao tác.',result.errors);return;}
            if(deleting(form))form.dataset.completed='true';
            location.assign(result.redirect||location.pathname);
        }catch(error){errors(form,error instanceof TypeError?'Không thể kết nối. Vui lòng thử lại.':error.message||'Không thể kết nối. Vui lòng thử lại.');}
        finally{delete form.dataset.saving;if(!form.dataset.completed)buttons.forEach(button=>button.disabled=false);}
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
        document.getElementById('productListMessage').hidden=true;document.getElementById('productSelectionHint').hidden=false;
        const link=document.getElementById('editSelectedProduct');link.href='/CuaHang/SuaSanPham/'+encodeURIComponent(selectedProduct);link.classList.remove('disabled');link.removeAttribute('aria-disabled');
        document.getElementById('deleteSelectedProduct').disabled=false;document.getElementById('productSelectionHint').textContent='Đang chọn: '+selectedProduct;
    });
    document.getElementById('deleteSelectedProduct')?.addEventListener('click',()=>{
        if(!selectedProduct)return;const form=document.querySelector('#productDeleteModal form');reset(form);form.elements.maSP.value=selectedProduct;
        document.getElementById('productListMessage').hidden=true;document.getElementById('productSelectionHint').hidden=false;dialogs.productDeleteModal.show();
    });
    function discountForm(button) {
        const form=document.querySelector('#discountModal form');reset(form);
        const editing=!!button?.dataset.discountEdit;
        form.action=editing?'/CuaHang/SuaChietKhau':'/CuaHang/ThemChietKhau';
        document.getElementById('discountTitle').textContent=editing?'Sửa ưu đãi chiết khấu':'Thêm ưu đãi chiết khấu mới';
        if(editing){form.elements.MaCK.value=button.dataset.discountEdit;form.elements.MocTichLuy.value=String(Number(button.dataset.threshold));form.elements.TyLeChietKhau.value=String(Number(button.dataset.rate));}
        dialogs.discountModal.show();
    }
    document.getElementById('newDiscountButton')?.addEventListener('click',()=>discountForm());
    document.querySelectorAll('[data-discount-edit]').forEach(button=>button.addEventListener('click',()=>discountForm(button)));
    document.querySelectorAll('[data-discount-delete]').forEach(button=>button.addEventListener('click',()=>{const form=document.querySelector('#discountDeleteModal form');reset(form);form.elements.maCK.value=button.dataset.discountDelete;document.getElementById('deleteDiscountThreshold').value=Number(button.dataset.threshold).toLocaleString('vi-VN');document.getElementById('deleteDiscountRate').value=Number(button.dataset.rate).toLocaleString('vi-VN');dialogs.discountDeleteModal.show();}));
    activateRows('[data-unlock-request]:not([data-unlock-request=""])',row=>{
        const data=JSON.parse(row.dataset.unlockRequest);if(data.status!=='Chờ duyệt')return;const form=document.querySelector('#unlockReviewModal form');reset(form);form.elements.maYC.value=data.id;
        form.querySelectorAll('[data-review]').forEach(element=>{const key=element.dataset.review;const value=key==='name'?data.name+' · '+(data.role||''):key==='count'?data.count+' lần':data[key]||'';if(element.tagName==='TEXTAREA')element.value=value;else element.textContent=value;});
        const pending=data.status==='Chờ duyệt';form.querySelector('.review-actions').hidden=!pending;
        dialogs.unlockReviewModal.show();
    });
    const notice=document.getElementById('catalogResultModal')?.dataset.notice;if(notice)notify(notice);
})();
