using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Handlers;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders; // <-- Підключили репозиторій замовлень
using APIHandler.Entities;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class SubmitGroupReservationHandler : IApiRequestHandler
{
    private readonly IGuestSessionRepository _sessionRepository;
    private readonly IOrderRepository _orderRepository; // <-- Додали залежність

    public SubmitGroupReservationHandler(
        IGuestSessionRepository sessionRepository,
        IOrderRepository orderRepository)
    {
        _sessionRepository = sessionRepository;
        _orderRepository = orderRepository;
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        request.PathParameters.TryGetValue("groupReservationId", out var groupReservationId);

        if (string.IsNullOrEmpty(groupReservationId))
        {
            return new APIGatewayProxyResponse { StatusCode = 400 };
        }

        // 1. Отримуємо всі сесії гостей для цієї групи
        var dbSessions = await _sessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);

        // 2. Відбираємо ТІЛЬКИ ті сесії, які гості вже підтвердили
        var submittedSessions = dbSessions.Where(s => s.IsSubmitted).ToList();

        if (!submittedSessions.Any())
        {
            return new APIGatewayProxyResponse 
            { 
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new { message = "Немає підтверджених сесій для консолідації." })
            };
        }

        // 3. Консолідація (групуємо однакові страви і сумуємо кількість)
        var consolidatedDishes = submittedSessions
            .Where(s => s.Dishes != null)
            .SelectMany(s => s.Dishes)
            .GroupBy(d => d.DishId)
            .Select(group => new OrderItem
            {
                DishId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        // 4. Отримуємо існуюче замовлення для цієї броні
        var order = await _orderRepository.GetOrderByReservationIdAsync(groupReservationId);
        
        if (order == null)
        {
            // Якщо замовлення ще не існує, створюємо нове і заповнюємо ВСІ обов'язкові поля
            order = new APIHandler.Entities.Order 
            { 
                OrderId = System.Guid.NewGuid().ToString(),
                CustomerId = "GroupReservation", // тимчасовий ID для групи
                ReservationId = groupReservationId,
                ReservationAddress = "Table TBD", // заглушка для адреси
                ReservationTime = "Time TBD",     // заглушка для часу
                Dishes = consolidatedDishes
            };
        }
        else
        {
            // Якщо замовлення вже існує, просто оновлюємо його страви
            order.Dishes = consolidatedDishes;
        }

        // 5. Зберігаємо згруповані страви у замовлення
        
        
        await _orderRepository.SaveOrderAsync(order);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new { message = "Order consolidated and saved successfully!", totalItems = consolidatedDishes.Count }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}