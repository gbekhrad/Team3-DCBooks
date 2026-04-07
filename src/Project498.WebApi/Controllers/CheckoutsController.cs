using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutsController(AppDbContext appDb, ComicsDbContext comicsDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCheckouts()
    {
        var checkouts = await appDb.Checkouts
            .OrderByDescending(c => c.CheckoutDate)
            .ToListAsync();
        return Ok(checkouts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCheckout(int id)
    {
        var checkout = await appDb.Checkouts.FindAsync(id);
        return checkout is null
            ? NotFound(new ErrorResponse("CHECKOUT_NOT_FOUND", $"Checkout {id} was not found."))
            : Ok(checkout);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserCheckouts(int userId)
    {
        var checkouts = await appDb.Checkouts
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CheckoutDate)
            .ToListAsync();
        return Ok(checkouts);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId is null)
        {
            return Unauthorized(new ErrorResponse("UNAUTHORIZED", "A valid user token is required."));
        }

        var userExists = await appDb.Users.AnyAsync(u => u.UserId == userId.Value);
        if (!userExists)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {userId.Value} was not found."));
        }

        var comic = await comicsDb.Comics.FindAsync(request.ComicId);
        if (comic is null)
        {
            return NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {request.ComicId} was not found."));
        }

        if (!string.Equals(comic.Status, "available", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ErrorResponse("COMIC_UNAVAILABLE", "Comic is already checked out."));
        }

        var now = DateTime.UtcNow;
        var checkout = new Checkout
        {
            UserId = userId.Value,
            ComicId = request.ComicId,
            CheckoutDate = now,
            DueDate = now.AddDays(14),
            Status = "checked_out"
        };

        comic.Status = "checked_out";
        comic.CheckedOutBy = userId.Value;
        appDb.Checkouts.Add(checkout);

        await appDb.SaveChangesAsync();
        await comicsDb.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCheckout), new { id = checkout.CheckoutId }, checkout);
    }

    [Authorize]
    [HttpPut("{id:int}/return")]
    public Task<IActionResult> Return(int id)
    {
        return ReturnInternal(id);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public Task<IActionResult> ReturnCompat(int id)
    {
        return ReturnInternal(id);
    }

    private async Task<IActionResult> ReturnInternal(int id)
    {
        var userId = GetUserIdFromToken();
        if (userId is null)
        {
            return Unauthorized(new ErrorResponse("UNAUTHORIZED", "A valid user token is required."));
        }

        var checkout = await appDb.Checkouts.FindAsync(id);
        if (checkout is null)
        {
            return NotFound(new ErrorResponse("CHECKOUT_NOT_FOUND", $"Checkout {id} was not found."));
        }

        if (checkout.UserId != userId.Value)
        {
            return Forbid();
        }

        if (checkout.ReturnDate is not null || string.Equals(checkout.Status, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ErrorResponse("CHECKOUT_ALREADY_RETURNED", "Checkout has already been returned."));
        }

        var comic = await comicsDb.Comics.FindAsync(checkout.ComicId);
        if (comic is null)
        {
            return NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {checkout.ComicId} was not found."));
        }

        checkout.ReturnDate = DateTime.UtcNow;
        checkout.Status = "returned";
        comic.Status = "available";
        comic.CheckedOutBy = null;

        await appDb.SaveChangesAsync();
        await comicsDb.SaveChangesAsync();

        return Ok(checkout);
    }

    private int? GetUserIdFromToken()
    {
        var claim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}

public record CheckoutRequest(int ComicId);