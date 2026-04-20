// Event settings hub
function SettingsNav({ section, setSection }) {
  const items = [
    { id: "general", label: "General", icon: "Settings", desc: "Name, date, venue, website" },
    { id: "registration", label: "Registration", icon: "Users", desc: "Policy, windows, waitlist" },
    { id: "email", label: "Email", icon: "Mail", desc: "Templates, SMTP, sender" },
    { id: "access", label: "Access", icon: "Zap", desc: "Coupons & recipient lists" },
    { id: "danger", label: "Danger zone", icon: "Trash", desc: "Cancel or archive" },
  ];
  return (
    <nav className="flex flex-col gap-1">
      {items.map(it => {
        const Ic = Icons[it.icon];
        const active = section===it.id;
        return (
          <button key={it.id}
                  onClick={()=>setSection(it.id)}
                  className="side-item flex-col items-start !p-3 gap-0.5" data-active={active?"true":"false"}>
            <div className="flex items-center gap-2 w-full">
              <Ic size={14} className={active ? "text-primary" : "text-muted-fg"}/>
              <span className="text-[13.5px] font-medium">{it.label}</span>
              {active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary"></span>}
            </div>
            <div className="text-[11.5px] text-muted-fg pl-6">{it.desc}</div>
          </button>
        );
      })}
    </nav>
  );
}

function Field({ label, children, hint, required, badge }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
      <div>
        <label className="text-[13.5px] font-medium flex items-center gap-1.5">
          {label}
          {required && <span className="text-destructive">*</span>}
          {badge && <span className="badge badge-muted" style={{fontSize:"0.65rem"}}>{badge}</span>}
        </label>
        {hint && <p className="text-[12px] text-muted-fg mt-0.5 leading-snug">{hint}</p>}
      </div>
      <div className="min-w-0">{children}</div>
    </div>
  );
}

function GeneralSettings() {
  return (
    <div>
      <div className="flex items-start justify-between mb-5">
        <div>
          <h2 className="font-display text-[22px] font-semibold">General</h2>
          <p className="text-[13.5px] text-muted-fg">Public-facing event details.</p>
        </div>
        <div className="flex gap-2">
          <button className="btn btn-ghost btn-sm">Discard</button>
          <button className="btn btn-primary btn-sm"><Icons.Check size={14}/>Save changes</button>
        </div>
      </div>

      <div className="card">
        <div className="px-6 divide-y-border">
          <Field label="Event name" required hint="Shown on the public page and in all emails.">
            <input className="input" defaultValue="Azure Fest"/>
          </Field>
          <Field label="URL slug" hint="Used in registration links." badge="Immutable">
            <div className="flex items-center gap-2">
              <span className="text-[13px] text-muted-fg font-mono">admitto.app/azurefest-nl/</span>
              <input className="input max-w-[220px] font-mono" defaultValue="azurefest-2026" disabled/>
            </div>
          </Field>
          <Field label="Tagline" hint="One short line under the title.">
            <input className="input" defaultValue="The Dutch Azure community day"/>
          </Field>
          <Field label="Date & time" required>
            <div className="grid grid-cols-2 gap-2">
              <input className="input" defaultValue="2026-09-23"/>
              <input className="input" defaultValue="09:00 – 18:00"/>
            </div>
          </Field>
          <Field label="Timezone">
            <select className="input"><option>Europe/Amsterdam (CEST, UTC+2)</option></select>
          </Field>
          <Field label="Venue">
            <div className="grid grid-cols-1 md:grid-cols-[1fr_200px] gap-2">
              <input className="input" defaultValue="Pathé Ede"/>
              <input className="input" defaultValue="Ede, Netherlands"/>
            </div>
          </Field>
          <Field label="Website">
            <div className="flex items-center gap-2">
              <span className="text-[13px] text-muted-fg">https://</span>
              <input className="input" defaultValue="www.azurefest.nl"/>
            </div>
          </Field>
          <Field label="Description" hint="Supports Markdown.">
            <textarea className="input textarea" defaultValue="Azure Fest is the largest community-run Azure event in the Netherlands. A full day of talks, workshops, and conversation with cloud practitioners from across the region."/>
          </Field>
        </div>
      </div>
    </div>
  );
}

