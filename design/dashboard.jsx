// Event dashboard — shared hero + three layout variants exposed as "layout" prop
// layout: "editorial" | "community" | "linear" (but mostly the same with subtle shifts)
// For simplicity we render ONE great dashboard; personality comes from theme tokens.

function TicketStubHero() {
  const e = AZURE_FEST.event;
  const pct = Math.round((e.registered / e.capacity) * 100);
  return (
    <div className="card overflow-hidden relative">
      <div className="hero-gradient p-7 relative">
        <div className="flex items-start justify-between gap-6">
          <div className="min-w-0">
            <div className="flex items-center gap-2 mb-3">
              <span className="badge badge-success"><span className="pulse-dot"></span>{e.status}</span>
              <span className="badge badge-muted"><Icons.Clock size={11}/>{e.daysUntil} days to go</span>
            </div>
            <h1 className="font-display text-[40px] leading-[1.05] font-semibold tracking-tight">{e.name}</h1>
            <p className="text-muted-fg mt-1 text-[15px]">{e.tagline}</p>
            <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2 text-[13.5px]">
              <div className="flex items-center gap-1.5"><Icons.Calendar size={14} className="text-muted-fg"/><span>{e.date}</span></div>
              <div className="flex items-center gap-1.5"><Icons.Clock size={14} className="text-muted-fg"/><span>{e.time}</span></div>
              <div className="flex items-center gap-1.5"><Icons.Pin size={14} className="text-muted-fg"/><span>{e.location}, {e.locationCity}</span></div>
              <a className="flex items-center gap-1.5 text-primary font-medium hover:underline" href="#"><Icons.Globe size={14}/>{e.url}</a>
            </div>
          </div>
          <div className="flex flex-col items-end gap-2 flex-none">
            <button className="btn btn-outline btn-sm"><Icons.Copy size={14}/>Copy link</button>
            <button className="btn btn-primary btn-sm"><Icons.Sparkles size={14}/>Send reminder</button>
          </div>
        </div>
      </div>
      <div className="ticket-perf"></div>
      <div className="grid grid-cols-4 divide-x divide-default">
        <HeroStat label="Registered" value={e.registered} sub={`of ${e.capacity}`} pct={pct}/>
        <HeroStat label="Confirmed" value={e.confirmed} sub={`${Math.round(e.confirmed/e.registered*100)}% of reg.`}/>
        <HeroStat label="Checked in" value={e.checkedIn} sub="event day stat" muted/>
        <HeroStat label="Waitlist" value={e.waitlist} sub="opens at capacity" muted/>
      </div>
    </div>
  );
}

function HeroStat({ label, value, sub, pct, muted }) {
  return (
    <div className="p-5">
      <div className="eyebrow">{label}</div>
      <div className="flex items-baseline gap-2 mt-1.5">
        <span className={"num text-[28px] font-semibold " + (muted ? "text-muted-fg" : "")}>{value}</span>
        <span className="text-[12px] text-muted-fg">{sub}</span>
      </div>
      {pct != null && (
        <div className="mt-3">
          <div className="bar"><span style={{width: pct + "%"}}></span></div>
          <div className="flex justify-between text-[11px] text-muted-fg mt-1.5">
            <span className="num">{pct}%</span>
            <span>capacity</span>
          </div>
        </div>
      )}
    </div>
  );
}

function Sparkline({ data, color = "var(--primary)" }) {
  const w = 320, h = 56, pad = 4;
  const max = Math.max(...data);
  const min = 0;
  const step = (w - pad*2) / (data.length - 1);
  const pts = data.map((v,i) => [pad + i*step, h - pad - ((v-min)/(max-min)) * (h - pad*2)]);
  const path = "M " + pts.map(p => p.join(",")).join(" L ");
  const area = path + ` L ${pts[pts.length-1][0]},${h-pad} L ${pts[0][0]},${h-pad} Z`;
  return (
    <svg viewBox={`0 0 ${w} ${h}`} className="sparkline" preserveAspectRatio="none">
      <defs>
        <linearGradient id="spark" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.25"/>
          <stop offset="100%" stopColor={color} stopOpacity="0"/>
        </linearGradient>
      </defs>
      <path d={area} fill="url(#spark)"/>
      <path d={path} fill="none" stroke={color} strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"/>
      {pts.map((p,i) => <circle key={i} cx={p[0]} cy={p[1]} r={i===pts.length-1?3:0} fill={color}/>)}
    </svg>
  );
}

