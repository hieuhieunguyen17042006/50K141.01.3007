(() => {
    const account=document.getElementById('headerAccount'),button=document.getElementById('accountMenuButton'),menu=document.getElementById('accountMenu');
    if(!account||!button||!menu)return;
    function close(focus=false){menu.hidden=true;button.setAttribute('aria-expanded','false');if(focus)button.focus();}
    function open(focus=false){menu.hidden=false;button.setAttribute('aria-expanded','true');if(focus)menu.querySelector('[role=menuitem]').focus();}
    button.addEventListener('click',event=>{if(menu.hidden)open(event.detail===0);else close();});
    button.addEventListener('keydown',event=>{if(event.key==='ArrowDown'){event.preventDefault();open(true);}});
    document.addEventListener('pointerdown',event=>{if(!account.contains(event.target))close();});
    document.addEventListener('keydown',event=>{if(event.key==='Escape'&&!menu.hidden){event.preventDefault();close(true);}});
    account.addEventListener('focusout',()=>requestAnimationFrame(()=>{if(!account.contains(document.activeElement))close();}));

    const element=document.getElementById('logoutConfirmation'),trigger=document.getElementById('accountLogout');
    if(!element||!trigger)return;
    // Keep the confirmation outside the header's stacking context and above its backdrop.
    document.body.append(element);
    const dialog=bootstrap.Modal.getOrCreateInstance(element),form=document.getElementById('headerLogoutForm');
    const no=document.getElementById('cancelLogout'),yes=document.getElementById('confirmLogout');
    function position(){
        const avatar=button.getBoundingClientRect(),box=element.querySelector('.modal-dialog');
        box.style.right=Math.max(12,document.documentElement.clientWidth-avatar.right+2)+'px';
        box.style.top=Math.max(12,Math.min(avatar.bottom+35,window.innerHeight-box.offsetHeight-12))+'px';
    }
    trigger.addEventListener('click',()=>{close();dialog.show();});
    element.addEventListener('show.bs.modal',()=>{document.body.classList.add('logout-confirmation-open');position();});
    element.addEventListener('shown.bs.modal',()=>{position();no.focus();});
    element.addEventListener('hide.bs.modal',event=>{if(form.dataset.logoutSending)event.preventDefault();});
    element.addEventListener('hidden.bs.modal',()=>{document.body.classList.remove('logout-confirmation-open');close(true);});
    window.addEventListener('resize',()=>{if(element.classList.contains('show'))position();});
    form.addEventListener('submit',event=>{
        if(!element.classList.contains('show')||event.submitter!==yes||form.dataset.logoutSending){event.preventDefault();return;}
        form.dataset.logoutSending='true';no.disabled=true;yes.disabled=true;
        // The existing document submit handler sends this POST with its antiforgery token.
    });
    window.addEventListener('pageshow',()=>{delete form.dataset.logoutSending;no.disabled=false;yes.disabled=false;});
})();
