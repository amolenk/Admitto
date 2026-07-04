// Mock data for Azure Fest
const AZURE_FEST = {
  team: { name: "Azure Fest NL", slug: "azurefest-nl" },
  event: {
    name: "Azure Fest",
    slug: "azurefest-2026",
    tagline: "The Dutch Azure community day",
    date: "Wednesday, September 23, 2026",
    dateShort: "Sep 23",
    time: "09:00 – 18:00 CEST",
    location: "Pathé Ede",
    locationCity: "Ede, Netherlands",
    url: "azurefest.nl",
    capacity: 250,
    registered: 187,
    confirmed: 162,
    waitlist: 0,
    checkedIn: 0,
    daysUntil: 157,
    status: "Registration open",
  },
  ticketTypes: [
    { name: "Early bird", slug: "early-bird", price: 0, sold: 120, cap: 120, status: "Sold out", closed: true },
    { name: "Regular", slug: "regular", price: 0, sold: 58, cap: 100, status: "On sale", closed: false },
    { name: "Student", slug: "student", price: 0, sold: 9, cap: 20, status: "On sale", closed: false },
    { name: "Speaker", slug: "speaker", price: 0, sold: 0, cap: 10, status: "Invite only", closed: false, hidden: true },
  ],
  // 14-day trend: new registrations/day
  trend: [2, 5, 3, 7, 4, 9, 12, 8, 6, 11, 14, 9, 7, 10],
  activity: [
    { t: "2m ago",  who: "Sanne de Vries",     what: "registered",            ticket: "Regular" },
    { t: "18m ago", who: "Mark Janssen",       what: "registered",            ticket: "Regular" },
    { t: "41m ago", who: "Priya Shah",         what: "confirmed email",       ticket: "Student" },
    { t: "1h ago",  who: "Lukas Bauer",        what: "changed ticket type",   ticket: "Regular" },
    { t: "3h ago",  who: "Emma Visser",        what: "registered",            ticket: "Regular" },
    { t: "5h ago",  who: "Niels van den Berg", what: "cancelled",             ticket: "Early bird" },
  ],
};

// A small set of team events for the sidebar
const TEAM_EVENTS = [
  { name: "Azure Fest",            slug: "azurefest-2026",   active: true },
  { name: "dotNext Amsterdam",     slug: "dotnext-ams-2026", active: false },
  { name: "CloudNative Workshop",  slug: "cn-workshop-03",   active: false },
];

window.AZURE_FEST = AZURE_FEST;
window.TEAM_EVENTS = TEAM_EVENTS;
