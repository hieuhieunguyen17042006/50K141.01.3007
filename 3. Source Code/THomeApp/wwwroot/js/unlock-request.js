(() => {
    const form=document.getElementById('unlockRequestForm');if(!form)return;
    form.addEventListener('submit',async event=>{
        event.preventDefault();if(form.dataset.saving)return;
        THomeUi.clear(form);const reason=form.elements.lyDoMoKhoa;
        if(!reason.value.trim()||reason.value.length>500){THomeUi.errors(form,{lyDoMoKhoa:['Không hợp lệ!']});return;}
        const body=new FormData(form);form.dataset.saving='true';const buttons=form.querySelectorAll('button');buttons.forEach(b=>b.disabled=true);
        try{
            const response=await fetch(form.getAttribute('action'),{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':form.elements.requestId.value}});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể gửi yêu cầu. Kiểm tra quyền và phiên đăng nhập.');
            const result=await response.json();if(!response.ok||!result.success){THomeUi.errors(form,result.errors,result.message||'Không hợp lệ!');return;}
            form.dataset.sent='true';
            const parent=form.closest('.modal');
            parent.addEventListener('hidden.bs.modal',()=>THomeUi.notify('Đã yêu cầu mở khóa.'),{once:true});
            // Release the save guard before requesting the parent dialog to close.
            delete form.dataset.saving;bootstrap.Modal.getOrCreateInstance(parent).hide();
        }catch(e){THomeUi.errors(form,{},e.message);}finally{delete form.dataset.saving;buttons.forEach(b=>b.disabled=false);if(form.dataset.sent)form.querySelector('[type=submit]').disabled=true;}
    });
})();
