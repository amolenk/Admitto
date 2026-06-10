<!doctype html>
<html lang="${(locale.currentLanguageTag)!'en'}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>${realm.displayName!'Admitto'}</title>
    <link rel="stylesheet" href="${url.resourcesPath}/css/login.css">
</head>
<body class="admitto-login-page">
<main class="admitto-login-shell">
    <header class="admitto-wordmark" aria-label="Admitto">
        <div class="wordmark-ticket">
            <svg
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.2"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true"
            >
                <path d="M4 8v8M8 6v12M12 6v12M16 6v12M20 8v8"></path>
            </svg>
        </div>
        <span>Admitto</span>
    </header>

    <section class="admitto-login-card admitto-logout-card" aria-labelledby="admitto-logout-title">
        <div class="admitto-login-heading">
            <h1 id="admitto-logout-title">${msg("logoutConfirmTitle")}</h1>
            <p>${msg("logoutConfirmHeader")}</p>
        </div>

        <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="admitto-alert admitto-alert-${message.type}" role="alert">
                <span>${kcSanitize(message.summary)?no_esc}</span>
            </div>
        </#if>

        <form class="admitto-form" action="${url.logoutConfirmAction}" method="post">
            <#if logoutConfirm?? && logoutConfirm.code??>
                <input type="hidden" name="session_code" value="${logoutConfirm.code}">
            </#if>

            <div class="admitto-actions">
                <button class="admitto-button" name="confirmLogout" id="kc-logout" type="submit">
                    ${msg("doLogout")}
                </button>

                <#if client?? && client.baseUrl?has_content>
                    <a class="admitto-button admitto-button-secondary" href="${client.baseUrl}">
                        ${msg("backToApplication")}
                    </a>
                </#if>
            </div>
        </form>
    </section>
</main>
</body>
</html>
