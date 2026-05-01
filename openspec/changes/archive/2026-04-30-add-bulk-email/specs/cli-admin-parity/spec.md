## ADDED Requirements

### Requirement: CLI exposes bulk-email management

The Admitto CLI SHALL expose commands under `admitto event bulk-email` to preview recipients, start a bulk send, list past jobs, show job detail, and cancel an in-flight job. Each command SHALL invoke the corresponding admin endpoint via the regenerated `ApiClient` and SHALL contain no business logic beyond input mapping, slug resolution, and output formatting.

#### Scenario: Preview recipients from a criteria source
- **WHEN** an operator runs `admitto event bulk-email preview -t <team> -e <event> --ticket-types workshop-a,workshop-b --status registered`
- **THEN** the CLI SHALL call `POST /admin/teams/{team}/events/{event}/bulk-emails/preview` with the criteria source
- **AND** SHALL print the matched count and a sample of recipient addresses

#### Scenario: Start a bulk send using a saved template
- **WHEN** an operator runs `admitto event bulk-email start -t <team> -e <event> --type ticket --ticket-types workshop-a`
- **THEN** the CLI SHALL call `POST /admin/teams/{team}/events/{event}/bulk-emails` with `emailType=ticket` and the criteria source, and SHALL print the new job id

#### Scenario: Start a bulk send with ad-hoc content
- **WHEN** an operator runs `admitto event bulk-email start -t <team> -e <event> --type bulk-custom --subject "Schedule update" --text-body @body.txt --html-body @body.html --ticket-types workshop-a`
- **THEN** the CLI SHALL call the create endpoint with the ad-hoc subject/body fields populated

#### Scenario: Start a bulk send to an external list
- **WHEN** an operator runs `admitto event bulk-email start -t <team> -e <event> --type bulk-custom --subject "Invite" --text-body @body.txt --external-list @recipients.csv`
- **THEN** the CLI SHALL parse the CSV (one `email[,displayName]` per line) and call the create endpoint with an external-list source

#### Scenario: List bulk-email jobs for an event
- **WHEN** an operator runs `admitto event bulk-email list -t <team> -e <event>`
- **THEN** the CLI SHALL call `GET /admin/teams/{team}/events/{event}/bulk-emails` and print a table of jobs newest-first

#### Scenario: Show one job's audit detail
- **WHEN** an operator runs `admitto event bulk-email show -t <team> -e <event> --id <jobId>`
- **THEN** the CLI SHALL call `GET /admin/teams/{team}/events/{event}/bulk-emails/{jobId}` and print status, totals, source, trigger user, timestamps, and last error

#### Scenario: Cancel a pending job
- **WHEN** an operator runs `admitto event bulk-email cancel -t <team> -e <event> --id <jobId>`
- **THEN** the CLI SHALL call `POST /admin/teams/{team}/events/{event}/bulk-emails/{jobId}/cancel` and SHALL exit `0` on success or `1` if the job is no longer cancellable
