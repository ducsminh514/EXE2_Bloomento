window.initGoogleSignIn = (dotNetHelper, clientId) => {
    try {
        if (!window.google) {
            console.error("Google SDK not loaded yet.");
            return;
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                // Pass the credential (JWT) back to Blazor
                dotNetHelper.invokeMethodAsync('HandleGoogleCredential', response.credential);
            },
            auto_select: false,
            cancel_on_tap_outside: true
        });

        // Trigger the prompt or link to a button
        // For now, we manually trigger the popup when the user clicks our custom button
        google.accounts.id.prompt();
    } catch (e) {
        console.error("Error initializing Google Sign-In:", e);
    }
};

window.triggerGoogleSignIn = () => {
    try {
        google.accounts.id.prompt();
    } catch (e) {
        console.error("Error triggering Google prompt:", e);
    }
};
