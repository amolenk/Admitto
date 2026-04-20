// Tiny icon set as inline SVG components — lucide-style, 16px default stroke
const Icon = ({ path, size = 16, className = "", stroke = 1.6, fill = "none" }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill={fill} stroke="currentColor"
       strokeWidth={stroke} strokeLinecap="round" strokeLinejoin="round" className={className}>
    {path}
  </svg>
);

const Icons = {
  Home:       (p) => <Icon {...p} path={<><path d="M3 10.5 12 3l9 7.5"/><path d="M5 9.5V21h14V9.5"/></>} />,
  Calendar:   (p) => <Icon {...p} path={<><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M8 3v4M16 3v4M3 10h18"/></>} />,
  Ticket:     (p) => <Icon {...p} path={<><path d="M3 10V8a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4Z"/><path d="M9 8v8"/></>} />,
  Users:      (p) => <Icon {...p} path={<><circle cx="9" cy="8" r="3.2"/><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6"/><path d="M16 5.5a3 3 0 1 1 0 5"/><path d="M20.5 20c0-2.5-1.5-4.5-4-5.5"/></>} />,
  Settings:   (p) => <Icon {...p} path={<><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3h0a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9v0a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z"/></>} />,
  Mail:       (p) => <Icon {...p} path={<><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/></>} />,
  Chevron:    (p) => <Icon {...p} path={<path d="m6 9 6 6 6-6"/>} />,
  ChevUp:     (p) => <Icon {...p} path={<path d="m18 15-6-6-6 6"/>} />,
  ChevRight:  (p) => <Icon {...p} path={<path d="m9 6 6 6-6 6"/>} />,
  Plus:       (p) => <Icon {...p} path={<><path d="M12 5v14M5 12h14"/></>} />,
  Search:     (p) => <Icon {...p} path={<><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></>} />,
  Dots:       (p) => <Icon {...p} path={<><circle cx="5" cy="12" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="19" cy="12" r="1.3"/></>} />,
  Download:   (p) => <Icon {...p} path={<><path d="M12 3v12"/><path d="m7 10 5 5 5-5"/><path d="M4 21h16"/></>} />,
  Upload:     (p) => <Icon {...p} path={<><path d="M12 21V9"/><path d="m7 14 5-5 5 5"/><path d="M4 3h16"/></>} />,
  Check:      (p) => <Icon {...p} path={<path d="m5 12 5 5L20 7"/>} />,
  CheckCircle:(p) => <Icon {...p} path={<><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></>} />,
  Clock:      (p) => <Icon {...p} path={<><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>} />,
  Pin:        (p) => <Icon {...p} path={<><path d="M12 22s7-7 7-12a7 7 0 1 0-14 0c0 5 7 12 7 12Z"/><circle cx="12" cy="10" r="2.5"/></>} />,
  Link:       (p) => <Icon {...p} path={<><path d="M10 14a4 4 0 0 0 5.66 0l3-3a4 4 0 0 0-5.66-5.66l-1 1"/><path d="M14 10a4 4 0 0 0-5.66 0l-3 3a4 4 0 0 0 5.66 5.66l1-1"/></>} />,
  Globe:      (p) => <Icon {...p} path={<><circle cx="12" cy="12" r="9"/><path d="M3 12h18"/><path d="M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/></>} />,
  Sparkles:   (p) => <Icon {...p} path={<><path d="m12 3 2 5 5 2-5 2-2 5-2-5-5-2 5-2z"/></>} />,
  Eye:        (p) => <Icon {...p} path={<><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></>} />,
  EyeOff:     (p) => <Icon {...p} path={<><path d="M3 3l18 18"/><path d="M10.6 6.2A10 10 0 0 1 22 12s-1.2 2.2-3.3 4.2"/><path d="M6 7.5C3.5 9.4 2 12 2 12s4 7 10 7a10 10 0 0 0 5-1.3"/><path d="M14.1 14.1a3 3 0 0 1-4.2-4.2"/></>} />,
  Copy:       (p) => <Icon {...p} path={<><rect x="9" y="9" width="11" height="11" rx="2"/><path d="M5 15V5a2 2 0 0 1 2-2h10"/></>} />,
  Edit:       (p) => <Icon {...p} path={<><path d="M4 20h4L20 8l-4-4L4 16z"/></>} />,
  Trash:      (p) => <Icon {...p} path={<><path d="M3 6h18"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/><path d="M6 6l1 14a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-14"/></>} />,
  Zap:        (p) => <Icon {...p} path={<path d="M13 2 3 14h7l-1 8 10-12h-7z"/>} />,
  ArrowUp:    (p) => <Icon {...p} path={<><path d="M12 19V5"/><path d="m5 12 7-7 7 7"/></>} />,
  ArrowDown:  (p) => <Icon {...p} path={<><path d="M12 5v14"/><path d="m5 12 7 7 7-7"/></>} />,
  Moon:       (p) => <Icon {...p} path={<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z"/>} />,
  Sun:        (p) => <Icon {...p} path={<><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></>} />,
  Palette:    (p) => <Icon {...p} path={<><path d="M12 3a9 9 0 1 0 0 18c1 0 1.5-.8 1.5-1.5S13 18 13 17.2s.8-1.5 1.7-1.5H17a4 4 0 0 0 4-4 9 9 0 0 0-9-9Z"/><circle cx="7" cy="11" r="1"/><circle cx="9.5" cy="7" r="1"/><circle cx="15" cy="7" r="1"/><circle cx="17" cy="11" r="1"/></>} />,
  LogOut:     (p) => <Icon {...p} path={<><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5"/><path d="M21 12H9"/></>} />,
  Qr:         (p) => <Icon {...p} path={<><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><path d="M14 14h3v3h-3zM20 14v3M14 20h3M20 20v1"/></>} />,
  Filter:     (p) => <Icon {...p} path={<path d="M4 5h16l-6 8v6l-4-2v-4z"/>} />,
};

window.Icons = Icons;
