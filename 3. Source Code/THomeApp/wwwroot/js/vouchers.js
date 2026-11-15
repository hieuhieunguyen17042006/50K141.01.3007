(() => {
    const page=document.querySelector('[data-voucher-kind]');if(!page)return;
    const kind=page.dataset.voucherKind, data=JSON.parse(document.getElementById('voucherData').textContent), form=page.querySelector('form.voucher-form');
    const money=n=>Number(n).toLocaleString('vi-VN',{maximumFractionDigits:2})+' đ';
    const put=(selector,n)=>page.querySelectorAll(selector).forEach(e=>e.textContent=money(n));
    const round=n=>{const floor=Math.floor(n),fraction=n-floor;return Math.abs(fraction-.5)<1e-8?(floor%2===0?floor:floor+1):Math.round(n);};
    const integer=(n,min=1,max=100000)=>Number.isInteger(n)&&n>=min&&n<=max;
    let saved=false,busy=false,redirect;
    const modalElement=document.getElementById('voucherSuccessModal');document.body.append(modalElement);
    const modal=bootstrap.Modal.getOrCreateInstance(modalElement);
    modalElement.addEventListener('hidden.bs.modal',()=>{if(saved&&redirect)location.assign(redirect);});
    function clearErrors(){form.querySelectorAll('.is-invalid').forEach(e=>{e.classList.remove('is-invalid');e.removeAttribute('aria-invalid');});form.querySelectorAll('[data-error-for],.voucher-field-error').forEach(e=>e.textContent='');form.querySelector('.voucher-error').hidden=true;}
    function invalid(input,message='Không hợp lệ!'){
        if(input){input.classList.add('is-invalid');input.setAttribute('aria-invalid','true');const hint=input.parentElement.querySelector('[data-error-for],.voucher-field-error');if(hint)hint.textContent=message;}
        form.querySelector('.voucher-error').hidden=false;return false;
    }
    function fieldError(name,messages){
        const input=[...form.querySelectorAll('[name]')].find(e=>e.name.toLowerCase()===name.toLowerCase());
        const hint=[...form.querySelectorAll('[data-error-for]')].find(e=>e.dataset.errorFor.toLowerCase()===name.toLowerCase());
        if(input)invalid(input,Array.isArray(messages)?messages.join(' '):String(messages));
        if(hint)hint.textContent=Array.isArray(messages)?messages.join(' '):String(messages);
        if(!input&&!hint&&kind==='override'&&name==='itemsJson')form.querySelectorAll('.select-sp').forEach(e=>invalid(e));
    }
    function previewExpense(){
        const amount=Number(form.elements.soTien.value),valid=Number.isFinite(amount)&&amount>=0,customer=form.elements.loaiChiPhi.value==='ThuThemKhach';
        if(!valid){invalid(form.elements.soTien);return;}
        const total=data.total+(customer?amount:0);put('#cppsAddition',amount);put('#cppsNewTotal',total);put('#cppsNewDebt',Math.max(total-data.netPaid,0));
        document.getElementById('expenseHint').textContent=customer?'Phí chưa thu không thay đổi số tiền đã thu.':'Cửa hàng tự chi thực tế: không cộng vào khoản khách phải trả.';
    }
    function returnRows(){return [...document.querySelectorAll('#returnItems tbody tr')].filter(row=>row.querySelector('[type=checkbox]').checked);}
    function previewReturn(){
        let valid=true,returned=0;
        document.querySelectorAll('#returnItems tbody tr').forEach(row=>{
            const selected=row.querySelector('[type=checkbox]').checked,input=row.querySelector('.return-quantity'),quantity=Number(input.value);
            if(selected&&!integer(quantity,1,Number(row.dataset.remaining))){invalid(input);valid=false;return;}
            const amount=selected?quantity*Number(row.dataset.price):0;row.querySelector('.return-line-value').textContent=money(amount);returned+=amount;
        });
        const exchanging=form.elements.loaiPhieu.value==='DoiHang',select=form.elements.maSPDoi,qty=form.elements.soLuongDoi,price=Number(select.selectedOptions[0]?.dataset.price||0);
        if(exchanging&&!integer(Number(qty.value))){invalid(qty);valid=false;}
        if(!valid)return;
        const replacement=exchanging?price*Number(qty.value):0,difference=replacement-returned,offset=Math.min(Math.max(-difference,0),data.debt),total=data.total+difference;
        const refund=Math.max(Math.max(data.netPaid-total,0)-data.refund,0);
        put('#displayReturnVal',returned);put('#displayExchangeVal,#exchangeLineValue',replacement);put('[data-return-difference]',difference);
        put('#returnOffset',offset);put('[data-return-refund]',refund);put('[data-return-extra]',Math.max(difference,0));put('#returnNewTotal',total);
        const direct=form.elements.hinhThucXuLy.value==='HoanTienTrucTiep';
        document.getElementById('returnPlanHint').textContent=(offset>0?'Cấn '+money(offset)+' vào công nợ trước, không tạo giao dịch chi. ':'')+(direct?'Phần phải hoàn chờ xác nhận chi tiền riêng.':'Phần vượt công nợ được ghi phải hoàn; xác nhận chi tiền riêng.');
    }
    function addOverride(line={code:'',quantity:1,price:0,returned:0}){
        const row=document.createElement('tr');row.dataset.originalCode=line.code;row.dataset.originalPrice=line.price;row.dataset.returned=line.returned;
        row.innerHTML='<td><select class="form-select select-sp" aria-label="Sản phẩm"><option value="">Chọn sản phẩm</option></select><span class="voucher-field-error"></span></td><td class="col-unit"></td><td><input type="number" class="form-control col-qty" aria-label="Số lượng" step="1" max="100000" /><span class="voucher-field-error"></span></td><td><input type="number" class="form-control col-price" aria-label="Đơn giá" step="1" min="1" /><span class="voucher-field-error"></span></td><td class="col-total">0 đ</td><td><button type="button" class="voucher-remove-row" aria-label="Xóa dòng">×</button></td>';
        const select=row.querySelector('select'),qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price');
        data.products.filter(p=>!p.deleted||p.code===line.code).forEach(p=>{const option=new Option(p.name,p.code);select.add(option);});select.value=line.code;qty.value=line.quantity;qty.min=Math.max(line.returned,1);price.value=line.price;
        row.querySelector('.col-unit').textContent=data.products.find(p=>p.code===line.code)?.unit||'';
        if(line.returned>0){select.disabled=true;row.querySelector('.voucher-remove-row').disabled=true;}
        select.addEventListener('change',()=>{const p=data.products.find(p=>p.code===select.value);price.value=p?(p.code===row.dataset.originalCode?Number(row.dataset.originalPrice):p.price):0;row.querySelector('.col-unit').textContent=p?.unit||'';previewOverride();});
        row.querySelector('.voucher-remove-row').addEventListener('click',()=>{row.remove();previewOverride();});
        document.getElementById('overrideTableBody').append(row);
    }
    function overrideRows(){return [...document.querySelectorAll('#overrideTableBody tr')];}
    function previewOverride(){
        let subtotal=0,valid=true;overrideRows().forEach(row=>{
            const qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price'),q=Number(qty.value),p=Number(price.value);
            if(!integer(q,Math.max(Number(row.dataset.returned),1))){invalid(qty);valid=false;}
            if(!Number.isFinite(p)||p<=0||!Number.isInteger(p)){if(row.querySelector('select').value)invalid(price);valid=false;}
            if(!valid)return;const total=q*p;row.querySelector('.col-total').textContent=money(total);subtotal+=total;
        });
        if(!valid)return;
        const discount=round(subtotal*data.discountRate),tax=round((subtotal-discount)*data.taxRate),total=subtotal-discount+tax+data.extraCharges;
        put('#displayNewSubtotal',subtotal);put('#overrideDiscount',-discount);put('#overrideTax',tax);put('#displayNewTotal',total);put('#boxCanThuThem',Math.max(total-data.netPaid,0));put('#boxPhaiTraKhach',Math.max(data.netPaid-total,0));
    }
    function validate(){
        let valid=true;
        const reason=kind==='override'?form.elements.lyDoGhiDe:form.elements.lyDo;
        if(!reason.value.trim()||reason.value.length>500){invalid(reason);valid=false;}
        if(kind==='expense'){
            const amount=Number(form.elements.soTien.value);if(!Number.isFinite(amount)||amount<=0){invalid(form.elements.soTien);valid=false;}
        }else if(kind==='return'){
            const rows=returnRows();if(!rows.length){form.querySelector('[data-error-for=returns]').textContent='Không hợp lệ! Chọn ít nhất một mặt hàng.';valid=false;}
            rows.forEach(row=>{const input=row.querySelector('.return-quantity');if(!integer(Number(input.value),1,Number(row.dataset.remaining))){invalid(input);valid=false;}});
            if(form.elements.tinhTrangHang.value!=='Còn nguyên vẹn'){invalid(form.elements.tinhTrangHang);valid=false;}
            if(form.elements.loaiPhieu.value==='DoiHang'){
                if(!form.elements.maSPDoi.value){invalid(form.elements.maSPDoi);valid=false;}
                if(!integer(Number(form.elements.soLuongDoi.value))){invalid(form.elements.soLuongDoi);valid=false;}
            }
        }else{
            const rows=overrideRows(),seen=new Set();if(!rows.length){fieldError('itemsJson','Không hợp lệ! Chọn ít nhất một sản phẩm.');valid=false;}
            rows.forEach((row,index)=>{
                const select=row.querySelector('select'),qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price');select.name=`items[${index}].MaSP`;qty.name=`items[${index}].SoLuong`;price.name=`items[${index}].DonGia`;
                if(!select.value||seen.has(select.value)){invalid(select);valid=false;}seen.add(select.value);
                if(!integer(Number(qty.value),Math.max(Number(row.dataset.returned),1))){invalid(qty);valid=false;}
                if(!Number.isInteger(Number(price.value))||Number(price.value)<=0){invalid(price);valid=false;}
            });
        }
        if(!valid){form.querySelector('.voucher-error').hidden=false;form.querySelector('.is-invalid')?.focus();}return valid;
    }
    form.addEventListener('submit',async event=>{
        event.preventDefault();if(saved||busy)return;clearErrors();if(!validate())return;
        const body=new FormData(form);
        for(const key of [...body.keys()])if(/^(returns|items)\[/.test(key))body.delete(key);
        if(kind==='return')returnRows().forEach((row,index)=>{
            body.set(`returns[${index}].MaSP`,row.dataset.sourceProduct);body.set(`returns[${index}].SourceVoucherLineId`,row.dataset.sourceVoucherLine||'');
            body.set(`returns[${index}].SoLuong`,row.querySelector('.return-quantity').value);
            row.querySelector('.return-quantity').name=`returns[${index}].SoLuong`;
        });
        if(kind==='override')body.set('itemsJson',JSON.stringify(overrideRows().map(row=>({MaSP:row.querySelector('select').value,SoLuong:Number(row.querySelector('.col-qty').value),DonGia:Number(row.querySelector('.col-price').value)}))));
        busy=true;const button=document.getElementById('saveVoucher');button.disabled=true;
        try{
            const response=await fetch(form.getAttribute('action'),{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':form.elements.requestId.value}});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể lưu phiếu. Kiểm tra phiên đăng nhập và quyền thực hiện.');
            const result=await response.json();
            if(!response.ok||!result.success){form.querySelector('.voucher-error').hidden=false;for(const [key,messages] of Object.entries(result.errors||{}))fieldError(key,messages);const summary=form.querySelector('.voucher-error');summary.textContent=result.message==='Không hợp lệ!'?'Không hợp lệ!':'Không hợp lệ! '+(result.message||'');return;}
            saved=true;redirect=result.redirect||'/BanHang/ChiTietHoaDon/'+encodeURIComponent(form.elements.maHD.value);
            document.getElementById('voucherSuccessTitle').textContent=result.message||'Tạo phiếu thành công.';document.getElementById('voucherSavedCode').textContent='Mã phiếu: '+result.maPhieu;modal.show();
        }catch(error){const summary=form.querySelector('.voucher-error');summary.hidden=false;summary.textContent=error.message;}
        finally{busy=false;if(!saved)button.disabled=false;}
    });
    form.querySelectorAll('a').forEach(link=>link.addEventListener('click',event=>{if(busy||saved)event.preventDefault();}));
    form.addEventListener('input',event=>{if(event.target.matches('[type=checkbox],[type=radio]'))return;form.querySelector('.voucher-error').hidden=true;event.target.classList.remove('is-invalid');event.target.removeAttribute('aria-invalid');const hint=event.target.parentElement.querySelector('[data-error-for],.voucher-field-error');if(hint)hint.textContent='';if(kind==='expense')previewExpense();else if(kind==='return')previewReturn();else previewOverride();});
    if(kind==='expense'){form.elements.loaiChiPhi.addEventListener('change',previewExpense);previewExpense();}
    if(kind==='return'){
        document.querySelectorAll('[data-return-mode]').forEach(button=>button.addEventListener('click',()=>{
            const exchanging=button.dataset.returnMode==='DoiHang';form.elements.loaiPhieu.value=button.dataset.returnMode;
            document.querySelectorAll('[data-return-mode]').forEach(b=>b.setAttribute('aria-selected',String(b===button)));
            document.querySelectorAll('[data-exchange-only]').forEach(e=>e.hidden=!exchanging);document.getElementById('exchangeSection').hidden=!exchanging;
            form.elements.maSPDoi.disabled=!exchanging;form.elements.soLuongDoi.disabled=!exchanging;
            document.getElementById('returnQuantityHeading').textContent=exchanging?'SL đổi':'SL trả';document.getElementById('returnValueHeading').textContent=exchanging?'Giá trị đổi':'Giá trị trả';page.querySelector('[data-return-word]').textContent=exchanging?'đổi':'trả';previewReturn();
        }));
        document.querySelectorAll('#returnItems [type=checkbox]').forEach(check=>check.addEventListener('change',()=>{const qty=check.closest('tr').querySelector('.return-quantity');qty.disabled=!check.checked;qty.value=check.checked?1:0;qty.classList.remove('is-invalid');previewReturn();}));
        form.addEventListener('change',previewReturn);previewReturn();
    }
    if(kind==='override'){data.lines.forEach(addOverride);document.getElementById('addOverrideRow').addEventListener('click',()=>addOverride());previewOverride();window.calcOverrideTotals=previewOverride;}
})();
