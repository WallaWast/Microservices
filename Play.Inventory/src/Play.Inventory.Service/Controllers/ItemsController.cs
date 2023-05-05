using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";

        private readonly IRepository<InventoryItem> _inventoryitemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;

        public ItemsController(IRepository<InventoryItem> inventoryitemsRepository,
        IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryitemsRepository = inventoryitemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return BadRequest();

            // Get the userId from the claims
            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.Parse(currentUserId) != userId)
            {
                if (!User.IsInRole(AdminRole))
                    return Forbid();
            }

            var inventoryItemsEntities = await _inventoryitemsRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemsEntities.Select(item => item.CatalogItemId);
            var catalogItemEntities = await _catalogItemsRepository.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemsDtos = inventoryItemsEntities
                                        .Select(ii =>
                                        {
                                            var catalogItem = catalogItemEntities.Single(ci => ci.Id == ii.CatalogItemId);
                                            return ii.AsDto(catalogItem.Name, catalogItem.Description);
                                        });

            return Ok(inventoryItemsDtos);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await _inventoryitemsRepository.GetAsync(item => item.UserId == grantItemsDto.UserId &&
                                        item.CatalogItemId == grantItemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _inventoryitemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await _inventoryitemsRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }
    }
}