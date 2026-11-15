(() => {
    const form=document.getElementById('loginForm'),element=document.getElementById('loginResultModal');
    const dialog=bootstrap.Modal.getOrCreateInstance(element);
    function show(message){document.getElementById('loginResultTitle').textContent=message;dialog.show();}
    form.addEventListener('submit',event=>{
        if(!form.elements.tenDN.value.trim()||!form.elements.matKhau.value.trim()){
            event.preventDefault();show('Không được để trống!');
        }
    });
    if(element.dataset.message)show(element.dataset.message);
})();
