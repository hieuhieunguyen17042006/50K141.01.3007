(() => {
    const root=document.getElementById('orderApp');if(!root)return;
    const map={MaKH:'selectCustomer',DiaChiGiao:'deliveryAddress',GhiChu:'orderNote',ChiPhiVanChuyen:'inputShipping',TyLeDatCoc:'inputDepositPercent'};
    const summary=document.getElementById('orderFormError');let busy=false;
    function showError(key,message){
        const field=document.getElementById(map[key]);if(field){field.classList.add('is-invalid');field.setAttribute('aria-invalid','true');}
        const match=key.replace(/^\$\./,'').match(/^SanPhams\[(\d+)\]\.(MaSP|SoLuong|DonGia)$/i);
        if(match){const row=root.querySelectorAll('#productTableBody tr')[Number(match[1])];if(row){if(match[2].toLowerCase()==='soluong'){row.querySelector('.col-qty').classList.add('is-invalid');}else setProductSelectionError(row,message);}}
    }
    function clear(){summary.hidden=true;summary.querySelector('[data-form-error-message]').textContent='';root.querySelectorAll('.is-invalid').forEach(e=>{e.classList.remove('is-invalid');e.removeAttribute('aria-invalid');});root.querySelectorAll('[data-order-error],.quantity-error').forEach(e=>e.textContent='');root.querySelectorAll('#productTableBody tr').forEach(row=>setProductSelectionError(row,''));}
    function message(text,errors={}){
        const keys=Object.keys(errors),searchOnly=!document.getElementById('orderProductSearchError').hidden&&keys.length>0&&keys.every(k=>/^SanPhams(?:\[\d+\]\.MaSP)?$/.test(k));
        summary.hidden=searchOnly;summary.querySelector('[data-form-error-message]').textContent=keys.length?'Không hợp lệ!':text;
        for(const [key,value]of Object.entries(errors))showError(key,Array.isArray(value)?value.join(' '):value);root.querySelector('.is-invalid')?.focus();
    }
    function collect(){
        const errors={},items=[],seen=new Set();
        if(!document.getElementById('selectCustomer').value)errors.MaKH='Vui lòng chọn khách hàng.';
        root.querySelectorAll('#productTableBody tr').forEach((row,index)=>{
            const product=getSelectedProduct(row),qty=Number(row.querySelector('.col-qty').value);
            if(!product||product.DonGia<=0)errors[`SanPhams[${index}].MaSP`]='Vui lòng chọn sản phẩm có giá hợp lệ từ danh sách.';
            else if(seen.has(product.MaSP))errors[`SanPhams[${index}].MaSP`]='Sản phẩm đã có ở dòng khác.';
            if(product)seen.add(product.MaSP);
            if(!Number.isInteger(qty)||qty<=0||qty>100000)errors[`SanPhams[${index}].SoLuong`]='Số lượng phải là số nguyên từ 1 đến 100.000.';
            items.push({MaSP:product?.MaSP||'',SoLuong:qty,DonGia:product?.DonGia||0});
        });
        if(items.length===0||items.length>100)errors.SanPhams='Chọn từ 1 đến 100 dòng sản phẩm.';
        const shipping=Number(document.getElementById('inputShipping').value),isDeposit=document.getElementById('checkDeposit').checked,pct=Number(document.getElementById('inputDepositPercent').value);
        if(!Number.isFinite(shipping)||shipping<0||shipping>1e12)errors.ChiPhiVanChuyen='Phí vận chuyển không hợp lệ.';
        if(isDeposit&&(!Number.isFinite(pct)||pct<30||pct>50))errors.TyLeDatCoc='Tỷ lệ đặt cọc phải từ 30% đến 50%.';
        const address=document.getElementById('deliveryAddress').value,note=document.getElementById('orderNote').value;
        if(address.length>255)errors.DiaChiGiao='Địa chỉ tối đa 255 ký tự.';if(note.length>500)errors.GhiChu='Ghi chú tối đa 500 ký tự.';
        return {errors,payload:{MaKH:document.getElementById('selectCustomer').value,DiaChiGiao:address,GhiChu:note,ChiPhiVanChuyen:shipping,ApDungVAT:document.getElementById('checkVAT').checked,IsDatCoc:isDeposit,TyLeDatCoc:isDeposit?pct:0,SanPhams:items}};
    }
    window.prepareOrder=async()=>{
        if(busy)return;clear();const {errors,payload}=collect();
        if(Object.keys(errors).length){message('Thông tin đơn chưa hợp lệ. Vui lòng kiểm tra các ô được đánh dấu.',errors);return;}
        busy=true;const button=document.getElementById('btnCompleteAndQR');button.disabled=true;
        try{
            const response=await fetch('/BanHang/ChuanBiDonTamTinh',{method:'POST',headers:{'Content-Type':'application/json',Accept:'application/json'},body:JSON.stringify(payload)});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể chuẩn bị đơn. Kiểm tra phiên đăng nhập và thử lại.');
            const result=await response.json();if(!response.ok||!result.success){message(result.message||'Không thể chuẩn bị đơn.',result.errors);return;}
            location.assign(result.redirect);
        }catch(error){message(error.message||'Không thể kết nối. Dữ liệu nhập vẫn được giữ để thử lại.');}
        finally{busy=false;button.disabled=false;}
    };
    root.addEventListener('input',event=>{if(event.target.matches('input,select,textarea')){event.target.classList.remove('is-invalid');event.target.removeAttribute('aria-invalid');}});
    window.addEventListener('DOMContentLoaded',()=>{
        const element=document.getElementById('restoredOrder');if(!element)return;
        const data=JSON.parse(element.textContent);if(!data)return;
        const select=document.getElementById('selectCustomer');select.value=data.MaKH;onCustomerChange(select);
        document.getElementById('deliveryAddress').value=data.DiaChiGiao||'';document.getElementById('orderNote').value=data.GhiChu||'';
        document.getElementById('inputShipping').value=data.ChiPhiVanChuyen;document.getElementById('checkVAT').checked=data.ApDungVAT;
        document.getElementById(data.IsDatCoc?'checkDeposit':'payFull').checked=true;document.getElementById('inputDepositPercent').value=data.IsDatCoc?data.TyLeDatCoc:30;
        [...root.querySelectorAll('#productTableBody tr')].forEach(row=>removeProductRow(row.id));
        for(const item of data.SanPhams){addProductRow(false);const row=document.getElementById('productTableBody').lastElementChild,product=availableProducts.find(p=>p.MaSP===item.MaSP);if(product)selectProduct(row,product);else{row.querySelector('.product-name').value=item.MaSP;setProductSelectionError(row,'Sản phẩm không còn bán. Vui lòng chọn lại.');}row.querySelector('.col-qty').value=item.SoLuong;updateProductRow(row);}
        toggleDeposit();calculateTotal();
    });
})();
