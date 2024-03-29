using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class SubtractItemsConsumer : IConsumer<SubtractItems>
{
    private readonly IRepository<InventoryItem> _inventoryitemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public SubtractItemsConsumer(IRepository<InventoryItem> inventoryitemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryitemsRepository = inventoryitemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<SubtractItems> context)
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

        if (inventoryItem != null)
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity -= message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await _inventoryitemsRepository.UpdateAsync(inventoryItem);

            await context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        }

        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
    }
}