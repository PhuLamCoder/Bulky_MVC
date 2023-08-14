using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Company());
            }
            else
            {
                Company companyObj = _unitOfWork.Company.Get(company => company.Id == id);
                return View(companyObj);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(Company CompanyObj)
        {
            if (ModelState.IsValid)
            {
                if (CompanyObj.Id == 0)
                {
                    await _unitOfWork.Company.AddAsync(CompanyObj);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                    _unitOfWork.Company.Update(CompanyObj);
                    TempData["success"] = "Company updated successfully!";
                }

                await _unitOfWork.SaveAsync();
                return RedirectToAction("Index");
            }
            return View(CompanyObj);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.Company.Get(Company => Company.Id == id);
            if (CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting!" });
            }
            _unitOfWork.Company.Remove(CompanyToBeDeleted);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Record has been deleted!" });
        }

        #endregion
    }
}
