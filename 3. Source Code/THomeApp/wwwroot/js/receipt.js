(() => {
    const form=document.querySelector('#collectMoneyModal form');
    if(form) form.addEventListener('submit',async event=>{
        event.preventDefault();if(form.dataset.saving)return;
        THomeUi.clear(form);const fields={};
        if(!Number.isFinite(Number(form.elements.soTienThu.value))||!form.elements.soTienThu.validity.valid||Number(form.elements.soTienThu.value)<=0)fields.soTienThu=['Không hợp lệ!'];
        if(!['Tiền mặt','Chuyển khoản'].includes(form.elements.phuongThuc.value))fields.phuongThuc=['Không hợp lệ!'];
        if(Object.keys(fields).length){THomeUi.errors(form,fields);return;}
        const body=new FormData(form);form.dataset.saving='true';const button=form.querySelector('[type=submit]');button.disabled=true;
        try{
            const response=await fetch(form.getAttribute('action'),{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':thomeFormRequestId(form)}});
            if(!(response.headers.get('content-type')||'').includes('json'))throw Error('Không thể lưu khoản thu. Kiểm tra phiên đăng nhập.');
            const result=await response.json();if(!response.ok||!result.success){THomeUi.errors(form,result.errors,result.message||'Không hợp lệ!');return;}
            location.assign(result.redirect);
        }catch(e){THomeUi.errors(form,{},e.message);}finally{delete form.dataset.saving;button.disabled=false;}
    });
    if(document.getElementById('receiptConfirmed'))bootstrap.Modal.getOrCreateInstance(document.getElementById('receiptSuccessModal')).show();
})();