function TicketBreakdown() {
  const types = AZURE_FEST.ticketTypes.filter(t => !t.hidden);
  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <div className="eyebrow">Ticket types</div>
          <h3 className="font-display text-[18px] font-semibold mt-0.5">Availability</h3>
        </div>
        <button className="btn btn-ghost btn-sm text-muted-fg">Manage <Icons.ChevRight size={12}/></button>
      </div>
      <div className="flex flex-col gap-3.5">
        {types.map(t => {
          const pct = Math.round((t.sold / t.cap) * 100);
          return (
            <div key={t.slug}>
              <div className="flex items-baseline justify-between mb-1.5">
                <div className="flex items-center gap-2">
                  <span className="text-[14px] font-medium">{t.name}</span>
                  {t.closed && <span className="badge badge-muted" style={{fontSize:"0.68rem"}}>Closed</span>}
                  {!t.closed && <span className="badge badge-primary" style={{fontSize:"0.68rem"}}>{t.status}</span>}
                </div>
                <div className="text-[12.5px] text-muted-fg"><span className="num text-fg font-medium">{t.sold}</span> / {t.cap}</div>
              </div>
              <div className="bar"><span style={{width: pct+"%", background: t.closed ? "var(--muted-foreground)" : undefined, opacity: t.closed ? 0.5 : 1}}></span></div>
            </div>
          );
        })}
      </div>
      <div className="hr my-4"></div>
      <button className="btn btn-outline btn-sm w-full"><Icons.Plus size={14}/>Add ticket type</button>
    </div>
  );
}

function TrendCard() {
  const total = AZURE_FEST.trend.reduce((a,b)=>a+b,0);
  const prev = AZURE_FEST.trend.slice(0,7).reduce((a,b)=>a+b,0);
  const curr = AZURE_FEST.trend.slice(7).reduce((a,b)=>a+b,0);
  const delta = Math.round(((curr - prev) / prev) * 100);
  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-1">
        <div>
          <div className="eyebrow">Registrations · last 14 days</div>
          <div className="flex items-baseline gap-3 mt-1.5">
            <span className="font-display num text-[32px] font-semibold leading-none">{total}</span>
            <span className={"badge " + (delta>=0 ? "badge-success" : "badge-warning")}>
              {delta>=0 ? <Icons.ArrowUp size={11}/> : <Icons.ArrowDown size={11}/>}
              {Math.abs(delta)}% vs prior week
            </span>
          </div>
        </div>
        <div className="tab-group">
          <button className="tab" data-active="false">24h</button>
          <button className="tab" data-active="true">14d</button>
          <button className="tab" data-active="false">90d</button>
        </div>
      </div>
      <div className="mt-4"><Sparkline data={AZURE_FEST.trend}/></div>
      <div className="flex justify-between text-[10.5px] text-muted-fg mt-1 px-1 num">
        <span>Apr 06</span><span>Apr 12</span><span>Today</span>
      </div>
    </div>
  );
}

