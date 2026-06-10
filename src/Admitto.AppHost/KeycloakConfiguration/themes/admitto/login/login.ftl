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

    <section class="admitto-login-card" aria-labelledby="admitto-login-title">
        <div class="admitto-login-heading">
            <h1 id="admitto-login-title">Sign in</h1>
        </div>

        <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="admitto-alert admitto-alert-${message.type}" role="alert">
                <span>${kcSanitize(message.summary)?no_esc}</span>
            </div>
        </#if>

        <form id="kc-form-login" class="admitto-form" action="${url.loginAction}" method="post">
            <#if usernameHidden?? && usernameHidden>
                <input type="hidden" id="username" name="username" value="${login.username!''}">
            <#else>
                <div class="admitto-field">
                    <label for="username">
                        <#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if>
                    </label>
                    <input
                        id="username"
                        name="username"
                        type="text"
                        value="${(login.username!'')}"
                        autocomplete="username"
                        autofocus
                        aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                    >
                    <#if messagesPerField.existsError('username','password')>
                        <p class="admitto-field-error">${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}</p>
                    </#if>
                </div>
            </#if>

            <div class="admitto-field">
                <label for="password">${msg("password")}</label>
                <input
                    id="password"
                    name="password"
                    type="password"
                    autocomplete="current-password"
                    aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                >
            </div>

            <#if (realm.rememberMe && !(usernameHidden??)) || realm.resetPasswordAllowed>
                <div class="admitto-form-options">
                    <#if realm.rememberMe && !(usernameHidden??)>
                    <label class="admitto-checkbox" for="rememberMe">
                        <input
                            id="rememberMe"
                            name="rememberMe"
                            type="checkbox"
                            <#if login.rememberMe??>checked</#if>
                        >
                        <span>${msg("rememberMe")}</span>
                    </label>
                    </#if>

                    <#if realm.resetPasswordAllowed>
                    <a href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a>
                    </#if>
                </div>
            </#if>

            <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>>
            <button class="admitto-button" name="login" id="kc-login" type="submit">${msg("doLogIn")}</button>
        </form>

    </section>
</main>
</body>
</html>
