using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Handlers;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Dishes; // <-- Підключили репозиторій страв
using APIHandler.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

// 1. Моделі для формування JSON-відповіді (DTO)
public record GroupSessionsResponse(
    [property: JsonPropertyName("participants")] List<ParticipantDto> Participants,
    [property: JsonPropertyName("total")] decimal Total
);

public record ParticipantDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("items")] List<OrderItemDto> Items,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("isSubmitted")] bool IsSubmitted
);

public record OrderItemDto(
    [property: JsonPropertyName("dish")] DishDto Dish,
    [property: JsonPropertyName("quantity")] int Quantity
);

public record DishDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price
);

// 2. Сам обробник (Handler)
public class GetGroupSessionsHandler : IApiRequestHandler
{
    private readonly IGuestSessionRepository _sessionRepository;
    private readonly IDishRepository _dishRepository; // <-- Додали залежність

    public GetGroupSessionsHandler(
        IGuestSessionRepository sessionRepository,
        IDishRepository dishRepository) 
    {
        _sessionRepository = sessionRepository;
        _dishRepository = dishRepository;
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        request.PathParameters.TryGetValue("groupReservationId", out var groupReservationId);

        if (string.IsNullOrEmpty(groupReservationId))
        {
            return new APIGatewayProxyResponse { StatusCode = 400 };
        }

        // 1. Отримуємо сесії гостей
        var dbSessions = await _sessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        var participants = new List<ParticipantDto>();

        // 2. Формуємо дані
        foreach (var session in dbSessions)
        {
            var items = new List<OrderItemDto>();
            decimal participantTotal = 0;

            if (session.Dishes != null)
            {
                foreach (var item in session.Dishes)
                {
                    // Шукаємо страву в базі, щоб дізнатися її справжню ціну та назву
                    var dish = await _dishRepository.GetDishByIdAsync(item.DishId);
                    
                    if (dish != null)
                    {
                        var dishDto = new DishDto(Id: dish.Id, Name: dish.Name, Price: dish.Price);
                        items.Add(new OrderItemDto(Dish: dishDto, Quantity: item.Quantity));
                        
                        participantTotal += dish.Price * item.Quantity;
                    }
                }
            }

            participants.Add(new ParticipantDto(
                Id: session.Id, 
                Type: "Guest", 
                Name: session.GuestName, 
                Items: items,
                Total: participantTotal,
                IsSubmitted: session.IsSubmitted
            ));
        }

        // 3. Рахуємо загальну суму всієї групи
        var groupTotal = participants.Sum(p => p.Total);
        var responseData = new GroupSessionsResponse(participants, groupTotal);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(responseData),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}