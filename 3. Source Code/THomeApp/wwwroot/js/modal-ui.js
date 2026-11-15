// Shared error region and stacked Bootstrap dialogs. A child never dismisses its parent.
(() => {
    // Keep dialogs outside layout containers that establish blur/stacking contexts.
    document.querySelectorAll('.modal').forEach(element=>document.body.append(element));
    const dialogs=[],origins=new WeakMap();
    document.addEventListener('show.bs.modal',event=>{
        const element=event.target;if(!element.classList.contains('modal'))return;
        origins.set(element,document.activeElement);
        dialogs.forEach(parent=>{bootstrap.Modal.getInstance(parent)?._focustrap.deactivate();parent.inert=true;});
        const index=dialogs.indexOf(element);if(index>=0)dialogs.splice(index,1);
        dialogs.push(element);element.style.zIndex=String(1055+20*(dialogs.length-1));
        queueMicrotask(()=>{const backdrops=document.querySelectorAll('.modal-backdrop');const last=backdrops[backdrops.length-1];if(last)last.style.zIndex=String(1050+20*(dialogs.length-1));});
        document.body.classList.add('catalog-modal-open');
    });
    document.addEventListener('hidden.bs.modal',event=>{
        const index=dialogs.indexOf(event.target);if(index>=0)dialogs.splice(index,1);
        const parent=dialogs.at(-1);
        event.target.inert=false;
        if(parent){parent.inert=false;document.body.classList.add('modal-open');bootstrap.Modal.getInstance(parent)?._focustrap.activate();const origin=origins.get(event.target);if(origin&&parent.contains(origin))origin.focus();}
        else document.body.classList.remove('catalog-modal-open');
    });
    window.THomeUi={
        clear(form){form.querySelectorAll('.is-invalid').forEach(input=>{input.classList.remove('is-invalid');input.removeAttribute('aria-invalid');});const region=form.querySelector('.form-error-bottom');if(region){region.hidden=true;(region.querySelector('[data-form-error-message]')||region).textContent='';}},
        errors(form,fields={},message='Không hợp lệ!'){
            for(const key of Object.keys(fields)){const input=[...form.querySelectorAll('[name]')].find(e=>e.name.toLowerCase()===key.toLowerCase());if(input){input.classList.add('is-invalid');input.setAttribute('aria-invalid','true');}}
            const region=form.querySelector('.form-error-bottom');if(region){(region.querySelector('[data-form-error-message]')||region).textContent=message;region.hidden=false;}
            form.querySelector('.is-invalid:not([readonly]):not(:disabled)')?.focus();
        },
        notify(message,id='catalogResultModal',kind='success'){
            const element=document.getElementById(id);if(!element)return;
            element.dataset.noticeKind=kind;element.querySelector('h2').textContent=message;bootstrap.Modal.getOrCreateInstance(element).show();
        }
    };
})();
