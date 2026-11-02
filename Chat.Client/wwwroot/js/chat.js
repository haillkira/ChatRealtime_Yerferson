"use strict";

const userForm = document.getElementById("userForm");
const chatForm = document.getElementById("chatForm");
const userInput = document.getElementById("userInput");
const joinButton = document.getElementById("joinButton");
const currentUser = document.getElementById("currentUser");
const messageInput = document.getElementById("messageInput");
const sendButton = document.getElementById("sendButton");
const connectionStatus = document.getElementById("connectionStatus");
const errorMessage = document.getElementById("errorMessage");
const messagesList = document.getElementById("messagesList");

const userColors = [
    "#b91c1c",
    "#7e22ce",
    "#1d4ed8",
    "#15803d",
    "#c2410c",
    "#0e7490",
    "#be185d",
    "#4338ca"
];

const displayedMessages = new Set();

let activeUser = "";
let joined = false;
let joining = false;
let sending = false;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7101/chatHub")
    .withAutomaticReconnect()
    .build();

function getUserColor(user) {
    let hash = 0;

    for (const character of user.trim().toLowerCase()) {
        hash = (hash * 31 + character.codePointAt(0)) >>> 0;
    }

    return userColors[hash % userColors.length];
}

function updateControls() {
    const connected =
        connection.state === signalR.HubConnectionState.Connected;

    joinButton.disabled =
        !connected || joining || activeUser !== "";

    userInput.disabled = joining || activeUser !== "";

    messageInput.disabled =
        !connected || !joined || joining;

    sendButton.disabled =
        !connected || !joined || joining || sending;
}

function showCurrentUser() {
    const userName = document.createElement("span");
    userName.textContent = activeUser;
    userName.style.color = getUserColor(activeUser);
    userName.style.fontWeight = "normal";

    currentUser.style.fontWeight = "normal";
    currentUser.replaceChildren("Participando como: ", userName);
}

function showMessage(message) {
    if (displayedMessages.has(message.id))
        return;

    displayedMessages.add(message.id);

    const item = document.createElement("li");
    item.className = "list-group-item";
    item.dataset.messageId = message.id;
    item.style.whiteSpace = "pre-wrap";
    item.style.overflowWrap = "anywhere";

    const date = new Date(message.sentAt);
    const time = date.toLocaleString("es-DO");

    const userName = document.createElement("span");
    userName.textContent = message.user;
    userName.style.color = getUserColor(message.user);
    userName.style.fontWeight = "normal";

    item.append(
        `[${time}] `,
        userName,
        `: ${message.message}`
    );

    const nextItem = Array.from(messagesList.children)
        .find(element =>
            Number(element.dataset.messageId) > message.id);

    messagesList.insertBefore(item, nextItem ?? null);
}

async function loadHistory() {
    const history = await connection.invoke("GetHistory");

    for (const message of history)
        showMessage(message);

    messagesList.lastElementChild?.scrollIntoView({
        block: "nearest"
    });
}

async function registerUser(name) {
    joining = true;
    updateControls();

    try {
        const registeredName =
            await connection.invoke("JoinChat", name);

        activeUser = registeredName;
        userInput.value = registeredName;
        joined = true;

        showCurrentUser();
        errorMessage.textContent = "";

        try {
            await loadHistory();
        } catch (error) {
            errorMessage.textContent =
                "Entraste al chat, pero no se pudo cargar el historial.";

            console.error(error);
        }
    } catch (error) {
        joined = false;
        activeUser = "";
        currentUser.textContent = "";

        errorMessage.textContent =
            error.message || "No se pudo reservar el nombre.";

        console.error(error);
    } finally {
        joining = false;
        updateControls();

        if (joined)
            messageInput.focus();
        else
            userInput.focus();
    }
}

userForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (joining || activeUser !== "")
        return;

    const name = userInput.value
        .trim()
        .normalize("NFKC");

    if (!name || name.length > 40) {
        errorMessage.textContent =
            "Escribe un nombre de hasta 40 caracteres.";
        return;
    }

    if (connection.state !== signalR.HubConnectionState.Connected) {
        errorMessage.textContent =
            "Espera a que conecte el chat.";
        return;
    }

    await registerUser(name);
});

connection.on("ReceiveMessage", (message) => {
    if (!joined)
        return;

    showMessage(message);

    messagesList.lastElementChild?.scrollIntoView({
        block: "nearest"
    });
});

connection.onreconnecting(() => {
    joined = false;
    connectionStatus.textContent = "Reconectando...";
    updateControls();
});

connection.onreconnected(async () => {
    connectionStatus.textContent = "Conectado";
    errorMessage.textContent = "";

    if (activeUser)
        await registerUser(activeUser);
    else
        updateControls();
});

connection.onclose(() => {
    joined = false;
    connectionStatus.textContent = "Desconectado";
    updateControls();

    setTimeout(startConnection, 5000);
});

async function startConnection() {
    if (connection.state !== signalR.HubConnectionState.Disconnected)
        return;

    try {
        connectionStatus.textContent = "Conectando...";

        await connection.start();

        connectionStatus.textContent = "Conectado";
        errorMessage.textContent = "";

        if (activeUser)
            await registerUser(activeUser);
        else
            updateControls();
    } catch (error) {
        joined = false;

        connectionStatus.textContent =
            "Sin conexion. Reintentando en 5 segundos...";

        console.error(error);
        updateControls();

        setTimeout(startConnection, 5000);
    }
}

chatForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (sending || joining)
        return;

    errorMessage.textContent = "";

    if (!joined) {
        errorMessage.textContent =
            "Primero entra al chat.";
        return;
    }

    const message = messageInput.value.trim();

    if (!message || message.length > 1000) {
        errorMessage.textContent =
            "Escribe un mensaje de hasta 1000 caracteres.";
        return;
    }

    if (connection.state !== signalR.HubConnectionState.Connected) {
        errorMessage.textContent =
            "No hay conexion con el servidor.";
        return;
    }

    sending = true;
    updateControls();

    try {
        await connection.invoke("SendMessage", message);

        messageInput.value = "";
        messageInput.focus();
    } catch (error) {
        errorMessage.textContent =
            "No se pudo confirmar el envio. Revisa los mensajes antes de reintentar.";

        console.error(error);
    } finally {
        sending = false;
        updateControls();
    }
});

updateControls();
startConnection();