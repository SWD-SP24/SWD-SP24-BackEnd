﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;
using SWD392.Repositories;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class MembershipPackagesController : ControllerBase
    {
        private readonly IMembershipPackageRepository _repository;

    
        public MembershipPackagesController(IMembershipPackageRepository repository)
        {
            _repository = repository;
        }

        // GET: api/MembershipPackages
            [HttpGet]
        public async Task<ActionResult<IEnumerable<GetMembershipPackageDTO>>> GetMembershipPackages()
        {
            var membershipPackages = await _repository.GetMembershipPackagesAsync();
            var response = new
            {
                status = "success",
                data = membershipPackages
            };
            return Ok(response);
        }

        /*// GET: api/MembershipPackages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MembershipPackage>> GetMembershipPackage(int id)
        {
            var membershipPackage = await _context.MembershipPackages.FindAsync(id);

            if (membershipPackage == null)
            {
                return NotFound();
            }

            return membershipPackage;
        }

        // PUT: api/MembershipPackages/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMembershipPackage(int id, MembershipPackage membershipPackage)
        {
            if (id != membershipPackage.MembershipPackageId)
            {
                return BadRequest();
            }

            _context.Entry(membershipPackage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MembershipPackageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/MembershipPackages
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<MembershipPackage>> PostMembershipPackage(MembershipPackage membershipPackage)
        {
            _context.MembershipPackages.Add(membershipPackage);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMembershipPackage", new { id = membershipPackage.MembershipPackageId }, membershipPackage);
        }

        // DELETE: api/MembershipPackages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMembershipPackage(int id)
        {
            var membershipPackage = await _context.MembershipPackages.FindAsync(id);
            if (membershipPackage == null)
            {
                return NotFound();
            }

            _context.MembershipPackages.Remove(membershipPackage);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MembershipPackageExists(int id)
        {
            return _context.MembershipPackages.Any(e => e.MembershipPackageId == id);
        }*/
    }
}
