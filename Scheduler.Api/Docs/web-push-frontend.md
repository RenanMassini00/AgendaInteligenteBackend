# Web Push no frontend

O backend já expõe a chave pública e salva a assinatura do navegador. No site, use HTTPS em produção ou `localhost` em desenvolvimento.

## Service worker

Crie um arquivo público, por exemplo `/push-sw.js`:

```js
self.addEventListener("push", (event) => {
  const data = event.data ? event.data.json() : {};

  event.waitUntil(
    self.registration.showNotification(data.title || "Notificação", {
      body: data.body || "",
      icon: data.icon || undefined,
      badge: data.badge || undefined,
      tag: data.tag || undefined,
      data: {
        url: data.url || "/",
        appointmentId: data.appointmentId || null
      }
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const url = event.notification.data?.url || "/";

  event.waitUntil(
    clients.matchAll({ type: "window", includeUncontrolled: true }).then((windows) => {
      const current = windows.find((client) => "focus" in client);

      if (current) {
        current.navigate(url);
        return current.focus();
      }

      return clients.openWindow(url);
    })
  );
});
```

## Registrar o dispositivo

Chame isso depois que o usuário fizer login e aceitar notificações:

```js
function urlBase64ToUint8Array(value) {
  const padding = "=".repeat((4 - (value.length % 4)) % 4);
  const base64 = (value + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  return Uint8Array.from([...raw].map((char) => char.charCodeAt(0)));
}

export async function enablePushNotifications(apiBaseUrl, userId) {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
    return false;
  }

  const permission = await Notification.requestPermission();
  if (permission !== "granted") {
    return false;
  }

  const keyResponse = await fetch(`${apiBaseUrl}/api/push/public-key`);
  const keyData = await keyResponse.json();

  if (!keyData.enabled || !keyData.publicKey) {
    return false;
  }

  const registration = await navigator.serviceWorker.register("/push-sw.js");
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(keyData.publicKey)
  });

  const subscriptionJson = subscription.toJSON();

  await fetch(`${apiBaseUrl}/api/push/subscriptions?userId=${userId}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      endpoint: subscription.endpoint,
      expirationTime: subscription.expirationTime,
      keys: subscriptionJson.keys,
      userAgent: navigator.userAgent,
      deviceName: navigator.platform
    })
  });

  return true;
}
```

## Testar

Depois de registrar, chame:

```http
POST /api/push/test?userId=1
Content-Type: application/json

{
  "title": "Teste de notificação",
  "body": "Seu app já recebe push.",
  "url": "/appointments"
}
```