function RegistrationSettings() {
  return (
    <div>
      <div className="flex items-start justify-between mb-5">
        <div>
          <h2 className="font-display text-[22px] font-semibold">Registration</h2>
          <p className="text-[13.5px] text-muted-fg">Control when and how people can register.</p>
        </div>
        <button className="btn btn-primary btn-sm"><Icons.Check size={14}/>Save changes</button>
      </div>
      <div className="card">
        <div className="px-6 divide-y-border">
          <Field label="Registration status" hint="Temporarily close registration without cancelling.">
            <div className="flex items-center gap-3">
              <button className="btn btn-outline btn-sm bg-primary/5 text-primary" style={{borderColor:"color-mix(in oklch, var(--primary) 30%, transparent)"}}>
                <span className="h-1.5 w-1.5 rounded-full bg-primary"></span> Open
              </button>
              <button className="btn btn-ghost btn-sm text-muted-fg">Close</button>
            </div>
          </Field>
          <Field label="Registration window" hint="Outside these dates, registration is closed.">
            <div className="grid grid-cols-2 gap-2">
              <input className="input" defaultValue="2026-04-01 00:00"/>
              <input className="input" defaultValue="2026-09-20 23:59"/>
            </div>
          </Field>
          <Field label="Capacity" hint="Total tickets across all types.">
            <div className="flex items-center gap-3">
              <input className="input max-w-[120px] num" defaultValue="250"/>
              <span className="text-[13px] text-muted-fg">tickets</span>
            </div>
          </Field>
          <Field label="Per-attendee limit" hint="Max tickets one person can claim.">
            <input className="input max-w-[120px] num" defaultValue="1"/>
          </Field>
          <Field label="Waitlist" hint="Automatically enrol attendees when capacity is reached.">
            <label className="flex items-center gap-2">
              <input type="checkbox" className="h-4 w-4" defaultChecked/>
              <span className="text-[13.5px]">Enable waitlist</span>
            </label>
          </Field>
          <Field label="Reconfirm policy" hint="Ask registrants to reconfirm in the week before the event." badge="Optional">
            <label className="flex items-center gap-2">
              <input type="checkbox" className="h-4 w-4" defaultChecked/>
              <span className="text-[13.5px]">Send reconfirm email <span className="num text-muted-fg">7 days</span> before</span>
            </label>
          </Field>
        </div>
      </div>
    </div>
  );
}