function CheckInCard() {
  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-3">
        <div>
          <div className="eyebrow">Check-in</div>
          <h3 className="font-display text-[18px] font-semibold mt-0.5">Event day</h3>
        </div>
        <span className="badge badge-muted"><Icons.Clock size={11}/>157 days</span>
      </div>
      <div className="rounded-xl border border-default p-4 bg-grid">
        <div className="flex items-start gap-4">
          <div className="h-14 w-14 rounded-lg bg-card border border-default grid place-items-center flex-none">
            <Icons.Qr size={24} className="text-muted-fg"/>
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-[13.5px] leading-relaxed">Check-in opens automatically at <span className="num font-medium">08:00 CEST</span> on event day. Share the QR scanner link with your door team.</p>
            <div className="flex gap-2 mt-3">
              <button className="btn btn-outline btn-sm"><Icons.Qr size={14}/>Scanner</button>
              <button className="btn btn-ghost btn-sm text-muted-fg"><Icons.Copy size={14}/>Share link</button>
            </div>
          </div>
        </div>
      </div>
      <div className="grid grid-cols-3 mt-4 gap-3 text-center">
        <CheckinPill n="0" l="Checked in"/>
        <CheckinPill n="187" l="Expected" primary/>
        <CheckinPill n="0%" l="Complete" muted/>
      </div>
    </div>
  );
}
function CheckinPill({ n, l, primary, muted }) {
  return (
    <div className={"rounded-lg border border-default py-2.5 " + (primary ? "bg-primary/5" : "bg-muted")}>
      <div className={"num text-[18px] font-semibold " + (muted ? "text-muted-fg" : primary ? "text-primary" : "")}>{n}</div>
      <div className="text-[11px] text-muted-fg mt-0.5">{l}</div>
    </div>
  );
}

function EventDetailsCard() {
  const e = AZURE_FEST.event;
  const items = [
    ["Status", <span className="badge badge-success">Registration open</span>],
    ["Date", <span className="num">{e.date}</span>],
    ["Time", <span className="num">{e.time}</span>],
    ["Venue", `${e.location}, ${e.locationCity}`],
    ["Website", <a className="text-primary hover:underline" href="#">{e.url}</a>],
    ["Capacity", <span className="num">{e.capacity} attendees</span>],
    ["Organizer", "Azure Fest NL"],
  ];
  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <div className="eyebrow">Event</div>
          <h3 className="font-display text-[18px] font-semibold mt-0.5">Key details</h3>
        </div>
        <button className="btn btn-ghost btn-sm text-muted-fg"><Icons.Edit size={14}/>Edit</button>
      </div>
      <dl className="divide-y divide-default">
        {items.map(([k,v], i) => (
          <div key={i} className="grid grid-cols-[110px_1fr] gap-3 py-2.5 text-[13.5px]">
            <dt className="text-muted-fg">{k}</dt>
            <dd className="min-w-0 truncate">{v}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}

function ActivityCard() {
  return (
    <div className="card p-5">
      <div className="flex items-center justify-between mb-3">
        <div>
          <div className="eyebrow">Live</div>
          <h3 className="font-display text-[18px] font-semibold mt-0.5 flex items-center gap-2">
            Recent activity <span className="pulse-dot"></span>
          </h3>
        </div>
        <button className="btn btn-ghost btn-sm text-muted-fg">View all</button>
      </div>
      <div className="flex flex-col divide-y divide-default">
        {AZURE_FEST.activity.map((a,i) => (
          <div key={i} className="flex items-center gap-3 py-2.5">
            <div className="h-7 w-7 rounded-full bg-muted grid place-items-center text-[11px] font-semibold text-muted-fg">
              {a.who.split(" ").map(n=>n[0]).slice(0,2).join("")}
            </div>
            <div className="min-w-0 flex-1 text-[13px]">
              <span className="font-medium">{a.who}</span>
              <span className="text-muted-fg"> {a.what} · </span>
              <span className="text-muted-fg">{a.ticket}</span>
            </div>
            <span className="text-[11.5px] text-muted-fg num whitespace-nowrap">{a.t}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function Dashboard() {
  return (
    <div className="flex flex-col gap-5">
      <TicketStubHero/>
      <div className="grid grid-cols-12 gap-5">
        <div className="col-span-12 lg:col-span-8 flex flex-col gap-5">
          <TrendCard/>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <TicketBreakdown/>
            <CheckInCard/>
          </div>
        </div>
        <div className="col-span-12 lg:col-span-4 flex flex-col gap-5">
          <EventDetailsCard/>
          <ActivityCard/>
        </div>
      </div>
    </div>
  );
}

window.Dashboard = Dashboard;
