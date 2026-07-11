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
    /// <summary>
    /// Controlador que expone los endpoints del chatbot y gestión de sesiones de chat.
    /// Maneja el envío de mensajes al bot de IA, escalamientos a asesores humanos
    /// y notificaciones en tiempo real vía SignalR.
    /// </summary>
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

        /// <summary>
        /// Constructor que inyecta las dependencias necesarias mediante inyección de dependencias.
        /// </summary>
        /// <param name="chatSessionService">Servicio para gestión de sesiones de chat.</param>
        /// <param name="chatMessageService">Servicio para persistencia de mensajes.</param>
        /// <param name="chatbotClient">Cliente HTTP para comunicarse con el servicio de chatbot (FastAPI).</param>
        /// <param name="chatEscalationService">Servicio para gestión de escalamientos a asesores.</param>
        /// <param name="unitOfWork">Unit of Work para acceso a datos.</param>
        /// <param name="hubContext">Contexto de SignalR para enviar notificaciones en tiempo real.</param>
        /// <param name="logger">Logger para registro de eventos.</param>
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
        /// Endpoint para obtener el historial de mensajes de una sesión de chat.
        /// No requiere autenticación; el sessionId actúa como identificador de capacidad.
        /// </summary>
        /// <param name="sessionId">ID de la sesión de chat.</param>
        /// <returns>HTTP 200 con la lista de mensajes de la sesión.</returns>
        /// <remarks>
        /// Este endpoint es usado por el frontend para cargar el historial de conversación
        /// cuando un cliente se reconecta o refresca la página. El sessionId es conocido
        /// solo por el cliente que inició la conversación, actuando como identificador de capacidad.
        /// </remarks>
        [HttpGet("session/{sessionId:int}/messages")]
        [AllowAnonymous] // Permite acceso sin autenticación (sessionId actúa como capacidad)
        public async Task<IActionResult> GetSessionMessages(int sessionId)
        {
            // Obtiene todos los mensajes de la sesión especificada
            var messages = await _chatMessageService.GetBySessionIdAsync(sessionId);
            return Ok(messages);
        }

        /// <summary>
        /// Endpoint principal del chatbot: envía un mensaje del cliente y obtiene la respuesta del bot.
        /// Maneja sesiones anónimas y autenticadas, persistencia de mensajes y escalamientos.
        /// </summary>
        /// <param name="request">DTO con el mensaje, sessionId y customerId opcional.</param>
        /// <returns>
        /// HTTP 200 con la respuesta del bot, estado de la conversación, datos de venta (si aplica).
        /// HTTP 400 si el mensaje está vacío.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - No requiere autenticación (permite sesiones anónimas)
        /// - Tiene rate limiting (30 requests por minuto por IP)
        /// - Crea o reutiliza sesiones de chat
        /// - Vincula sesiones anónimas a usuarios autenticados
        /// - Persiste ambos mensajes (cliente y bot)
        /// - Maneja escalamientos a asesores humanos cuando el bot responde WAITING_HUMAN_AGENT
        /// - Notifica a asesores vía SignalR cuando hay un nuevo escalamiento
        /// </remarks>
        [HttpPost("message")]
        [AllowAnonymous] // Permite mensajes anónimos (usados por chatbot)
        [EnableRateLimiting("chatbot")] // Aplica el limitador "chatbot" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            // Valida que el mensaje no esté vacío
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "El mensaje no puede estar vacío." });

            // ==============================================================================
            // RESOLUCIÓN DEL CUSTOMER
            // ==============================================================================
            // Intenta resolver el Customer del usuario autenticado o del request
            var customerId = await ResolveCustomerIdAsync(request.CustomerId);
            _logger.LogInformation(
                "Chat message auth: customerId={CustomerId}, sessionId={SessionId}, userAuthenticated={Auth}",
                customerId,
                request.SessionId,
                User?.Identity?.IsAuthenticated);

            // ==============================================================================
            // GESTIÓN DE SESIÓN DE CHAT
            // ==============================================================================
            ChatSessionDto session;
            if (customerId.HasValue)
            {
                // ========================================================================
                // CLIENTE AUTENTICADO
                // ========================================================================
                // Si el cliente está autenticado, nunca reutiliza una sesión anónima sin vincular
                if (int.TryParse(request.SessionId, out var sessionId))
                {
                    var existing = await _chatSessionService.GetByIdAsync(sessionId);
                    if (existing is null)
                    {
                        // La sesión no existe, crea una nueva vinculada al cliente
                        session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                        {
                            CustomerId = customerId,
                        });
                    }
                    else if (!existing.CustomerId.HasValue)
                    {
                        // La sesión existe pero es anónima, intenta vincularla al cliente
                        session = await _chatSessionService.LinkCustomerAsync(existing.ChatSessionId, customerId.Value)
                                  ?? existing;
                        if (!session.CustomerId.HasValue)
                        {
                            // Si falló la vinculación, crea una nueva sesión
                            session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                            {
                                CustomerId = customerId,
                            });
                        }
                    }
                    else
                    {
                        // La sesión ya está vinculada al cliente, reutilízala
                        session = existing;
                    }
                }
                else
                {
                    // No hay sessionId, crea una nueva sesión vinculada al cliente
                    session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest
                    {
                        CustomerId = customerId,
                    });
                }
            }
            else if (int.TryParse(request.SessionId, out var anonSessionId))
            {
                // ========================================================================
                // CLIENTE ANÓNIMO CON SESIÓN EXISTENTE
                // ========================================================================
                var existing = await _chatSessionService.GetByIdAsync(anonSessionId);
                session = existing ?? await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }
            else
            {
                // ========================================================================
                // CLIENTE ANÓNIMO SIN SESIÓN
                // ========================================================================
                session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }

            // ==============================================================================
            // PERSISTENCIA DEL MENSAJE DEL CLIENTE
            // ==============================================================================
            // SenderTypeId = 2 corresponde a "Cliente" en la tabla SenderTypes
            await _chatMessageService.CreateAsync(new CreateChatMessageRequest
            {
                ChatSessionId = session.ChatSessionId,
                SenderTypeId = 2, // Cliente
                Content = request.Message,
            });

            // ==============================================================================
            // ENVÍO AL CHATBOT (FASTAPI)
            // ==============================================================================
            // Envía el mensaje al servicio externo de chatbot y obtiene la respuesta
            var botResponse = await _chatbotClient.SendMessageAsync(
                session.ChatSessionId.ToString(),
                request.Message);

            var responseText = botResponse.Response;
            var state = botResponse.State;

            // ==============================================================================
            // MANEJO DE ESCALAMIENTO A HUMANO
            // ==============================================================================
            if (string.Equals(botResponse.State, "WAITING_HUMAN_AGENT", StringComparison.OrdinalIgnoreCase))
            {
                // Re-resolver el Customer por si el cliente se autenticó a mitad de conversación
                customerId ??= await ResolveCustomerIdAsync(request.CustomerId);
                if (!session.CustomerId.HasValue && customerId.HasValue)
                {
                    // Vincula la sesión al cliente autenticado
                    session = await _chatSessionService.LinkCustomerAsync(session.ChatSessionId, customerId.Value)
                              ?? session;
                }

                if (!session.CustomerId.HasValue)
                {
                    // ====================================================================
                    // NO HAY CUSTOMER VINCULADO: REQUIERE LOGIN
                    // ====================================================================
                    state = "NEED_LOGIN_FOR_AGENT";
                    responseText =
                        "Para hablar con un asesor necesitas una cuenta. " +
                        "Inicia sesión o regístrate e intenta de nuevo.";

                    await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                    {
                        ChatSessionId = session.ChatSessionId,
                        SenderTypeId = 1, // Bot
                        Content = responseText,
                    });
                }
                else
                {
                    // ====================================================================
                    // HAY CUSTOMER VINCULADO: CREAR ESCALAMIENTO
                    // ====================================================================
                    // Persiste el mensaje del bot indicando escalamiento
                    await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                    {
                        ChatSessionId = session.ChatSessionId,
                        SenderTypeId = 1, // Bot
                        Content = botResponse.Response,
                    });

                    try
                    {
                        // Crea el escalamiento en la base de datos
                        var (escalation, created) = await _chatEscalationService.CreateAsync(new CreateChatEscalationRequest
                        {
                            SessionId = session.ChatSessionId.ToString(),
                            Reason = "Escalación solicitada desde el chatbot",
                        });
                        // Si se creó un nuevo escalamiento, notifica a los asesores vía SignalR
                        if (created)
                            await _hubContext.Clients.Group("Advisors").SendAsync("NewEscalation", escalation);
                    }
                    catch (Exception ex)
                    {
                        // Si falla la creación del escalamiento, maneja el error
                        _logger.LogWarning(ex, "No se pudo crear escalación automática para sesión {SessionId}", session.ChatSessionId);
                        var needsLogin = ex.Message.Contains("iniciar sesión", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("cuenta", StringComparison.OrdinalIgnoreCase);
                        if (needsLogin)
                        {
                            // El error indica que falta login
                            state = "NEED_LOGIN_FOR_AGENT";
                            responseText = ex.Message;
                        }
                        else
                        // Otro error: mantiene estado WAITING para reintentar
                        {
                            responseText = "Te estoy conectando con un asesor. En un momento te atienden.";
                        }
                    }
                }
            }
            else
            {
                // ==============================================================================
                // RESPUESTA NORMAL DEL BOT (NO ESCALAMIENTO)
                // ==============================================================================
                await _chatMessageService.CreateAsync(new CreateChatMessageRequest
                {
                    ChatSessionId = session.ChatSessionId,
                    SenderTypeId = 1, // Bot
                    Content = botResponse.Response,
                });
            }

            // ==============================================================================
            // DETERMINACIÓN DEL ORIGEN DE VENTA
            // ==============================================================================
            var saleOrigin = botResponse.SaleOrigin;
            if (string.IsNullOrWhiteSpace(saleOrigin)
                && (!string.IsNullOrWhiteSpace(botResponse.InvoiceNumber)
                    || string.Equals(state, "SALE_COMPLETED", StringComparison.OrdinalIgnoreCase)))
            {
                // Si el bot generó una factura o completó una venta, marca origen como CHATBOT
                saleOrigin = "CHATBOT";
            }

            // ==============================================================================
            // RETORNO DE RESPUESTA
            // ==============================================================================
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

        /// <summary>
        /// Endpoint para crear un escalamiento manual a un asesor humano.
        /// Puede ser llamado por el chatbot Python o por el frontend.
        /// </summary>
        /// <param name="request">DTO con sessionId y razón del escalamiento.</param>
        /// <returns>
        /// HTTP 200 con los datos del escalamiento creado.
        /// HTTP 400 si hay error de validación.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - No requiere autenticación
        /// - Tiene rate limiting
        /// - Intenta vincular la sesión al Customer si hay usuario autenticado
        /// - Notifica a asesores vía SignalR cuando se crea el escalamiento
        /// </remarks>
        [HttpPost("escalate")]
        [AllowAnonymous] // Permite escalamientos anónimos (usados por chatbot)
        [EnableRateLimiting("chatbot")] // Aplica el limitador "chatbot" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> Escalate([FromBody] CreateChatEscalationRequest request)
        {
            // Normaliza los campos del request (snake_case a PascalCase)
            request.NormalizeFromSnakeCase();
            var sessionId = request.SessionId;
            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Escalación desde chatbot"
                : request.Reason;

            // Valida que se haya proporcionado el sessionId
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "sessionId es requerido." });

            // ==============================================================================
            // VINCULACIÓN DE CUSTOMER (SI HAY USUARIO AUTENTICADO)
            // ==============================================================================
            // Si la sesión aún no tiene Customer vinculado, intenta vincularlo desde el JWT
            // (el bot Python llama sin token; el cliente ya debió vincularse en /message)
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
                // ==============================================================================
                // CREACIÓN DE ESCALAMIENTO
                // ==============================================================================
                var (escalation, created) = await _chatEscalationService.CreateAsync(new CreateChatEscalationRequest
                {
                    SessionId = sessionId,
                    Reason = reason,
                });

                // Si se creó un nuevo escalamiento, notifica a los asesores vía SignalR
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
                // Error de validación (ej: ya existe escalamiento para la sesión)
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Método privado que resuelve el CustomerId del usuario autenticado.
        /// Usa múltiples estrategias para encontrar el Customer vinculado al usuario.
        /// </summary>
        /// <param name="customerIdFromClient">CustomerId enviado por el frontend como respaldo.</param>
        /// <returns>ID del Customer si se encuentra, null si no se encuentra.</returns>
        /// <remarks>
        /// Estrategias de resolución en orden de prioridad:
        /// 1. CustomerId enviado por el frontend (respaldo inmediato)
        /// 2. Claim "customerId" o "customer_id" del JWT (tokens nuevos)
        /// 3. Claim "nameid"/"sub" del JWT para buscar el User y luego su Customer vinculado
        /// 4. Claim "email" del JWT para buscar Customer por email
        /// Si el User no tiene Customer vinculado, lo crea/enlaza por email (igual que el login).
        /// </remarks>
        private async Task<int?> ResolveCustomerIdAsync(int? customerIdFromClient)
        {
            // ==============================================================================
            // ESTRATEGIA 1: RESPALDO INMEDIATO DEL FRONTEND
            // ==============================================================================
            if (customerIdFromClient is int cid && cid > 0)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(cid);
                if (customer is not null)
                    return cid;
            }

            // ==============================================================================
            // ESTRATEGIA 2: CLAIM CUSTOMERID DEL JWT (TOKENS NUEVOS)
            // ==============================================================================
            var customerClaim =
                User.FindFirstValue("customerId") ??
                User.FindFirstValue("customer_id");
            if (int.TryParse(customerClaim, out var fromClaim) && fromClaim > 0)
                return fromClaim;

            // ==============================================================================
            // ESTRATEGIA 3: CLAIM USERID DEL JWT PARA BUSCAR USER Y LUEGO CUSTOMER
            // ==============================================================================
            var raw =
                User.FindFirstValue("nameid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (int.TryParse(raw, out var userId) && userId > 0)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user is not null)
                {
                    // Si el User no tiene Customer vinculado, lo crea/enlaza
                    if (user.CustomerId is null)
                    {
                        await EnsureCustomerLinkedAsync(user);
                        user = await _unitOfWork.Users.GetByIdAsync(userId);
                    }

                    if (user?.CustomerId is int linked)
                        return linked;
                }
            }

            // ==============================================================================
            // ESTRATEGIA 4: FALLBACK POR EMAIL DEL JWT
            // ==============================================================================
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

        /// <summary>
        /// Método privado que asegura que un User esté vinculado a un Customer.
        /// Reutiliza un Customer existente con el mismo email o crea uno nuevo si no existe.
        /// </summary>
        /// <param name="user">Usuario que debe ser vinculado a un Customer.</param>
        private async Task EnsureCustomerLinkedAsync(User user)
        {
            // Busca si ya existe un Customer con el mismo email que el usuario
            var existing = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            if (existing is null)
            {
                // Si no existe, crea un nuevo Customer
                existing = new Customer(
                    new CustomerName(user.Name.Value),
                    new CustomerEmail(user.Email.Value));
                await _unitOfWork.Customers.AddAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            // Si el usuario ya está vinculado al Customer correcto, no hace nada
            if (user.CustomerId == existing.Id)
                return;

            // Vincula el usuario al Customer encontrado o creado
            user.LinkCustomer(existing.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    /// <summary>
    /// DTO interno para la request de mensaje de chat.
    /// Contiene el mensaje, sessionId y customerId opcional.
    /// </summary>
    public class ChatMessageRequest
    {
        /// <summary>ID de la sesión de chat (puede ser string para sesiones nuevas).</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Contenido del mensaje del cliente.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Opcional: customerId del cliente logueado (respaldo del frontend).
        /// Se usa como fallback si no se puede resolver el Customer desde el JWT.
        /// </summary>
        public int? CustomerId { get; set; }
    }
}