function EmailSettings() {
  return (
    <div>
      <div className="flex items-start justify-between mb-5">
        <div>
          <h2 className="font-display text-[22px] font-semibold">Email</h2>
          <p className="text-[13.5px] text-muted-fg">Sender identity, SMTP, and templates.</p>
        </div>
        <button className="btn btn-primary btn-sm"><Icons.Check size={14}/>Save changes</button>
      </div>
      <div className="card">
        <div className="px-6 divide-y-border">
          <Field label="From name" hint="Shown in recipients' inbox.">
            <input className="input" defaultValue="Azure Fest NL"/>
          </Field>
          <Field label="From address" required>
            <input className="input" defaultValue="hello@azurefest.nl"/>
          </Field>
          <Field label="Reply-to">
            <input className="input" defaultValue="organizers@azurefest.nl"/>
          </Field>
          <Field label="SMTP host" badge="Azure Communication">
            <input className="input font-mono" defaultValue="smtp.azurecomm.net"/>
          </Field>
          <Field label="Port & auth">
            <div className="grid grid-cols-[120px_1fr] gap-2">
              <input className="input num" defaultValue="587"/>
              <select className="input"><option>STARTTLS + Basic auth</option></select>
            </div>
          </Field>
        </div>
      </div>

      <div className="mt-5">
        <h3 className="font-display text-[16px] font-semibold mb-2.5">Templates</h3>
        <div className="card divide-y-border">
          {[
            ["Verification email","Sent when someone starts registration","Default"],
            ["Ticket confirmation","Sent after successful registration","Customised"],
            ["Reconfirm","One-week-out reconfirmation","Default"],
            ["Cancellation","When an attendee cancels","Default"],
          ].map(([n,d,s]) => (
            <div key={n} className="flex items-center gap-4 p-4">
              <div className="h-8 w-8 rounded-md bg-muted grid place-items-center"><Icons.Mail size={14} className="text-muted-fg"/></div>
              <div className="flex-1 min-w-0">
                <div className="text-[13.5px] font-medium">{n}</div>
                <div className="text-[12px] text-muted-fg">{d}</div>
              </div>
              <span className={"badge " + (s==="Customised" ? "badge-primary" : "badge-muted")}>{s}</span>
              <button className="btn btn-ghost btn-sm">Edit</button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function SettingsHub() {
  const [section, setSection] = useState("general");
  return (
    <div>
      <div className="mb-5">
        <div className="eyebrow">Settings</div>
        <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">Azure Fest</h1>
      </div>
      <div className="grid grid-cols-12 gap-8">
        <div className="col-span-12 lg:col-span-3">
          <SettingsNav section={section} setSection={setSection}/>
        </div>
        <div className="col-span-12 lg:col-span-9">
          {section==="general" && <GeneralSettings/>}
          {section==="registration" && <RegistrationSettings/>}
          {section==="email" && <EmailSettings/>}
          {section==="access" && <AccessSettings/>}
          {section==="danger" && <DangerZone/>}
        </div>
      </div>
    </div>
  );
}

function AccessSettings() {
  return (
    <div>
      <div className="flex items-start justify-between mb-5">
        <div>
          <h2 className="font-display text-[22px] font-semibold">Access</h2>
          <p className="text-[13.5px] text-muted-fg">Coupons and closed recipient lists.</p>
        </div>
        <button className="btn btn-primary btn-sm"><Icons.Plus size={14}/>New coupon</button>
      </div>
      <div className="card p-10 text-center bg-grid">
        <div className="mx-auto w-12 h-12 rounded-xl bg-card border border-default grid place-items-center mb-3">
          <Icons.Ticket size={20} className="text-muted-fg"/>
        </div>
        <p className="font-display text-[18px] font-medium">No coupons yet</p>
        <p className="text-[13px] text-muted-fg mt-1 max-w-sm mx-auto">Create a coupon to grant access to invite-only ticket types like <span className="font-mono">speaker</span> or <span className="font-mono">sponsor</span>.</p>
      </div>
    </div>
  );
}

function DangerZone() {
  return (
    <div>
      <div className="mb-5">
        <h2 className="font-display text-[22px] font-semibold">Danger zone</h2>
        <p className="text-[13.5px] text-muted-fg">Permanent actions. No undo.</p>
      </div>
      <div className="card border-default divide-y-border" style={{borderColor: "color-mix(in oklch, var(--destructive) 30%, var(--border))"}}>
        {[
          ["Cancel event","Notify all registrants and stop accepting new registrations.","Cancel event"],
          ["Archive event","Hide from the dashboard and make read-only. Can be restored.","Archive"],
        ].map(([t,d,a]) => (
          <div key={t} className="flex items-center gap-4 p-5">
            <div className="flex-1 min-w-0">
              <div className="text-[14px] font-medium">{t}</div>
              <div className="text-[12.5px] text-muted-fg mt-0.5">{d}</div>
            </div>
            <button className="btn btn-outline btn-sm text-destructive" style={{borderColor:"color-mix(in oklch, var(--destructive) 35%, var(--border))"}}>{a}</button>
          </div>
        ))}
      </div>
    </div>
  );
}

window.SettingsHub = SettingsHub;
