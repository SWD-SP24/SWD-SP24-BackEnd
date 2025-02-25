using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserMembershipsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserMembershipsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UserMemberships
    }
}