using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.Contracts.Services;
using Application.DTOs.Chats.ChatSession;
using Application.DTOs.Chats.ChatMessage;
using Application.DTOs.Chats.ChatEscalation;
using Domain.Entities.Customers;
using Domain.Entities.Users;
using Domain.ValueObject.Customers.Customer;
using Api.Hubs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatSessionService _chatSessionService;
        private readonly IChatMessageService _chatMessageService;
        private readonly IChatbotClient _chatbotClient;
        private readonly IChatEscalationService _chatEscalationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatSessionService chatSessionService,
            IChatMessageService chatMessageService,
            IChatbotClient chatbotClient,
            IChatEscalationService chatEscalationService,
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatController> logger)
        {
            _chatSessionService = chatSessionService;
            _chatMessageService = chatMessageService;
            _chatbotClient = chatbotClient;
            _chatEscalationService = chatEscalationService;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Historial de mensajes de una sesión (cliente FAB / chatbot).
        /// AllowAnonymous: el sessionId actúa como capacidad; el front solo conoce el suyo.
        /// </summary>
        [HttpGet("session/{sessionId:int}/messages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSessionMessages(int sessionId)
        {
            var messages = await _chatMessageService.GetBySessionIdAsync(sessionId);
            return Ok(messages);
        }

        [HttpPost("message")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "El mensaje no puede estar vacío." });

            var customerId = await ResolveCustomerIdAsync(request.CustomerId);
            _logger.LogInformation(
                "Chat message auth: customerId={CustomerId}, sessionId={SessionId}, userAuthenticated={Auth}",
                customerId,
                request.SessionId,
                User?.Identity?.IsAuthenticated);

            ChatSessionDto session;
            if (customerId.HasValue)
            {
                // Cliente autenticado: nunca reutilizar sesión anónima sin vincular.
                if (int.TryParse(request.SessionId, out var sessionId))
                {
                    var existing = await _chatSessionService.GetByIdAsync(sessionId);
                    if (existing is null)
                    {
                        session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                        {
                            CustomerId = customerId,
                        });
                    }
                    else if (!existing.CustomerId.HasValue)
                    {
                        session = await _chatSessionService.LinkCustomerAsync(existing.ChatSessionId, customerId.Value)
                                  ?? existing;
                        if (!session.CustomerId.HasValue)
                        {
                            session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                            {
                                CustomerId = customerId,
                            });
                        }
                    }
                    else
                    {
                        session = existing;
                    }
                }
                else
                {
                    session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                    {
                        CustomerId = customerId,
                    });
                }
            }
            else if (int.TryParse(request.SessionId, out var anonSessionId))
            {
                var existing = await _chatSessionService.GetByIdAsync(anonSessionId);
                session = existing ?? await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }
            else
            {
                session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }

            await _chatMessageService.CreateAsync(new CreateChatMessageRequest
            {
                ChatSessionId = session.ChatSessionId,
                SenderTypeId = 2,
                Content = request.Message,
            });

            var botResponse = await _chatbotClient.SendMessageAsync(
                session.ChatSessionId.ToString(),
                request.Message);

            var responseText = botResponse.Response;
            var state = botResponse.State;

            if (string.Equals(botResponse.State, "WAITING_HUMAN_AGENT", StringComparison.OrdinalIgnoreCase))
            {
                // Re-resolver por si el cliente se autenticó a mitad de conversación
                customerId ??= await ResolveCustomerIdAsync(request.CustomerId);
                if (!session.CustomerId.HasValue && customerId.HasValue)
                {
                    session = await _chatSessionService.LinkCustomerAsync(session.ChatSessionId, customerId.Value)
                              ?? session;
                }

                if (!session.CustomerId.HasValue)
                {
                    state = "NEED_LOGIN_FOR_AGENT";
                    responseText =
                        "Para hablar con un asesor necesitas una cuenta. " +
                        "Inicia sesión o regístrate e intenta de nuevo.";

                    await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                    {
                        ChatSessionId = session.ChatSessionId,
                        SenderTypeId = 1,
                        Content = responseText,
                    });
                }
                else
                {
                    await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                    {
                        ChatSessionId = session.ChatSessionId,
                        SenderTypeId = 1,
                        Content = botResponse.Response,
                    });

                    try
                    {
                        var (escalation, created) = await _chatEscalationService.CreateAsync(new CreateChatEscalationRequest
                        {
                            SessionId = session.ChatSessionId.ToString(),
                            Reason = "Escalación solicitada desde el chatbot",
                        });
                        if (created)
                            await _hubContext.Clients.Group("Advisors").SendAsync("NewEscalation", escalation);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo crear escalación automática para sesión {SessionId}", session.ChatSessionId);
                        var needsLogin = ex.Message.Contains("iniciar sesión", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("cuenta", StringComparison.OrdinalIgnoreCase);
                        if (needsLogin)
                        {
                            state = "NEED_LOGIN_FOR_AGENT";
                            responseText = ex.Message;
                        }
                        else
                        {
                            // Mantener WAITING: el front / asesor pueden reintentar; no inventar login
                            responseText = "Te estoy conectando con un asesor. En un momento te atienden.";
                        }
                    }
                }
            }
            else
            {
                await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                {
                    ChatSessionId = session.ChatSessionId,
                    SenderTypeId = 1,
                    Content = botResponse.Response,
                });
            }

            var saleOrigin = botResponse.SaleOrigin;
            if (string.IsNullOrWhiteSpace(saleOrigin)
                && (!string.IsNullOrWhiteSpace(botResponse.InvoiceNumber)
                    || string.Equals(state, "SALE_COMPLETED", StringComparison.OrdinalIgnoreCase)))
            {
                saleOrigin = "CHATBOT";
            }

            return Ok(new
            {
                sessionId = session.ChatSessionId.ToString(),
                response = responseText,
                state,
                invoiceNumber = botResponse.InvoiceNumber,
                saleOrigin,
                products = botResponse.Products,
            });
        }

        [HttpPost("escalate")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> Escalate([FromBody] CreateChatEscalationRequest request)
        {
            request.NormalizeFromSnakeCase();
            var sessionId = request.SessionId;
            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Escalación desde chatbot"
                : request.Reason;

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "sessionId es requerido." });

            // Si la sesión aún no tiene cliente, intentar vincularlo desde el JWT
            // (el bot Python llama sin token; el cliente ya debió vincularse en /message).
            if (int.TryParse(sessionId, out var chatSessionId))
            {
                var customerId = await ResolveCustomerIdAsync(null);
                if (customerId.HasValue)
                {
                    var existing = await _chatSessionService.GetByIdAsync(chatSessionId);
                    if (existing is not null && !existing.CustomerId.HasValue)
                        await _chatSessionService.LinkCustomerAsync(chatSessionId, customerId.Value);
                }
            }

            try
            {
                var (escalation, created) = await _chatEscalationService.CreateAsync(new CreateChatEscalationRequest
                {
                    SessionId = sessionId,
                    Reason = reason,
                });

                if (created)
                    await _hubContext.Clients.Group("Advisors").SendAsync("NewEscalation", escalation);

                return Ok(new
                {
                    success = true,
                    escalation_id = escalation.ChatEscalationId,
                    chatEscalationId = escalation.ChatEscalationId,
                    chatSessionId = escalation.ChatSessionId,
                    customerName = escalation.CustomerName,
                    statusName = escalation.StatusName,
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Resuelve el CustomerId del usuario autenticado.
        /// Si el User no tiene Customer vinculado, lo crea/enlaza por email (igual que el login).
        /// También acepta un customerId enviado por el front como respaldo.
        /// </summary>
        private async Task<int?> ResolveCustomerIdAsync(int? customerIdFromClient)
        {
            // 1) Respaldo inmediato del front
            if (customerIdFromClient is int cid && cid > 0)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(cid);
                if (customer is not null)
                    return cid;
            }

            // 2) Claim customerId del JWT (tokens nuevos)
            var customerClaim =
                User.FindFirstValue("customerId") ??
                User.FindFirstValue("customer_id");
            if (int.TryParse(customerClaim, out var fromClaim) && fromClaim > 0)
                return fromClaim;

            var raw =
                User.FindFirstValue("nameid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (int.TryParse(raw, out var userId) && userId > 0)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user is not null)
                {
                    if (user.CustomerId is null)
                    {
                        await EnsureCustomerLinkedAsync(user);
                        user = await _unitOfWork.Users.GetByIdAsync(userId);
                    }

                    if (user?.CustomerId is int linked)
                        return linked;
                }
            }

            // 3) Fallback por email del JWT
            var email =
                User.FindFirstValue("email") ??
                User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _unitOfWork.Customers.GetByEmailAsync(email);
                if (byEmail is not null)
                    return byEmail.Id;
            }

            return null;
        }

        private async Task EnsureCustomerLinkedAsync(User user)
        {
            var existing = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            if (existing is null)
            {
                existing = new Customer(
                    new CustomerName(user.Name.Value),
                    new CustomerEmail(user.Email.Value));
                await _unitOfWork.Customers.AddAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            if (user.CustomerId == existing.Id)
                return;

            user.LinkCustomer(existing.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public class ChatMessageRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        /// <summary>Opcional: customerId del cliente logueado (respaldo del front).</summary>
        public int? CustomerId { get; set; }
    }
}
