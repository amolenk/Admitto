// Tweaks panel
function TweaksPanel({ theme, setTheme, dark, setDark, density, setDensity, page, setPage, open, setOpen }) {
  if (!open) return null;
  return (
    <div className="tweaks">
      <div className="flex items-center gap-2 mb-3">
        <Icons.Palette size={14} className="text-primary"/>
        <span className="font-display font-semibold text-[15px]">Tweaks</span>
        <button className="ml-auto btn btn-ghost btn-sm" onClick={()=>setOpen(false)} style={{padding:"0 .4rem", height:"1.4rem"}}>✕</button>
      </div>

      <div className="flex flex-col gap-3">
        <TweakGroup label="Theme">
          <div className="grid grid-cols-3 gap-1">
            {[["editorial","Editorial"],["community","Community"],["linear","Linear"]].map(([v,l]) => (
              <button key={v} onClick={()=>setTheme(v)} className="tab text-center justify-center" data-active={theme===v?"true":"false"} style={{padding:".35rem .3rem", fontSize:"12px"}}>{l}</button>
            ))}
          </div>
        </TweakGroup>

        <TweakGroup label="Mode">
          <div className="grid grid-cols-2 gap-1">
            <button onClick={()=>setDark(false)} className="tab justify-center" data-active={!dark?"true":"false"} style={{padding:".35rem", fontSize:"12px"}}><Icons.Sun size={12}/>Light</button>
            <button onClick={()=>setDark(true)} className="tab justify-center" data-active={dark?"true":"false"} style={{padding:".35rem", fontSize:"12px"}}><Icons.Moon size={12}/>Dark</button>
          </div>
        </TweakGroup>

        <TweakGroup label="Density">
          <div className="grid grid-cols-3 gap-1">
            {["compact","comfortable","roomy"].map(d => (
              <button key={d} onClick={()=>setDensity(d)} className="tab justify-center" data-active={density===d?"true":"false"} style={{padding:".35rem", fontSize:"12px", textTransform:"capitalize"}}>{d}</button>
            ))}
          </div>
        </TweakGroup>

        <TweakGroup label="Page">
          <div className="grid grid-cols-1 gap-1">
            {[["dashboard","Dashboard"],["tickets","Ticket types"],["settings","Settings hub"]].map(([v,l]) => (
              <button key={v} onClick={()=>setPage(v)} className="tab justify-start" data-active={page===v?"true":"false"} style={{padding:".4rem .5rem", fontSize:"12.5px"}}>{l}</button>
            ))}
          </div>
        </TweakGroup>
      </div>
    </div>
  );
}
function TweakGroup({ label, children }) {
  return (
    <div>
      <div className="eyebrow mb-1.5">{label}</div>
      {children}
    </div>
  );
}

function TweaksToggle({ open, setOpen }) {
  if (open) return null;
  return (
    <button className="tweaks" style={{width:"auto", padding:"8px 12px"}} onClick={()=>setOpen(true)}>
      <div className="flex items-center gap-2">
        <Icons.Palette size={14} className="text-primary"/>
        <span className="text-[13px] font-medium">Tweaks</span>
      </div>
    </button>
  );
}

window.TweaksPanel = TweaksPanel;
window.TweaksToggle = TweaksToggle;
