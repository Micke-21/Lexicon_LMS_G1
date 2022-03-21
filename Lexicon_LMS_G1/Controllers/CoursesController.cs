﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lexicon_LMS_G1.Entities.Entities;
using Lexicon_LMS_G1.Data.Data;
using Lexicon_LMS_G1.Data.Repositores;
using System.Linq.Expressions;
using Lexicon_LMS_G1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Lexicon_LMS_G1.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBaseRepository<Course> repo;
        private readonly UserManager<ApplicationUser> userManager;

        public CoursesController(ApplicationDbContext context, IBaseRepository<Course> repo, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            this.repo = repo;
            this.userManager = userManager;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.ToListAsync());
        }

        public async Task<IActionResult> IndexTeacher()
        {
            var module = await repo.GetIncludeAsync(c => c.Modules);
            module = module.OrderBy(m => m.StartTime);
            return View(module);

        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,StartTime")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,StartTime")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
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
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> StudentIndex()
        {
            ApplicationUser user = await userManager.GetUserAsync(User);

            StudentViewCourseViewModel viewModel = await GetStudentViewCourseViewModel(user.CourseId, user.Id);

            return View("IndexStudent", viewModel);
        }
        
        private async Task<StudentViewCourseViewModel> GetStudentViewCourseViewModel(int? courseId, string userId)
        {
            if (courseId == null)
                return new StudentViewCourseViewModel 
                { 
                    Name = "Nothing lmao",
                    Assignments = new List<Activity>(),
                    FinishedAssignments = new List<Activity>(),
                    Attendees = new List<ApplicationUser>(),
                    Modules = new List<Module>()
                };
            /*
                        var model = _context.Courses.FirstOrDefault(c => c.Id ==courseId).Where //new StudentViewCourseViewModel
                        //{
                        //   // Assignments = c.Modules.Select(m => m.Activities).Where(a => )//Where(a => a.ActivityType.Name == "Assignment"))

                        //}).FirstOrDefault(c => c.)  

                        var modelt = _context.Courses.Select(c => new StudentViewCourseViewModel
                        {
                            Assignments = c.Modules.Select(m => m.Activities.Where(a => a.ActivityType.Name == "Assignment"))
                        });
            */

            var finishedAssignments = (await _context.Users
                    .FindAsync(userId))
                    .FinishedActivities;

            StudentViewCourseViewModel viewModel = new StudentViewCourseViewModel
            {
                Name = (await _context.Courses.FindAsync(courseId)).Name,
                Assignments = await _context.Activities         
                    .Include(a => a.ActivityType)
                    .Include(a => a.Module)
                    .ThenInclude(m => m.Activities)
                    .Where(a => a.Module.CourseId == courseId)
                    .Where(a => a.ActivityType.Name == "Assignment")
                    //.Where(a => !finishedAssignments.Contains(a.Id))
                    .ToListAsync(),
                FinishedAssignments = (IEnumerable<Activity>)finishedAssignments,
                Attendees = (await _context.Courses
                    .FindAsync(courseId))
                    .Attendees,
                Modules = await _context.Modules                
                    .Where(m => m.CourseId == courseId)
                    .ToListAsync()
            };

            return viewModel;
        }
    }
}
