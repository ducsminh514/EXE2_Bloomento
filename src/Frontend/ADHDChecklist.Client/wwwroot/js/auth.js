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
                    // If suppressed, we might need a manual button
                }
                if (notification.isSkippedMoment()) {
                    console.warn("Prompt skipped. Reason:", notification.getSkippedReason());
                }
                if (notification.isDismissedMoment()) {
                    console.warn("Prompt dismissed. Reason:", notification.getDismissedReason());
                }
            });
            console.log("Google prompt triggered.");
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
