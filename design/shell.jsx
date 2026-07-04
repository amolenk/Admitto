// Sidebar + header + tabs shell
const { useState } = React;

function Wordmark() {
  return (
    <div className="flex items-center gap-2.5 px-1.5 py-1">
      <div className="wordmark-ticket">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M4 8v8M8 6v12M12 6v12M16 6v12M20 8v8"/>
        </svg>
      </div>
      <div className="flex flex-col leading-tight">
        <span className="font-display text-[17px] font-semibold tracking-tight text-fg">Admitto</span>
        <span className="text-[10px] uppercase tracking-wider text-muted-fg">Event ticketing</span>
      </div>
    </div>
  );
}

function TeamSwitcher() {
  return (
    <button className="side-item mt-1 justify-between" style={{padding:"0.5rem 0.625rem"}}>
      <div className="flex items-center gap-2.5 min-w-0">
        <div className="grid place-items-center h-7 w-7 rounded-md bg-primary/15 text-primary font-display font-semibold text-[13px]">AF</div>
        <div className="flex flex-col min-w-0 text-left">
          <span className="text-[13px] font-medium truncate">Azure Fest NL</span>
          <span className="text-[11px] text-muted-fg truncate">Community team</span>
        </div>
      </div>
      <Icons.Chevron size={14} className="text-muted-fg" />
    </button>
  );
}

function SidebarNav({ page, setPage }) {
  return (
    <aside className="bg-sidebar text-sidebar-fg border-r border-default flex flex-col"
           style={{ width: 248, minHeight: "100%", borderColor: "var(--sidebar-border)" }}>
      <div className="px-3 pt-3"><Wordmark /></div>
      <div className="px-3 mt-2"><TeamSwitcher /></div>

      <div className="px-3 mt-5">
        <div className="eyebrow px-2 mb-1.5">Events</div>
        <div className="flex flex-col gap-0.5">
          {TEAM_EVENTS.map(ev => (
            <button key={ev.slug}
                    className="side-item"
                    data-active={ev.active ? "true" : "false"}>
              <span className={"h-1.5 w-1.5 rounded-full " + (ev.active ? "bg-primary" : "")} style={!ev.active ? {background:"var(--border)"} : null}></span>
              <span className="truncate flex-1">{ev.name}</span>
              {ev.active && <span className="text-[10px] text-muted-fg">SEP 23</span>}
            </button>
          ))}
          <button className="side-item text-muted-fg mt-1">
            <Icons.Plus size={14} />
            <span>New event</span>
          </button>
        </div>
      </div>

      <div className="px-3 mt-5">
        <div className="eyebrow px-2 mb-1.5">Azure Fest</div>
        <div className="flex flex-col gap-0.5">
          <button className="side-item" data-active={page==="dashboard"?"true":"false"} onClick={()=>setPage("dashboard")}>
            <Icons.Home size={14}/> Dashboard
          </button>
          <button className="side-item" data-active={page==="registrations"?"true":"false"} onClick={()=>setPage("registrations")}>
            <Icons.Users size={14}/> Registrations
            <span className="ml-auto num text-[11px] text-muted-fg">187</span>
          </button>
          <button className="side-item" data-active={page==="tickets"?"true":"false"} onClick={()=>setPage("tickets")}>
            <Icons.Ticket size={14}/> Ticket types
          </button>
          <button className="side-item">
            <Icons.Mail size={14}/> Emails
          </button>
          <button className="side-item" data-active={page==="settings"?"true":"false"} onClick={()=>setPage("settings")}>
            <Icons.Settings size={14}/> Settings
          </button>
        </div>
      </div>

      <div className="flex-1"></div>

      <div className="px-3 pb-3 mt-4">
        <div className="hr mb-3"></div>
        <button className="side-item">
          <div className="h-6 w-6 rounded-full bg-primary/15 text-primary grid place-items-center text-[11px] font-semibold">AM</div>
          <div className="flex flex-col leading-tight min-w-0 text-left flex-1">
            <span className="text-[12.5px] font-medium truncate">Anne Molenkamp</span>
            <span className="text-[10.5px] text-muted-fg truncate">anne@azurefest.nl</span>
          </div>
          <Icons.ChevUp size={12} className="text-muted-fg" />
        </button>
      </div>
    </aside>
  );
}

function AppHeader({ page, setPage, showDark, setShowDark }) {
  const crumbs = {
    dashboard:     ["Azure Fest NL", "Azure Fest", "Dashboard"],
    registrations: ["Azure Fest NL", "Azure Fest", "Registrations"],
    tickets:       ["Azure Fest NL", "Azure Fest", "Ticket types"],
    settings:      ["Azure Fest NL", "Azure Fest", "Settings"],
  }[page];

  return (
    <header className="h-14 border-b border-default flex items-center gap-3 px-5" style={{background: "color-mix(in oklch, var(--background) 70%, transparent)", backdropFilter: "blur(8px)"}}>
      <nav className="flex items-center gap-1.5 text-[13px] text-muted-fg min-w-0">
        {crumbs.map((c, i) => (
          <React.Fragment key={i}>
            {i>0 && <Icons.ChevRight size={12} />}
            <span className={i===crumbs.length-1 ? "text-fg font-medium truncate" : "truncate hover:text-fg cursor-pointer"}>{c}</span>
          </React.Fragment>
        ))}
      </nav>
      <div className="flex-1"></div>
      <div className="relative hidden md:flex items-center">
        <Icons.Search size={14} className="absolute left-2.5 text-muted-fg" />
        <input className="input pl-8 pr-14 h-8 w-64 text-[13px]" placeholder="Search attendees, tickets…"/>
        <kbd className="absolute right-2">⌘K</kbd>
      </div>
      <button className="btn btn-ghost btn-sm" onClick={()=>setShowDark(!showDark)} title="Toggle theme">
        {showDark ? <Icons.Sun size={14}/> : <Icons.Moon size={14}/>}
      </button>
      <button className="btn btn-outline btn-sm">
        <Icons.Eye size={14}/> View public page
      </button>
    </header>
  );
}

window.SidebarNav = SidebarNav;
window.AppHeader = AppHeader;
