using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(IUnitOfWork unitOfWork, 
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> RoleManagement(string userId) 
        {
            RoleManagementVM obj = new RoleManagementVM()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(user => user.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.Select(role =>  new SelectListItem
                {
                    Text = role.Name,
                    Value = role.Name
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(company => new SelectListItem
                {
                    Text = company.Name,
                    Value = company.Id.ToString()
                })
            };
            
            obj.ApplicationUser.Role = (await _userManager
                .GetRolesAsync(_unitOfWork.ApplicationUser.Get(user => user.Id == userId))).FirstOrDefault();

            return View(obj);
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagement(RoleManagementVM roleManagementVM)
        {
            string oldRole = (await _userManager
                .GetRolesAsync(_unitOfWork.ApplicationUser
                .Get(user => user.Id == roleManagementVM.ApplicationUser.Id))).FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser
                    .Get(user => user.Id == roleManagementVM.ApplicationUser.Id);
            if (roleManagementVM.ApplicationUser.Role != oldRole)
            {
                // A role was updated
                if (roleManagementVM.ApplicationUser.Role == SD.Role_Company)
                {
					applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;

				}
                else if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
				}
                _unitOfWork.ApplicationUser.Update(applicationUser);
                await _unitOfWork.SaveAsync();
                await _userManager.RemoveFromRoleAsync(applicationUser, oldRole);
                await _userManager.AddToRoleAsync(applicationUser, roleManagementVM.ApplicationUser.Role);
			}
            else
            {
                if (oldRole == SD.Role_Company && applicationUser.CompanyId != roleManagementVM.ApplicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    await _unitOfWork.SaveAsync();
                }
            }
			return RedirectToAction(nameof(Index));
        }

        #region API CALLS

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();

            foreach (var user in objUserList)
            {
                user.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new Company() { Name = "" };
                }
            }
            return Json(new { data = objUserList });
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody]string id)
        {
            var objFromDb = _unitOfWork.ApplicationUser.Get(user => user.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking!" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // User is currently locked and we need to unlock it
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Locking/Unlocking successfully!!" });
        }

        #endregion
    }
}
