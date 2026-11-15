(() => {
    if(!document.querySelector('[data-sales-list]'))return;
    let busy=false,last=0;
    function remember(){
        const url=new URL(location.href),tab=url.searchParams.get('tab')||'dontamtinh';
        try{
            sessionStorage.setItem('thome:sales-filter:'+tab,url.search);
            sessionStorage.setItem('thome:sales-active-tab',tab);
            document.querySelectorAll('.sales-tabs-toolbar a[href*="tab="]').forEach(a=>{const target=new URL(a.href).searchParams.get('tab'),saved=sessionStorage.getItem('thome:sales-filter:'+target);if(saved)a.href='/BanHang/Index'+saved;});
        }catch{}
    }
    async function refresh(){
        if(busy||document.hidden||Date.now()-last<500)return;busy=true;last=Date.now();
        try{
            const response=await fetch(location.href,{cache:'no-store',headers:{'X-Requested-With':'XMLHttpRequest'}});
            if(!response.ok||!response.url.includes('/BanHang'))return;
            const next=new DOMParser().parseFromString(await response.text(),'text/html').querySelector('[data-sales-list]');
            if(next){document.querySelector('[data-sales-list]').replaceWith(next);remember();}
        }catch{/* Keep the current list and filters when offline. */}finally{busy=false;}
    }
    remember();window.addEventListener('pageshow',e=>{if(e.persisted)refresh();});window.addEventListener('focus',refresh);
    document.addEventListener('visibilitychange',()=>{if(!document.hidden)refresh();});
    window.addEventListener('storage',e=>{if(e.key==='thome:sales-change')refresh();});
})();
