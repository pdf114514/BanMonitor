// https://github.com/dotnet-presentations/blazor-workshop/blob/main/src/BlazingPizza.ComponentsLibrary/wwwroot/pushNotifications.js

(function () {
    // Note: Replace with your own key pair before deploying
    const applicationServerPublicKey = 'BJwAkITyhY5ssKl6SMRnPRxY_khXZmmw2JId4Z081Y0Xme8ZGjbf9OiZeaO3E8SHcip6POp6hZ9yxNuWTg1v8Tk';

    window.blazorPushNotifications = {
        isSupported: async () => {
            if ("Notification" in window)
                return true;
            return false;
        },
        askPermission: async () => {
            return new Promise((resolve, reject) => {
                Notification.requestPermission((permission) => {
                    resolve(permission);
                });
            });
        },
        requestSubscription: async () => {
            const worker = await navigator.serviceWorker.getRegistration();
            const existingSubscription = await worker.pushManager.getSubscription();
            if (!existingSubscription) {
                const newSubscription = await subscribe(worker);
                if (newSubscription) {
                    return {
                        url: newSubscription.endpoint,
                        p256dh: arrayBufferToBase64(newSubscription.getKey('p256dh')),
                        auth: arrayBufferToBase64(newSubscription.getKey('auth'))
                    };
                }
            }
        }
    };

    async function subscribe(worker) {
        try {
            return await worker.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerPublicKey
            });
        } catch (error) {
            if (error.name === 'NotAllowedError') {
                return null;
            }
            throw error;
        }
    }

    function arrayBufferToBase64(buffer) {
        // https://stackoverflow.com/a/9458996
        var binary = '';
        var bytes = new Uint8Array(buffer);
        var len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }
})();