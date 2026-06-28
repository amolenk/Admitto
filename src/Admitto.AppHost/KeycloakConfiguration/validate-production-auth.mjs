import fs from "node:fs";
import path from "node:path";

const root = path.dirname(new URL(import.meta.url).pathname);
const realmPath = path.join(root, "AdmittoRealm.Deployment.json");
const localRealmPath = path.join(root, "AdmittoRealm.Local.json");
const emailThemePath = path.join(root, "themes", "admitto", "email");
const realm = JSON.parse(fs.readFileSync(realmPath, "utf8"));
const localRealm = JSON.parse(fs.readFileSync(localRealmPath, "utf8"));

const flows = new Map(realm.authenticationFlows.map(flow => [flow.alias, flow]));
const activeAuthenticators = new Set();
const visited = new Set();

function walkFlow(alias) {
  if (visited.has(alias)) return;
  visited.add(alias);

  const flow = flows.get(alias);
  if (!flow) throw new Error(`Missing authentication flow '${alias}'.`);

  for (const execution of flow.authenticationExecutions ?? []) {
    if (execution.requirement === "DISABLED") continue;

    if (execution.authenticator) activeAuthenticators.add(execution.authenticator);
    if (execution.flowAlias) walkFlow(execution.flowAlias);
  }
}

walkFlow(realm.browserFlow);

assert(realm.browserFlow === "admitto passkey browser", "Deployment realm must use the passkey browser flow.");
assert(realm.emailTheme === "admitto", "Deployment realm must use the Admitto email theme.");
assert(activeAuthenticators.has("webauthn-authenticator-passwordless"), "Active browser flow must require WebAuthn passwordless.");
assert(!activeAuthenticators.has("auth-username-form"), "Production browser flow must not require entering an email before passkey detection.");
assert(!activeAuthenticators.has("auth-username-password-form"), "Active browser flow must not include password form fallback.");
assert(!activeAuthenticators.has("auth-password-form"), "Active browser flow must not include password form fallback.");
assert(realm.webAuthnPolicyPasswordlessRequireResidentKey === "Yes", "Production passkeys must require resident keys for loginless sign-in.");
assert(realm.webAuthnPolicyPasswordlessUserVerificationRequirement === "required", "Production passkeys must require user verification.");

const adminUi = realm.clients.find(client => client.clientId === "admitto-ui");
assert(adminUi, "Deployment realm must include the admitto-ui client.");
assert(adminUi.directAccessGrantsEnabled === false, "admitto-ui must not allow password grant in production.");
assert(adminUi.redirectUris?.includes("${ADMITTO_UI_PUBLIC_URL}"), "admitto-ui must allow the Admin UI root redirect used after invite setup.");
assert(adminUi.redirectUris?.includes("${ADMITTO_UI_PUBLIC_URL}/api/auth/oauth2/callback/generic-oauth"), "admitto-ui must allow Better Auth's OAuth callback path.");

const accountConsole = realm.clients.find(client => client.clientId === "account-console");
const localAccountConsole = localRealm.clients.find(client => client.clientId === "account-console");
const account = realm.clients.find(client => client.clientId === "account");
const localAccount = localRealm.clients.find(client => client.clientId === "account");
assert(account?.enabled === true, "Deployment realm must keep the account client enabled for action-token flows.");
assert(localAccount?.enabled === true, "Local realm must keep the account client enabled for action-token flows.");
assert(accountConsole?.enabled === false, "Deployment realm must disable the Keycloak account console client.");
assert(localAccountConsole?.enabled === false, "Local realm must disable the Keycloak account console client.");

const smtp = realm.smtpServer;
assert(smtp, "Deployment realm must configure Keycloak SMTP settings.");
assertSmtpUsesEnvironmentSubstitution(smtp, "Deployment");

const localSmtp = localRealm.smtpServer;
assert(localSmtp, "Local realm must configure Keycloak SMTP settings.");
assertSmtpUsesEnvironmentSubstitution(localSmtp, "Local");

