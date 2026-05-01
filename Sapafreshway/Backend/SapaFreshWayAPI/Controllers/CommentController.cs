using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace SapaFreshWayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly SapaFreshContext _context;
        private readonly IReservationService _reservationService;


        public CommentController(SapaFreshContext context, IReservationService reservationService)
        {
            _context = context;
            _reservationService = reservationService;
        }

        [HttpGet("getAllComment")]
        public async Task<ActionResult<List<Comment>>> GetAll()
        {
            return await _context.Comment.ToListAsync();
        }

        [HttpGet("canComment/{userId}")]
        public async Task<ActionResult<bool>> GetCanComment(int userId)
        {

            var result = await _reservationService.GetReservationsByCustomerCommentAsync(userId);
            return (bool)result;
        }

        public class AddCommentDto
        {
            public string Email { get; set; } = string.Empty;

            public string CommentString { get; set; } = string.Empty;

            public int Rate { get; set; }
        }

        [HttpPost("AddComment")]
        public async Task<ActionResult<bool>> AddComment([FromBody] AddCommentDto model)
        {
            if (model == null)
                return BadRequest(false);
            var comment = new Comment
            {
                Email = model.Email,
                CommentString = model.CommentString,
                Rate = model.Rate
            };
            await _context.Comment.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Return the created comment (simple POCO) instead of EntityEntry which contains the DbContext
            return Ok(comment);
        }
    }
}
