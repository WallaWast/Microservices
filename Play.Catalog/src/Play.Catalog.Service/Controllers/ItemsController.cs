using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private static readonly List<ItemDto> items = new()
        {
            new ItemDto(Guid.NewGuid(),"Potion", "Restore a small amount of HP", 5, DateTimeOffset.UtcNow),
            new ItemDto(Guid.NewGuid(),"Antidote", "Cures Poison", 7, DateTimeOffset.UtcNow),
            new ItemDto(Guid.NewGuid(),"Bronze Sword", "Deals a small amount of damage",20, DateTimeOffset.UtcNow)
        };

        [HttpGet]
        public IEnumerable<ItemDto> Get()
        {
            return items;
        }

        [HttpGet("{id}")]
        public ActionResult<ItemDto> GetbyId(Guid id)
        {
            var item = items.Where(items => items.Id == id).SingleOrDefault();

            if (item == null)
                return NotFound();

            return item;
        }

        [HttpPost]
        public ActionResult<ItemDto> Post(CreateItemDto createItemDto)
        {
            var item = new ItemDto(Guid.NewGuid(), createItemDto.Name, createItemDto.Description, createItemDto.Price, DateTimeOffset.UtcNow);
            items.Add(item);

            return CreatedAtAction(nameof(GetbyId), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public IActionResult Put(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = items.Where(item => item.Id == id).SingleOrDefault();

            if (existingItem == null)
            {
                var item = new ItemDto(id, updateItemDto.Name, updateItemDto.Description, updateItemDto.Price, DateTimeOffset.UtcNow);
                items.Add(item);

                return CreatedAtAction(nameof(GetbyId), new { id = item.Id }, item);
            }
            else
            {
                var updatedItem = existingItem with
                {
                    Name = updateItemDto.Name,
                    Description = updateItemDto.Description,
                    Price = updateItemDto.Price
                };

                var index = items.FindIndex(existingItem => existingItem.Id == id);
                items[index] = updatedItem;

                return NoContent();
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            var index = items.FindIndex(existingItem => existingItem.Id == id);

            if (index < 0)
                return NotFound();

            items.RemoveAt(index);

            return NoContent();
        }
    }
}