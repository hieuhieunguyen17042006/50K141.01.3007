(() => {
    const ui=window.THomeUi;
    const modal=id=>bootstrap.Modal.getOrCreateInstance(document.getElementById(id));
    const byName=(form,name)=>form.elements.namedItem(name);
    function company(form){
        const business=byName(form,'LoaiKhach').value==='Doanh nghiệp',fields=form.querySelector('.customer-fields');
        form.classList.toggle('customer-business',business);
        const order=business?['LoaiKhach','HoTen','TenCongTy','DiaChi','GioiTinh','MaSoThue','NgaySinh','SDT','Email','GhiChu']:['LoaiKhach','HoTen','GioiTinh','DiaChi','NgaySinh','SDT','Email','GhiChu','TenCongTy','MaSoThue'];
        for(const name of order)fields.append(fields.querySelector(`[data-customer-row="${name}"]`));
        fields.querySelectorAll('[data-company-field]').forEach(row=>{row.hidden=!business;row.querySelector('input').required=business;});
        const bottom=form.querySelector('.customer-code-total');
        if(bottom){
            for(const [kind,name]of [['code','LoaiKhach'],['total','GioiTinh']]){
                const label=form.querySelector(`[data-system-field="${kind}"]`),slot=fields.querySelector(`[data-customer-row="${name}"] .customer-meta-slot`);
                slot.hidden=!business;(business?slot:bottom).append(label);
            }
            bottom.hidden=business;
        }
    }
    function reset(form){form.reset();ui.clear(form);delete form.dataset.saving;if(form.elements.requestId)window.thomeFormRequestId(form,true);company(form);}
    function validate(form){
        const errors={},editing=form.dataset.customerForm==='edit';
        form.querySelectorAll('input,select,textarea').forEach(input=>{
            if(!input.name||input.readOnly||input.disabled||input.closest('[hidden]')||input.type==='hidden')return;
            if((input.required&&!input.value.trim())||!input.validity.valid)errors[input.name]=['Không hợp lệ!'];
        });
        if(!/^0[0-9]{9}$/.test(byName(form,'SDT').value))errors.SDT=['Không hợp lệ!'];
        if(!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(byName(form,'Email').value))errors.Email=['Không hợp lệ!'];
        if(!editing){
            if(!['Cá nhân','Doanh nghiệp'].includes(byName(form,'LoaiKhach').value))errors.LoaiKhach=['Không hợp lệ!'];
            if(!['Nam','Nữ'].includes(byName(form,'GioiTinh').value))errors.GioiTinh=['Không hợp lệ!'];
            const birth=byName(form,'NgaySinh').value;if(!birth||new Date(birth)>new Date())errors.NgaySinh=['Không hợp lệ!'];
            if(byName(form,'LoaiKhach').value==='Doanh nghiệp'&&!/^(?:[0-9]{10}|[0-9]{13})$/.test(byName(form,'MaSoThue').value))errors.MaSoThue=['Không hợp lệ!'];
        }
        if(Object.keys(errors).length){ui.errors(form,errors);return false;}return true;
    }
    document.querySelectorAll('[data-customer-form]').forEach(form=>{
        byName(form,'LoaiKhach').addEventListener('change',()=>company(form));
        company(form);
        form.closest('.modal').addEventListener('hide.bs.modal',event=>{if(form.dataset.saving)event.preventDefault();});
        form.addEventListener('submit',async event=>{
            event.preventDefault();if(form.dataset.saving||form.dataset.readOnly==='true')return;ui.clear(form);if(!validate(form))return;
            const body=new FormData(form),quick=form.closest('.modal').id==='quickAddCustomerModal';
            const controls=form.querySelectorAll('button');form.dataset.saving='true';controls.forEach(b=>b.disabled=true);
            try{
                let endpoint=form.getAttribute('action'),options={method:'POST',body,headers:{Accept:'application/json','X-Request-Id':window.thomeFormRequestId(form)}};
                if(quick){endpoint='/KhachHang/LuuKhachHangAjax';const input=Object.fromEntries(body);options.body=JSON.stringify(input);options.headers['Content-Type']='application/json';}
                const response=await fetch(endpoint,options);if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể lưu. Vui lòng kiểm tra phiên đăng nhập.');
                const result=await response.json();if(!response.ok||!result.success){ui.errors(form,result.errors,result.message||'Không hợp lệ!');return;}
                if(!quick){
                    const redirect=result.redirect||'/KhachHang/Index';
                    const notice=document.getElementById('catalogResultModal');
                    notice.addEventListener('hidden.bs.modal',()=>location.assign(redirect),{once:true});
                    ui.notify(form.dataset.customerForm==='edit'?'Cập nhật thành công.':'Thêm thành công.');return;
                }
                const k=result.kh,select=document.getElementById('selectCustomer'),option=new Option(`${k.hoTen} (${k.sdt})${k.tenCongTy?' - '+k.tenCongTy:''}`,k.maKH);
                Object.assign(option.dataset,{name:k.hoTen,phone:k.sdt,address:k.diaChi||'',type:k.loaiKhach,company:k.tenCongTy||'',mst:k.maSoThue||'',accum:String(k.tongTichLuy||0)});select.add(option);select.value=k.maKH;onCustomerChange(select);
                ui.notify('Thêm thành công.');document.getElementById('catalogResultModal').addEventListener('hidden.bs.modal',()=>bootstrap.Modal.getInstance(form.closest('.modal')).hide(),{once:true});
            }catch(error){ui.errors(form,{},error.message);}finally{delete form.dataset.saving;controls.forEach(b=>b.disabled=false);}
        });
    });
    document.getElementById('newCustomerButton')?.addEventListener('click',()=>{reset(document.querySelector('#customerAddModal form'));modal('customerAddModal').show();});
    document.getElementById('quickAddCustomerModal')?.addEventListener('show.bs.modal',event=>reset(event.target.querySelector('form')));
    function enableEditing(){const form=document.querySelector('#customerDetailModal form');form.dataset.readOnly='false';form.querySelectorAll('[data-customer-contact]').forEach(input=>input.readOnly=false);document.getElementById('editCustomerButton').hidden=true;document.getElementById('saveCustomerButton').disabled=false;form.querySelector('[data-customer-contact]')?.focus();}
    document.getElementById('editCustomerButton')?.addEventListener('click',enableEditing);
    async function showCustomer(id){
        try{
            const response=await fetch('/KhachHang/ThongTin?id='+encodeURIComponent(id),{headers:{Accept:'application/json'}});if(!response.ok)throw new Error('Không thể tải thông tin khách hàng.');const data=await response.json();
            const form=document.querySelector('#customerDetailModal form');reset(form);form.dataset.readOnly='true';byName(form,'maKH').value=data.maKH;
            form.querySelectorAll('[data-customer-field]').forEach(input=>{const key=input.dataset.customerField;input.value=key==='ngaySinh'?(data[key]||'').slice(0,10):data[key]??'';});
            form.querySelectorAll('[data-customer-contact]').forEach(input=>input.readOnly=true);document.getElementById('editCustomerButton').hidden=false;document.getElementById('saveCustomerButton').disabled=true;
            document.getElementById('customerCode').value=data.maKH;document.getElementById('customerLifetimeTotal').value=String(Number(data.tongTichLuy||0));company(form);modal('customerDetailModal').show();
        }catch(error){ui.notify(error.message);}
    }
    document.querySelectorAll('[data-customer-id]').forEach(row=>{row.addEventListener('click',()=>showCustomer(row.dataset.customerId));row.addEventListener('keydown',event=>{if(['Enter',' '].includes(event.key)){event.preventDefault();showCustomer(row.dataset.customerId);}});});
    document.getElementById('customerPurchasesLink')?.addEventListener('click',async()=>{
        const id=document.querySelector('#customerDetailModal [name=maKH]').value,tbody=document.getElementById('customerPurchaseRows'),error=document.getElementById('customerPurchaseError');tbody.replaceChildren();error.hidden=true;modal('customerPurchasesModal').show();
        try{
            const response=await fetch('/KhachHang/HoaDonDaMua?id='+encodeURIComponent(id),{headers:{Accept:'application/json'}});if(!response.ok)throw new Error('Không thể tải hóa đơn.');const rows=await response.json();
            if(!rows.length){const row=tbody.insertRow(),cell=row.insertCell();cell.colSpan=3;cell.textContent='Không có dữ liệu!';}
            rows.forEach(item=>{const row=tbody.insertRow();[item.code,new Date(item.date).toLocaleDateString('vi-VN'),String(Number(item.total))].forEach(value=>row.insertCell().textContent=value);});
        }catch(e){error.textContent=e.message;error.hidden=false;}
    });
    const query=new URLSearchParams(location.search);if(query.get('customer')&&document.getElementById('customerDetailModal'))showCustomer(query.get('customer'));if(query.get('add')==='1')document.getElementById('newCustomerButton')?.click();
})();
