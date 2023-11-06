using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<InventoryItem> _inventoryitemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public GrantItemsConsumer(IRepository<InventoryItem> inventoryitemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryitemsRepository = inventoryitemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        var message = context.Message;

        var item = await _catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }

        var inventoryItem = await _inventoryitemsRepository
                                    .GetAsync(item =>
                                        item.UserId == message.UserId &&
                                        item.CatalogItemId == message.CatalogItemId);

        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            await _inventoryitemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += message.Quantity;
            await _inventoryitemsRepository.UpdateAsync(inventoryItem);
        }

        await context.Publish(new InventoryItemsGranted(message.CorrelationId));
    }
}