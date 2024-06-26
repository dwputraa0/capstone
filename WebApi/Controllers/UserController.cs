﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Models;
using BCrypt.Net;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("/users")]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _context;

        public UsersController(DataContext context)
        {
            _context = context;
        }

        // GET: {{base_url}}/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (!(User.Identity.IsAuthenticated && User.IsInRole("Admin")))
            {
                return Unauthorized(new { Message = "Admin privileges required" });
            }
            var users =  await _context.Users.ToListAsync();
            return Ok(new
            {
                Message = "Success",
                Data = users.Select(u => new {
                    id = u.Id,
                    name = u.Name,
                    initials = u.InitialChar,
                    is_admin = u.IsAdmin,
                    is_active = u.IsActive,
                })
            });
        }


        // GET: {{base_url}}/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { Message = "Login required" });
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { Message = "user not found" } ) ;
            }

            return Ok(new
            {
                Message = "Success",
                Data = new {
                    id = user.Id,
                    name = user.Name,
                    initials = user.InitialChar,
                    is_admin = user.IsAdmin,
                    is_active = user.IsActive,
                }
            });
        }

        // POST: {{base_url}}/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] UserRequest request)
        {
            if (!(User.Identity.IsAuthenticated && User.IsInRole("Admin")))
            {
                return Unauthorized(new { Message = "Admin privileges required" });
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (_context.Users.Any(u => u.InitialChar == request.initials))
                {
                    return Conflict(new { Message = "User with the same initial already exist" });
                }

                var newUser = new User
                {
                    Name = request.name,
                    InitialChar = request.initials,
                    IsAdmin = request.is_admin,
                    IsActive = request.is_active,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.password),
                    Email = request.email,
                };
                
                _context.Users.Add(newUser);
                _context.SaveChanges();

                return CreatedAtAction(
                        nameof(GetUser), 
                        new { id = newUser.Id }, 
                        new { Message = "Success", 
                              Data = new {
                                  id = newUser.Id,
                                  name = newUser.Name,
                                  initials = newUser.InitialChar,
                                  is_admin = newUser.IsAdmin,
                                  is_active = newUser.IsActive,
                              }
                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");

                return StatusCode(500, new { Message = "Internal Server Error", Data = ex.Message });
            }
        }

        // PUT: {{base_url}}/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateRequest request)
        {
            if (!(User.Identity.IsAuthenticated && User.IsInRole("Admin")))
            {
                return Unauthorized(new { Message = "Admin privileges required" });
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { Message = "user not found" });
                }

                if (user.InitialChar == "ADM")
                {
                    return Unauthorized(new { Message = "Cannot change superadmin" });
                }

                if (_context.Users.Any(u => u.InitialChar == request.initials) && user.InitialChar != request.initials)
                {
                    return Conflict(new { Message = "User with the same initial already exist" });
                }
                user.Name = request.name ?? user.Name;
                user.InitialChar = request.initials ?? user.InitialChar;
                user.IsAdmin = request.is_admin;
                user.IsActive = request.is_active;
                user.Email = request.email ?? user.Email;
                if (request.password != null) user.Password = BCrypt.Net.BCrypt.HashPassword(request.password);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Success",
                    Data = new
                    {
                        id = user.Id,
                        name = user.Name,
                        initials = user.InitialChar,
                        is_admin = user.IsAdmin,
                        is_active = user.IsActive,
                    }
                });
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");

                return StatusCode(500, new { Message = "Internal Server Error", Data = ex.Message });
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}

public class UserRequest
{
    public required string name { get; set; }
    public required string initials { get; set; }
    public required bool is_admin { get; set; }
    public required string password { get; set; }
    public required bool is_active { get; set; }
    public string? email { get; set; }
}

public class UserUpdateRequest
{
    public string? name { get; set; }
    public string? initials { get; set; }
    public required bool is_admin { get; set; }
    public string? password { get; set; }
    public required bool is_active { get; set; }
    public string? email { get; set; }
}
