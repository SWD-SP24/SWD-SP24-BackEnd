﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.Mapper;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the total number of children with active status.
        /// </summary>
        /// <response code="200">Returns the total number of children</response>
        [HttpGet("total-children")]
        public async Task<IActionResult> GetTotalChildren()
        {
            var totalChildren = await _context.Children.CountAsync(c => c.Status == 1);
            return Ok(totalChildren);
        }

        /// <summary>
        /// Get the total revenue from payment transactions.
        /// </summary>
        /// <response code="200">Returns the total revenue</response>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue()
        {
            var totalRevenue = await _context.PaymentTransactions.SumAsync(pt => pt.Amount);
            return Ok(totalRevenue);
        }

        /// <summary>
        /// Get the vaccination completion count grouped by vaccine name.
        /// </summary>
        /// <response code="200">Returns the vaccination completion count</response>
        [HttpGet("vaccination-completion")]
        public async Task<IActionResult> GetVaccinationCompletion()
        {
            var vaccinationCompletion = await _context.VaccineRecords
                .GroupBy(vr => vr.Vaccine.Name)
                .Select(g => new
                {
                    VaccineName = g.Key,
                    CompletionCount = g.Count()
                })
                .ToListAsync();

            return Ok(vaccinationCompletion);
        }

        /// <summary>
        /// Get the number of doses administered within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns the number of doses administered</response>
        [HttpGet("doses-administered")]
        public async Task<IActionResult> GetDosesAdministered(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.VaccineRecords.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(vr => vr.AdministeredDate >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(vr => vr.AdministeredDate <= endTime.Value);
            }

            var dosesAdministered = await query.CountAsync();

            return Ok(dosesAdministered);
        }

        /// <summary>
        /// Get the average weight and height by age group.
        /// </summary>
        /// <response code="200">Returns the average weight, height, and count of children by age group</response>
        [HttpGet("average-weight-height-by-age-group")]
        public async Task<IActionResult> GetAverageWeightHeightByAgeGroup()
        {
            var currentYear = DateTime.Now.Year;
            var averageWeightHeight = await _context.GrowthIndicators
                .GroupBy(gi => gi.Children.Dob.HasValue ? gi.Children.Dob.Value.Year : 0)
                .Select(g => new
                {
                    AgeGroup = currentYear - g.Key,
                    AverageWeight = g.Average(gi => gi.Weight),
                    AverageHeight = g.Average(gi => gi.Height),
                    Count = g.Count()
                })
                .Where(g => g.AgeGroup >= 0 && g.AgeGroup <= 30)
                .ToListAsync();

            return Ok(averageWeightHeight);
        }


        /// <summary>
        /// Get children with abnormal growth deviations.
        /// </summary>
        /// <response code="200">Returns children with abnormal growth deviations</response>
        [HttpGet("children-with-abnormal-growth-deviations")]
        public async Task<IActionResult> GetChildrenWithAbnormalGrowthDeviations()
        {
            var abnormalGrowthDeviations = await _context.GrowthIndicators
                .Where(gi => gi.Bmi > 30 || gi.Bmi < 15) // Example thresholds for abnormal BMI
                .Select(gi => new
                {
                    ChildId = gi.ChildrenId,
                    ChildName = gi.Children.FullName,
                    gi.Bmi,
                    gi.Height,
                    gi.Weight
                })
                .ToListAsync();

            return Ok(abnormalGrowthDeviations);
        }

        /// <summary>
        /// Get active user accounts.
        /// </summary>
        /// <response code="200">Returns active user accounts</response>
        [HttpGet("active-user-accounts")]
        public async Task<IActionResult> GetActiveUserAccounts()
        {
            var activeUsers = await _context.Users
                .Where(u => u.Status == "active")
                .ToListAsync();

            var activeUserDTOs = activeUsers.Select(u => u.ToGetUserDTO()).ToList();

            return Ok(activeUserDTOs);
        }

        /// <summary>
        /// Get new membership subscriptions within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns new membership subscriptions</response>
        [HttpGet("new-membership-subscriptions")]
        public async Task<IActionResult> GetNewMembershipSubscriptions(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.UserMemberships.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(um => um.StartDate >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(um => um.StartDate <= endTime.Value);
            }

            var newSubscriptions = await query.ToListAsync();

            return Ok(newSubscriptions);
        }

        /// <summary>
        /// Get revenue from subscriptions within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns revenue from subscriptions</response>
        [HttpGet("revenue-from-subscriptions")]
        public async Task<IActionResult> GetRevenueFromSubscriptions(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.PaymentTransactions.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(pt => pt.TransactionDate >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(pt => pt.TransactionDate <= endTime.Value);
            }

            var totalRevenue = await query.SumAsync(pt => pt.Amount);

            return Ok(totalRevenue);
        }

        /// <summary>
        /// Get monthly revenue within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns monthly revenue</response>
        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> GetMonthlyRevenue(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.PaymentTransactions.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(pt => pt.TransactionDate >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(pt => pt.TransactionDate <= endTime.Value);
            }

            var monthlyRevenue = await query
                .GroupBy(pt => new { pt.TransactionDate.Year, pt.TransactionDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(pt => pt.Amount)
                })
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(monthlyRevenue);
        }

        /// <summary>
        /// Get active subscriptions within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns active subscriptions</response>
        [HttpGet("active-subscriptions")]
        public async Task<IActionResult> GetActiveSubscriptions(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.UserMemberships
                .Include(um => um.MembershipPackage)
                .Where(um => um.EndDate == null || um.EndDate > DateTime.Now)
                .AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(um => um.StartDate >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(um => um.StartDate <= endTime.Value);
            }

            var activeSubscriptions = await query
                .GroupBy(um => new { um.StartDate.Year, um.StartDate.Month, um.MembershipPackage.MembershipPackageName })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    SubscriptionType = g.Key.MembershipPackageName,
                    ActiveCount = g.Count()
                })
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(activeSubscriptions);
        }

        /// <summary>
        /// Get user growth over time.
        /// </summary>
        /// <response code="200">Returns user growth over time</response>
        [HttpGet("user-growth-over-time")]
        public async Task<IActionResult> GetUserGrowthOverTime()
        {
            var userGrowth = await _context.Users
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    UserCount = g.Count()
                })
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(userGrowth);
        }

        /// <summary>
        /// Get children with allergies.
        /// </summary>
        /// <response code="200">Returns children with specific allergies</response>
        [HttpGet("children-with-allergies")]
        public async Task<IActionResult> GetChildrenWithSpecificAllergies()
        {
            var childrenWithAllergies = await _context.Children
                .Where(c => !string.IsNullOrEmpty(c.Allergies))
                .ToListAsync();

            return Ok(childrenWithAllergies);
        }

        /// <summary>
        /// Get the average growth rate within a specified date range.
        /// </summary>
        /// <param name="startTime">Start date for the range</param>
        /// <param name="endTime">End date for the range</param>
        /// <response code="200">Returns the average growth rate</response>
        [HttpGet("average-growth-rate")]
        public async Task<IActionResult> GetAverageGrowthRate([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
            var query = _context.GrowthIndicators.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(gi => gi.RecordTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(gi => gi.RecordTime <= endTime.Value);
            }

            var growthRates = await query
                .GroupBy(gi => gi.ChildrenId)
                .Select(g => new
                {
                    ChildId = g.Key,
                    HeightGrowthRate = g.Max(gi => gi.Height) - g.Min(gi => gi.Height),
                    WeightGrowthRate = g.Max(gi => gi.Weight) - g.Min(gi => gi.Weight)
                })
                .ToListAsync();

            return Ok(growthRates);
        }

        /// <summary>
        /// Get children with chronic conditions.
        /// </summary>
        /// <response code="200">Returns children with chronic conditions</response>
        [HttpGet("children-with-chronic-conditions")]
        public async Task<IActionResult> GetChildrenWithChronicConditions()
        {
            var childrenWithChronicConditions = await _context.Children
                .Where(c => !string.IsNullOrEmpty(c.ChronicConditions))
                .ToListAsync();

            return Ok(childrenWithChronicConditions);
        }

        /// <summary>
        /// Get the vaccination schedule compliance rate.
        /// </summary>
        /// <response code="200">Returns the vaccination schedule compliance rate</response>
        [HttpGet("vaccination-schedule-compliance")]
        public async Task<IActionResult> GetVaccinationScheduleCompliance()
        {
            var totalChildren = await _context.Children.CountAsync();
            var compliantChildren = await _context.VaccineRecords
                .GroupBy(vr => vr.ChildId)
                .CountAsync();

            var complianceRate = (double)compliantChildren / totalChildren * 100;
            return Ok(complianceRate);
        }

        /// <summary>
        /// Get missed vaccinations.
        /// </summary>
        /// <response code="200">Returns missed vaccinations</response>
        [HttpGet("missed-vaccinations")]
        public async Task<IActionResult> GetMissedVaccinations()
        {
            var missedVaccinations = await _context.VaccineRecords
                .Where(vr => vr.NextDoseDate < DateTime.Now)
                .ToListAsync();

            return Ok(missedVaccinations);
        }

        /// <summary>
        /// Get expired memberships.
        /// </summary>
        /// <response code="200">Returns expired memberships</response>
        [HttpGet("expired-memberships")]
        public async Task<IActionResult> GetExpiredMemberships()
        {
            var expiredMemberships = await _context.UserMemberships
                .Where(um => um.EndDate < DateTime.Now)
                .ToListAsync();

            return Ok(expiredMemberships);
        }
    }
}
