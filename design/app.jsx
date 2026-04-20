// Main app
function App() {
  const [theme, setTheme] = useState(() => localStorage.getItem("admitto.theme") || "editorial");
  const [dark, setDark] = useState(() => localStorage.getItem("admitto.dark") === "1");
  const [density, setDensity] = useState(() => localStorage.getItem("admitto.density") || "comfortable");
  const [page, setPage] = useState(() => localStorage.getItem("admitto.page") || "dashboard");
  const [tweaksOpen, setTweaksOpen] = useState(false);

  React.useEffect(() => {
    document.body.className = `theme-${theme} density-${density}` + (dark ? " dark" : "");
    localStorage.setItem("admitto.theme", theme);
    localStorage.setItem("admitto.dark", dark ? "1" : "0");
    localStorage.setItem("admitto.density", density);
    localStorage.setItem("admitto.page", page);
  }, [theme, dark, density, page]);

  return (
    <div className="flex h-screen w-screen overflow-hidden">
      <SidebarNav page={page} setPage={setPage}/>
      <div className="flex-1 flex flex-col min-w-0">
        <AppHeader page={page} showDark={dark} setShowDark={setDark}/>
        <main className="flex-1 overflow-auto">
          <div className="max-w-[1360px] mx-auto px-6 lg:px-8 py-7">
            {page==="dashboard" && <Dashboard/>}
            {page==="tickets" && <TicketsPage/>}
            {page==="settings" && <SettingsHub/>}
            {page==="registrations" && <Dashboard/>}
          </div>
        </main>
      </div>
      <TweaksToggle open={tweaksOpen} setOpen={setTweaksOpen}/>
      <TweaksPanel theme={theme} setTheme={setTheme} dark={dark} setDark={setDark}
                   density={density} setDensity={setDensity}
                   page={page} setPage={setPage}
                   open={tweaksOpen} setOpen={setTweaksOpen}/>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
