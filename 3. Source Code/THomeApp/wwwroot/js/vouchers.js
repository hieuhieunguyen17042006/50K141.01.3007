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
    function clearErrors(){form.querySelectorAll('.is-invalid').forEach(e=>{e.classList.remove('is-invalid');e.removeAttribute('aria-invalid');});form.querySelectorAll('[data-error-for],.voucher-field-error:not(.voucher-line-constraint)').forEach(e=>e.textContent='');form.querySelector('.voucher-error').hidden=true;form.querySelector('.voucher-error span').textContent='Không hợp lệ!';}
    function invalid(input,message='Không hợp lệ!'){
        if(input){input.classList.add('is-invalid');input.setAttribute('aria-invalid','true');}
        form.querySelector('.voucher-error').hidden=false;return false;
    }
    function fieldError(name,messages){
        const input=[...form.querySelectorAll('[name]')].find(e=>e.name.toLowerCase()===name.toLowerCase());
        const hint=[...form.querySelectorAll('[data-error-for]')].find(e=>e.dataset.errorFor.toLowerCase()===name.toLowerCase());
        if(input)invalid(input,Array.isArray(messages)?messages.join(' '):String(messages));

        if(!input&&!hint&&kind==='override'&&name==='itemsJson')form.querySelectorAll('.select-sp').forEach(e=>invalid(e));
    }
    function previewExpense(){
        const amount=Number(form.elements.soTien.value),valid=Number.isFinite(amount)&&amount>=0;
        if(!valid){invalid(form.elements.soTien);return;}
        const total=data.total+amount;put('#cppsAddition',amount);put('#cppsNewTotal',total);put('#cppsNewDebt',Math.max(total-data.netPaid,0));
    }
    function returnRows(){return [...document.querySelectorAll('#returnItems tbody tr')].filter(row=>row.querySelector('[type=checkbox]').checked);}
    function previewReturn(){
        let valid=true,returned=0;
        document.querySelectorAll('#returnItems tbody tr').forEach(row=>{
            const selected=row.querySelector('[type=checkbox]').checked,input=row.querySelector('.return-quantity'),quantity=Number(input.value);
            if(selected&&!integer(quantity,1,Number(row.dataset.remaining))){invalid(input);valid=false;return;}
            const amount=selected?quantity*Number(row.dataset.price):0;row.querySelector('.return-line-value').textContent=money(amount);returned+=amount;
        });
        const exchanging=form.elements.loaiPhieu.value==='DoiHang';
        let replacement=0;
        exchangeRows().forEach((row,index)=>{
            const select=row.querySelector('select'),qty=row.querySelector('input'),product=data.products.find(p=>p.code===select.value),price=Number(product?.price||0),quantity=Number(qty.value);
            select.name=`replacements[${index}].MaSP`;qty.name=`replacements[${index}].SoLuong`;
            if(exchanging&&!integer(quantity)){invalid(qty);valid=false;}
            const value=integer(quantity)?price*quantity:0;row.querySelector('.exchange-price').textContent=money(price);row.querySelector('.exchange-value').textContent=money(value);if(exchanging)replacement+=value;
        });
        if(!valid)return;
        const difference=replacement-returned,offset=Math.min(Math.max(-difference,0),data.debt),total=data.total+difference;
        const refund=Math.max(data.netPaid-total,0);
        put('#displayReturnVal',returned);put('#displayExchangeVal',replacement);put('[data-return-difference]',difference);
        put('#returnOffset',offset);put('[data-return-refund]',refund);put('[data-return-extra],#returnDebt',Math.max(total-data.netPaid,0));put('#returnNewTotal,#returnNewTotalSide',total);
        const direct=form.elements.hinhThucXuLy.value==='HoanTienTrucTiep';
        document.getElementById('returnPlanHint').textContent=(offset>0?'Cấn '+money(offset)+' vào công nợ trước, không tạo giao dịch chi. ':'')+(direct?'Phần phải hoàn chờ xác nhận chi tiền riêng.':'Phần vượt công nợ được ghi phải hoàn; xác nhận chi tiền riêng.');
    }
    function exchangeRows(){return [...document.querySelectorAll('#exchangeItems tr')];}
    function addExchange(){
        const row=document.createElement('tr');row.innerHTML='<td><select class="form-select" aria-label="Sản phẩm đổi mới"><option value="">Chọn sản phẩm</option></select></td><td><input type="number" class="form-control" aria-label="Số lượng đổi mới" min="1" max="100000" step="1" value="1" /></td><td class="exchange-price">0 đ</td><td class="exchange-value">0 đ</td><td><button class="voucher-remove-row" type="button" aria-label="Xóa dòng đổi mới">×</button></td>';
        data.products.filter(p=>p.price>0).forEach(p=>row.querySelector('select').add(new Option(p.name+' ('+p.code+')',p.code)));
        row.querySelector('button').addEventListener('click',()=>{row.remove();previewReturn();});document.getElementById('exchangeItems').append(row);previewReturn();
    }
    function addOverride(line){
        const row=document.createElement('tr');row.dataset.originalCode=line.code;row.dataset.originalPrice=line.price;row.dataset.returned=line.protectedQuantity??line.returned;
        row.innerHTML='<td><strong class="override-product-name"></strong><input type="hidden" class="select-sp" /><span class="voucher-field-error"></span></td><td class="col-unit"></td><td><input type="number" class="form-control col-qty" aria-label="Số lượng" step="1" max="100000" /><span class="voucher-field-error"></span></td><td><span class="col-price"></span></td><td class="col-total">0 đ</td>';
        const select=row.querySelector('.select-sp'),qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price');
        row.querySelector('.override-product-name').textContent=data.products.find(p=>p.code===line.code)?.name||line.code;select.value=line.code;qty.value=line.quantity;qty.min=Math.max(line.returned,1);price.dataset.price=line.price;price.textContent=money(line.price);
        row.querySelector('.col-unit').textContent=data.products.find(p=>p.code===line.code)?.unit||'';
        if(Number(row.dataset.returned)>0){select.disabled=true;const hint=row.querySelector('.voucher-field-error');hint.classList.add('voucher-line-constraint');qty.min=row.dataset.returned;select.title='Giữ dòng gốc đã giao/đổi trả.';}
        select.addEventListener('change',()=>{const p=data.products.find(p=>p.code===select.value);const amount=p?(p.code===row.dataset.originalCode?Number(row.dataset.originalPrice):p.price):0;price.dataset.price=amount;price.textContent=money(amount);row.querySelector('.override-product-name').textContent=p?.name||select.value;row.querySelector('.col-unit').textContent=p?.unit||'';previewOverride();});
        document.getElementById('overrideTableBody').append(row);
    }
    function overrideRows(){return [...document.querySelectorAll('#overrideTableBody tr')];}
    function previewOverride(){
        let subtotal=0,valid=true;overrideRows().forEach(row=>{
            const qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price'),q=Number(qty.value),p=Number(price.dataset.price);
            if(!integer(q,Math.max(Number(row.dataset.returned),1))){invalid(qty);valid=false;}
            if(!Number.isFinite(p)||p<=0||!Number.isInteger(p)){if(row.querySelector('.select-sp').value)invalid(row.querySelector('.select-sp'));valid=false;}
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
            const rows=returnRows();if(!rows.length){form.querySelector('.voucher-error').hidden=false;valid=false;}
            rows.forEach(row=>{const input=row.querySelector('.return-quantity');if(!integer(Number(input.value),1,Number(row.dataset.remaining))){invalid(input);valid=false;}});
            if(form.elements.tinhTrangHang.value!=='Còn nguyên vẹn'){invalid(form.elements.tinhTrangHang);valid=false;}
            if(form.elements.loaiPhieu.value==='DoiHang'){
                if(!exchangeRows().length){fieldError('replacements','Không hợp lệ!');valid=false;}
                exchangeRows().forEach(row=>{const select=row.querySelector('select'),qty=row.querySelector('input');if(!select.value){invalid(select);valid=false;}if(!integer(Number(qty.value))){invalid(qty);valid=false;}});
            }
        }else{
            const rows=overrideRows(),seen=new Set();if(rows.length!==data.lines.length){fieldError('itemsJson','Phiếu ghi đè phải giữ nguyên số dòng hiện có. Vui lòng tải lại.');valid=false;}
            rows.forEach((row,index)=>{
                const select=row.querySelector('.select-sp'),qty=row.querySelector('.col-qty'),price=row.querySelector('.col-price');select.name=`items[${index}].MaSP`;qty.name=`items[${index}].SoLuong`;
                if(!select.value||seen.has(select.value)){invalid(select);valid=false;}seen.add(select.value);
                if(!integer(Number(qty.value),Math.max(Number(row.dataset.returned),1))){invalid(qty);valid=false;}
                if(!Number.isInteger(Number(price.dataset.price))||Number(price.dataset.price)<=0){invalid(select);valid=false;}
            });
        }
        if(!valid){form.querySelector('.voucher-error').hidden=false;form.querySelector('.is-invalid')?.focus();}return valid;
    }
    form.addEventListener('submit',async event=>{
        event.preventDefault();if(saved||busy)return;clearErrors();if(!validate())return;
        const body=new FormData(form);
        for(const key of [...body.keys()])if(/^(returns|items|replacements)\[/.test(key))body.delete(key);
        if(kind==='return')returnRows().forEach((row,index)=>{
            body.set(`returns[${index}].MaSP`,row.dataset.sourceProduct);body.set(`returns[${index}].SourceVoucherLineId`,row.dataset.sourceVoucherLine||'');
            body.set(`returns[${index}].SoLuong`,row.querySelector('.return-quantity').value);
            row.querySelector('.return-quantity').name=`returns[${index}].SoLuong`;
        });
        if(kind==='return'&&form.elements.loaiPhieu.value==='DoiHang')exchangeRows().forEach((row,index)=>{body.set(`replacements[${index}].MaSP`,row.querySelector('select').value);body.set(`replacements[${index}].SoLuong`,row.querySelector('input').value);});
        if(kind==='override')body.set('itemsJson',JSON.stringify(overrideRows().map(row=>({OriginalMaSP:row.dataset.originalCode,MaSP:row.querySelector('.select-sp').value,SoLuong:Number(row.querySelector('.col-qty').value),DonGia:Number(row.querySelector('.col-price').dataset.price)}))));
        busy=true;const button=document.getElementById('saveVoucher');button.disabled=true;
        try{
            const response=await fetch(form.getAttribute('action'),{method:'POST',body,headers:{Accept:'application/json','X-Request-Id':form.elements.requestId.value}});
            if(!(response.headers.get('content-type')||'').includes('json'))throw new Error('Không thể lưu phiếu. Kiểm tra phiên đăng nhập và quyền thực hiện.');
            const result=await response.json();
            if(!response.ok||!result.success){form.querySelector('.voucher-error').hidden=false;for(const [key,messages] of Object.entries(result.errors||{}))fieldError(key,messages);const summary=form.querySelector('.voucher-error');summary.querySelector('span').textContent='Không hợp lệ!';return;}
            saved=true;redirect=result.redirect||'/BanHang/ChiTietHoaDon/'+encodeURIComponent(form.elements.maHD.value);
            document.getElementById('voucherSuccessTitle').textContent=result.message||'Tạo phiếu thành công.';modal.show();
        }catch(error){const summary=form.querySelector('.voucher-error');summary.hidden=false;summary.querySelector('span').textContent=error.message;}
        finally{busy=false;if(!saved)button.disabled=false;}
    });
    form.querySelectorAll('a').forEach(link=>link.addEventListener('click',event=>{if(busy||saved)event.preventDefault();}));
    form.addEventListener('input',event=>{if(event.target.matches('[type=checkbox],[type=radio]'))return;form.querySelector('.voucher-error').hidden=true;event.target.classList.remove('is-invalid');event.target.removeAttribute('aria-invalid');const hint=event.target.parentElement.querySelector('[data-error-for],.voucher-field-error');if(hint)hint.textContent='';if(kind==='expense')previewExpense();else if(kind==='return')previewReturn();else previewOverride();});
    if(kind==='expense')previewExpense();
    if(kind==='return'){
        document.getElementById('addExchangeRow').addEventListener('click',addExchange);
        document.querySelectorAll('[data-return-mode]').forEach(button=>button.addEventListener('click',()=>{
            const exchanging=button.dataset.returnMode==='DoiHang';form.elements.loaiPhieu.value=button.dataset.returnMode;
            document.querySelectorAll('[data-return-mode]').forEach(b=>b.setAttribute('aria-selected',String(b===button)));
            document.querySelectorAll('[data-exchange-only]').forEach(e=>e.hidden=!exchanging);document.getElementById('exchangeSection').hidden=!exchanging;
            if(exchanging&&!exchangeRows().length)addExchange();
            document.getElementById('returnQuantityHeading').textContent=exchanging?'SL đổi':'SL trả';document.getElementById('returnValueHeading').textContent=exchanging?'Giá trị đổi':'Giá trị trả';page.querySelectorAll('[data-return-word]').forEach(e=>e.textContent=exchanging?'đổi':'trả');previewReturn();
        }));
        document.querySelectorAll('#returnItems [type=checkbox]').forEach(check=>check.addEventListener('change',()=>{const qty=check.closest('tr').querySelector('.return-quantity');qty.disabled=!check.checked;qty.value=check.checked?1:0;qty.classList.remove('is-invalid');previewReturn();}));
        form.addEventListener('change',previewReturn);previewReturn();
    }
    if(kind==='override'){data.lines.forEach(addOverride);previewOverride();window.calcOverrideTotals=previewOverride;}
})();
