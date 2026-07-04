// Ticket types management
function TicketTypeCard({ t }) {
  const pct = Math.round((t.sold / t.cap) * 100);
  const remaining = t.cap - t.sold;
  return (
    <div className="card overflow-hidden">
      <div className="p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h3 className="font-display text-[18px] font-semibold">{t.name}</h3>
              {t.closed ? (
                <span className="badge badge-muted">Closed</span>
              ) : t.hidden ? (
                <span className="badge badge-muted"><Icons.EyeOff size={11}/>Hidden</span>
              ) : (
                <span className="badge badge-success"><span className="pulse-dot"></span>On sale</span>
              )}
            </div>
            <p className="text-[12.5px] text-muted-fg font-mono">{t.slug}</p>
          </div>
          <button className="btn btn-ghost btn-sm"><Icons.Dots size={16}/></button>
        </div>

        <div className="grid grid-cols-3 gap-4 mt-4">
          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-fg">Sold</div>
            <div className="num text-[22px] font-semibold mt-0.5">{t.sold}</div>
          </div>
          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-fg">Remaining</div>
            <div className={"num text-[22px] font-semibold mt-0.5 " + (remaining===0 ? "text-muted-fg" : "")}>{remaining}</div>
          </div>
          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-fg">Price</div>
            <div className="num text-[22px] font-semibold mt-0.5">{t.price === 0 ? "Free" : `€${t.price}`}</div>
          </div>
        </div>

        <div className="mt-4">
          <div className="bar"><span style={{width: pct + "%", background: t.closed ? "var(--muted-foreground)" : undefined, opacity: t.closed ? 0.5 : 1}}></span></div>
          <div className="flex justify-between text-[11px] text-muted-fg mt-1.5 num">
            <span>{pct}% sold</span>
            <span>cap {t.cap}</span>
          </div>
        </div>
      </div>
      <div className="border-t border-default px-5 py-3 flex items-center gap-2 bg-muted">
        <button className="btn btn-ghost btn-sm"><Icons.Edit size={14}/>Edit</button>
        <button className="btn btn-ghost btn-sm"><Icons.Copy size={14}/>Duplicate</button>
        <div className="flex-1"></div>
        {!t.closed && <button className="btn btn-ghost btn-sm text-destructive">Close sales</button>}
      </div>
    </div>
  );
}

function TicketsPage() {
  const types = AZURE_FEST.ticketTypes;
  const totalSold = types.reduce((s,t)=>s+t.sold,0);
  const totalCap  = types.reduce((s,t)=>s+t.cap,0);
  return (
    <div>
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="eyebrow">Ticket types</div>
          <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">Tickets</h1>
          <p className="text-[13.5px] text-muted-fg mt-1">
            <span className="num text-fg font-medium">{totalSold}</span> sold of <span className="num">{totalCap}</span> across <span className="num">{types.length}</span> ticket types.
          </p>
        </div>
        <div className="flex gap-2">
          <button className="btn btn-outline btn-sm"><Icons.Download size={14}/>Export</button>
          <button className="btn btn-primary btn-sm"><Icons.Plus size={14}/>New ticket type</button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
        {types.map(t => <TicketTypeCard key={t.slug} t={t}/>)}
        <button className="card border-default border-dashed p-6 flex flex-col items-center justify-center gap-2 text-muted-fg hover:text-fg hover:bg-accent transition bg-grid" style={{borderStyle:"dashed", minHeight: 260}}>
          <div className="h-10 w-10 rounded-lg border border-default grid place-items-center bg-card"><Icons.Plus size={18}/></div>
          <div className="font-display text-[15px] font-medium text-fg">Add a ticket type</div>
          <div className="text-[12px] text-center max-w-[200px]">Free, paid, early-bird, or invite-only — all supported.</div>
        </button>
      </div>
    </div>
  );
}

window.TicketsPage = TicketsPage;
