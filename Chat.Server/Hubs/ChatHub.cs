using System.Text;
using Chat.Server.Services;
using Chat.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Server.Hubs;

public sealed class ChatHub(
    ChatUsersService users,
    MessagesService messages) : Hub
{
    public string JoinChat(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new HubException(
                "Escribe un nombre para entrar.");

        name = name.Trim().Normalize(NormalizationForm.FormKC);

        if (name.Length > 40)
            throw new HubException(
                "El nombre permite hasta 40 caracteres.");

        if (!users.TryRegister(Context.ConnectionId, name))
            throw new HubException(
                "Ese nombre ya esta en uso. Elige otro.");

        return users.GetName(Context.ConnectionId)!;
    }

    public async Task<IEnumerable<ChatMessage>> GetHistory()
    {
        if (users.GetName(Context.ConnectionId) is null)
            throw new HubException(
                "Primero entra al chat.");

        return await messages.GetHistoryAsync();
    }

    public async Task SendMessage(string message)
    {
        var user = users.GetName(Context.ConnectionId);

        if (user is null)
            throw new HubException(
                "Primero entra al chat.");

        if (string.IsNullOrWhiteSpace(message))
            throw new HubException(
                "El mensaje es obligatorio.");

        message = message.Trim();

        if (message.Length > 1000)
            throw new HubException(
                "El mensaje permite hasta 1000 caracteres.");

        var chatMessage = await messages.SaveAsync(user, message);

        await Clients.All.SendAsync(
            "ReceiveMessage",
            chatMessage);
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        users.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}