# Web Push no frontend

O backend já envia o Web Push para o usuário profissional quando um novo agendamento é criado. Para o aviso chegar no celular, o frontend precisa registrar cada celular/navegador do funcionário após o login.

## Requisitos

- Produção obrigatoriamente em HTTPS.
- Android: Chrome ou outro navegador compatível com Web Push.
- iPhone/iPad: iOS/iPadOS 16.4 ou superior, instalação pela opção **Adicionar à Tela de Início** do Safari e permissão concedida dentro do PWA. O site aberto apenas em uma aba do Safari não recebe push de forma confiável.
- O funcionário deve ativar as notificações por meio de um botão ou outra ação do usuário; navegadores bloqueiam a solicitação automática no carregamento da página.
- O arquivo do Service Worker deve estar na raiz pública do frontend para controlar toda a aplicação.

O backend já expõe a chave pública e salva a assinatura do navegador. No site, use HTTPS em produção ou `localhost` em desenvolvimento.

## Service worker

Crie um arquivo público, por exemplo `/push-sw.js`:

```js
self.addEventListener("push", (event) => {
  event.waitUntil(
    (async () => {
      let data = {};
      try {
        data = event.data ? event.data.json() : {};
      } catch {
        data = { title: "Notificação", body: event.data ? event.data.text() : "" };
      }

      await self.registration.showNotification(data.title || "Notificação", {
        body: data.body || "",
        icon: data.icon || undefined,
        badge: data.badge || undefined,
        tag: data.tag || undefined,
        renotify: true,
        vibrate: [200, 100, 200],
        data: {
          url: data.url || "/",
          appointmentId: data.appointmentId || null
        }
      });
    })()
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const url = new URL(event.notification.data?.url || "/", self.location.origin).href;

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

Associe `enablePushNotifications` ao botão **Ativar notificações no celular** exibido após o login do funcionário. Execute a função novamente quando o funcionário entrar em outro celular; a API identifica a assinatura pelo endpoint e mantém os dispositivos ativos separadamente.

Exemplo de uso no React:

```jsx
<button
  type="button"
  onClick={() => enablePushNotifications(API_URL, loggedUser.id)}
>
  Ativar notificações no celular
</button>
```

O frontend também deve disponibilizar a remoção da assinatura no logout ou em uma tela de dispositivos:

```js
await fetch(`${apiBaseUrl}/api/push/subscriptions?userId=${userId}`, {
  method: "DELETE",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ endpoint: subscription.endpoint })
});
```

## Diagnóstico da assinatura

Depois de ativar as notificações, confirme no frontend que o `userId` usado no cadastro é exatamente o ID do usuário profissional autenticado. Não use `clientId`, `professionalUserId` ou o ID da empresa no lugar dele.

Consulte o status antes de enviar o teste:

```http
GET /api/push/subscriptions/status?userId=1
```

Uma resposta válida para o funcionário deve conter:

```json
{
  "userId": 1,
  "userExists": true,
  "webPushConfigured": true,
  "activeSubscriptions": 1
}
```

Se `activeSubscriptions` for `0`, a permissão do navegador foi concedida, mas o `POST /api/push/subscriptions?userId=1` não foi salvo ou foi salvo para outro usuário. O frontend deve verificar `response.ok` no POST e exibir o `userId` usado.

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

Se a API retornar sucesso, isso significa que o provedor aceitou a mensagem; não significa que o sistema operacional já a exibiu. Confira também:

1. `GET /api/push/subscriptions/status?userId=ID` e confirme `activeSubscriptions` maior que zero.
2. Confirme `lastSuccessAt` após o teste.
3. No celular, abra o endereço do app, verifique a permissão de notificações e atualize o Service Worker. Se necessário, desinstale e instale novamente a PWA.
4. No Chrome Android, verifique **Configurações > Notificações > Notificações de sites** e permita o domínio.
5. No iPhone, abra a PWA adicionada à Tela de Início; o Safari em uma aba comum não exibe Web Push de forma confiável.

O retorno do teste agora informa `providerAccepted`, `lastSuccessAt` e `lastFailureAt`.
