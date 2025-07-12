using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Controllers
{
    public class UnitsController : Controller
    {
        private readonly RemoteDesktopDbContext _context;

        public UnitsController(RemoteDesktopDbContext context)
        {
            _context = context;
        }

        // GET: Units
        public async Task<IActionResult> Index()
        {
            var remoteDesktopDbContext = _context.Units.Include(u => u.CreatedByUser).Include(u => u.Manager).Include(u => u.ParentUnit);
            return View(await remoteDesktopDbContext.ToListAsync());
        }

        // GET: Units/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.CreatedByUser)
                .Include(u => u.Manager)
                .Include(u => u.ParentUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        // GET: Units/Create
        public IActionResult Create()
        {
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "ClientId");
            ViewData["ManagerId"] = new SelectList(_context.Users, "Id", "ClientId");
            ViewData["ParentUnitId"] = new SelectList(_context.Units, "Id", "Name");
            return View();
        }

        // POST: Units/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Code,ParentUnitId,ManagerId,IsActive,CreatedAt,UpdatedAt,CreatedByUserId,Location,PhoneExtension,Email")] Unit unit)
        {
            if (ModelState.IsValid)
            {
                _context.Add(unit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "ClientId", unit.CreatedByUserId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "Id", "ClientId", unit.ManagerId);
            ViewData["ParentUnitId"] = new SelectList(_context.Units, "Id", "Name", unit.ParentUnitId);
            return View(unit);
        }

        // GET: Units/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "ClientId", unit.CreatedByUserId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "Id", "ClientId", unit.ManagerId);
            ViewData["ParentUnitId"] = new SelectList(_context.Units, "Id", "Name", unit.ParentUnitId);
            return View(unit);
        }

        // POST: Units/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Code,ParentUnitId,ManagerId,IsActive,CreatedAt,UpdatedAt,CreatedByUserId,Location,PhoneExtension,Email")] Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(unit);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UnitExists(unit.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "ClientId", unit.CreatedByUserId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "Id", "ClientId", unit.ManagerId);
            ViewData["ParentUnitId"] = new SelectList(_context.Units, "Id", "Name", unit.ParentUnitId);
            return View(unit);
        }

        // GET: Units/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.CreatedByUser)
                .Include(u => u.Manager)
                .Include(u => u.ParentUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        // POST: Units/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                _context.Units.Remove(unit);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UnitExists(int id)
        {
            return _context.Units.Any(e => e.Id == id);
        }
    }
}
