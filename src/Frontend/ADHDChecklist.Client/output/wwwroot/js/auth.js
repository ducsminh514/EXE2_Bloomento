window.initGoogleSignIn = (dotNetHelper, clientId) => {
    console.log("initGoogleSignIn called with clientId:", clientId);

    if (!clientId) {
        console.error("clientId is null or empty!");
        return;
    }

    const init = () => {
        try {
            if (!window.google || !window.google.accounts || !window.google.accounts.id) {
                console.log("Google SDK not ready, retrying in 100ms...");
                setTimeout(init, 100);
                return;
            }

            console.log("Google SDK ready, initializing...");
            google.accounts.id.initialize({
                client_id: clientId,
                callback: (response) => {
                    console.log("Google Credential received!");
                    dotNetHelper.invokeMethodAsync('HandleGoogleCredential', response.credential);
                },
                auto_select: false,
                cancel_on_tap_outside: true
            });

            google.accounts.id.prompt((notification) => {
                console.log("Google Prompt Status:", notification);
                if (notification.isNotDisplayed()) {
                    console.warn("Prompt not displayed. Reason:", notification.getNotDisplayedReason());
                }
                if (notification.isSkippedMoment()) {
                    console.warn("Prompt skipped. Reason:", notification.getSkippedReason());
                }
                if (notification.isDismissedMoment()) {
                    console.warn("Prompt dismissed. Reason:", notification.getDismissedReason());
                }
            });

            // Always render the official button as a fallback/manual option
            const btnContainer = document.getElementById("google-button-container");
            if (btnContainer) {
                console.log("Rendering Google Sign-In button...");
                google.accounts.id.renderButton(btnContainer, {
                    theme: "outline",
                    size: "large",
                    shape: "rectangular",
                    width: btnContainer.offsetWidth || 350,
                    text: "signin_with",
                    locale: "vi"
                });
            }
            console.log("Google initialization complete.");
        } catch (e) {
            console.error("Error initializing Google Sign-In:", e);
        }
    };

    init();
};

window.triggerGoogleSignIn = () => {
    try {
        google.accounts.id.prompt();
    } catch (e) {
        console.error("Error triggering Google prompt:", e);
    }
};
