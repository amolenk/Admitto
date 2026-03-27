# 3. Context and scope

## 3.1 Business context

```mermaid
flowchart LR
  Admin["Admin"]
  TeamMember["Team member"]
  EventSite["External event site"]
  Attendee["Attendee"]

  Admin --> Admitto["Admitto"]
  TeamMember --> Admitto
  EventSite <--> Admitto
  Attendee --> EventSite
  Admitto -.->|email| Attendee
```

| Neighbor | Direction | Exchanged value / data |
| :------- | :-------- | :--------------------- |
| Admin | Inbound | Creates and manages teams, events, and attendee operations |
| Team member | Inbound | Manages events and attendee operations within their team scope |
| External event site | Bidirectional | Calls Admitto API to query ticket types, register attendees, etc. Owns the attendee-facing UX |
| Attendee | Outbound only | Admitto may send emails (e.g. confirmation) directly to attendees. Attendees never interact with Admitto directly — they use external event sites |

## 3.2 Technical context

```mermaid
flowchart LR
  Admin["Admin"] -->|HTTPS| AdminUI["Admin UI"]
  TeamMember["Team member"] -->|HTTPS| AdminUI
  EventSite["External event site"] <-->|HTTPS| PublicAPI["Public API"]

  subgraph Admitto
    AdminUI
    PublicAPI
  end

  Admitto -->|SMTP| Attendee["Attendee"]
```

| Interface | Consumers | Protocol / format |
| :-------- | :-------- | :---------------- |
| Admin UI | Admins, team members | HTTPS |
| Public API | External event sites | HTTPS / JSON |
| E-mail | Attendees | SMTP |

