(() => {
    const form=document.getElementById('preparedOrderSave');if(!form)return;
    let busy=false,saved=false,redirect;
    const button=document.getElementById('saveQrButton'),cancel=document.getElementById('cancelQrButton'),error=document.getElementById('qrSaveError'),dialog=document.getElementById('orderCreatedModal');
    document.body.append(dialog);const modal=bootstrap.Modal.getOrCreateInstance(dialog);
    dialog.addEventListener('hidden.bs.modal',()=>{if(saved&&redirect)location.assign(redirect);});
    cancel.addEventListener('click',event=>{if(busy||saved)event.preventDefault();});
    form.addEventListener('submit',async event=>{
        event.preventDefault();if(busy||saved)return;busy=true;button.disabled=true;error.hidden=true;
        try{
            const response=await fetch(form.getAttribute('action'),{method:'POST',headers:{'Content-Type':'application/json',Accept:'application/json','X-Request-Id':form.dataset.preparedId},body:JSON.stringify({PreparedId:form.dataset.preparedId})});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể lưu đơn. Vui lòng kiểm tra kết nối/phiên đăng nhập rồi thử lại.');
            const result=await response.json();
            if(!response.ok||!result.success){error.textContent=[result.message||'Không thể lưu đơn.',...Object.values(result.errors||{}).flat()].join(' ');error.hidden=false;return;}
            saved=true;redirect=result.redirect;modal.show();
        }catch(exception){error.textContent=exception.message||'Không thể kết nối. Dữ liệu đang được giữ để thử lại.';error.hidden=false;}
        finally{busy=false;if(!saved)button.disabled=false;}
    });
})();
