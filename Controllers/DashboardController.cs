using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;

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

        [HttpGet("total-children")]
        public async Task<IActionResult> GetTotalChildren()
        {
            var totalChildren = await _context.Children.CountAsync(c => c.Status == 1);
            return Ok(totalChildren);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue()
        {
            var totalRevenue = await _context.PaymentTransactions.SumAsync(pt => pt.Amount);
            return Ok(totalRevenue);
        }

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

        [HttpGet("average-weight-height-by-age-group")]
        public async Task<IActionResult> GetAverageWeightHeightByAgeGroup()
        {
            var averageWeightHeight = await _context.GrowthIndicators
                .GroupBy(gi => gi.Children.Dob.Value.Year)
                .Select(g => new
                {
                    AgeGroup = DateTime.Now.Year - g.Key,
                    AverageWeight = g.Average(gi => gi.Weight),
                    AverageHeight = g.Average(gi => gi.Height)
                })
                .ToListAsync();

            return Ok(averageWeightHeight);
        }

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

        [HttpGet("active-user-accounts")]
        public async Task<IActionResult> GetActiveUserAccounts()
        {
            var activeUsers = await _context.Users
                .Where(u => u.Status == "active")
                .ToListAsync();

            return Ok(activeUsers);
        }

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

        [HttpGet("children-by-age-group")]
        public async Task<IActionResult> GetChildrenByAgeGroup()
        {
            var childrenByAgeGroup = await _context.Children
                .GroupBy(c => DateTime.Now.Year - c.Dob.Value.Year)
                .Select(g => new
                {
                    AgeGroup = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(childrenByAgeGroup);
        }

        [HttpGet("children-with-specific-allergies")]
        public async Task<IActionResult> GetChildrenWithSpecificAllergies([FromQuery] string allergy)
        {
            var childrenWithAllergies = await _context.Children
                .Where(c => !string.IsNullOrEmpty(c.Allergies))
                .ToListAsync();

            return Ok(childrenWithAllergies);
        }

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

            var averageGrowthRate = await query
                .GroupBy(gi => gi.ChildrenId)
                .Select(g => new
                {
                    ChildId = g.Key,
                    AverageHeightGrowth = g.Average(gi => gi.Height),
                    AverageWeightGrowth = g.Average(gi => gi.Weight)
                })
                .ToListAsync();

            return Ok(averageGrowthRate);
        }

        [HttpGet("children-with-chronic-conditions")]
        public async Task<IActionResult> GetChildrenWithChronicConditions()
        {
            var childrenWithChronicConditions = await _context.Children
                .Where(c => !string.IsNullOrEmpty(c.ChronicConditions))
                .ToListAsync();

            return Ok(childrenWithChronicConditions);
        }

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

        [HttpGet("missed-vaccinations")]
        public async Task<IActionResult> GetMissedVaccinations()
        {
            var missedVaccinations = await _context.VaccineRecords
                .Where(vr => vr.NextDoseDate < DateTime.Now)
                .ToListAsync();

            return Ok(missedVaccinations);
        }

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
