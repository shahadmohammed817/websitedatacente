using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    /// <summary>
    /// لا يوجد تسجيل دخول أو صلاحيات حالياً - كل زائر يرى كل بيانات الأمانة والبلدية.
    /// تم الإبقاء على هذه الخدمة بنفس الأسماء لأنها تحتوي منطق التنقّل الشجري
    /// (البنود ← الإدارات العامة ← الإدارات الفرعية...) المستخدم في كل الصفحات،
    /// لكن تم حذف أي تحقق من صلاحيات المستخدم منها بالكامل.
    /// </summary>
    public interface IPermissionService
    {
        Task<bool> CanAccessScopeAsync(int userId, ScopeType scope);
        Task<bool> CanAccessSectionAsync(int userId, int sectionId);
        Task<List<Section>> GetVisibleTopSectionsAsync(int userId, ScopeType scope);
        Task<Section?> GetAccessibleSectionAsync(int userId, int sectionId, ScopeType scope);
        Task<List<Section>> GetVisibleChildSectionsAsync(int userId, int parentSectionId);
        Task<List<Section>> GetSectionAncestorsAsync(int sectionId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;

        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        public Task<bool> CanAccessScopeAsync(int userId, ScopeType scope) => Task.FromResult(true);

        public Task<bool> CanAccessSectionAsync(int userId, int sectionId) => Task.FromResult(true);

        public async Task<List<Section>> GetVisibleTopSectionsAsync(int userId, ScopeType scope)
        {
            return await _db.Sections
                .Where(s => s.Scope == scope && s.ParentSectionId == null && s.IsActive)
                .Include(s => s.SubSections.Where(sub => sub.IsActive))
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task<Section?> GetAccessibleSectionAsync(int userId, int sectionId, ScopeType scope)
        {
            var section = await _db.Sections.FirstOrDefaultAsync(s => s.Id == sectionId && s.IsActive);
            if (section == null || section.Scope != scope) return null;
            return section;
        }

        public async Task<List<Section>> GetVisibleChildSectionsAsync(int userId, int parentSectionId)
        {
            var parent = await _db.Sections.FindAsync(parentSectionId);
            if (parent == null || !parent.IsActive) return new List<Section>();

            return await _db.Sections
                .Where(s => s.ParentSectionId == parentSectionId && s.IsActive)
                .Include(s => s.SubSections.Where(sub => sub.IsActive))
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task<List<Section>> GetSectionAncestorsAsync(int sectionId)
        {
            var chain = new List<Section>();
            var current = await _db.Sections.FindAsync(sectionId);

            while (current != null)
            {
                chain.Insert(0, current);
                if (current.ParentSectionId == null) break;
                current = await _db.Sections.FindAsync(current.ParentSectionId);
            }

            return chain;
        }
    }
}