function assertSmtpUsesEnvironmentSubstitution(smtpServer, label) {
  assert(smtpServer.host === "${KEYCLOAK_SMTP_HOST}", `${label} Keycloak SMTP host must use environment substitution.`);
  assert(smtpServer.port === "${KEYCLOAK_SMTP_PORT}", `${label} Keycloak SMTP port must use environment substitution.`);
  assert(smtpServer.from === "${KEYCLOAK_SMTP_FROM}", `${label} Keycloak SMTP from address must use environment substitution.`);
  assert(
    smtpServer.fromDisplayName === "${KEYCLOAK_SMTP_FROM_DISPLAY_NAME}",
    `${label} Keycloak SMTP from display name must use environment substitution.`);
  assert(smtpServer.auth === "${KEYCLOAK_SMTP_AUTH}", `${label} Keycloak SMTP auth flag must use environment substitution.`);
  assert(smtpServer.user === "${KEYCLOAK_SMTP_USERNAME}", `${label} Keycloak SMTP user must use environment substitution.`);
  assert(smtpServer.password === "${KEYCLOAK_SMTP_PASSWORD}", `${label} Keycloak SMTP password must use environment substitution.`);
  assert(smtpServer.ssl === "${KEYCLOAK_SMTP_SSL}", `${label} Keycloak SMTP SSL flag must use environment substitution.`);
  assert(smtpServer.starttls === "${KEYCLOAK_SMTP_STARTTLS}", `${label} Keycloak SMTP STARTTLS flag must use environment substitution.`);
}

// Custom Keycloak login-page theming has been removed; production login pages use
// the stock keycloak.v2 theme until a Keycloakify-based theme is reintroduced. The
// custom email theme remains the branding surface for account-action emails.
const emailMessagesPath = path.join(emailThemePath, "messages", "messages_en.properties");
assert(fs.existsSync(path.join(emailThemePath, "theme.properties")), "Email theme properties missing.");
assert(fs.existsSync(emailMessagesPath), "Email theme English messages missing.");

const emailMessages = fs.readFileSync(emailMessagesPath, "utf8");
assert(emailMessages.includes("executeActionsSubject=You are invited to Admitto"), "Execute-actions email subject must be invitation-oriented.");
assert(emailMessages.includes("Create your Admitto passkey"), "Execute-actions email must mention passkey setup.");
assert(!emailMessages.includes("following action"), "Execute-actions email must not expose Keycloak action-oriented copy.");

const dockerfile = fs.readFileSync(path.join(root, "Dockerfile"), "utf8");
assert(dockerfile.includes("--spi-user-profile--declarative-user-profile--read-only-attributes=email"), "Keycloak image must configure email as a read-only user profile attribute.");

const localFlows = new Map(localRealm.authenticationFlows.map(flow => [flow.alias, flow]));
const localBrowser = localFlows.get(localRealm.browserFlow);
const localForms = localFlows.get("forms");
assert(localRealm.browserFlow === "admitto local browser", "Local realm must use the Admitto local browser flow.");
assert(localBrowser, "Local realm must include the browser flow.");
assert(hasAuthenticator(localBrowser, "auth-username-password-form"), "Local browser flow must show the standard username/password form.");
assert(hasAuthenticator(localBrowser, "webauthn-authenticator-passwordless"), "Local browser flow must offer passkey as an alternative.");
assert(localForms, "Local realm must include the forms flow.");
assert(hasAuthenticator(localForms, "auth-username-password-form"), "Local forms flow must show the standard username/password form.");
assert(localRealm.webAuthnPolicyPasswordlessRequireResidentKey === "Yes", "Local passkeys must require resident keys for loginless sign-in.");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function hasAuthenticator(flow, authenticator) {
  return (flow.authenticationExecutions ?? []).some(execution => execution.authenticator === authenticator);
}

function hasFlow(flow, alias) {
  return (flow.authenticationExecutions ?? []).some(execution => execution.flowAlias === alias);
}

console.log("Production Keycloak passkey realm and Admitto email theme validated.");
